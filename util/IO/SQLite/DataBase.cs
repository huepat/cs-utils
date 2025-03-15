using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace HuePat.Util.IO.SQLite {
    public class DataBase: IDisposable {
        public enum DataType {
            INTEGER,
            REAL,
            TEXT,
            BLOB
        }

        public const string ROW_ID_COLUMN = "rowid";

        public static DataBase Create(string file) {
            SQLiteConnection.CreateFile(file);
            return new DataBase(file);
        }

        private SQLiteConnection connection;

        public string Path { get; private set; }

        public DataBase(string file) {
            connection = new SQLiteConnection(
                $"Data Source={file}; foreign keys=true;");
            connection.Open();
            Path = file;
        }

        public void Dispose() {
            connection.Close();
            connection.Dispose();
        }

        public void CreateTable(
                TableDescriptor table,
                bool ifNotExists = false) {
            Execute(
                $"CREATE TABLE{(ifNotExists ? " IF NOT EXISTS" : "")} {table}");
        }

        public void DropTable(string name) {
            Execute($"DROP TABLE {name}");
        }

        public bool TableExists(string table) {
            return Select(
                new Selection("sqlite_master") {
                    Where = $"type = 'table' AND name = '{table}'"
                }).Any();
        }

        public long Insert(string table, IList<object> values) {
            return InsertValues(table, new string[0], values);
        }

        public long Insert(
                string table,
                IList<string> columns,
                IList<object> values) {
            if (columns.Count != values.Count) {
                throw new ArgumentException(
                    $"Trying to insert {values.Count} in {columns.Count} columns in table '{table}'{Environment.NewLine}" +
                    $"  Columns: {columns.Join()}{Environment.NewLine}" +
                    $"  values: {values.Select(value => value.ToString()).Join()}");
            }
            return InsertValues(table, columns, values);
        }
        
        public IEnumerable<object[]> Select(Selection selection) {
            using (SQLiteCommand command = new SQLiteCommand(selection.Command, connection)) {
                using (SQLiteDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        yield return ExtractValues(reader);
                    }
                }
            }
        }

        private object[] ExtractValues(SQLiteDataReader reader) {
            int fieldCount = reader.FieldCount;
            object[] values = new object[fieldCount];
            for (int i = 0; i < fieldCount; i++) {
                values[i] = reader[i];
            }
            return values;
        }

        private long InsertValues(string table, IList<string> columns, IList<object> values) {
            Execute(
                $"INSERT INTO {table}{(columns.Count > 0 ? $"({columns.Join()})" : "")} " +
                $"VALUES ({Enumerable.Range(0, values.Count).Select(value => "?").Join()})",
                values);
            return connection.LastInsertRowId;
        }

        protected void Execute(string command) {
            Execute(command, new object[0]);
        }

        protected void Execute(string commandText, IList<object> parameters) {
            using (SQLiteCommand command = new SQLiteCommand(commandText, connection)) {
                foreach (object parameter in parameters) {
                    command.Parameters.AddWithValue(null, parameter);
                }
                try {
                    command.ExecuteNonQuery();
                }
                catch (SQLiteException e) {
                    throw new SQLiteException(
                        $"{e.Message}. At command: {commandText}");
                }
            }
        }
    }
}