using HuePat.Util.Object.Properties;
using System.Collections.Generic;

namespace HuePat.Util.IO.PLY.Reading {
    class FaceSection : HeaderSection {
        public readonly int VertexCountIndex = 0;
        public readonly int[] VertexIndexIndices = new int[] { 1, 2, 3 };

        public PropertyType[] AdditionalListPropertyTypes { get; private set; }

        public override Dictionary<int, PropertyType> PropertyTypes {
            get {

                Dictionary<int, PropertyType> types = base.PropertyTypes;

                types.Add(VertexCountIndex, PropertyType.BYTE);
                types.Add(VertexIndexIndices[0], PropertyType.INTEGER);
                types.Add(VertexIndexIndices[1], PropertyType.INTEGER);
                types.Add(VertexIndexIndices[2], PropertyType.INTEGER);

                return types;
            }
        }

        public FaceSection(
                HeaderSection headerSection,
                PropertyType[] additionalListPropertyTypes) : 
                    base(headerSection) {

            AdditionalListPropertyTypes = additionalListPropertyTypes;
        }
    }
}