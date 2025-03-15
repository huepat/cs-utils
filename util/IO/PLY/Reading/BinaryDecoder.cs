using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Object;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HuePat.Util.IO.PLY.Reading {
    class BinaryDecoder : IDecoder {
        private bool littleEndian;
        private bool areVertexCoordinatesFloat;
        private bool isVertexNormalVectorFloat;
        private bool isFaceNormalVectorFloat;

        public Action<Point> PointProcessor { get; set; }
        public Action<Face> FaceProcessor { get; set; }

        public BinaryDecoder(
                bool littleEndian,
                bool areVertexCoordinatesFloat,
                bool isVertexNormalVectorFloat,
                bool isFaceNormalVectorFloat) {

            this.littleEndian = littleEndian;
            this.areVertexCoordinatesFloat = areVertexCoordinatesFloat;
            this.isVertexNormalVectorFloat = isVertexNormalVectorFloat;
            this.isFaceNormalVectorFloat = isFaceNormalVectorFloat;
        }

        public List<Point> ReadPoints(
                string file, 
                Header header) {

            return ReadPoints(
                CreateReader(file, header),
                header);
        }

        public void ReadMesh(
                string file,
                bool switchNormals,
                Header header,
                out List<Point> vertices,
                out List<Face> faces) {

            int propertyIndex, maxPropertyIndex;
            object[] properties;
            Dictionary<int, PropertyType> propertyTypes;

            BinaryReader reader = CreateReader(
                file, 
                header);

            vertices = ReadPoints(
                reader, 
                header);

            faces = new List<Face>();
            propertyTypes = header.FaceSection.PropertyTypes;
            maxPropertyIndex = propertyTypes.Keys.Max();
            properties = new object[maxPropertyIndex + 1];

            // ToDo: this will not work when faces have named single properties
            // and also additional list properties at the same time

            while (faces.Count < header.FaceSection.Count) {

                propertyIndex = 0;

                while (propertyIndex <= maxPropertyIndex) {

                    properties[propertyIndex] = ReadProperty(
                        reader,
                        propertyTypes[propertyIndex]);

                    propertyIndex++;
                }

                faces.Add(
                    ParseFace(
                        properties,
                        switchNormals,
                        header.FaceSection,
                        vertices));

                foreach (PropertyType additionalListPropertyType in header.FaceSection.AdditionalListPropertyTypes) {

                    byte listCount = reader.ReadByte();

                    for (int i = 0; i < listCount; i++) {

                        ReadProperty(
                            reader,
                            additionalListPropertyType);
                    }
                }
            }
        }

        private BinaryReader CreateReader(
                string file,
                Header header) {

            BinaryReader reader = new BinaryReader(
                File.Open(
                    file, 
                    FileMode.Open, 
                    FileAccess.Read),
                littleEndian ?
                    Encoding.Unicode :
                    Encoding.BigEndianUnicode);

            reader.BaseStream.Position = header.VertexSectionStartPosition;

            return reader;
        }

        private List<Point> ReadPoints(
                BinaryReader reader,
                Header header) {

            int propertyIndex, maxPropertyIndex;
            object[] properties;
            List<Point> points = new List<Point>();
            Dictionary<int, PropertyType> propertyTypes = header.VertexSection.PropertyTypes;

            maxPropertyIndex = propertyTypes.Keys.Max();
            properties = new object[maxPropertyIndex + 1];

            while (points.Count < header.VertexSection.Count) {

                propertyIndex = 0;

                while (propertyIndex <= maxPropertyIndex) {

                    properties[propertyIndex] = ReadProperty(
                        reader, 
                        propertyTypes[propertyIndex]);

                    propertyIndex++;
                }

                points.Add(
                    ParsePoint(
                        properties,
                        header.VertexSection));
            }

            return points;
        }

        private object ReadProperty(
                BinaryReader reader,
                PropertyType propertyType) {

            switch (propertyType) {
                case PropertyType.BYTE:
                    return reader.ReadByte();
                case PropertyType.INTEGER:
                    return reader.ReadInt32();
                case PropertyType.LONG:
                    return reader.ReadInt64();
                case PropertyType.FLOAT:
                    return reader.ReadSingle();
                case PropertyType.DOUBLE:
                    return reader.ReadDouble();
                case PropertyType.VECTOR3D:
                case PropertyType.COLOR:
                case PropertyType.BOOL:
                default:
                    break;
            }

            throw new ArgumentException();
        }

        private Point ParsePoint(
                object[] properties,
                VertexSection vertexSection) {

            Point point = new Point(
                ParseVector3d(
                    properties, 
                    vertexSection.CoordinateIndices,
                    areVertexCoordinatesFloat));

            if (vertexSection.HasColor) {
                point.SetColor(
                    ParseColor(
                        properties, 
                        vertexSection.ColorIndices));
            }

            if (vertexSection.HasNormalVector) {
                point.SetNormalVector(
                    ParseVector3d(
                        properties, 
                        vertexSection.NormalVectorIndices,
                        isVertexNormalVectorFloat));
            }

            ParseProperties(
                point,
                properties, 
                vertexSection);

            if (PointProcessor != null) {
                PointProcessor(point);
            }

            return point;
        }

        private Face ParseFace(
                object[] properties,
                bool switchNormals,
                FaceSection faceSection,
                List<Point> vertices) {

            byte vertex_count = ParseByte(
                properties, 
                faceSection.VertexCountIndex);

            if (vertex_count != 3) {
                throw new ArgumentException(
                    "BinaryDecorder currently only parses faces with three indices.");
            }

            int[] vertexIndices = new int[] {
                ParseInteger(properties, faceSection.VertexIndexIndices[0]),
                ParseInteger(properties, faceSection.VertexIndexIndices[1]),
                ParseInteger(properties, faceSection.VertexIndexIndices[2])
            };

            Face face = new Face(
                vertexIndices[0],
                switchNormals ? vertexIndices[2] : vertexIndices[1],
                switchNormals ? vertexIndices[1] : vertexIndices[2],
                vertices
            );

            if (faceSection.HasNormalVector) {
                face.SetNormalVector(
                    ParseVector3d(
                        properties,
                        faceSection.NormalVectorIndices,
                        isFaceNormalVectorFloat));
            }

            ParseProperties(
                face,
                properties,
                faceSection);

            FaceProcessor?.Invoke(face);

            return face;
        }

        private byte ParseByte(
                object[] properties,
                int index) {

            return (byte)properties[index];
        }

        private int ParseInteger(
                object[] properties, 
                int index) {

            return (int)properties[index];
        }

        private float ParseFloat(
                object[] properties, 
                int index) {

            return (float)properties[index];
        }

        private double ParseDouble(
                object[] properties, 
                int index) {

            return (double)properties[index];
        }

        private Vector3d ParseVector3d(
                object[] properties,
                int[] indices,
                bool isFloat) {

            if (isFloat) {
                return new Vector3d(
                    (float)properties[indices[0]],
                    (float)properties[indices[1]],
                    (float)properties[indices[2]]);
            }

            return new Vector3d(
                (double)properties[indices[0]],
                (double)properties[indices[1]],
                (double)properties[indices[2]]);
        }

        private Color ParseColor(
                object[] properties,
                int[] indices) {

            if (indices.Length == 4) {
                return new Color(
                    (byte)properties[indices[0]],
                    (byte)properties[indices[1]],
                    (byte)properties[indices[2]],
                    (byte)properties[indices[3]]);
            }

            return new Color(
                    (byte)properties[indices[0]],
                    (byte)properties[indices[1]],
                    (byte)properties[indices[2]]);
        }

        private void ParseProperties(
                IObject @object,
                object[] properties,
                HeaderSection headerSection) {

            ParseByteProperties(@object, properties, headerSection);
            ParseIntegerProperties(@object, properties, headerSection);
            ParseFloatProperties(@object, properties, headerSection);
            ParseDoubleProperties(@object, properties, headerSection);
            ParseVector3dProperties(@object, properties, headerSection);
            ParseColorProperties(@object, properties, headerSection);
        }

        private void ParseByteProperties(
                IObject @object,
                object[] properties,
                HeaderSection headerSection) {

            int index;

            foreach (string propertyName in headerSection.BytePropertyIndices.Keys) {

                index = headerSection.BytePropertyIndices[propertyName];

                if (!headerSection.IndicesNotInFormat.Contains(index)) {

                    @object.SetByteProperty(
                        propertyName,
                        ParseByte(properties, index));
                }
            }
        }

        private void ParseIntegerProperties(
                IObject @object,
                object[] properties,
                HeaderSection headerSection) {

            int index;

            foreach (string propertyName in headerSection.IntegerPropertyIndices.Keys) {

                index = headerSection.IntegerPropertyIndices[propertyName];

                if (!headerSection.IndicesNotInFormat.Contains(index)) {

                    @object.SetIntegerProperty(
                        propertyName,
                        ParseInteger(properties, index));
                }   
            }
        }

        private void ParseFloatProperties(
                IObject @object,
                object[] properties,
                HeaderSection headerSection) {

            int index;

            foreach (string propertyName in headerSection.FloatPropertyIndices.Keys) {

                index = headerSection.FloatPropertyIndices[propertyName];

                if (!headerSection.IndicesNotInFormat.Contains(index)) {

                    @object.SetFloatProperty(
                        propertyName,
                        ParseFloat(properties, index));
                }
            }
        }

        private void ParseDoubleProperties(
                IObject @object,
                object[] properties,
                HeaderSection headerSection) {

            int index;

            foreach (string propertyName in headerSection.DoublePropertyIndices.Keys) {

                index = headerSection.DoublePropertyIndices[propertyName];

                if (!headerSection.IndicesNotInFormat.Contains(index)) {

                    @object.SetDoubleProperty(
                        propertyName,
                        ParseDouble(properties, index));
                }
            }
        }

        private void ParseVector3dProperties(
                IObject @object,
                object[] properties,
                HeaderSection headerSection) {

            int[] indices;

            foreach (string propertyName in headerSection.Vector3dPropertyIndices.Keys) {

                indices = headerSection.Vector3dPropertyIndices[propertyName];

                if (!indices.Any(index => headerSection.IndicesNotInFormat.Contains(index))) {

                    @object.SetVector3Property(
                        propertyName,
                        ParseVector3d(
                            properties,
                            indices,
                            false));
                }
            }
        }

        private void ParseColorProperties(
                IObject @object,
                object[] properties,
                HeaderSection headerSection) {

            int[] indices;

            foreach (string propertyName in headerSection.ColorPropertyIndices.Keys) {

                indices = headerSection.ColorPropertyIndices[propertyName];

                if (!indices.Any(index => headerSection.IndicesNotInFormat.Contains(index))) {

                    @object.SetColorProperty(
                        propertyName,
                        ParseColor(properties, indices));
                }
            }
        }
    }
}