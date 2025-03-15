using System.Collections.Generic;

namespace HuePat.Util.IO.SQLite {
    public class TableDescriptor {
        private string name;
        private IList<string> primaryKeyColumns;
        private IList<ColumnDescriptor> columns;
        private IList<ForeignKey> foreignKeys;

        private string ColumnsList {
            get {
                return string.Join(", ", columns);
            }
        }

        private string PrimaryKeyColumnsList {
            get {
                if (primaryKeyColumns.Count > 0) {
                    return $", PRIMARY KEY ({primaryKeyColumns.Join()})";
                }
                return "";
            }
        }

        private string ForeignKeysList {
            get {
                if (foreignKeys.Count > 0) {
                    return $", {string.Join(", ", foreignKeys)}";
                }
                return "";
            }
        }

        public TableDescriptor(string name) {
            this.name = name;
            primaryKeyColumns = new string[0];
            columns = new List<ColumnDescriptor>();
            foreignKeys = new ForeignKey[0];
        }

        public TableDescriptor AddColumn(ColumnDescriptor column) {
            columns.Add(column);
            return this;
        }

        public TableDescriptor SetPrimaryKey(string columnName) {
            return SetPrimaryKey(new string[] { columnName });
        }

        public TableDescriptor SetPrimaryKey(IList<string> columnNames) {
            primaryKeyColumns = columnNames;
            return this;
        }

        public TableDescriptor SetForeignKey(ForeignKey foreignKey) {
            return SetForeignKeys(new ForeignKey[] { foreignKey });
        }

        public TableDescriptor SetForeignKeys(IList<ForeignKey> foreignKeys) {
            this.foreignKeys = foreignKeys;
            return this;
        }

        public override string ToString() {
            return $"{name} ({ColumnsList}{PrimaryKeyColumnsList}{ForeignKeysList})";
        }
    }
}