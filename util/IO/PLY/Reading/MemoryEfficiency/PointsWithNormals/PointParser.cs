using HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals;
using System;

namespace HuePat.Util.IO.PLY.Reading.MemoryEfficiency.PointsWithNormals {
    class PointParser: BaseParser {
        private Action<Point> processor;

        public PointParser(
                Header header) : 
                    base(header) {
        } 

        public Point ParsePoint(
                string line) {

            string[] values = SplitValues(line);

            return new Point(
                ParseVector3d(
                    values, 
                    header.VertexSection.CoordinateIndices),
                ParseVector3d(
                    values,
                    header.VertexSection.NormalVectorIndices));
        }
    }
}