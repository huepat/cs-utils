using OpenTK.Mathematics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuePat.Util.Math.Geometry.SpatialIndices.OctTree.Grid {
    class Cell {
        private List<Vector3i> gridCoordinates;

        public bool IsLeaf { get; private set; }
        public Vector3i Min { get; private set; }
        public Vector3i Max { get; private set; }
        public Cell Parent { get; private set; }
        public Cell[] Children { get; private set; }

        public bool ContainsGridCoordinates {
            get {
                return IsLeaf 
                    && gridCoordinates.Count > 0;
            }
        }

        public List<Vector3i> GridCoordinates {
            get {

                if (IsLeaf) {
                    return gridCoordinates;
                }

                return Children
                    .SelectMany(childCell => childCell.GridCoordinates)
                    .ToList();
            }
        }

        public Cell(
                int maxOccupation,
                int minOccupationForParallel,
                Vector3i min,
                Vector3i max,
                List<Vector3i> gridCoordinates) {

            Min = min;
            Max = max;

            ProcessGridCoordinates(
                maxOccupation,
                minOccupationForParallel,
                gridCoordinates);
        }

        private Cell(
                int maxOccupation,
                int minOccupationForParallel,
                Vector3i min,
                Vector3i max,
                Cell parent,
                List<Vector3i> gridCoordinates) :
                    this(
                        maxOccupation,
                        minOccupationForParallel,
                        min,
                        max,
                        gridCoordinates) {

            Parent = parent;
        }

        private void ProcessGridCoordinates(
                int maxOccupation,
                int minOccupationForParallel,
                List<Vector3i> gridCoordinates) {

            if (gridCoordinates.Count > maxOccupation) {

                IsLeaf = false;

                Split(
                    maxOccupation,
                    minOccupationForParallel,
                    gridCoordinates);
            }
            else {

                IsLeaf = true;
                this.gridCoordinates = gridCoordinates;
            }
        }

        private void Split(
                int maxOccupation,
                int minOccupationForParallel,
                List<Vector3i> gridCoordinates) {

            Vector3i s = (0.5 * (Max.ToVector3d() - Min.ToVector3d()))
                .Floor()
                .ToVector3i();

            (Vector3i, Vector3i)[] childExtents = new (Vector3i, Vector3i)[] {
                    (
                        Min,
                        Min + s
                    ),
                    (
                        new Vector3i(Min.X + s.X, Min.Y, Min.Z),
                        new Vector3i(Max.X, Min.Y + s.Y, Min.Z + s.Z)
                    ),
                    (
                        new Vector3i(Min.X + s.X, Min.Y + s.Y, Min.Z),
                        new Vector3i(Max.X, Max.Y, Min.Z + s.Z)
                    ),
                    (
                        new Vector3i(Min.X, Min.Y + s.Y, Min.Z),
                        new Vector3i(Min.X + s.X, Max.Y, Min.Z + s.Z)
                    ),
                    (
                        new Vector3i(Min.X, Min.Y, Min.Z + s.Z),
                        new Vector3i(Min.X + s.X, Min.Y + s.Y, Max.Z)
                    ),
                    (
                        new Vector3i(Min.X + s.X, Min.Y, Min.Z + s.Z),
                        new Vector3i(Max.X, Min.Y + s.Y, Max.Z)
                    ),
                    (
                        Min + s,
                        Max
                    ),
                    (
                        new Vector3i(Min.X, Min.Y + s.Y, Min.Z + s.Z),
                        new Vector3i(Min.X + s.X, Max.Y, Max.Z)
                    )
                };

            List<Vector3i>[] buckets = gridCoordinates.Count >= minOccupationForParallel ?
                Split_Parallel(gridCoordinates, childExtents) :
                Split_Sequential(gridCoordinates, childExtents);

            Children = gridCoordinates.Count >= minOccupationForParallel ? 
                SpawnChildren_Parallel(
                    maxOccupation,
                    minOccupationForParallel,
                    buckets, 
                    childExtents) :
                SpawnChildren_Sequential(
                    maxOccupation,
                    minOccupationForParallel,
                    buckets,
                    childExtents);
        }

        private List<Vector3i>[] Split_Sequential(
                List<Vector3i> gridCoordinates,
                (Vector3i, Vector3i)[] childExtents) {

            List<Vector3i>[] buckets = InitializeBuckets();

            foreach (Vector3i gridCoordinate in gridCoordinates) {

                for (int i = 0; i < 8; i++) {

                    if (Contains(
                            childExtents[i].Item1,
                            childExtents[i].Item2,
                            gridCoordinate)) {

                        buckets[i].Add(gridCoordinate);
                    }
                }
            }

            return buckets;
        }

        private List<Vector3i>[] Split_Parallel(
                List<Vector3i> gridCoordinates,
                (Vector3i, Vector3i)[] childExtents) {

            object @lock = new object();
            List<Vector3i>[] buckets = InitializeBuckets();

            Parallel.ForEach(
                Partitioner.Create(0, gridCoordinates.Count),
                InitializeBuckets,
                (partition, loopState, partitionBuckets) => {

                    for (int i = partition.Item1; i < partition.Item2; i++) {

                        for (int j = 0; j < 8; j++) {

                            if (Contains(
                                    childExtents[j].Item1,
                                    childExtents[j].Item2,
                                    gridCoordinates[i])) {

                                partitionBuckets[j].Add(gridCoordinates[i]);
                            }
                        }
                    }

                    return partitionBuckets;
                },
                partitionBuckets => {

                    for (int i = 0; i < 8; i++) {

                        lock (@lock) {
                            buckets[i].AddRange(partitionBuckets[i]);
                        }
                    }
                });

            return buckets;
        }

        private List<Vector3i>[] InitializeBuckets() {

            return Enumerable
                .Range(0, 8)
                .Select(index => new List<Vector3i>())
                .ToArray();
        }

        private bool Contains(
                Vector3i min,
                Vector3i max,
                Vector3i gridCoordinate) {

            return min.X <= gridCoordinate.X
                && max.X >= gridCoordinate.X
                && min.Y <= gridCoordinate.Y
                && max.Y >= gridCoordinate.Y
                && min.Z <= gridCoordinate.Z
                && max.Z >= gridCoordinate.Z;
        }

        private Cell[] SpawnChildren_Sequential(
                int maxOccupation,
                int minOccupationForParallel,
                List<Vector3i>[] buckets,
                (Vector3i, Vector3i)[] childExtents) {

            Cell[] children = new Cell[8];

            for (int i = 0; i < 8; i++) {

                children[i] = new Cell(
                    maxOccupation,
                    minOccupationForParallel,
                    childExtents[i].Item1,
                    childExtents[i].Item2,
                    this,
                    buckets[i]);
            }

            return children;
        }

        private Cell[] SpawnChildren_Parallel(
                int maxOccupation,
                int minOccupationForParallel,
                List<Vector3i>[] buckets,
                (Vector3i, Vector3i)[] childExtents) {

            Cell[] children = new Cell[8];

            Parallel.For(
                0,
                8,
                i => {
                    Cell child = new Cell(
                        maxOccupation,
                        minOccupationForParallel,
                        childExtents[i].Item1,
                        childExtents[i].Item2,
                        this,
                        buckets[i]);

                    children[i] = child;
                });

            return children;
        }
    }
}
