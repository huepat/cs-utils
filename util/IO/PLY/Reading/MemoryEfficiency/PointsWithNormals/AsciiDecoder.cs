using HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals;
using System;
using System.Collections.Generic;
using System.IO;

namespace HuePat.Util.IO.PLY.Reading.MemoryEfficiency.PointsWithNormals {
    class AsciiDecoder : IDecoder {

        public List<Point> ReadPoints(
                string file, 
                Header header) {

            List<Point> points = new List<Point>();

            PointParser pointParser = new PointParser(header);

            ReadLines(file,
                header.HeaderLineCount,
                header.HeaderLineCount + header.VertexSection.Count,
                line => {
                    points.Add(
                        pointParser.ParsePoint(line));
                });

            return points;
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