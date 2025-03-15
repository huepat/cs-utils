using System.Collections.Generic;
using HuePat.Util.Math.Geometry;
using OpenTK.Mathematics;

namespace HuePat.Util.IO.PLY.Writing {
    public class PlyWriter : PlyWriterBase {
        public void Write(
                string file,
                IEnumerable<Vector3d> coordinates) {

            Write(
                file,
                new PointCloud(coordinates));
        }

        public void Write(
                string file,
                IEnumerable<Point> points) {

            Write(
                file,
                new PointCloud(points));
        }

        public void Write(
                string file,
                PointCloud pointCloud) {

            WriteHeader(
                file, 
                pointCloud.Count, 
                0);

            using (IEncoder encoder = GetEncoder(file)) {

                for (int i = 0; i < pointCloud.Count; i++) {

                    encoder.Encode(pointCloud[i]);
                }
            }
        }

        public void Write(
                string file,
                IEnumerable<IFiniteGeometry> geometry) {

            Write(
                file,
                Mesh.From(geometry));
        }

        public void Write(
                string file,
                Mesh mesh) {

            WriteHeader(
                file,
                mesh.Vertices.Count,
                mesh.Count);

            using (IEncoder encoder = GetEncoder(file)) {

                for (int i = 0; i < mesh.Vertices.Count; i++) {
                    encoder.Encode(mesh.Vertices[i]);
                }

                for (int i = 0; i < mesh.Count; i++) {
                    encoder.Encode(mesh[i], 0);
                }
            }
        }
    }
}