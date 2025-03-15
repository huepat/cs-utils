using System.IO;
using System;
using System.Collections.Generic;
using HuePat.Util.Object.Properties;
using HuePat.Util.Math.Geometry;

using SmallPoint = HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals.Point;

namespace HuePat.Util.IO.PLY.Reading.MemoryEfficiency.PointsWithNormals {
    public class PlyReader {
        private bool areCoordinatesFloat;
        private bool isNormalVectorFloat;
        private HeaderParser headerParser;

        public bool AreCoordinatesFloat {
            set {
                areCoordinatesFloat = value;
                headerParser.AreVertexCoordinatesFloat = value;
            }
        }

        public bool IsNormalVectorFloat {
            set {
                isNormalVectorFloat = value;
                headerParser.IsVertexNormalVectorFloat = value;
            }
        }

        public int LineBreakByteSize {
            set {
                headerParser.LineBreakByteSize = value;
            }
        }

        public PlyReader() {

            headerParser = new HeaderParser() {
                VertexFormat = new PointFormat() { 
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddNormalVector()
                }.Create(),
                FaceFormat = new FaceFormat().Create()
            };

            AreCoordinatesFloat = true;
            IsNormalVectorFloat = true;
            LineBreakByteSize = 1;
        }

        public List<SmallPoint> ReadPoints(
                string file) {

            Header header = ReadHeader(file);

            return GetDecoder(header)
                .ReadPoints(file, header);
        }

        private Header ReadHeader(
                string file) {

            Header header;

            headerParser.Initialize();

            foreach (string line in File.ReadLines(file)) {

                header = headerParser.ParseHeaderLine(line);

                if (header != null) {
                    return header;
                }
            }

            return null;
        }

        private IDecoder GetDecoder(
                Header header) {

            IDecoder decoder;

            switch (header.Encoding) {
                case PlyEncoding.BINARY_LITTLE_ENDIAN:
                    decoder = new BinaryDecoder(
                        true,
                        areCoordinatesFloat,
                        isNormalVectorFloat);
                    break;
                case PlyEncoding.BINARY_BIG_ENDIAN:
                    decoder = new BinaryDecoder(
                        false,
                        areCoordinatesFloat,
                        isNormalVectorFloat);
                    break;
                case PlyEncoding.ASCII:
                    decoder = new AsciiDecoder();
                    break;
                default:
                    throw new ApplicationException();
            }

            return decoder;
        }
    }
}