namespace HuePat.Util.Object.Properties {
    public class StringProperty : IProperty {
        public string Value { get; set; }

        public IProperty Clone() {
            return new StringProperty {
                Value = Value
            };
        }
    }
}