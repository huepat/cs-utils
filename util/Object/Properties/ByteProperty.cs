namespace HuePat.Util.Object.Properties {
    public class ByteProperty : IProperty {
        public byte Value { get; set; }

        public IProperty Clone() {
            return new ByteProperty {
                Value = Value
            };
        }
    }
}