using System;
using System.Collections.Generic;

namespace HuePat.Util.IO.SQLite {
    public class ForeignKey {
        private string foreignTable;
        private IList<string> columns;
        private IList<string> foreignColumns;

        public ForeignKey(
            string foreignTable,
            string column,
            string foreignColumn): 
                this(
                    foreignTable,
                    new string[] { column },
                    new string[] { foreignColumn }) {
        }

        public ForeignKey(
                string foreignTable,
                IList<string> columns, 
                IList<string> foreignColumns) {
            if (columns == null 
                    || foreignColumns == null
                    || columns.Count != foreignColumns.Count 
                    || columns.Count == 0) {
                throw new ArgumentException(
                    "Foreign key needs to have same number (>0) of columns and foreign columns.");
            }
            this.foreignTable = foreignTable;
            this.columns = columns;
            this.foreignColumns = foreignColumns;
        }

        public override string ToString() {
            return $"FOREIGN KEY ({columns.Join()}) REFERENCES {foreignTable}({foreignColumns.Join()})";
        }
    }
}
