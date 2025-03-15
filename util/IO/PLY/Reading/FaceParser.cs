using System;
using System.Collections.Generic;
using HuePat.Util.Math.Geometry;

namespace HuePat.Util.IO.PLY.Reading {
    class FaceParser : BaseParser {
        private bool switchNormals;
        private List<Point> vertices;
        private Action<Face> processor;

        public FaceParser(
                bool switchNormals,
                Header header,
                List<Point> vertices,
                Action<Face> processor) : 
                    base(header) {

            this.switchNormals = switchNormals;
            this.vertices = vertices;
            this.processor = processor;
        }

        public Face ParseFace(string line) {

            string[] values = SplitValues(line);

            if (int.Parse(values[0]) != 3) {
                throw new ArgumentException(
                    "FaceParser currently only parses faces with three indices.");
            }

            Face face = new Face(
                int.Parse(values[1]),
                int.Parse(switchNormals ? values[3] : values[2]),
                int.Parse(switchNormals ? values[2] : values[3]),
                vertices
            );

            if (header.VertexSection.HasNormalVector) {
                face.SetNormalVector(
                    ParseVector3d(
                        values, 
                        header.VertexSection.NormalVectorIndices));
            }

            ParseProperties(
                face, 
                values, 
                header.FaceSection);

            if (processor != null) {
                processor(face);
            }

            return face;
        }
    }
}