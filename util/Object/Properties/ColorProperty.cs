using HuePat.Util.Colors;

namespace HuePat.Util.Object.Properties {
    public class ColorProperty : IProperty {
        public Color Value { get; set; }

        public IProperty Clone() {
            return new ColorProperty() {
                Value = Value.Clone()
            };
        }
    }
}