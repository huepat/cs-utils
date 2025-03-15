using HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HuePat.Util.IO.PLY.Reading.MemoryEfficiency.PointsWithNormals {
    class BinaryDecoder : IDecoder {
        private bool littleEndian;
        private bool areCoordinatesFloat;
        private bool isNormalVectorFloat;

        public BinaryDecoder(
                bool littleEndian,
                bool areCoordinatesFloat,
                bool isNormalVectorFloat) {

            this.littleEndian = littleEndian;
            this.areCoordinatesFloat = areCoordinatesFloat;
            this.isNormalVectorFloat = isNormalVectorFloat;
        }

        public List<Point> ReadPoints(
                string file, 
                Header header) {

            return ReadPoints(
                CreateReader(file, header),
                header);
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

            return new Point(
                ParseVector3d(
                    properties, 
                    vertexSection.CoordinateIndices,
                    areCoordinatesFloat),
                ParseVector3d(
                    properties,
                    vertexSection.NormalVectorIndices,
                    isNormalVectorFloat));
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
    }
}