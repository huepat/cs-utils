namespace HuePat.Util.Object.Properties {
    public class DoubleProperty: IProperty {
        public double Value { get; set; }

        public IProperty Clone() {
            return new DoubleProperty() {
                Value = Value
            };
        }
    }
}