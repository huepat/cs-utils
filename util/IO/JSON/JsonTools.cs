using HuePat.Util.Math.Geometry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace HuePat.Util.IO.JSON {
    public static class JSONTools {
        public static JToken Read(
                string file) {

            using (StreamReader streamReader = File.OpenText(file)) {
                using (JsonTextReader jsonReader = new JsonTextReader(streamReader)) {

                    return JToken.ReadFrom(jsonReader);
                }
            }
        }

        public static void Write(
                IEnumerable<Point> points, 
                string file, 
                string key = "points") {

            using (StreamWriter writer = new StreamWriter(file)) {

                writer.WriteLine($"{"{"}\"{key}\":[");
                bool first = true;

                foreach (Point point in points) {

                    if (point == null) {
                        writer.WriteLine($"{(first ? "" : ", ")}[]");
                    }
                    else {
                        writer.WriteLine(
                            $"{(first ? "" : ", ")}[{point.X}, {point.Y}, {point.Z}]");
                    }
                    if (first) {
                        first = false;
                    }
                }

                writer.WriteLine("]}");
            }
        }
    }
}