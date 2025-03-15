using HuePat.Util.Object.Properties;
using System;
using System.Collections.Generic;

namespace HuePat.Util.IO.PLY.Reading {
    class FaceSectionParser : HeaderSectionParser {
        private const int INDEX_OFFSET = 4;
        private List<PropertyType> additionalListPropertyTypes = new List<PropertyType>();

        public override string SectionName {
            get {
                return "face";
            }
        }

        public FaceSection Create(
                int count) {

            return new FaceSection(
                Create(
                    count, 
                    INDEX_OFFSET),
                additionalListPropertyTypes.ToArray());
        }

        public void ParseAdditionalListProperty(
                string headerLine) {

            string propertyTypeIdentifier = headerLine.Split(" ")[3];

            PropertyType propertyType = GetPropertyType(propertyTypeIdentifier);

            additionalListPropertyTypes.Add(propertyType);
        }

        private PropertyType GetPropertyType(
                string propertyTypeIdentifier) {

            switch (propertyTypeIdentifier) {

                case INTEGER_TYPE:
                    return PropertyType.INTEGER;

                case FLOAT_TYPE:
                    return PropertyType.FLOAT;

                default:
                    throw new ApplicationException(
                        $"List property type identifier {propertyTypeIdentifier} not defined.");
            }
        }
    }
}