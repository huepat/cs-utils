using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry {
    public class AABox : IFiniteGeometry {
        public static AABox FromCenterAndSize(
                Vector3d center,
                Vector3d size) {

            Vector3d s = size / 2;

            return new AABox(
                center - s,
                center + s);
        }

        public static AABox FromContainedGeometries<T>(
                IList<T> geometries,
                bool useParallel = false) 
                    where T : IFiniteGeometry {

            return useParallel ?
                FromContainedGeometries_Parallel(geometries) :
                FromContainedGeometries_Sequential(geometries);
        }

        private static AABox FromContainedGeometries_Sequential<T>(
                IList<T> geometries) 
                    where T : IFiniteGeometry {

            geometries
                .SelectMany(geometry => {

                    AABox bbox = geometry.BBox;

                    return new Vector3d[] { 
                        bbox.Min, 
                        bbox.Max 
                    };

                })
                .GetMinMax(
                    out Vector3d min, 
                    out Vector3d max);

            return new AABox(min, max);
        }

        private static AABox FromContainedGeometries_Parallel<T>(
                IList<T> geometries) 
                    where T : IFiniteGeometry {

            object @lock = new object();

            (Vector3d, Vector3d) bounds = (
                new Vector3d(double.MaxValue),
                new Vector3d(double.MinValue));

            Parallel.ForEach(
                Partitioner.Create(0, geometries.Count),
                () => (
                    new Vector3d(double.MaxValue),
                    new Vector3d(double.MinValue)),
                (partition, loopState, localBounds) => {

                    AABox bBox;

                    for (int i = partition.Item1; i < partition.Item2; i++) {

                        bBox = geometries[i].BBox;

                        if (bBox.Min.X < bounds.Item1.X) {
                            bounds.Item1.X = bBox.Min.X;
                        }
                        if (bBox.Min.Y < bounds.Item1.Y) {
                            bounds.Item1.Y = bBox.Min.Y;
                        }
                        if (bBox.Min.Z < bounds.Item1.Z) {
                            bounds.Item1.Z = bBox.Min.Z;
                        }
                        if (bBox.Max.X > bounds.Item2.X) {
                            bounds.Item2.X = bBox.Max.X;
                        }
                        if (bBox.Max.Y > bounds.Item2.Y) {
                            bounds.Item2.Y = bBox.Max.Y;
                        }
                        if (bBox.Max.Z > bounds.Item2.Z) {
                            bounds.Item2.Z = bBox.Max.Z;
                        }
                    }

                    return localBounds;
                },
                localBounds => {

                    lock (@lock) {
                        if (localBounds.Item1.X < bounds.Item1.X) {
                            bounds.Item1.X = localBounds.Item1.X;
                        }
                        if (localBounds.Item1.Y < bounds.Item1.Y) {
                            bounds.Item1.Y = localBounds.Item1.Y;
                        }
                        if (localBounds.Item1.Z < bounds.Item1.Z) {
                            bounds.Item1.Z = localBounds.Item1.Z;
                        }
                        if (localBounds.Item2.X > bounds.Item2.X) {
                            bounds.Item2.X = localBounds.Item2.X;
                        }
                        if (localBounds.Item2.Y > bounds.Item2.Y) {
                            bounds.Item2.Y = localBounds.Item2.Y;
                        }
                        if (localBounds.Item2.Z > bounds.Item2.Z) {
                            bounds.Item2.Z = localBounds.Item2.Z;
                        }
                    }
                });

            return new AABox(
                bounds.Item1,
                bounds.Item2);
        }

        public Dictionary<string, IProperty> Properties { get; set; }
        public Vector3d Min { get; protected set; }
        public Vector3d Max { get; protected set; }

        public double Volumn {
            get {
                return (Max.X - Min.X)
                    * (Max.Y - Min.Y)
                    * (Max.Z - Min.Z);
            }
        }

        public Vector3d Center {
            get {
                return (Max + Min) / 2;
            }
        }

        public Vector3d Size {
            get {
                return Max - Min;
            }
        }

        public AABox BBox {
            get {
                return this;
            }
        }

        public virtual Mesh Mesh {
            get {

                Point[] vertices = new Point[] {
                    new Point(Min),
                    new Point(new Vector3d(Min.X, Max.Y, Min.Z)),
                    new Point(new Vector3d(Min.X, Max.Y, Max.Z)),
                    new Point(new Vector3d(Min.X, Min.Y, Max.Z)),
                    new Point(new Vector3d(Max.X, Min.Y, Min.Z)),
                    new Point(new Vector3d(Max.X, Max.Y, Min.Z)),
                    new Point(Max),
                    new Point(new Vector3d(Max.X, Min.Y, Max.Z))
                };

                Mesh mesh = new Mesh(
                    vertices,
                    new Face[] {
                        new Face(0, 3, 1, vertices),
                        new Face(1, 3, 2, vertices),
                        new Face(5, 2, 6, vertices),
                        new Face(1, 2, 5, vertices),
                        new Face(2, 3, 6, vertices),
                        new Face(3, 7, 6, vertices),
                        new Face(4, 6, 7, vertices),
                        new Face(4, 5, 6, vertices),
                        new Face(0, 7, 3, vertices),
                        new Face(0, 4, 7, vertices),
                        new Face(0, 1, 4, vertices),
                        new Face(1, 5, 4, vertices)
                    }
                );

                mesh.Properties = Properties;

                return mesh;
            }
        }

        public AABox(
                Vector3d min, 
                Vector3d max) {

            Min = min;
            Max = max;
        }

        public AABox(
                IEnumerable<Vector3d> positions) {

            positions.GetMinMax(
                out Vector3d min, 
                out Vector3d max);

            Min = min;
            Max = max;
        }

        protected AABox(
            AABox box): 
                this(box.Min, box.Max) {
        }

        public AABox Clone() {

            AABox clone = new AABox(Min, Max);

            clone.Properties = Properties.Clone();

            return clone;
        }

        public void UpdateBBox() {
            // nothing to to
        }

        public bool ApproximateEquals(
                AABox box) {

            return Min.ApproximateEquals(box.Min) 
                && Max.ApproximateEquals(box.Max);
        }

        public bool Contains(
                Vector3d position) {

            return position.X >= Min.X && position.X <= Max.X 
                && position.Y >= Min.Y && position.Y <= Max.Y 
                && position.Z >= Min.Z && position.Z <= Max.Z;
        }

        public double DistanceTo(
                Vector3d position) {

            if (Contains(position)) {
                return 0.0;
            }

            return (
                Vector3d.Clamp(
                    position, 
                    Min, 
                    Max) - position
            ).Length;
        }

        public bool Intersects(
                AABox box) {

            return Min.X <= box.Max.X && Max.X >= box.Min.X 
                && Min.Y <= box.Max.Y && Max.Y >= box.Min.Y 
                && Min.Z <= box.Max.Z && Max.Z >= box.Min.Z;
        }

        public bool Overlaps(
                AABox box) {

            return Min.X < box.Max.X && Max.X > box.Min.X 
                && Min.Y < box.Max.Y && Max.Y > box.Min.Y 
                && Min.Z < box.Max.Z && Max.Z > box.Min.Z;
        }

        public bool Intersects(
                Ray ray) {

            return Intersect(
                ray, 
                out _, 
                out _);
        }

        public List<Intersection> Intersect(
                Ray ray) {

            List<Intersection> result = new List<Intersection>();

            if (!Intersect(
                    ray, 
                    out double minDistance, 
                    out double maxDistance)) {

                return result;
            }

            if (minDistance > 0.0) {

                result.Add(
                    new Intersection(
                        minDistance, 
                        ray.At(minDistance)));
            }

            result.Add(
                new Intersection(
                    maxDistance, 
                    ray.At(maxDistance)));

            return result;
        }

        public IEnumerable<AABox> ClipOn(
                AABox box) {

            if (Min.X >= box.Max.X || Max.X <= box.Min.X
                    || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y
                    || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z) {

                return new AABox[] { Clone() };
            }

            if (Min.X >= box.Min.X && Max.X <= box.Max.X
                    && Min.Y >= box.Min.Y && Max.Y <= box.Max.Y
                    && Min.Z >= box.Min.Z && Max.Z <= box.Max.Z) {

                return new AABox[0];
            }

            List<AABox> clippingRest = new List<AABox>();

            if (Max.X > box.Max.X) {

                clippingRest.Add(new AABox(
                    new Vector3d(box.Max.X, Min.Y, Min.Z),
                    Max));
            }

            if (Min.X < box.Min.X) {

                clippingRest.Add(new AABox(
                    Min,
                    new Vector3d(box.Min.X, Max.Y, Max.Z)));
            }

            if (Max.Y > box.Max.Y) {

                clippingRest.Add(new AABox(
                    new Vector3d(
                        System.Math.Max(Min.X, box.Min.X), 
                        box.Max.Y,
                        Min.Z),
                    new Vector3d(
                        System.Math.Min(Max.X, box.Max.X), 
                        Max.Y,
                        Max.Z)));
            }

            if (Min.Y < box.Min.Y) {

                clippingRest.Add(new AABox(
                    new Vector3d(
                        System.Math.Max(Min.X, box.Min.X), 
                        Min.Y,
                        Min.Z),
                    new Vector3d(
                        System.Math.Min(Max.X, box.Max.X), 
                        box.Min.Y,
                        Max.Z)));
            }

            if (Max.Z > box.Max.Z) {

                clippingRest.Add(new AABox(
                    new Vector3d(
                        System.Math.Max(Min.X, box.Min.X),
                        System.Math.Max(Min.Y, box.Min.Y),
                        box.Max.Z),
                    new Vector3d(
                        System.Math.Min(Max.X, box.Max.X),
                        System.Math.Min(Max.Y, box.Max.Y),
                        Max.Z)));
            }

            if (Min.Z < box.Min.Z) {

                clippingRest.Add(new AABox(
                    new Vector3d(
                        System.Math.Max(Min.X, box.Min.X),
                        System.Math.Max(Min.Y, box.Min.Y),
                        Min.Z),
                    new Vector3d(
                        System.Math.Min(Max.X, box.Max.X),
                        System.Math.Min(Max.Y, box.Max.Y),
                        box.Min.Z)));
            }

            return clippingRest;
        }

        private bool Intersect(
                Ray ray, 
                out double minDistance, 
                out double maxDistance) {

            double tMinX = (Min.X - ray.Origin.X) * ray.InverseDirection.X;
            double tMaxX = (Max.X - ray.Origin.X) * ray.InverseDirection.X;
            double tMinY = (Min.Y - ray.Origin.Y) * ray.InverseDirection.Y;
            double tMaxY = (Max.Y - ray.Origin.Y) * ray.InverseDirection.Y;
            double tMinZ = (Min.Z - ray.Origin.Z) * ray.InverseDirection.Z;
            double tMaxZ = (Max.Z - ray.Origin.Z) * ray.InverseDirection.Z;

            minDistance = System.Math.Max(
                System.Math.Max(
                    System.Math.Min(tMinX, tMaxX), 
                    System.Math.Min(tMinY, tMaxY)), 
                System.Math.Min(tMinZ, tMaxZ));

            maxDistance = System.Math.Min(
                System.Math.Min(
                    System.Math.Max(tMinX, tMaxX), 
                    System.Math.Max(tMinY, tMaxY)), 
                System.Math.Max(tMinZ, tMaxZ));

            return maxDistance >= 0.0 
                && maxDistance >= minDistance;
        }
    }
}