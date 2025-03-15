using System.IO;

namespace HuePat.Util.IO.PLY.Writing {
    static class HeaderWriter {
        public static void Write(
                string file,
                bool coordinatesAsFloat,
                PlyEncoding encoding,
                long vertexCount,
                long faceCount,
                Format vertexFormat,
                Format faceFormat) {

            using (StreamWriter writer = new StreamWriter(file)) {

                writer.Write("ply\n");
                writer.Write($"format {encoding.GetString()} 1.0\n");
                writer.Write($"element vertex {vertexCount}\n");
                writer.WriteVertexAttributes(coordinatesAsFloat, vertexFormat);
                writer.Write($"element face {faceCount}\n");
                writer.Write("property list uchar int vertex_indices\n");
                writer.WriteFaceAttributes(faceFormat);
                writer.Write("end_header\n");
            }
        }

        private static void WriteVertexAttributes(
                this StreamWriter writer,
                bool coordinatesAsFloat,
                Format format) {

            if (coordinatesAsFloat) {
                writer.WriteVector3PropertyDescriptor(
                    format.CoordinateIdentifiers);
            }
            else {
                writer.WriteVector3dPropertyDescriptor(
                    format.CoordinateIdentifiers);
            }

            foreach (string propertyName in format.BytePropertyIdentifiers.Keys) {
                writer.WriteBytePropertyDescriptor(
                    format.BytePropertyIdentifiers[propertyName]);
            }

            foreach (string propertyName in format.IntegerPropertyIdentifiers.Keys) {
                writer.WriteIntegerPropertyDescriptor(
                    format.IntegerPropertyIdentifiers[propertyName]);
            }

            foreach (string propertyName in format.FloatPropertyIdentifiers.Keys) {
                writer.WriteFloatPropertyDescriptor(
                    format.FloatPropertyIdentifiers[propertyName]);
            }

            foreach (string propertyName in format.DoublePropertyIdentifiers.Keys) {
                writer.WriteDoublePropertyDescriptor(
                    format.DoublePropertyIdentifiers[propertyName]);
            }

            if (format.HasNormalVector) {
                writer.WriteVector3dPropertyDescriptor(
                    format.NormalVectorIdentifiers);
            }

            foreach (string propertyName in format.Vector3dPropertyIdentifiers.Keys) {
                writer.WriteVector3dPropertyDescriptor(
                    format.Vector3dPropertyIdentifiers[propertyName]);
            }

            if (format.HasColor) {
                writer.WriteColorPropertyDescriptor(
                    format.ColorIdentifiers);
            }

            foreach (string propertyName in format.ColorPropertyIdentifiers.Keys) {
                writer.WriteColorPropertyDescriptor(
                    format.ColorPropertyIdentifiers[propertyName]);
            }
        }

        private static void WriteFaceAttributes(
                this StreamWriter writer,
                Format format) {

            foreach (string propertyName in format.BytePropertyIdentifiers.Keys) {
                writer.WriteBytePropertyDescriptor(
                    format.BytePropertyIdentifiers[propertyName]);
            }

            foreach (string propertyName in format.IntegerPropertyIdentifiers.Keys) {
                writer.WriteIntegerPropertyDescriptor(
                    format.IntegerPropertyIdentifiers[propertyName]);
            }

            foreach (string propertyName in format.DoublePropertyIdentifiers.Keys) {
                writer.WriteDoublePropertyDescriptor(
                    format.DoublePropertyIdentifiers[propertyName]);
            }

            if (format.HasNormalVector) {
                writer.WriteVector3dPropertyDescriptor(
                    format.NormalVectorIdentifiers);
            }

            foreach (string propertyName in format.Vector3dPropertyIdentifiers.Keys) {
                writer.WriteVector3dPropertyDescriptor(
                    format.Vector3dPropertyIdentifiers[propertyName]);
            }

            foreach (string propertyName in format.ColorPropertyIdentifiers.Keys) {
                writer.WriteColorPropertyDescriptor(
                    format.ColorPropertyIdentifiers[propertyName]);
            }
        }

        private static void WriteBytePropertyDescriptor(
                this StreamWriter writer,
                string propertyIdentifier) {

            writer.WritePropertyDescriptor(
                "uchar", 
                propertyIdentifier);
        }

        private static void WriteIntegerPropertyDescriptor(
                this StreamWriter writer,
                string propertyIdentifier) {

            writer.WritePropertyDescriptor(
                "int", 
                propertyIdentifier);
        }

        private static void WriteFloatPropertyDescriptor(
                this StreamWriter writer,
                string propertyIdentifier) {

            writer.WritePropertyDescriptor(
                "float", 
                propertyIdentifier);
        }

        private static void WriteDoublePropertyDescriptor(
                this StreamWriter writer,
                string propertyIdentifier) {

            writer.WritePropertyDescriptor(
                "double", 
                propertyIdentifier);
        }

        private static void WriteVector3PropertyDescriptor(
                this StreamWriter writer,
                string[] propertyIdentifier) {

            writer.WriteFloatPropertyDescriptor(propertyIdentifier[0]);
            writer.WriteFloatPropertyDescriptor(propertyIdentifier[1]);
            writer.WriteFloatPropertyDescriptor(propertyIdentifier[2]);
        }

        private static void WriteVector3dPropertyDescriptor(
                this StreamWriter writer,
                string[] propertyIdentifier) {

            writer.WriteDoublePropertyDescriptor(propertyIdentifier[0]);
            writer.WriteDoublePropertyDescriptor(propertyIdentifier[1]);
            writer.WriteDoublePropertyDescriptor(propertyIdentifier[2]);
        }

        private static void WriteColorPropertyDescriptor(
                this StreamWriter writer,
                string[] propertyIdentifier) {

            writer.WriteBytePropertyDescriptor(propertyIdentifier[0]);
            writer.WriteBytePropertyDescriptor(propertyIdentifier[1]);
            writer.WriteBytePropertyDescriptor(propertyIdentifier[2]);

            if (propertyIdentifier.Length == 4) {
                writer.WriteBytePropertyDescriptor(propertyIdentifier[3]);
            }
        }

        private static void WritePropertyDescriptor(
                this StreamWriter writer,
                string propertyType,
                string propertyIdentifier) {

            writer.Write($"property {propertyType} {propertyIdentifier}\n");
        }
    }
}
