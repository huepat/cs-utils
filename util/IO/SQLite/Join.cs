namespace HuePat.Util.IO.SQLite {
    public class Join {
        public enum JoinType {
            INNER,
            LEFT,
            CROSS
        }

        public JoinType Type { get; private set; }
        public string ForeignTable { get; private set; }
        public string TableColumn { get; private set; }
        public string ForeignTableColumn { get; private set; }

        public Join(
                string foreignTable,
                string tableColumn,
                string foreignTableColumn) {
            ForeignTable = foreignTable;
            TableColumn = tableColumn;
            ForeignTableColumn = foreignTableColumn;
            Type = JoinType.INNER;
        }
    }
}