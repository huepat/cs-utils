using HuePat.Util.Object.Properties;
using System.Collections.Generic;

namespace HuePat.Util.IO.PLY.Reading {
    class HeaderSection {
        private bool isNormalVectorFloat;

        public int Count { get; private set; }
        public int[] NormalVectorIndices { get; set; }
        public HashSet<int> IndicesNotInFormat { get; set; }
        public Dictionary<string, int> BytePropertyIndices { get; private set; }
        public Dictionary<string, int> IntegerPropertyIndices { get; private set; }
        public Dictionary<string, int> FloatPropertyIndices { get; private set; }
        public Dictionary<string, int> DoublePropertyIndices { get; private set; }
        public Dictionary<string, int[]> Vector3dPropertyIndices { get; private set; }
        public Dictionary<string, int[]> ColorPropertyIndices { get; private set; }

        public bool HasNormalVector {
            get {
                return NormalVectorIndices != null;
            }
        }

        public virtual Dictionary<int, PropertyType> PropertyTypes {
            get {

                Dictionary<int, PropertyType> types = new Dictionary<int, PropertyType>();

                if (HasNormalVector) {

                    if (isNormalVectorFloat) {
                        types.Add(NormalVectorIndices[0], PropertyType.FLOAT);
                        types.Add(NormalVectorIndices[1], PropertyType.FLOAT);
                        types.Add(NormalVectorIndices[2], PropertyType.FLOAT);
                    }
                    else {
                        types.Add(NormalVectorIndices[0], PropertyType.DOUBLE);
                        types.Add(NormalVectorIndices[1], PropertyType.DOUBLE);
                        types.Add(NormalVectorIndices[2], PropertyType.DOUBLE);
                    }
                }

                foreach (int index in BytePropertyIndices.Values) {
                    types.Add(index, PropertyType.BYTE);
                }

                foreach (int index in IntegerPropertyIndices.Values) {
                    types.Add(index, PropertyType.INTEGER);
                }

                foreach (int index in FloatPropertyIndices.Values) {
                    types.Add(index, PropertyType.FLOAT);
                }

                foreach (int index in DoublePropertyIndices.Values) {
                    types.Add(index, PropertyType.DOUBLE);
                }

                foreach (int[] indices in Vector3dPropertyIndices.Values) {
                    types.Add(indices[0], PropertyType.DOUBLE);
                    types.Add(indices[1], PropertyType.DOUBLE);
                    types.Add(indices[2], PropertyType.DOUBLE);
                }

                foreach (int[] indices in ColorPropertyIndices.Values) {

                    types.Add(indices[0], PropertyType.BYTE);
                    types.Add(indices[1], PropertyType.BYTE);
                    types.Add(indices[2], PropertyType.BYTE);

                    if (indices.Length == 4) {
                        types.Add(indices[3], PropertyType.BYTE);
                    }
                }

                return types;
            }
        }

        public HeaderSection(
                int count,
                bool isNormalVectorFloat) {
            Count = count;
            this.isNormalVectorFloat = isNormalVectorFloat;
            IndicesNotInFormat = new HashSet<int>();
            BytePropertyIndices = new Dictionary<string, int>();
            IntegerPropertyIndices = new Dictionary<string, int>();
            FloatPropertyIndices = new Dictionary<string, int>();
            DoublePropertyIndices = new Dictionary<string, int>();
            Vector3dPropertyIndices = new Dictionary<string, int[]>();
            ColorPropertyIndices = new Dictionary<string, int[]>();
        }

        public HeaderSection(
                HeaderSection headerSection) {
            Count = headerSection.Count;
            IndicesNotInFormat = headerSection.IndicesNotInFormat;
            isNormalVectorFloat = headerSection.isNormalVectorFloat;
            NormalVectorIndices = headerSection.NormalVectorIndices;
            BytePropertyIndices = headerSection.BytePropertyIndices;
            IntegerPropertyIndices = headerSection.IntegerPropertyIndices;
            FloatPropertyIndices = headerSection.FloatPropertyIndices;
            DoublePropertyIndices = headerSection.DoublePropertyIndices;
            Vector3dPropertyIndices = headerSection.Vector3dPropertyIndices;
            ColorPropertyIndices = headerSection.ColorPropertyIndices;
        }
    }
}