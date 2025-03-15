using HuePat.Util.Math.Geometry.SpatialIndices;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.Math.Geometry {
    public class MeshCreator {
        private readonly List<Mesh> meshes = new List<Mesh>();

        public MeshCreator Add(
                Mesh mesh) {

            meshes.Add(mesh);

            return this;
        }

        public MeshCreator Add(
                IEnumerable<Mesh> meshes) {

            this.meshes.AddRange(meshes);

            return this;
        }

        public Mesh Create() {

            return Create(new BruteForceIndex<Face>());
        }

        public Mesh Create(
                ISpatialIndex<Face> spatialIndex) {

            int offset = 0;

            List<Point> vertices = meshes
                .SelectMany(mesh => mesh.Vertices)
                .ToList();

            List<Face> faces = new List<Face>();

            foreach (Mesh mesh in meshes) {

                faces.AddRange(
                    mesh.Select(face => {

                        Face copy = new Face(
                            face.VertexIndex1 + offset,
                            face.VertexIndex2 + offset,
                            face.VertexIndex3 + offset,
                            vertices);

                        copy.Properties = face.Properties;

                        return copy;
                    }));

                offset += mesh.Vertices.Count;
            }

            return new Mesh(
                vertices,
                faces,
                spatialIndex);
        }
    }
}