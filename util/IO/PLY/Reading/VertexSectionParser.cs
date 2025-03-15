using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.IO.PLY.Reading {
    class VertexSectionParser : HeaderSectionParser {
        private Dictionary<string, int> coordinateIndices;
        private Dictionary<string, int> colorIndices;

        public bool AreCoordinatesFloat { private get; set; }

        public override string SectionName {
            get {
                return "vertex";
            }
        }

        public override void Initialize() {

            base.Initialize();

            InitializeCoordinateIndices();
            InitializeColorIndices();
        }

        public override void ParsePropertyIndex(
                string propertyType, 
                string propertyName) {

            if (coordinateIndices.ContainsKey(propertyName)
                    && (propertyType == FLOAT_TYPE 
                        || propertyType == DOUBLE_TYPE)) {

                coordinateIndices[propertyName] = propertyIndex;
                propertyIndex++;
            }
            else if (colorIndices.ContainsKey(propertyName) 
                    && propertyType == BYTE_TYPE) {

                colorIndices[propertyName] = propertyIndex;
                propertyIndex++;
            }
            else {
                base.ParsePropertyIndex(propertyType, propertyName);
            }
        }

        public new VertexSection Create(
                int count, 
                int indexOffset = 0) {

            VertexSection section = new VertexSection(
                AreCoordinatesFloat,
                base.Create(count, indexOffset));

            section.CoordinateIndices = new int[] {
                coordinateIndices[Format.CoordinateIdentifiers[0]],
                coordinateIndices[Format.CoordinateIdentifiers[1]],
                coordinateIndices[Format.CoordinateIdentifiers[2]]
            };

            if (Format.HasColor) {

                section.ColorIndices = Format.ColorIdentifiers
                    .Select(identifier => colorIndices[identifier])
                    .Where(index => index != NOT_SET_INDEX)
                    .ToArray();
            }

            return section;
        }

        protected override void Check() {

            base.Check();

            foreach (string identifier in coordinateIndices.Keys) {
                if (coordinateIndices[identifier] == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("coordinate component", identifier);
                }
            }

            foreach (string identifier in colorIndices.Keys) {
                if (colorIndices[identifier] == NOT_SET_INDEX) {
                    ReportUnsetPropertyIndex("color component", identifier);
                }
            }
        }

        private void InitializeCoordinateIndices() {

            coordinateIndices = new Dictionary<string, int>();

            foreach (string identifier in Format.CoordinateIdentifiers) {
                coordinateIndices.Add(identifier, NOT_SET_INDEX);
            }
        }

        private void InitializeColorIndices() {

            colorIndices = new Dictionary<string, int>();

            if (!Format.HasColor) {
                return;
            }

            foreach (string identifier in Format.ColorIdentifiers) {
                colorIndices.Add(identifier, NOT_SET_INDEX);
            }
        }
    }
}