using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry.Processing.Rotating;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Math.Geometry.SpatialIndices;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.Math.Geometry {
    public class Mesh: 
            IReadOnlyList<Face>, 
            IFiniteGeometryCollection<Face>, 
            IShape {

        private class VertexCloud : PointCloud {
            public VertexCloud(
                    IEnumerable<Point> vertices) :
                        base(vertices) {
            }

            public void SetBBox(
                    AABox bBox) {

                BBox = bBox;
            }

            public override void UpdateBBox() {
                // nothing to do, Mesh controls the BBox via its own SpatialIndex
            }
        }

        public static Mesh From(
                IEnumerable<Mesh> meshes) {

            return new MeshCreator()
                .Add(meshes)
                .Create();
        }

        public static Mesh From(
                IEnumerable<IFiniteGeometry> geometries) {

            return new MeshCreator()
                .Add(
                    geometries.Select(geometry => geometry.Mesh))
                .Create();
        }

        private ISpatialIndex<Face> spatialIndex;
        private readonly VertexCloud vertices;
        private readonly Face[] faces;

        public AABox BBox { get; private set; }
        public Dictionary<string, IProperty> Properties { get; set; }

        public int Count {
            get {
                return faces.Length;
            }
        }

        public PointCloud Vertices {
            get {

                vertices.SetBBox(BBox);

                return vertices;
            }
        }

        Mesh IFiniteGeometry.Mesh { 
            get {
                return this;
            }
        }

        public ShapeType Type {
            get {
                return ShapeType.MESH;
            }
        }

        public Face this[int i] {
            get {
                return faces[i];
            }
        }

        public Mesh() :
            this(
                new Point[0], 
                new Face[0]) {
        }

        public Mesh(
                IReadOnlyList<Point> vertices,
                IEnumerable<Face> faces) :
                    this(
                        vertices, 
                        faces, 
                        new BruteForceIndex<Face>()) {
        }

        public Mesh(
                IReadOnlyList<Point> vertices,
                IEnumerable<Face> faces,
                ISpatialIndex<Face> spatialIndex) {

            this.faces = faces.ToArray();
            this.vertices = new VertexCloud(vertices);
            this.spatialIndex = spatialIndex;

            UpdateBBox();
        }

        public void UpdateBBox() {

            spatialIndex.Load(this);

            BBox = spatialIndex.BBox;
        }

        public Mesh Apply(
                ISpatialIndex<Face> spatialIndex) {

            this.spatialIndex = spatialIndex;
            this.spatialIndex.Load(faces);

            return this;
        }

        public Mesh SetColor(
                Color color) {

            vertices.SetColor(color);

            return this;
        }

        public IEnumerator<Face> GetEnumerator() {

            return faces
                .AsEnumerable()
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {

            return GetEnumerator();
        }

        IShape IShape.Clone() {

            return Clone();
        }

        public Mesh Clone() {

            Point[] clonedVertices = vertices
                .Select(vertex => vertex.Clone())
                .ToArray();

            return new Mesh(
                    clonedVertices,
                    faces.Select(face => face.Clone(clonedVertices)),
                    spatialIndex.CopyEmpty()) {

                Properties = Properties.Clone()
            };
        }

        public IReadOnlyList<double> GetSizeWeights() {

            return this
                .Select(face => face.Geometry.Area)
                .ToList();
        }

        public IReadOnlyList<Vector3d> GetNormals() {

            return this
                .Select(face => face.Geometry.Normal)
                .ToList();
        }

        public IReadOnlyList<Point> GetPoints() {

            return vertices;
        }

        public IReadOnlyList<IFiniteGeometry> GetGeometries() {

            return this;
        }

        public bool Intersects(
                AABox box) {

            return spatialIndex.Intersects(box);
        }

        public double DistanceTo(
                Vector3d position) {

            return spatialIndex.DistanceTo(position);
        }

        public Face GetNearest(
                Vector3d position, 
                Predicate<Face> filter,
                double? distanceThreshold) {

            return spatialIndex.GetNearest(
                position, 
                filter, 
                distanceThreshold);
        }

        public List<Face> Intersect(
                AABox box) {

            return spatialIndex.Intersect(box);
        }

        public List<MultiGeometryIntersection<Face>> Intersect(
                Ray ray) {

            return Intersect(
                ray, 
                null);
        }

        public List<MultiGeometryIntersection<Face>> Intersect(
                Ray ray,
                double? distanceTheshold) {

            return spatialIndex.Intersect(
                ray, 
                distanceTheshold);
        }

        List<Intersection> IGeometry.Intersect(
                Ray ray) {

            return spatialIndex
                .Intersect(ray)
                .Select(intersection => intersection as Intersection)
                .ToList();
        }
    }
}