using System;
using System.Collections.Generic;
using System.Linq;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry {
    public class AARectangle : IFiniteGeometry {
        private Vector3d[] vertices;

        public bool IsNormalDirectionPositive { get; private set; }
        public Dimension PerpendicularDimension { get; private set; }
        public Vector2d Min2D { get; private set; }
        public Vector2d Max2D { get; private set; }
        public Vector3d Min { get; private set; }
        public Vector3d Max { get; private set; }
        public Dictionary<string, IProperty> Properties { get; set; }

        public double PerpendicularDimensionCoordinate {
            get {
                return Min.Get(PerpendicularDimension);
            }
        }

        public double Area {
            get {
                return (Max2D.X - Min2D.X)
                    * (Max2D.Y - Min2D.Y);
            }
        }

        public AABox BBox {
            get {
                return new AABox(Min, Max);
            }
        }

        public Mesh Mesh {
            get {

                Point[] vertices = this.vertices
                    .Select(vertex => new Point(vertex))
                    .ToArray();

                return new Mesh(
                    new PointCloud(),
                    IsNormalDirectionPositive ?
                        new Face[] {
                            new Face(0, 1, 3, vertices),
                            new Face(1, 2, 3, vertices)
                        } :
                        new Face[] {
                            new Face(1, 3, 0, vertices),
                            new Face(1, 2, 3, vertices)
                        }
                );
            }
        }

        public AARectangle(
                Dimension perpendicularDimension,
                bool isNormalDirectionPositive,
                double perpendicularDimensionCoordinate,
                Vector2d min,
                Vector2d max) {

            PerpendicularDimension = perpendicularDimension;
            IsNormalDirectionPositive = isNormalDirectionPositive;

            InitializeGeometry(
                perpendicularDimensionCoordinate, 
                min, 
                max);
        }

        public virtual AARectangle Clone() {

            AARectangle clone = new AARectangle(
                PerpendicularDimension,
                IsNormalDirectionPositive,
                PerpendicularDimensionCoordinate,
                Min2D, 
                Max2D);

            clone.Properties = Properties.Clone();

            return clone;
        }

        public void UpdateBBox() {
            // nothing to do
        }

        public bool ApproximateEquals(
                AARectangle rectangle) {

            return PerpendicularDimension == rectangle.PerpendicularDimension
                && IsNormalDirectionPositive == rectangle.IsNormalDirectionPositive
                && Min.ApproximateEquals(rectangle.Min) 
                && Max.ApproximateEquals(rectangle.Max);
        }

        public bool Touches(
                AARectangle rectangle) {

            if (PerpendicularDimension != rectangle.PerpendicularDimension) {
                throw new ArgumentException(
                    "Currently, only AARectangles with the same PerpendicularDimension can be checked for touch.");
            }

            return PerpendicularDimensionCoordinate.ApproximateEquals(rectangle.PerpendicularDimensionCoordinate)
                && Min2D.X <= rectangle.Max2D.X 
                && Max2D.X >= rectangle.Min2D.X
                && Min2D.Y <= rectangle.Max2D.Y 
                && Max2D.Y >= rectangle.Min2D.Y;
        }

        public bool Overlaps(
                AARectangle rectangle) {

            if (PerpendicularDimension != rectangle.PerpendicularDimension) {
                throw new ArgumentException(
                    "Currently, only AARectangles with the same PerpendicularDimension can be checked for overlap.");
            }

            return PerpendicularDimensionCoordinate.ApproximateEquals(rectangle.PerpendicularDimensionCoordinate)
                && Min2D.X < rectangle.Max2D.X 
                && Max2D.X > rectangle.Min2D.X
                && Min2D.Y < rectangle.Max2D.Y 
                && Max2D.Y > rectangle.Min2D.Y;
        }

        public bool Intersects(
                AABox box) {

            return BBox.Intersects(box);
        }

        public bool Overlaps(
                AABox box) {

            return BBox.Overlaps(box);
        }

        public double DistanceTo(
                Vector3d position) {

            return BBox.DistanceTo(position);
        }

        public List<Intersection> Intersect(
                Ray ray) {

            return BBox.Intersect(ray);
        }

        public IEnumerable<AARectangle> ClipOn(
                AABox box) {

            double perpendicularDimensionCoordinate;

            if (PerpendicularDimensionCoordinate < box.Min.Get(PerpendicularDimension)) {

                perpendicularDimensionCoordinate = box.Min.Get(PerpendicularDimension);
            }
            else if (PerpendicularDimensionCoordinate > box.Max.Get(PerpendicularDimension)) {

                perpendicularDimensionCoordinate = box.Max.Get(PerpendicularDimension);
            }
            else {
                perpendicularDimensionCoordinate = PerpendicularDimensionCoordinate;
            }

            return ClipOn(new AARectangle(
                PerpendicularDimension,
                IsNormalDirectionPositive,
                perpendicularDimensionCoordinate,
                box.Min.To2D(PerpendicularDimension),
                box.Max.To2D(PerpendicularDimension)));
        }

        public IEnumerable<AARectangle> ClipOn(
                AARectangle rectangle) {

            if (PerpendicularDimension != rectangle.PerpendicularDimension) {
                throw new ArgumentException(
                    "Currently, only AARectangles with the same PerpendicularDimension can be clipped.");
            }

            if (!PerpendicularDimensionCoordinate.ApproximateEquals(
                        rectangle.PerpendicularDimensionCoordinate)
                    || Min2D.X >= rectangle.Max2D.X 
                    || Max2D.X <= rectangle.Min2D.X
                    || Min2D.Y >= rectangle.Max2D.Y 
                    || Max2D.Y <= rectangle.Min2D.Y) {

                return new AARectangle[] { Clone() };
            }

            if (Min2D.X >= rectangle.Min2D.X 
                    && Max2D.X <= rectangle.Max2D.X
                    && Min2D.Y >= rectangle.Min2D.Y 
                    && Max2D.Y <= rectangle.Max2D.Y) {

                return new AARectangle[0];
            }

            List<AARectangle> clippingRest = new List<AARectangle>();

            if (Max2D.X > rectangle.Max2D.X) {

                clippingRest.Add(GetSubRectangle(
                    new Vector2d(
                        rectangle.Max2D.X, 
                        Min2D.Y),
                    Max2D));
            }

            if (Min2D.X < rectangle.Min2D.X) {

                clippingRest.Add(GetSubRectangle(
                    Min2D,
                    new Vector2d(
                        rectangle.Min2D.X, 
                        Max2D.Y)));
            }

            if (Max2D.Y > rectangle.Max2D.Y) {

                clippingRest.Add(GetSubRectangle(
                    new Vector2d(
                        System.Math.Max(
                            Min2D.X, 
                            rectangle.Min2D.X), 
                        rectangle.Max2D.Y),
                    new Vector2d(
                        System.Math.Min(
                            Max2D.X, 
                            rectangle.Max2D.X), 
                        Max2D.Y)));
            }

            if (Min2D.Y < rectangle.Min2D.Y) {

                clippingRest.Add(GetSubRectangle(
                    new Vector2d(
                        System.Math.Max(
                            Min2D.X, 
                            rectangle.Min2D.X), 
                        Min2D.Y),
                    new Vector2d(
                        System.Math.Min(
                            Max2D.X, 
                            rectangle.Max2D.X), 
                        rectangle.Min2D.Y)));
            }

            return clippingRest;
        }

        private void InitializeGeometry(
                double perpendicularDimensionCoordinate,
                Vector2d min, 
                Vector2d max) {

            Min2D = min;
            Max2D = max;

            Min = min.To3D(
                PerpendicularDimension, 
                perpendicularDimensionCoordinate);
            Max = max.To3D(
                PerpendicularDimension, 
                perpendicularDimensionCoordinate);

            vertices = new Vector3d[] {
                GetLowerRightVertex(),
                Min,
                GetUpperLeftVertex(),
                Max
            };
        }

        private Vector3d GetLowerRightVertex() {

            if (PerpendicularDimension == Dimension.X) {

                return new Vector3d(Min.X, Min.Y, Max.Z);
            }
            else {
                return new Vector3d(Max.X, Min.Y, Min.Z);
            }
        }

        private Vector3d GetUpperLeftVertex() {

            if (PerpendicularDimension == Dimension.X) {

                return new Vector3d(Min.X, Max.Y, Min.Z);
            }
            return new Vector3d(Min.X, Max.Y, Max.Z);
        }

        private AARectangle GetSubRectangle(
                Vector2d min, 
                Vector2d max) {

            AARectangle subRectangle = Clone();

            subRectangle.InitializeGeometry(
                PerpendicularDimensionCoordinate, 
                min, 
                max);

            return subRectangle;
        }
    }
}