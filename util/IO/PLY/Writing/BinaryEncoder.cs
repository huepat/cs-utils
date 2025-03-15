using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Object.Properties;
using System.IO;
using System.Text;

namespace HuePat.Util.IO.PLY.Writing {
    class BinaryEncoder : IEncoder {
        private bool coordinatesAsFloat;
        private Format vertexFormat;
        private Format faceFormat;
        private BinaryWriter writer;

        public BinaryEncoder(
                string file,
                bool littleEndian,
                bool coordinatesAsFloat,
                Format vertexFormat,
                Format faceFormat) {

            this.coordinatesAsFloat = coordinatesAsFloat;
            this.vertexFormat = vertexFormat;
            this.faceFormat = faceFormat;

            writer = new BinaryWriter(
                File.Open(
                    file,
                    FileMode.Append,
                    FileAccess.Write),
                littleEndian ?
                    Encoding.Unicode :
                    Encoding.BigEndianUnicode);
        }

        public void Dispose() {

            writer.Dispose();
        }

        public void Encode(
                Point point) {

            if (coordinatesAsFloat) {
                writer.Write((float)point.X);
                writer.Write((float)point.Y);
                writer.Write((float)point.Z);
            }
            else {
                writer.Write(point.X);
                writer.Write(point.Y);
                writer.Write(point.Z);
            }

            foreach (string propertyName in vertexFormat.BytePropertyIdentifiers.Keys) {
                writer.Write(
                    point.GetByteProperty(propertyName));
            }

            foreach (string propertyName in vertexFormat.IntegerPropertyIdentifiers.Keys) {
                writer.Write(
                    point.GetIntegerProperty(propertyName));
            }

            foreach (string propertyName in vertexFormat.FloatPropertyIdentifiers.Keys) {
                writer.Write(
                    point.GetFloatProperty(propertyName));
            }

            foreach (string propertyName in vertexFormat.DoublePropertyIdentifiers.Keys) {
                writer.Write(
                    point.GetDoubleProperty(propertyName));
            }

            if (vertexFormat.HasNormalVector) {
                writer.Write(
                    point.GetNormalVector());
            }

            foreach (string propertyName in vertexFormat.Vector3dPropertyIdentifiers.Keys) {
                writer.Write(
                    point.GetVector3Property(propertyName));
            }

            if (vertexFormat.HasColor) {
                writer.Write(
                    point.GetColor(),
                    vertexFormat.ColorIdentifiers.Length == 4);
            }

            foreach (string propertyName in vertexFormat.ColorPropertyIdentifiers.Keys) {
                writer.Write(
                    point.GetColorProperty(propertyName),
                    vertexFormat.ColorIdentifiers.Length == 4);
            }
        }

        public void Encode(
                Face face, 
                int offset) {

            writer.Write((byte)3);
            writer.Write(face.VertexIndex1 + offset);
            writer.Write(face.VertexIndex2 + offset);
            writer.Write(face.VertexIndex3 + offset);

            foreach (string propertyName in faceFormat.BytePropertyIdentifiers.Keys) {
                writer.Write(
                    face.GetByteProperty(propertyName));
            }

            foreach (string propertyName in faceFormat.IntegerPropertyIdentifiers.Keys) {
                writer.Write(
                    face.GetIntegerProperty(propertyName));
            }

            foreach (string propertyName in faceFormat.FloatPropertyIdentifiers.Keys) {
                writer.Write(
                    face.GetFloatProperty(propertyName));
            }

            foreach (string propertyName in faceFormat.DoublePropertyIdentifiers.Keys) {
                writer.Write(
                    face.GetDoubleProperty(propertyName));
            }

            if (faceFormat.HasNormalVector) {
                writer.Write(
                    face.GetNormalVector());
            }

            foreach (string propertyName in faceFormat.Vector3dPropertyIdentifiers.Keys) {
                writer.Write(
                    face.GetVector3Property(propertyName));
            }

            foreach (string propertyName in faceFormat.ColorPropertyIdentifiers.Keys) {
                writer.Write(
                    face.GetColorProperty(propertyName),
                    faceFormat.ColorIdentifiers.Length == 4);
            }
        }
    }
}