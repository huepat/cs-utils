using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System.IO;

namespace HuePat.Util.IO.CSV.Writing {
    public static class CsvWriter {
        public static void Write(string file, PointCloud pointCloud) {
            Write(file, pointCloud, new PropertyDescriptor());
        }

        public static void Write(string file, PointCloud pointCloud, PropertyDescriptor descriptor) {
            using (StreamWriter writer = new StreamWriter(file)) {
                foreach (Point point in pointCloud) {
                    string line = $"{point.X}, {point.Y}, {point.Z}";
                    if (descriptor.HasColor()) {
                        line = Add(line, point.GetColor());
                    }
                    if (descriptor.HasNormalVector()) {
                        line = Add(line, point.GetNormalVector());
                    }
                    foreach (string propertyName in descriptor.DoubleProperties) {
                        line = Add(line, point.GetDoubleProperty(propertyName));
                    }
                    writer.WriteLine(line);
                }
            }
        }

        private static string Add(string line, Color color) {
            return Add(line, $"{color.R}, {color.G}, {color.B}");
        }

        private static string Add(string line, Vector3d vector) {
            return Add(line, $"{vector.X}, {vector.Y}, {vector.Z}");
        }

        private static string Add(string line, double value) {
            return Add(line, value.ToString());
        }

        private static string Add(string line, string newPart) {
            return $"{line}, {newPart}";
        }
    }
}
