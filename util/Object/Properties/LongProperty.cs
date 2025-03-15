namespace HuePat.Util.Object.Properties {
    public class LongProperty: IProperty {
        public long Value { get; set; }

        public IProperty Clone() {
            return new LongProperty() {
                Value = Value
            };
        }
    }
}
