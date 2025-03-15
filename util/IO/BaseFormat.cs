using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Object.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.IO {
    public abstract class BaseFormat {
        public static readonly string[] DEFAULT_NORMAL_VECTOR_IDENTIFIER = new string[] { "nx", "ny", "nz" };
        public static readonly string[] DEFAULT_COORDINATES_IDENTIFIER = new string[] { "x", "y", "z" };
        public static readonly string[] DEFAULT_COLOR_IDENTIFIER = new string[] { "red", "green", "blue", "alpha" };

        private string[] normalVectorIdentifier = DEFAULT_NORMAL_VECTOR_IDENTIFIER;
        private string[] coordinatesIdentifier = DEFAULT_COORDINATES_IDENTIFIER;
        private string[] colorIdentifier = DEFAULT_COLOR_IDENTIFIER;
        private Dictionary<string, string> bytePropertyIdentifiers = new Dictionary<string, string>();
        private Dictionary<string, string> integerPropertyIdentifiers = new Dictionary<string, string>();
        private Dictionary<string, string> floatPropertyIdentifiers = new Dictionary<string, string>();
        private Dictionary<string, string> doublePropertyIdentifiers = new Dictionary<string, string>();
        private Dictionary<string, string[]> vector3dPropertyIdentifiers = new Dictionary<string, string[]>();
        private Dictionary<string, string[]> colorPropertyIdentifiers = new Dictionary<string, string[]>();

        public PropertyDescriptor PropertyDescriptor { protected get; set; }

        public BaseFormat() {
            PropertyDescriptor = new PropertyDescriptor();
        }

        public void OverwriteNormalVectorIdentifier(string[] identifier) {
            if (!PropertyDescriptor.HasNormalVector()) {
                throw new ArgumentException(
                    "Normal vector identifier to overwrite does not exist in PropertyDescriptor.");
            }
            CheckVector3dIdentifier(identifier, "Normal vector");
            normalVectorIdentifier = identifier;
        }

        public void OverwriteVector3ComponentIdentifier(string[] identifier) {
            CheckVector3dIdentifier(identifier, "Vector3d component");
            coordinatesIdentifier = identifier;
        }

        public void OverwriteColorComponentIdentifier(string[] identifier) {
            CheckColorIdentifier(identifier, "Color component");
            colorIdentifier = identifier;
        }

        public void OverwriteBytePropertyIdentifier(
                string property,
                string identifier) {
            if (!PropertyDescriptor.ByteProperties.Contains(property)) {
                throw new ArgumentException(
                    "Byte property '{0}' to overwrite does not exist in PropertyDescriptor.",
                    property);
            }
            bytePropertyIdentifiers.Add(property, identifier);
        }

        public void OverwriteIntegerPropertyIdentifier(
                string property,
                string identifier) {
            if (!PropertyDescriptor.IntegerProperties.Contains(property)) {
                throw new ArgumentException(
                    "integer property '{0}' to overwrite does not exist in PropertyDescriptor.",
                    property);
            }
            integerPropertyIdentifiers.Add(property, identifier);
        }

        public void OverwriteFloatPropertyIdentifier(
                string property,
                string identifier) {
            if (!PropertyDescriptor.FloatProperties.Contains(property)) {
                throw new ArgumentException(
                    "Float property '{0}' to overwrite does not exist in PropertyDescriptor.",
                    property);
            }
            floatPropertyIdentifiers.Add(property, identifier);
        }

        public void OverwriteDoublePropertyIdentifier(
                string property, 
                string identifier) {
            if (!PropertyDescriptor.DoubleProperties.Contains(property)) {
                throw new ArgumentException(
                    "Double property '{0}' to overwrite does not exist in PropertyDescriptor.",
                    property);
            }
            doublePropertyIdentifiers.Add(property, identifier);
        }

        public void OverwriteVector3dPropertyIdentifier(
                string property, 
                string identifier) {
            OverwriteVector3dPropertyIdentifier(property, GetVector3dIdentifiers(identifier));
        }

        public void OverwriteVector3dPropertyIdentifier(
                string property, 
                string[] identifier) {
            if (!PropertyDescriptor.Vector3Properties.Contains(property)) {
                throw new ArgumentException(
                    "Vector3d property '{0}' to overwrite does not exist in PropertyDescriptor.",
                    property);
            }
            CheckVector3dIdentifier(identifier, $"Vector3d property '{identifier}'");
            vector3dPropertyIdentifiers.Add(property, identifier);
        }

        public void OverwriteColorPropertyIdentifier(
                string property, 
                string identifier) {
            OverwriteColorPropertyIdentifier(
                property, 
                GetColorIdentifiers(identifier));
        }

        public void OverwriteColorPropertyIdentifier(
                string property, 
                string[] identifier) {
            if (!PropertyDescriptor.ColorProperties.Contains(property)) {
                throw new ArgumentException(
                    "Color property '{0}' to overwrite does not exist in PropertyDescriptor.",
                    property);
            }
            CheckColorIdentifier(identifier, $"Color property '{identifier}'");
            colorPropertyIdentifiers.Add(property, identifier);
        }

        public virtual Format Create() {
            FinalizeIdentifiers();
            Format format = new Format();
            format.BytePropertyIdentifiers = bytePropertyIdentifiers;
            format.IntegerPropertyIdentifiers = integerPropertyIdentifiers;
            format.FloatPropertyIdentifiers = floatPropertyIdentifiers;
            format.DoublePropertyIdentifiers = doublePropertyIdentifiers;
            if (PropertyDescriptor.HasNormalVector()) {
                format.NormalVectorIdentifiers = normalVectorIdentifier;
            }
            format.Vector3dPropertyIdentifiers = vector3dPropertyIdentifiers;
            format.ColorPropertyIdentifiers = colorPropertyIdentifiers;
            return format;
        }

        protected void CheckVector3dIdentifier(
                string[] identifier, 
                string name) {
            if (identifier.Length != 3) {
                throw new ArgumentException(
                    $"{name} identifier must be size 3 but is size {identifier.Length}.");
            }
        }

        protected void CheckColorIdentifier(
                string[] identifier, 
                string name) {
            if (identifier.Length != 3 && identifier.Length != 4) {
                throw new ArgumentException(
                    $"{name} identifier must be size 3 or 4 but is size {identifier.Length}.");
            }
        }

        protected string[] GetVector3dIdentifiers(string identifier) {
            return new string[] {
                $"{identifier}_{coordinatesIdentifier[0]}",
                $"{identifier}_{coordinatesIdentifier[1]}",
                $"{identifier}_{coordinatesIdentifier[2]}"
            };
        }

        protected string[] GetColorIdentifiers(string identifier) {
            return new string[] {
                $"{identifier}_{colorIdentifier[0]}",
                $"{identifier}_{colorIdentifier[1]}",
                $"{identifier}_{colorIdentifier[2]}",
                $"{identifier}_{colorIdentifier[3]}"
            };
        }

        private void FinalizeIdentifiers() {
            FinalizeBytePropertyIdentifiers();
            FinalizeIntegerPropertyIdentifiers();
            FinalizeFloatPropertyIdentifiers();
            FinalizeDoublePropertyIdentifiers();
            FinalizeVector3PropertyIdentifiers();
            FinalizeColorPropertyIdentifiers();
            CheckIdentifierUniqueness();
        }

        private void FinalizeBytePropertyIdentifiers() {
            foreach (string property in PropertyDescriptor.ByteProperties) {
                if (!bytePropertyIdentifiers.ContainsKey(property)) {
                    bytePropertyIdentifiers.Add(property, property);
                }
            }
        }

        private void FinalizeIntegerPropertyIdentifiers() {
            foreach (string property in PropertyDescriptor.IntegerProperties) {
                if (!integerPropertyIdentifiers.ContainsKey(property)) {
                    integerPropertyIdentifiers.Add(property, property);
                }
            }
        }

        private void FinalizeFloatPropertyIdentifiers() {
            foreach (string property in PropertyDescriptor.FloatProperties) {
                if (!floatPropertyIdentifiers.ContainsKey(property)) {
                    floatPropertyIdentifiers.Add(property, property);
                }
            }
        }

        private void FinalizeDoublePropertyIdentifiers() {
            foreach (string property in PropertyDescriptor.DoubleProperties) {
                if (!doublePropertyIdentifiers.ContainsKey(property)) { 
                    doublePropertyIdentifiers.Add(property, property);
                }
            }
        }

        private void FinalizeVector3PropertyIdentifiers() {
            foreach (string property in PropertyDescriptor.Vector3Properties) {
                if (property == NormalVectorExtensions.NORMAL_VECTOR_PROPERTY_NAME ||
                        vector3dPropertyIdentifiers.ContainsKey(property)) {
                    continue;
                }
                vector3dPropertyIdentifiers.Add(
                    property, 
                    GetVector3dIdentifiers(property));
            }
        }

        private void FinalizeColorPropertyIdentifiers() {
            foreach (string property in PropertyDescriptor.ColorProperties) {
                if (property == ColorExtensions.COLOR_PROPERTY_NAME ||
                        colorPropertyIdentifiers.ContainsKey(property)) {
                    continue;
                }
                colorPropertyIdentifiers.Add(
                    property, 
                    GetColorIdentifiers(property));
            }
        }

        private void CheckIdentifierUniqueness() {
            List<string> allIdentifiers = GetAllIdentifiers();
            for (int i = 0; i < allIdentifiers.Count; i++) {
                for (int j = 0; j < allIdentifiers.Count; j++) {
                    if (i != j && allIdentifiers[i] == allIdentifiers[j]) {
                        throw new ArgumentException(
                            $"Property identifier '{allIdentifiers[i]}' occurs multiple times in FileDescriptor.");
                    }
                }
            }
        }

        protected virtual List<string> GetAllIdentifiers() {
            List<string> allIdentifiers = new List<string>();
            allIdentifiers.AddRange(bytePropertyIdentifiers.Values);
            allIdentifiers.AddRange(integerPropertyIdentifiers.Values);
            allIdentifiers.AddRange(floatPropertyIdentifiers.Values);
            allIdentifiers.AddRange(doublePropertyIdentifiers.Values);
            if (PropertyDescriptor.HasNormalVector()) {
                allIdentifiers.AddRange(normalVectorIdentifier);
            }
            allIdentifiers.AddRange(
                vector3dPropertyIdentifiers
                    .Values
                    .SelectMany(i => i));
            allIdentifiers.AddRange(
                colorPropertyIdentifiers
                    .Values
                    .SelectMany(i => i));
            return allIdentifiers;
        }
    }
}