namespace HuePat.Util.IO.SQLite {
    public class OrderByColumn {
        private string columnName;

        public bool Descending { private get; set; }

        public OrderByColumn(string columnName) {
            this.columnName = columnName;
        }

        public override string ToString() {
            if (Descending) {
                return $"{columnName} DESC";
            }
            return columnName;
        }
    }
}