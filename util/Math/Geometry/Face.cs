using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object.Properties;
using OpenCvSharp;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry {
    public class Face: IFiniteGeometry {
        public int VertexIndex1 { get; private set; }
        public int VertexIndex2 { get; private set; }
        public int VertexIndex3 { get; private set; }
        public Point Vertex1 { get; private set; }
        public Point Vertex2 { get; private set; }
        public Point Vertex3 { get; private set; }
        public Dictionary<string, IProperty> Properties { get; set; }

        public Triangle Geometry {
            get {
                return new Triangle(
                    Vertex1.Position,
                    Vertex2.Position,
                    Vertex3.Position);
            }
        }

        public Mesh Mesh {
            get {
                return Geometry.Mesh;
            }
        }

        public Mesh MeshWithProperties {
            get {

                Point[] vertices = new Point[] {
                    Vertex1,
                    Vertex2,
                    Vertex3
                };

                return new Mesh(
                    vertices,
                    new Face[] {
                        new Face(
                                0, 1, 2,
                                vertices) {

                            Properties = Properties
                        } 
                    });
            }
        }

        public AABox BBox { 
            get {
                return Geometry.BBox;
            }
        }

        public IEnumerable<Point> Vertices {
            get {
                yield return Vertex1;
                yield return Vertex2;
                yield return Vertex3;
            }
        }

        private Face() { }

        public Face(
                int vertexIndex1,
                int vertexIndex2,
                int vertexIndex3,
                IReadOnlyList<Point> vertices) {

            VertexIndex1 = vertexIndex1;
            VertexIndex2 = vertexIndex2;
            VertexIndex3 = vertexIndex3;

            Vertex1 = vertices[VertexIndex1];
            Vertex2 = vertices[VertexIndex2];
            Vertex3 = vertices[VertexIndex3];
        }

        public Face Clone() {

            return new Face(
                    0,
                    1,
                    2,
                    new Point[] { 
                        Vertex1.Clone(),
                        Vertex2.Clone(),
                        Vertex3.Clone()
                    }) {

                Properties = Properties.Clone()
            };
        }

        public Face Clone(
                IReadOnlyList<Point> clonedVertices) {

            return new Face(
                    VertexIndex1,
                    VertexIndex2,
                    VertexIndex3,
                    clonedVertices) {

                Properties = Properties.Clone()
            };
        }

        public void UpdateBBox() {

            Geometry.UpdateBBox();
        }

        public double DistanceTo(
                Vector3d position) {

            return Geometry.DistanceTo(position);
        }

        public bool Intersects(
                AABox box) {

            return Geometry.Intersects(box);
        }

        public List<Intersection> Intersect(
                Ray ray) {

            return Geometry.Intersect(ray);
        }
    }
}