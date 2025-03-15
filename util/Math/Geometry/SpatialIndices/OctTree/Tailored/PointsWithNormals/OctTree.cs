using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

using SmallPoint = HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals.Point;

namespace HuePat.Util.Math.Geometry.SpatialIndices.OctTree.Tailored.PointsWithNormals {
    public enum OctTreeCellFilter { ALL, LEAF, CONTAINS_GEOMETRIES }

    public class OctTree {
        public const int DEFAULT_MAX_OCCUPATION = 50;
        public const int DEFAULT_CLUMPING_DEPTH_THRESHOLD = 5;
        public const int DEFAULT_MIN_GEOMETRY_COUNT_FOR_PARALLEL = 10000;

        private readonly bool useParallel;
        private Cell root;

        public int MaxOccupation { private get; set; }
        public int ClumpingDepthThreshold { private get; set; }
        public int MinGeometryCountForParallel { private get; set; }

        public AABox BBox {
            get {
                return new AABox(
                    root.Min,
                    root.Max);
            }
        }

        public OctTree(
                bool useParallel) {

            this.useParallel = useParallel;

            MaxOccupation = DEFAULT_MAX_OCCUPATION;
            ClumpingDepthThreshold = DEFAULT_CLUMPING_DEPTH_THRESHOLD;
            MinGeometryCountForParallel = DEFAULT_MIN_GEOMETRY_COUNT_FOR_PARALLEL;
        }

        public void Load(
                List<SmallPoint> points) {

            if (!points.Any()) {

                throw new ArgumentException(
                    "OctTree cannot be created from empty geometry collections.");
            }

            root = new Cell(
                useParallel,
                MaxOccupation,
                ClumpingDepthThreshold,
                MinGeometryCountForParallel,
                points);
        }

        public double DistanceTo(
                Vector3d position) {

            SmallPoint? point = GetNearest(
                position,
                null,
                (double?)null);

            if (point == null) {
                throw new ApplicationException();
            }

            return point.Value.DistanceTo(position);
        }

        public SmallPoint? GetNearest(
                Vector3d position,
                Predicate<SmallPoint> filter,
                double? distanceThreshold) {

            Cell candidateCell = GetMinValueLeaf(
                root,
                cell => cell.DistanceTo(position));

            SmallPoint? candidate = GetNearest(
                position,
                candidateCell.Points,
                filter);

            if (candidate != null
                    && distanceThreshold.HasValue
                    && candidate.Value.DistanceTo(position) > distanceThreshold.Value) {

                return null;
            }

            while (candidate == null) {

                candidateCell = candidateCell.Parent;

                candidate = GetNearest(
                    position,
                    candidateCell.Points,
                    filter);
            }

            double distance = candidate.Value.DistanceTo(position);

            List<SmallPoint> extendedCandidates = GetCandidatePoints(
                root,
                cell => !cell.ApproximateEquals(candidateCell)
                    && cell.DistanceTo(position) < distance);

            extendedCandidates.Add(candidate.Value);

            candidate = GetNearest(
                position,
                extendedCandidates,
                filter);

            if (candidate != null
                    && distanceThreshold.HasValue
                    && candidate.Value.DistanceTo(position) > distanceThreshold.Value) {

                return null;
            }

            return candidate;
        }

        public bool Intersects(
                AABox box) {

            return GetIntersectionCandidates(box)
                .Any(point => box.Contains(point.Position));
        }

        public List<SmallPoint> Intersect(
                AABox box) {

            return GetIntersectionCandidates(box)
                .Where(point => box.Contains(point.Position))
                .ToList();
        }

        private List<Cell> GetCells(
                Predicate<Cell> filter) {

            List<Cell> cells = new List<Cell>();

            ForEachCell(
                root,
                cell => {
                    if (filter(cell)) {
                        cells.Add(cell);
                    }
                });

            return cells;
        }

        private Cell GetMinValueLeaf(
                Cell cell,
                Func<Cell, double> callback) {

            if (cell.IsLeaf) {
                return cell;
            }

            double value;
            double minValue = double.MaxValue;
            int minIndex = 0;

            for (int i = 0; i < cell.Children.Length; i++) {

                value = callback(cell.Children[i]);

                if (value < minValue) {
                    minValue = value;
                    minIndex = i;
                }
            }

            return GetMinValueLeaf(
                cell.Children[minIndex],
                callback);
        }

        private SmallPoint? GetNearest(
                Vector3d position,
                IEnumerable<SmallPoint> candidates,
                Predicate<SmallPoint> filter) {

            SmallPoint? nearest = null;
            double distance;
            double minDistance = double.MaxValue;

            foreach (SmallPoint point in candidates) {

                if (filter != null
                        && !filter(point)) {
                    continue;
                }

                distance = point.DistanceTo(position);

                if (distance < minDistance) {

                    minDistance = distance;
                    nearest = point;
                }
            }

            return nearest;
        }

        private List<SmallPoint> GetIntersectionCandidates(
                AABox box) {

            return GetCandidatePoints(
                root,
                cell => cell.Intersects(box));
        }

        private List<SmallPoint> GetCandidatePoints(
                Cell cell,
                Predicate<Cell> cellFilter) {

            return GetCandidatePoints(
                cell,
                cellFilter,
                geometry => true);
        }

        private List<SmallPoint> GetCandidatePoints(
                Cell cell,
                Predicate<Cell> cellFilter,
                Predicate<SmallPoint> pointFilter) {

            List<SmallPoint> candidates = new List<SmallPoint>();

            if (!cellFilter(cell)) {
                return candidates;
            }

            if (cell.IsLeaf) {

                candidates.AddRange(cell.Points);

                return candidates;
            }

            foreach (Cell childCell in cell.Children) {

                candidates.AddRange(
                    GetCandidatePoints(
                        childCell,
                        cellFilter,
                        pointFilter));
            }

            return candidates
                .Where(geometry => pointFilter(geometry))
                .ToList();
        }

        private void ForEachCell(
                Cell cell,
                Action<Cell> callback) {

            callback(cell);

            if (cell.IsLeaf) {
                return;
            }

            foreach (Cell childCell in cell.Children) {

                ForEachCell(
                    childCell,
                    callback);
            }
        }
    }
}