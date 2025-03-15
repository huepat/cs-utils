using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.IO;

namespace HuePat.Util.IO.JSON {
    public class JSONWriter : IDisposable {

        private StreamWriter fileWriter;
        private JsonTextWriter jsonWriter;

        public JSONWriter(
                string file) { 

            fileWriter = new StreamWriter(file);
            jsonWriter = new JsonTextWriter(fileWriter);

            jsonWriter.Formatting = Formatting.Indented;
            jsonWriter.WriteStartObject();
        }

        public void Write(
                string identifier,
                double value) {

            jsonWriter.WritePropertyName(identifier);
            jsonWriter.WriteValue(value);
        }

        public void Write(
                string identifier,
                Vector3d vector) {

            jsonWriter.WritePropertyName(identifier);

            jsonWriter.WriteStartArray();

            jsonWriter.WriteValue(vector.X);
            jsonWriter.WriteValue(vector.Y);
            jsonWriter.WriteValue(vector.Z);

            jsonWriter.WriteEndArray();
        }

        public void Write(
                string identifier,
                Matrix3d matrix) {

            jsonWriter.WritePropertyName(identifier);

            jsonWriter.WriteStartArray();

            for (int r = 0; r < 3; r++) {

                jsonWriter.WriteStartArray();

                for (int c = 0; c < 3; c++) {

                    jsonWriter.WriteValue(matrix[r, c]);
                }

                jsonWriter.WriteEndArray();
            }

            jsonWriter.WriteEndArray();
        }

        public void Dispose() {

            jsonWriter.WriteEndObject();
            
            jsonWriter.Flush();
            jsonWriter.Close();

            fileWriter.Dispose();
        }
    }
}
