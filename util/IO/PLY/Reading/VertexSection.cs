using HuePat.Util.Object.Properties;
using System.Collections.Generic;

namespace HuePat.Util.IO.PLY.Reading {
    class VertexSection : HeaderSection {
        private bool areCoordinatesFloat;
        public int[] CoordinateIndices { get; set; }
        public int[] ColorIndices { get; set; }

        public bool HasColor {
            get {
                return ColorIndices != null;
            }
        }

        public override Dictionary<int, PropertyType> PropertyTypes {
            get {

                Dictionary<int, PropertyType> types = base.PropertyTypes;

                if (areCoordinatesFloat) {
                    types.Add(CoordinateIndices[0], PropertyType.FLOAT);
                    types.Add(CoordinateIndices[1], PropertyType.FLOAT);
                    types.Add(CoordinateIndices[2], PropertyType.FLOAT);
                }
                else {
                    types.Add(CoordinateIndices[0], PropertyType.DOUBLE);
                    types.Add(CoordinateIndices[1], PropertyType.DOUBLE);
                    types.Add(CoordinateIndices[2], PropertyType.DOUBLE);
                }

                if (HasColor) {

                    types.Add(ColorIndices[0], PropertyType.BYTE);
                    types.Add(ColorIndices[1], PropertyType.BYTE);
                    types.Add(ColorIndices[2], PropertyType.BYTE);

                    if (ColorIndices.Length == 4) {
                        types.Add(ColorIndices[3], PropertyType.BYTE);
                    }
                }

                return types;
            }
        }

        public VertexSection(
                bool areCoordinatesFloat,
                HeaderSection headerSection) : 
                    base(headerSection) {

            this.areCoordinatesFloat = areCoordinatesFloat;
        }
    }
}