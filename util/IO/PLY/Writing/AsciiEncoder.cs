using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System.IO;

namespace HuePat.Util.IO.PLY.Writing {
    class AsciiEncoder : IEncoder {
        private bool coordinatesAsFloat;
        private Format vertexFormat;
        private Format faceFormat;
        private StreamWriter writer;

        public AsciiEncoder(
                string file,
                bool coordinatesAsFloat,
                Format vertexFormat,
                Format faceFormat) {

            this.coordinatesAsFloat = coordinatesAsFloat;
            this.vertexFormat = vertexFormat;
            this.faceFormat = faceFormat;

            writer = new StreamWriter(file, true);
        }

        public void Dispose() {
            writer.Dispose();
        }

        public void Encode(
                Point point) {

            string line = coordinatesAsFloat ? 
                $"{(float)point.X} {(float)point.Y} {(float)point.Z}" :
                Append("", point.Position);

            foreach (string propertyName in vertexFormat.BytePropertyIdentifiers.Keys) {
                line = Append(
                    line,
                    point.GetByteProperty(propertyName));
            }

            foreach (string propertyName in vertexFormat.IntegerPropertyIdentifiers.Keys) {
                line = Append(
                    line,
                    point.GetIntegerProperty(propertyName));
            }

            foreach (string propertyName in vertexFormat.FloatPropertyIdentifiers.Keys) {
                line = Append(
                    line,
                    point.GetFloatProperty(propertyName));
            }

            foreach (string propertyName in vertexFormat.DoublePropertyIdentifiers.Keys) {
                line = Append(
                    line, 
                    point.GetDoubleProperty(propertyName));
            }

            if (vertexFormat.HasNormalVector) {
                line = Append(
                    line, 
                    point.GetNormalVector());
            }

            foreach (string propertyName in vertexFormat.Vector3dPropertyIdentifiers.Keys) {
                line = Append(
                    line, 
                    point.GetVector3Property(propertyName));
            }

            if (vertexFormat.HasColor) {
                line = Append(
                    line, 
                    point.GetColor(),
                    vertexFormat.ColorIdentifiers.Length == 4);
            }

            foreach (string propertyName in vertexFormat.ColorPropertyIdentifiers.Keys) {
                line = Append(
                    line, 
                    point.GetColorProperty(propertyName),
                    vertexFormat.ColorIdentifiers.Length == 4);
            }

            writer.WriteLine(line);
        }

        public void Encode(
                Face face,
                int offset) {

            string line = $"3 {face.VertexIndex1 + offset} {face.VertexIndex2 + offset} {face.VertexIndex3 + offset}";

            foreach (string propertyName in faceFormat.BytePropertyIdentifiers.Keys) {
                line = Append(
                    line,
                    face.GetByteProperty(propertyName));
            }

            foreach (string propertyName in faceFormat.IntegerPropertyIdentifiers.Keys) {
                line = Append(
                    line,
                    face.GetIntegerProperty(propertyName));
            }

            foreach (string propertyName in faceFormat.FloatPropertyIdentifiers.Keys) {
                line = Append(
                    line,
                    face.GetFloatProperty(propertyName));
            }

            foreach (string propertyName in faceFormat.DoublePropertyIdentifiers.Keys) {
                line = Append(
                    line, 
                    face.GetDoubleProperty(propertyName));
            }

            if (faceFormat.HasNormalVector) {
                line = Append(
                    line, 
                    face.Geometry.Normal);
            }

            foreach (string propertyName in faceFormat.Vector3dPropertyIdentifiers.Keys) {
                line = Append(
                    line, 
                    face.GetVector3Property(propertyName));
            }

            foreach (string propertyName in faceFormat.ColorPropertyIdentifiers.Keys) {
                line = Append(
                    line, 
                    face.GetColorProperty(propertyName),
                    faceFormat.ColorIdentifiers.Length == 4);
            }

            writer.WriteLine(line);
        }

        private string Append(
                string line,
                byte value) {

            return $"{line} {value}";
        }

        private string Append(
                string line,
                int value) {

            return $"{line} {value}";
        }

        private string Append(
                string line,
                float value) {

            return $"{line} {value}";
        }

        private string Append(
                string line, 
                double value) {

            return $"{line} {value}";
        }

        private string Append(
                string line,
                Vector3d vector) {

            return $"{line} {vector.X} {vector.Y} {vector.Z}";
        }

        private string Append(
                string line, 
                Color color, 
                bool withAChannel) {

            return $"{line} {color.R} {color.G} {color.B}{(withAChannel ? $" {color.A}" : "")}";
        }
    }
}
