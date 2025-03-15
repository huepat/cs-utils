using HuePat.Util.Math.Geometry;
using System;

namespace HuePat.Util.IO.PLY.Reading {
    class PointParser: BaseParser {
        private Action<Point> processor;

        public PointParser(
                Header header,
                Action<Point> processor) : 
                    base(header) {

            this.processor = processor;
        } 

        public Point ParsePoint(
                string line) {

            string[] values = SplitValues(line);

            Point point = new Point(
                ParseVector3d(
                    values, 
                    header.VertexSection.CoordinateIndices));

            if (header.VertexSection.HasColor) {
                point.SetColor(
                    ParseColor(
                        values, 
                        header.VertexSection.ColorIndices));
            }

            if (header.VertexSection.HasNormalVector) {
                point.SetNormalVector(
                    ParseVector3d(
                        values, 
                        header.VertexSection.NormalVectorIndices));
            }

            ParseProperties(
                point, 
                values, 
                header.VertexSection);

            if (processor != null) {
                processor(point);
            }

            return point;
        }
    }
}