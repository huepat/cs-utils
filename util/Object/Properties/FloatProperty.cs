namespace HuePat.Util.Object.Properties {
    public class FloatProperty : IProperty {
        public float Value { get; set; }

        public IProperty Clone() {
            return new FloatProperty() {
                Value = Value
            };
        }
    }
}