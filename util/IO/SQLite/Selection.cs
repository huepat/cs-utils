using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.IO.SQLite {
    public class Selection {
        private string table;

        public bool Distinct { private get; set; }
        public string Where { private get; set; }
        public IList<string> Results { private get; set; }
        public IList<Join> Joins { private get; set; }
        public IList<OrderByColumn> OrderByColumns { private get; set; }

        public string Result {
            set {
                Results = new string[] { value };
            }
        }

        public Join Join {
            set {
                Joins = new Join[] { value };
            }
        }

        public OrderByColumn OrderByColumn {
            set {
                OrderByColumns = new OrderByColumn[] { value };
            }
        }

        public string Command {
            get {
                string command = "SELECT";
                if (Distinct) {
                    command = $"{command} DISTINCT";
                }
                command = $"{command} {Results.Join()} FROM {table}";
                foreach (Join join in Joins) {
                    command = 
                        $"{command} {join.Type} JOIN {join.ForeignTable} " +
                        $"ON {table}.{join.TableColumn} = {join.ForeignTable}.{join.ForeignTableColumn}";
                }
                if (Where != null) {
                    command = $"{command} WHERE {Where}";
                }
                if (OrderByColumns.Count > 0) {
                    command = $"{command} ORDER BY {OrderByColumns.Select(column => column.ToString()).Join()}";
                }
                return command;
            }
        }

        public Selection(string table) {
            this.table = table;
            Distinct = false;
            Result = "*";
            Joins = new Join[0];
            OrderByColumns = new OrderByColumn[0];
        }
    }
}