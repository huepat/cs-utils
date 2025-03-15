namespace HuePat.Util.Object.Properties {
    public class BoolProperty : IProperty {
        public bool Value { get; set; }

        public IProperty Clone() {
            return new BoolProperty { 
                Value = Value
            };
        }
    }
}