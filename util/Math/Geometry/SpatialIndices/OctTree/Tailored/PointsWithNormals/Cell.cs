using OpenTK.Mathematics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SmallPoint = HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals.Point;

namespace HuePat.Util.Math.Geometry.SpatialIndices.OctTree.Tailored.PointsWithNormals {
    class Cell {
        private class Config {
            public bool UseParallel { get; private set; }
            public int MaxOccupation { get; private set; }
            public int ClumpingDepthThreshold { get; private set; }
            public int MinGeometryCountForParallel { get; private set; }

            public Config(
                    bool useParallel,
                    int maxOccupation,
                    int clumpingDepthThreshold,
                    int minGeometryCountForParallel) {

                UseParallel = useParallel;
                MaxOccupation = maxOccupation;
                ClumpingDepthThreshold = clumpingDepthThreshold;
                MinGeometryCountForParallel = minGeometryCountForParallel;
            }
        }

        public Vector3d Min;
        public Vector3d Max;

        private int clumpingDepth;
        private int geometryCount;
        private Config config;
        private List<SmallPoint> points;

        public bool IsLeaf { get; private set; }
        public Cell Parent { get; private set; }
        public Cell[] Children { get; private set; }

        public bool ContainsGeometries {
            get {
                return IsLeaf && points.Count > 0;
            }
        }

        public List<SmallPoint> Points {
            get {

                if (IsLeaf) {
                    return points;
                }

                return Children
                    .SelectMany(childCell => childCell.Points)
                    .ToList();
            }
        }

        public Cell(
                bool useParallel,
                int maxOccupation,
                int clumpingDepthThreshold,
                int minGeometryCountForParallel,
                List<SmallPoint> points) {

            FromContainedPoints(
                points,
                useParallel);

            clumpingDepth = 0;
            geometryCount = points.Count;

            config = new Config(
                useParallel,
                maxOccupation,
                clumpingDepthThreshold,
                minGeometryCountForParallel);

            ProcessPoints(points);
        }

        private Cell(
                Vector3d min,
                Vector3d max,
                Cell parent,
                List<SmallPoint> points) {

            Min = min;
            Max = max;

            geometryCount = points.Count;
            clumpingDepth = parent.clumpingDepth;
            config = parent.config;
            Parent = parent;

            if (geometryCount == Parent.geometryCount) {
                clumpingDepth++;
            }
            else {
                clumpingDepth = 0;
            }

            if (clumpingDepth < config.ClumpingDepthThreshold) {
                ProcessPoints(points);
            }
            else {

                IsLeaf = true;
                this.points = points;
            }
        }

        public void FromContainedPoints(
                List<SmallPoint> points,
                bool useParallel = false) {

            if (useParallel) {
                FromContainedPoints_Parallel(points);
            }
            else {
                FromContainedPoints_Sequential(points);
            }
        }

        public bool ApproximateEquals(
                Cell cell) {

            return Min.ApproximateEquals(cell.Min)
                && Max.ApproximateEquals(cell.Max);
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

        public bool Contains(
                Vector3d position) {

            return position.X >= Min.X && position.X <= Max.X
                && position.Y >= Min.Y && position.Y <= Max.Y
                && position.Z >= Min.Z && position.Z <= Max.Z;
        }

        public bool Intersects(
                AABox box) {

            return Min.X <= box.Max.X && Max.X >= box.Min.X
                && Min.Y <= box.Max.Y && Max.Y >= box.Min.Y
                && Min.Z <= box.Max.Z && Max.Z >= box.Min.Z;
        }

        private void FromContainedPoints_Sequential(
                List<SmallPoint> points) {

            points
                .Select(point => point.Position)
                .GetMinMax(
                    out Min,
                    out Max);
        }

        private void FromContainedPoints_Parallel(
                List<SmallPoint> points) {

            object @lock = new object();

            (Vector3d, Vector3d) bounds = (
                new Vector3d(double.MaxValue),
                new Vector3d(double.MinValue));

            Parallel.ForEach(
                Partitioner.Create(0, points.Count),
                () => (
                    new Vector3d(double.MaxValue),
                    new Vector3d(double.MinValue)),
                (partition, loopState, localBounds) => {

                    for (int i = partition.Item1; i < partition.Item2; i++) {

                        if (points[i].Position.X < bounds.Item1.X) {
                            bounds.Item1.X = points[i].Position.X;
                        }
                        if (points[i].Position.Y < bounds.Item1.Y) {
                            bounds.Item1.Y = points[i].Position.Y;
                        }
                        if (points[i].Position.Z < bounds.Item1.Z) {
                            bounds.Item1.Z = points[i].Position.Z;
                        }
                        if (points[i].Position.X > bounds.Item2.X) {
                            bounds.Item2.X = points[i].Position.X;
                        }
                        if (points[i].Position.Y > bounds.Item2.Y) {
                            bounds.Item2.Y = points[i].Position.Y;
                        }
                        if (points[i].Position.Z > bounds.Item2.Z) {
                            bounds.Item2.Z = points[i].Position.Z;
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

            Min = bounds.Item1;
            Max = bounds.Item2;
        }

        private void ProcessPoints(
                List<SmallPoint> points) {

            if (points.Count > config.MaxOccupation) {

                IsLeaf = false;
                Split(points);
            }
            else {
                IsLeaf = true;
                this.points = points;
            }
        }

        private void Split(
                List<SmallPoint> points) {

            Vector3d s = (Max - Min) / 2;

            (Vector3d, Vector3d)[] childExtents = new (Vector3d, Vector3d)[] {
                    (
                        Min, 
                        Min + s
                    ),
                    (
                        new Vector3d(Min.X + s.X, Min.Y, Min.Z), 
                        new Vector3d(Max.X, Min.Y + s.Y, Min.Z + s.Z)
                    ),
                    (
                        new Vector3d(Min.X + s.X, Min.Y + s.Y, Min.Z),
                        new Vector3d(Max.X, Max.Y, Min.Z + s.Z)
                    ),
                    (
                        new Vector3d(Min.X, Min.Y + s.Y, Min.Z), 
                        new Vector3d(Min.X + s.X, Max.Y, Min.Z + s.Z)
                    ),
                    (
                        new Vector3d(Min.X, Min.Y, Min.Z + s.Z), 
                        new Vector3d(Min.X + s.X, Min.Y + s.Y, Max.Z)
                    ),
                    (
                        new Vector3d(Min.X + s.X, Min.Y, Min.Z + s.Z), 
                        new Vector3d(Max.X, Min.Y + s.Y, Max.Z)
                    ),
                    (
                        Min + s, 
                        Max
                    ),
                    (
                        new Vector3d(Min.X, Min.Y + s.Y, Min.Z + s.Z), 
                        new Vector3d(Min.X + s.X, Max.Y, Max.Z)
                    )
                };

            List<SmallPoint>[] pointBuckets = config.UseParallel 
                        && points.Count >= config.MinGeometryCountForParallel ?
                    Split_Parallel(points, childExtents) :
                    Split_Sequential(points, childExtents);

            Children = config.UseParallel ?
                SpawnChildren_Parallel(pointBuckets, childExtents) :
                SpawnChildren_Sequential(pointBuckets, childExtents);
        }

        private List<SmallPoint>[] Split_Sequential(
                List<SmallPoint> points,
                (Vector3d, Vector3d)[] childExtents) {

            List<SmallPoint>[] pointBuckets = InitializePointBuckets();

            foreach (SmallPoint point in points) {

                for (int i = 0; i < 8; i++) {

                    if (Intersects(
                            childExtents[i].Item1,
                            childExtents[i].Item2,
                            point)) {

                        pointBuckets[i].Add(point);
                    }
                }
            }

            return pointBuckets;
        }

        private List<SmallPoint>[] Split_Parallel(
                List<SmallPoint> points,
                (Vector3d, Vector3d)[] childExtents) {

            object @lock = new object();
            List<SmallPoint>[] pointBuckets = InitializePointBuckets();

            Parallel.ForEach(
                Partitioner.Create(0, points.Count),
                InitializePointBuckets,
                (partition, loopState, partitionBuckets) => {

                    for (int i = partition.Item1; i < partition.Item2; i++) {

                        for (int j = 0; j < 8; j++) {

                            if (Intersects(
                                    childExtents[j].Item1,
                                    childExtents[j].Item2,
                                    points[i])) {

                                partitionBuckets[j].Add(points[i]);
                            }
                        }
                    }

                    return partitionBuckets;
                },
                partitionBuckets => {

                    for (int i = 0; i < 8; i++) {

                        lock (@lock) {
                            pointBuckets[i].AddRange(partitionBuckets[i]);
                        }
                    }
                });

            return pointBuckets;
        }

        private List<SmallPoint>[] InitializePointBuckets() {

            return Enumerable
                .Range(0, 8)
                .Select(index => new List<SmallPoint>())
                .ToArray();
        }

        private bool Intersects(
                Vector3d min,
                Vector3d max,
                SmallPoint point) {

            return point.Position.X <= max.X
                && point.Position.X >= min.X
                && point.Position.Y <= max.Y
                && point.Position.Y >= min.Y
                && point.Position.Z <= max.Z
                && point.Position.Z >= min.Z;
        }

        private Cell[] SpawnChildren_Sequential(
                List<SmallPoint>[] pointBuckets,
                (Vector3d, Vector3d)[] childExtents) {

            Cell[] children = new Cell[8];

            for (int i = 0; i < 8; i++) {

                children[i] = new Cell(
                    childExtents[i].Item1,
                    childExtents[i].Item2,
                    this,
                    pointBuckets[i]);
            }

            return children;
        }

        private Cell[] SpawnChildren_Parallel(
                List<SmallPoint>[] pointBuckets,
                (Vector3d, Vector3d)[] childExtents) {

            object @lock = new object();
            Cell[] children = new Cell[8];

            Parallel.For(
                0,
                8,
                i => {
                    Cell child = new Cell(
                        childExtents[i].Item1,
                        childExtents[i].Item2,
                        this,
                        pointBuckets[i]);

                    lock (@lock) {
                        children[i] = child;
                    }
                });

            return children;
        }
    }
}