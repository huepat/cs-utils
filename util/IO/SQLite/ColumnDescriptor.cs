namespace HuePat.Util.IO.SQLite {
    public class ColumnDescriptor {
        public bool NotNull { get; private set; }
        public bool Unique { get; private set; }
        public DataBase.DataType DataType { get; private set; }
        public string Name { get; private set; }

        public ColumnDescriptor(
                string name,
                DataBase.DataType dataType) {
            DataType = dataType;
            Name = name;
            NotNull = false;
            Unique = false;
        }

        public ColumnDescriptor SetNotNull() {
            NotNull = true;
            return this;
        }

        public ColumnDescriptor SetUnique() {
            Unique = true;
            return this;
        }

        public override string ToString() {
            string s = $"{Name} {DataType}";
            if (NotNull) {
                s += " NOT NULL";
            }
            if (Unique) {
                s += " UNIQUE";
            }
            return s;
        }
    }
}