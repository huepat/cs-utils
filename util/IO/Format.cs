using System.Collections.Generic;

namespace HuePat.Util.IO {
    public class Format {
        public string[] CoordinateIdentifiers { get; set; }
        public string[] NormalVectorIdentifiers { get; set; }
        public string[] ColorIdentifiers { get; set; }
        public Dictionary<string, string> BytePropertyIdentifiers { get; set; }
        public Dictionary<string, string> IntegerPropertyIdentifiers { get; set; }
        public Dictionary<string, string> FloatPropertyIdentifiers { get; set; }
        public Dictionary<string, string> DoublePropertyIdentifiers { get; set; }
        public Dictionary<string, string[]> Vector3dPropertyIdentifiers { get; set; }
        public Dictionary<string, string[]> ColorPropertyIdentifiers { get; set; }

        public bool HasNormalVector {
            get {
                return NormalVectorIdentifiers != null;
            }
        }

        public bool HasColor {
            get {
                return ColorIdentifiers != null;
            }
        }
    }
}