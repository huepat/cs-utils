namespace HuePat.Util.Object.Properties {
    public class IntegerProperty: IProperty {
        public int Value { get; set; }

        public IProperty Clone() {
            return new IntegerProperty() {
                Value = Value
            }; ;
        }
    }
}