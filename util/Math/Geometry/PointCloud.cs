using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry.Processing.Rotating;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Math.Geometry.SpatialIndices;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry {
    public class PointCloud: 
            IReadOnlyList<Point>, 
            IFiniteGeometryCollection<Point>, 
            IShape {

        private readonly Point[] points;

        public static PointCloud CreateLinePoints(
                Vector3d from,
                Vector3d to,
                double resolution) {

            double length = from.DistanceTo(to);

            List<Vector3d> positions = new List<Vector3d>();

            for (double d = 0.0; d <= length; d += resolution) {
                positions.Add(from + d * (to - from).Normalized());
            }

            return new PointCloud(positions);
        }

        private ISpatialIndex<Point> spatialIndex;
        
        public Dictionary<string, IProperty> Properties { get; set; }

        public AABox BBox { get; protected set; }

        public Point this[int i] {
            get {
                return points[i];
            }
        }

        public int Count {
            get {
                return points.Length;
            }
        }

        public ShapeType Type {
            get {
                return ShapeType.POINT_CLOUD;
            }
        }

        public Mesh Mesh {
            get {
                return new Mesh(
                    this,
                    new Face[0]);
            }
        }

        public PointCloud(): 
            this(new List<Point>()) {
        }

        public PointCloud(
                IEnumerable<Vector3d> positions):
                    this(positions.Select(position => new Point(position))) {
        }

        public PointCloud(
                IEnumerable<Point> points): 
                    this(points, new BruteForceIndex<Point>()) {
        }

        public PointCloud(
                IEnumerable<Point> points, 
                ISpatialIndex<Point> spatialIndex) {

            this.points = points.ToArray();
            this.spatialIndex = spatialIndex;

            UpdateBBox();
        }

        public PointCloud Apply(
                ISpatialIndex<Point> spatialIndex) {

            this.spatialIndex = spatialIndex;
            this.spatialIndex.Load(points);

            return this;
        }

        public PointCloud SetColor(
                Color color) {

            foreach (Point point in this) {
                point.SetColor(color);
            }

            return this;
        }

        IShape IShape.Clone() {

            return Clone();
        }

        public PointCloud Clone() {

            return new PointCloud(
                    this.Select(point => point.Clone()),
                    spatialIndex.CopyEmpty()) {

                Properties = Properties.Clone()
            };
        }

        public IEnumerator<Point> GetEnumerator() {

            return points
                .AsEnumerable()
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {

            return GetEnumerator();
        }

        public virtual void UpdateBBox() {

            spatialIndex.Load(points);

            BBox = spatialIndex.BBox;
        }

        public IReadOnlyList<double> GetSizeWeights() {

            return Enumerable
                .Range(0, Count)
                .Select(i => 1.0)
                .ToList();
        }

        public IReadOnlyList<Vector3d> GetNormals() {

            return this
                .Select(point => point.GetNormalVector())
                .ToList();
        }

        public IReadOnlyList<Point> GetPoints() {

            return this;
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

        public Point GetNearest(
                Vector3d position, 
                Predicate<Point> filter,
                double? distanceThreshold) {

            return spatialIndex.GetNearest(
                position, 
                filter,
                distanceThreshold);
        }

        public List<Point> Intersect(
                AABox box) {

            return spatialIndex.Intersect(box);
        }

        public List<MultiGeometryIntersection<Point>> Intersect(
                Ray ray) {

            return Intersect(
                ray, 
                null);
        }

        public List<MultiGeometryIntersection<Point>> Intersect(
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