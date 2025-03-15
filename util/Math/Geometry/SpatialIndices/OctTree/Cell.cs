//#define OCTTREE_DEBUG_MODE
//#define VERBOSE_CONSOLE_OUTPUT

using HuePat.Util.IO.PLY.Writing;
using OpenTK.Mathematics;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if OCTTREE_DEBUG_MODE
using System.Diagnostics;
#endif
using System.Linq;
using System.Threading.Tasks;

namespace HuePat.Util.Math.Geometry.SpatialIndices.OctTree {
    class Cell<T>: AABox where T: IFiniteGeometry {
        private class Config {
            public bool UseParallel { get; private set; }
            public bool UseBBoxIntersects { get; private set; }
            public int MaxOccupation { get; private set; }
            public int ClumpingDepthThreshold { get; private set; }
            public int MinOccupationForParallel { get; private set; }

            public Config(
                    bool useParallel,
                    bool useBBoxIntersects,
                    int maxOccupation,
                    int clumpingDepthThreshold,
                    int minOccupationForParallel) {

                UseParallel = useParallel;
                UseBBoxIntersects = useBBoxIntersects;
                MaxOccupation = maxOccupation;
                ClumpingDepthThreshold = clumpingDepthThreshold;
                MinOccupationForParallel = minOccupationForParallel;
            }
        }

#if OCTTREE_DEBUG_MODE
        private static bool hasClumping;
        private static int maxDepth;
        private static int maxClumpingGeometryCount;
        private static string maxDepthId;
        private static string maxClumpingGeometryCountId;

        private readonly object @lock = new object();
#endif
        private int clumpingDepth;
        private int geometryCount;
        private Config config;
        private List<T> geometries;

#if OCTTREE_DEBUG_MODE
        public string Id { get; private set; }
#endif
        public bool IsLeaf { get; private set; }
        public Cell<T> Parent { get; private set; }
        public Cell<T>[] Children { get; private set; }

        public bool ContainsGeometries {
            get {
                return IsLeaf && geometries.Count > 0;
            }
        }

        public List<T> Geometries {
            get {

                if (IsLeaf) {
                    return geometries;
                }

                return Children
                    .SelectMany(childCell => childCell.Geometries)
                    .ToList();
            }
        }

        public Cell(
                bool useParallel,
                bool useBBoxIntersects,
                int maxOccupation,
                int clumpingDepthThreshold,
                int minOccupationForParallel,
                List<T> geometries) : 
                    base(FromContainedGeometries(
                        geometries,
                        useParallel)) {

#if OCTTREE_DEBUG_MODE
            Id = "0";
            maxDepth = 0;
#endif

            clumpingDepth = 0;
            geometryCount = geometries.Count;

            config = new Config(
                useParallel,
                useBBoxIntersects,
                maxOccupation,
                clumpingDepthThreshold,
                minOccupationForParallel);

            ProcessGeometries(geometries);

#if OCTTREE_DEBUG_MODE
#if VERBOSE_CONSOLE_OUTPUT
            Trace.WriteLine("_____________________________________________________");
#endif
            Trace.WriteLine($"MAX DEPTH: {maxDepth} ({maxDepthId})");
            if (hasClumping) {
                Trace.WriteLine($"MAX CLUMPING GEOMETRY COUNT: {maxClumpingGeometryCount} ({maxClumpingGeometryCountId})");
            }
#endif
        }

        private Cell(
#if OCTTREE_DEBUG_MODE
                int octantIndex,
#endif
                Vector3d min,
                Vector3d max,
                Cell<T> parent,
                List<T> geometries): 
                    base(min, max) {

#if OCTTREE_DEBUG_MODE

            Id = $"{parent.Id}{octantIndex}";

            if (Id.Length > maxDepth) {

                lock (@lock) {

                    maxDepth = Id.Length;
                    maxDepthId = Id;
                }
            }
#endif
            geometryCount = geometries.Count;
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
                ProcessGeometries(geometries);
            }
            else {

                IsLeaf = true;
                this.geometries = geometries;

#if OCTTREE_DEBUG_MODE
#if VERBOSE_CONSOLE_OUTPUT
                Print("CLUMPING STOP", geometries);
#endif
                hasClumping = true;

                if (geometries.Count > maxClumpingGeometryCount) {

                    lock (@lock) {

                        maxClumpingGeometryCount = geometries.Count;
                        maxClumpingGeometryCountId = Id;
                    }
                }
#endif
            }
        }

        private void ProcessGeometries(
                List<T> geometries) {

            if (geometries.Count > config.MaxOccupation) {

#if OCTTREE_DEBUG_MODE && VERBOSE_CONSOLE_OUTPUT
                Print("SPLIT", geometries);
#endif

                IsLeaf = false;
                Split(geometries);
            }
            else {

#if OCTTREE_DEBUG_MODE && VERBOSE_CONSOLE_OUTPUT
                Print("LEAF", geometries);
#endif

                IsLeaf = true;
                this.geometries = geometries;
            }
        }

#if OCTTREE_DEBUG_MODE
        private void Print(
                string identifier,
                List<T> geometries) {

            string postfixSpace = Enumerable
                .Range(0, Id.Length)
                .Select(i => "  ")
                .Join("");

            Trace.WriteLine($"{postfixSpace}{identifier} {Id} [{geometries.Count}] (DEPTH: {Id.Length})");
        }
#endif

        private void Split(
                List<T> geometries) {

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

            List<T>[] geometryBuckets = config.UseParallel 
                    && geometries.Count >= config.MinOccupationForParallel ?
                Split_Parallel(geometries, childExtents) :
                Split_Sequential(geometries, childExtents);

            Children = config.UseParallel
                    && geometries.Count >= config.MinOccupationForParallel ?
                SpawnChildren_Parallel(geometryBuckets, childExtents) :
                SpawnChildren_Sequential(geometryBuckets, childExtents);
        }

        private List<T>[] Split_Sequential(
                List<T> geometries,
                (Vector3d, Vector3d)[] childExtents) {

            List<T>[] geometryBuckets = InitializeGeometryBuckets();

            foreach (T geometry in geometries) {

                for (int i = 0; i < 8; i++) {

                    if (Intersects(
                            childExtents[i].Item1,
                            childExtents[i].Item2,
                            geometry)) {

                        geometryBuckets[i].Add(geometry);
                    }
                }
            }

            return geometryBuckets;
        }

        private List<T>[] Split_Parallel(
                List<T> geometries,
                (Vector3d, Vector3d)[] childExtents) {

            object @lock = new object();
            List<T>[] geometryBuckets = InitializeGeometryBuckets();

            Parallel.ForEach(
                Partitioner.Create(0, geometries.Count),
                InitializeGeometryBuckets,
                (partition, loopState, partitionBuckets) => {

                    for (int i = partition.Item1; i < partition.Item2; i++) {

                        for (int j = 0; j < 8; j++) {

                            if (Intersects(
                                    childExtents[j].Item1,
                                    childExtents[j].Item2,
                                    geometries[i])) {

                                partitionBuckets[j].Add(geometries[i]);
                            }
                        }
                    }

                    return partitionBuckets;
                },
                partitionBuckets => {

                    for (int i = 0; i < 8; i++) {

                        lock (@lock) {
                            geometryBuckets[i].AddRange(partitionBuckets[i]);
                        }
                    }
                });

            return geometryBuckets;
        }

        private List<T>[] InitializeGeometryBuckets() {

            return Enumerable
                .Range(0, 8)
                .Select(index => new List<T>())
                .ToArray();
        }

        private bool Intersects(
                Vector3d min,
                Vector3d max,
                T geometry) {

            AABox bBox = geometry.BBox;
            Vector3d bBoxMin = bBox.Min;
            Vector3d bBoxMax = bBox.Max;

            bool intersects = min.X <= bBoxMax.X
                && max.X >= bBoxMin.X
                && min.Y <= bBoxMax.Y
                && max.Y >= bBoxMin.Y
                && min.Z <= bBoxMax.Z
                && max.Z >= bBoxMin.Z;

            if (config.UseBBoxIntersects
                    || !intersects) {

                return intersects;
            }

            return geometry.Intersects(
                new AABox(min, max));
        }

        private Cell<T>[] SpawnChildren_Sequential(
                List<T>[] geometryBuckets,
                (Vector3d, Vector3d)[] childExtents) {

            Cell<T>[] children = new Cell<T>[8];

            for (int i = 0; i < 8; i++) {

                children[i] = new Cell<T>(
#if OCTTREE_DEBUG_MODE
                    i,
#endif
                    childExtents[i].Item1,
                    childExtents[i].Item2,
                    this,
                    geometryBuckets[i]);
            }

            return children;
        }

        private Cell<T>[] SpawnChildren_Parallel(
                List<T>[] geometryBuckets,
                (Vector3d, Vector3d)[] childExtents) {

            Cell<T>[] children = new Cell<T>[8];

            Parallel.For(
                0,
                8,
                i => {
                    Cell<T> child = new Cell<T>(
#if OCTTREE_DEBUG_MODE
                        i,
#endif
                        childExtents[i].Item1,
                        childExtents[i].Item2,
                        this,
                        geometryBuckets[i]);

                    children[i] = child;
                });

            return children;
        }
    }
}