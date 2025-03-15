using HuePat.Util.Math.Geometry;
using System;
using System.Collections.Generic;
using System.IO;

namespace HuePat.Util.IO.PLY.Reading {
    class AsciiDecoder : IDecoder {
        public Action<Point> PointProcessor { get; set; }
        public Action<Face> FaceProcessor { get; set; }

        public List<Point> ReadPoints(
                string file, 
                Header header) {

            List<Point> points = new List<Point>();

            PointParser pointParser = new PointParser(
                header,
                PointProcessor);

            ReadLines(file,
                header.HeaderLineCount,
                header.HeaderLineCount + header.VertexSection.Count,
                line => {
                    points.Add(
                        pointParser.ParsePoint(line));
                });

            return points;
        }

        public void ReadMesh(
                string file, 
                bool switchNormals,
                Header header,
                out List<Point> vertices,
                out List<Face> faces) {

            vertices = ReadPoints(
                file, 
                header);

            FaceParser faceParser = new FaceParser(
                switchNormals,
                header,
                vertices,
                FaceProcessor);

            List<Face> _faces = new List<Face>();

            ReadLines(file,
                header.HeaderLineCount + header.VertexSection.Count,
                header.HeaderLineCount + header.VertexSection.Count + header.FaceSection.Count,
                line => {
                    _faces.Add(
                        faceParser.ParseFace(line));
                });

            faces = _faces;
        }

        private static void ReadLines(
                string file,
                int startIndex,
                int stopIndex,
                Action<string> callback) {

            int index = 0;

            foreach (string line in File.ReadLines(file)) {

                if (index >= stopIndex) {
                    return;
                }

                if (index >= startIndex) {
                    callback(line);
                }

                index++;
            }
        }
    }
}