using System;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.IO.PLY.Reading {
    abstract class HeaderSectionParser {
        protected const int NOT_SET_INDEX = -1;
        protected const string INTEGER_TYPE = "int";
        protected const string FLOAT_TYPE = "float";
        protected const string DOUBLE_TYPE = "double";
        protected const string BYTE_TYPE = "uchar";

        private class PropertyIndex {
            public string InternalIdentifier { get; private set; }
            public int Index { get; set; }

            public PropertyIndex(
                    string internalIdentifier) :
                        this(
                            internalIdentifier, 
                            NOT_SET_INDEX) {
            }

            public PropertyIndex(
                    string internalIdentifier,
                    int index) {

                InternalIdentifier = internalIdentifier;
                Index = index;
            }
        }

        protected int propertyIndex;
        private HashSet<int> indicesNotInFormat;
        private Dictionary<string, int> normalVectorIndices;
        private Dictionary<string, PropertyIndex> bytePropertyIndices;
        private Dictionary<string, PropertyIndex> integerPropertyIndices;
        private Dictionary<string, PropertyIndex> floatPropertyIndices;
        private Dictionary<string, PropertyIndex> doublePropertyIndices;
        private Dictionary<string, PropertyIndex> vector3dPropertyIndices;
        private Dictionary<string, PropertyIndex> colorPropertyIndices;

        public bool IsNormalVectorFloat { private get; set; }
        public abstract string SectionName { get; }
        public Format Format { protected get; set; }

        public virtual void Initialize() {

            propertyIndex = 0;
            indicesNotInFormat = new HashSet<int>();

            InitializeNormalVectorIndices();
            InitializeBytePropertyIndices();
            InitializeIntegerPropertyIndices();
            InitializeFloatPropertyIndices();
            InitializeDoublePropertyIndices();
            InitializeVector3dPropertyIndices();
            InitializeColorPropertyIndices();
        }

        public virtual void ParsePropertyIndex(
                string propertyType, 
                string propertyName) {

            if (normalVectorIndices.ContainsKey(propertyName)
                    && (propertyType == FLOAT_TYPE || propertyType == DOUBLE_TYPE)) {

                normalVectorIndices[propertyName] = propertyIndex;
            }
            else if (bytePropertyIndices.ContainsKey(propertyName)
                    && propertyType == BYTE_TYPE) {

                bytePropertyIndices[propertyName].Index = propertyIndex;
            }
            else if (integerPropertyIndices.ContainsKey(propertyName)
                    && propertyType == INTEGER_TYPE) {

                integerPropertyIndices[propertyName].Index = propertyIndex;
            }
            else if (floatPropertyIndices.ContainsKey(propertyName)
                    && propertyType == FLOAT_TYPE) {

                floatPropertyIndices[propertyName].Index = propertyIndex;
            }
            else if (doublePropertyIndices.ContainsKey(propertyName) 
                    && propertyType == DOUBLE_TYPE) {

                doublePropertyIndices[propertyName].Index = propertyIndex;
            }
            else if (vector3dPropertyIndices.ContainsKey(propertyName) 
                    && propertyType == FLOAT_TYPE) {

                vector3dPropertyIndices[propertyName].Index = propertyIndex;
            }
            else if (colorPropertyIndices.ContainsKey(propertyName) 
                    && propertyType == BYTE_TYPE) {

                colorPropertyIndices[propertyName].Index = propertyIndex;
            }
            else {

                indicesNotInFormat.Add(propertyIndex);

                if (propertyType == BYTE_TYPE) {

                    bytePropertyIndices.Add(
                        propertyName,
                        new PropertyIndex(propertyName, propertyIndex));
                }
                else if (propertyType == INTEGER_TYPE) {

                    integerPropertyIndices.Add(
                        propertyName,
                        new PropertyIndex(propertyName, propertyIndex));
                }
                else if(propertyType == FLOAT_TYPE) {

                    floatPropertyIndices.Add(
                        propertyName,
                        new PropertyIndex(propertyName, propertyIndex));
                }
                else if(propertyType == DOUBLE_TYPE) {

                    doublePropertyIndices.Add(
                        propertyName,
                        new PropertyIndex(propertyName, propertyIndex));
                }
            }

            propertyIndex++;
        }

        protected HeaderSection Create(
                int count, 
                int indexOffset = 0) {

            HeaderSection section = new HeaderSection(
                count, 
                IsNormalVectorFloat);

            section.IndicesNotInFormat = indicesNotInFormat
                .Select(index => index + indexOffset)
                .ToHashSet();

            if (Format.HasNormalVector) {

                section.NormalVectorIndices = new int[] {
                    normalVectorIndices[Format.NormalVectorIdentifiers[0]] + indexOffset,
                    normalVectorIndices[Format.NormalVectorIdentifiers[1]] + indexOffset,
                    normalVectorIndices[Format.NormalVectorIdentifiers[2]] + indexOffset
                };
            }

            foreach (string identifier in bytePropertyIndices.Keys) {

                section.BytePropertyIndices.Add(
                    bytePropertyIndices[identifier].InternalIdentifier,
                    bytePropertyIndices[identifier].Index + indexOffset);
            }

            foreach (string identifier in integerPropertyIndices.Keys) {

                section.IntegerPropertyIndices.Add(
                    integerPropertyIndices[identifier].InternalIdentifier,
                    integerPropertyIndices[identifier].Index + indexOffset);
            }

            foreach (string identifier in floatPropertyIndices.Keys) {

                section.FloatPropertyIndices.Add(
                    floatPropertyIndices[identifier].InternalIdentifier,
                    floatPropertyIndices[identifier].Index + indexOffset);
            }

            foreach (string identifier in doublePropertyIndices.Keys) {

                section.DoublePropertyIndices.Add(
                    doublePropertyIndices[identifier].InternalIdentifier,
                    doublePropertyIndices[identifier].Index + indexOffset);
            }

            foreach (string internalIdentifier in Format.Vector3dPropertyIdentifiers.Keys) {

                section.Vector3dPropertyIndices.Add(
                    internalIdentifier,
                    Format.Vector3dPropertyIdentifiers[internalIdentifier]
                        .Select(identifier => vector3dPropertyIndices[identifier].Index + indexOffset)
                        .ToArray());
            }

            foreach (string internalIdentifier in Format.ColorPropertyIdentifiers.Keys) {

                section.ColorPropertyIndices.Add(
                    internalIdentifier,
                    Format.ColorPropertyIdentifiers[internalIdentifier]
                        .Select(identifier => colorPropertyIndices[identifier].Index + indexOffset)
                        .ToArray());
            }

            return section;
        }

        protected virtual void Check() {

            foreach (string identifier in normalVectorIndices.Keys) {
                if (normalVectorIndices[identifier] == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("normal component", identifier);
                }
            }

            foreach (string identifier in bytePropertyIndices.Keys) {
                if (bytePropertyIndices[identifier].Index == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("byte property", identifier);
                }
            }

            foreach (string identifier in integerPropertyIndices.Keys) {
                if (integerPropertyIndices[identifier].Index == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("integer property", identifier);
                }
            }

            foreach (string identifier in floatPropertyIndices.Keys) {
                if (floatPropertyIndices[identifier].Index == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("float property", identifier);
                }
            }

            foreach (string identifier in doublePropertyIndices.Keys) {
                if (doublePropertyIndices[identifier].Index == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("double property", identifier);
                }
            }

            foreach (string identifier in vector3dPropertyIndices.Keys) {
                if (vector3dPropertyIndices[identifier].Index == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("vector3 property component", identifier);
                }
            }

            foreach (string identifier in colorPropertyIndices.Keys) {
                if (colorPropertyIndices[identifier].Index == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("color property component", identifier);
                }
            }
        }

        protected void ReportUnsetPropertyIndex(
                string identifier, 
                string type) {

            throw new ArgumentException(
                $"Header does not contain expected {SectionName} {type} identifier '{identifier}'.");
        }

        private void InitializeNormalVectorIndices() {

            normalVectorIndices = new Dictionary<string, int>();

            if (!Format.HasNormalVector) {
                return;
            }

            foreach (string identifier in Format.NormalVectorIdentifiers) {

                normalVectorIndices.Add(
                    identifier, 
                    NOT_SET_INDEX);
            }
        }

        private void InitializeBytePropertyIndices() {

            bytePropertyIndices = new Dictionary<string, PropertyIndex>();

            foreach (string internalIdentifier in Format.BytePropertyIdentifiers.Keys) {

                integerPropertyIndices.Add(
                    Format.BytePropertyIdentifiers[internalIdentifier],
                    new PropertyIndex(internalIdentifier));
            }
        }

        private void InitializeIntegerPropertyIndices() {

            integerPropertyIndices = new Dictionary<string, PropertyIndex>();

            foreach (string internalIdentifier in Format.IntegerPropertyIdentifiers.Keys) {

                integerPropertyIndices.Add(
                    Format.IntegerPropertyIdentifiers[internalIdentifier],
                    new PropertyIndex(internalIdentifier));
            }
        }

        private void InitializeFloatPropertyIndices() {

            floatPropertyIndices = new Dictionary<string, PropertyIndex>();

            foreach (string internalIdentifier in Format.FloatPropertyIdentifiers.Keys) {

                floatPropertyIndices.Add(
                    Format.FloatPropertyIdentifiers[internalIdentifier],
                    new PropertyIndex(internalIdentifier));
            }
        }

        private void InitializeDoublePropertyIndices() {

            doublePropertyIndices = new Dictionary<string, PropertyIndex>();

            foreach (string internalIdentifier in Format.DoublePropertyIdentifiers.Keys) {

                doublePropertyIndices.Add(
                    Format.DoublePropertyIdentifiers[internalIdentifier],
                    new PropertyIndex(internalIdentifier));
            }
        }

        private void InitializeVector3dPropertyIndices() {

            vector3dPropertyIndices = new Dictionary<string, PropertyIndex>();

            foreach (string internalIdentifier in Format.Vector3dPropertyIdentifiers.Keys) {
                foreach (string identifier in Format.Vector3dPropertyIdentifiers[internalIdentifier]) {

                    vector3dPropertyIndices.Add(
                        identifier,
                        new PropertyIndex(internalIdentifier));
                }
            }
        }

        private void InitializeColorPropertyIndices() {

            colorPropertyIndices = new Dictionary<string, PropertyIndex>();

            foreach (string internalIdentifier in Format.ColorPropertyIdentifiers.Keys) {
                foreach (string identifier in Format.ColorPropertyIdentifiers[internalIdentifier]) {

                    colorPropertyIndices.Add(
                        identifier,
                        new PropertyIndex(internalIdentifier));
                }
            }
        }
    }
}