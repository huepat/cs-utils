using HuePat.Util.Colors;
using HuePat.Util.Object.Properties;
using System;
using System.Collections.Generic;

namespace HuePat.Util.IO {
    public class PointFormat: BaseFormat {
        private string[] coordinatesIdentifier = DEFAULT_COORDINATES_IDENTIFIER;
        private string[] colorIdentifier = DEFAULT_COLOR_IDENTIFIER;

        public PointFormat() : base() {
        }

        public void OverwriteCoordinatesIdentifier(string[] identifier) {
            CheckVector3dIdentifier(identifier, "Coordinates");
            coordinatesIdentifier = identifier;
        }

        public void OverwriteCoordinatesIdentifier(string identifier) {
            OverwriteCoordinatesIdentifier(
                GetVector3dIdentifiers(identifier));
        }

        public void OverwriteNormalVectorIdentifier(string identifier) {
            OverwriteNormalVectorIdentifier(
                GetVector3dIdentifiers(identifier));
        }

        public void OverwriteColorIdentifier(string[] identifier) {
            if (!PropertyDescriptor.HasColor()) {
                throw new ArgumentException(
                    "Color identifier to overwrite does not exist in PropertyDescriptor.");
            }
            CheckColorIdentifier(identifier, "Color");
            colorIdentifier = identifier;
        }

        public void OverwriteColorIdentifier(string identifier) {
            OverwriteColorIdentifier(GetColorIdentifiers(identifier));
        }

        public override Format Create() {
            Format format = base.Create();
            format.CoordinateIdentifiers = coordinatesIdentifier;
            if (PropertyDescriptor.HasColor()) {
                format.ColorIdentifiers = colorIdentifier;
            }
            return format;
        }

        protected override List<string> GetAllIdentifiers() {
            List<string> identifiers = base.GetAllIdentifiers();
            identifiers.AddRange(coordinatesIdentifier);
            if (PropertyDescriptor.HasColor()) {
                identifiers.AddRange(colorIdentifier);
            }
            return identifiers;
        }
    }
}