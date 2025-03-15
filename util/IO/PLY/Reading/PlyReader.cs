using System.Collections.Generic;
using System.IO;
using HuePat.Util.Math.Geometry.SpatialIndices;
using System;
using HuePat.Util.Math.Geometry;

namespace HuePat.Util.IO.PLY.Reading {
    public class PlyReader {
        private bool areVertexCoordinatesFloat;
        private bool isVertexNormalVectorFloat;
        private bool isFaceNormalVectorFloat;
        private HeaderParser headerParser;

        public bool InvertNormals { private get; set; }
        public ISpatialIndex<Point> PointIndex { private get; set; }
        public ISpatialIndex<Face> MeshIndex { private get; set; }
        public Action<Point> PointProcessor { private get; set; }
        public Action<Face> FaceProcessor { private get; set; }

        public bool AreVertexCoordinatesFloat {
            set {
                areVertexCoordinatesFloat = value;
                headerParser.AreVertexCoordinatesFloat = value;
            }
        }

        public bool IsVertexNormalVectorFloat {
            set {
                isVertexNormalVectorFloat = value;
                headerParser.IsVertexNormalVectorFloat = value;
            }
        }

        public bool IsFaceNormalVectorFloat {
            set {
                isFaceNormalVectorFloat = value;
                headerParser.IsFaceNormalVectorFloat = value;
            }
        }

        public int LineBreakByteSize {
            set {
                headerParser.LineBreakByteSize = value;
            }
        }

        public PointFormat PointFormat {
            set {
                headerParser.VertexFormat = value.Create();
            }
        }

        public FaceFormat FaceFormat {
            set {
                headerParser.FaceFormat = value.Create();
            }
        }

        public PlyReader() {

            PointIndex = new BruteForceIndex<Point>();
            MeshIndex = new BruteForceIndex<Face>();

            headerParser = new HeaderParser() {
                VertexFormat = new PointFormat().Create(),
                FaceFormat = new FaceFormat().Create()
            };

            AreVertexCoordinatesFloat = true;
            IsVertexNormalVectorFloat = true;
            IsFaceNormalVectorFloat = true;
            LineBreakByteSize = 1;
        }

        public PointCloud ReadPointCloud(
                string file) {

            Header header = ReadHeader(file);

            IDecoder decoder = GetDecoder(header);

            return ReadPointCloud(
                file,
                decoder,
                header);
        }

        public Mesh ReadMesh(
                string file) {

            Header header = ReadHeader(file);

            IDecoder decoder = GetDecoder(header);

            return ReadMesh(file, decoder, header);
        }

        public IShape ReadShape(
                string file) {

            Header header = ReadHeader(file);

            IDecoder decoder = GetDecoder(header);

            if (header.HasFaces) {

                return ReadMesh(
                    file,
                    decoder,
                    header);
            }

            return ReadPointCloud(
                file,
                decoder,
                header);
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
                        areVertexCoordinatesFloat,
                        isVertexNormalVectorFloat,
                        isFaceNormalVectorFloat);
                    break;
                case PlyEncoding.BINARY_BIG_ENDIAN:
                    decoder = new BinaryDecoder(
                        false,
                        areVertexCoordinatesFloat,
                        isVertexNormalVectorFloat,
                        isFaceNormalVectorFloat);
                    break;
                case PlyEncoding.ASCII:
                    decoder = new AsciiDecoder();
                    break;
                default:
                    throw new ApplicationException();
            }

            decoder.PointProcessor = PointProcessor;
            decoder.FaceProcessor = FaceProcessor;

            return decoder;
        }

        private PointCloud ReadPointCloud(
                string file,
                IDecoder decoder,
                Header header) {

            List<Point> points = decoder.ReadPoints(
                file, 
                header);

            return new PointCloud(
                points,
                PointIndex.CopyEmpty());
        }

        private Mesh ReadMesh(
                string file,
                IDecoder decoder,
                Header header) {

            decoder.ReadMesh(
                file,
                InvertNormals,
                header,
                out List<Point> vertices,
                out List<Face> faces);

            return new Mesh(
                vertices,
                faces,
                MeshIndex.CopyEmpty());
        }
    }
}