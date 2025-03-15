using HuePat.Util.Math.Geometry.Raytracing;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuePat.Util.Math.Geometry.SpatialIndices.OctTree.Grid {
    public class GridOctTree {
        public const int DEFAULT_MAX_OCCUPATION = 50;
        public const int DEFAULT_MIN_OCCUPATION_FOR_PARALLEL = 10000;

        private double resolution;
        private Vector3d minCoordinate;
        private Cell root;

        public GridOctTree(
                bool[,,] grid,
                double resolution,
                Vector3d minCoordinate,
                int maxOccupation = DEFAULT_MAX_OCCUPATION,
                int minOccupationForParallel = DEFAULT_MIN_OCCUPATION_FOR_PARALLEL) {

            this.resolution = resolution;
            this.minCoordinate = minCoordinate;

            List<Vector3i> gridCoordinates = GetGridCoordinates(grid);

            root = new Cell(
                maxOccupation,
                minOccupationForParallel,
                new Vector3i(0, 0, 0),
                new Vector3i(
                    grid.GetLength(0),
                    grid.GetLength(1),
                    grid.GetLength(2)),
                gridCoordinates);
        }

        public double DistanceTo(
                Vector3d position) {

            if (root == null) {
                throw new ApplicationException();
            }

            Vector3i? gridCoordinate = GetNearest(
                position,
                null,
                (double?)null);

            if (!gridCoordinate.HasValue) {
                throw new ApplicationException();
            }

            return GetCellGeometry(gridCoordinate.Value)
                .DistanceTo(position);
        }

        public Vector3i? GetNearest(
                Vector3d position,
                Predicate<Vector3i> filter,
                double? distanceThreshold) {

            if (root == null) {
                return null;
            }

            Cell candidateCell = GetMinValueLeafCell(
                root,
                cell => GetCellGeometry(cell)
                    .DistanceTo(position));

            Vector3i? candidate = GetNearest(
                position,
                candidateCell.GridCoordinates,
                filter);

            if (candidate.HasValue
                    && distanceThreshold.HasValue
                    && GetCellGeometry(candidate.Value)
                            .DistanceTo(position) 
                        > distanceThreshold.Value) {

                return null;
            }

            while (!candidate.HasValue) {

                candidateCell = candidateCell.Parent;

                candidate = GetNearest(
                    position,
                    candidateCell.GridCoordinates,
                    filter);
            }

            if (!candidate.HasValue) {

                return null;
            }

            double distance = GetCellGeometry(candidate.Value)
                .DistanceTo(position);

            List<Vector3i> extendedCandidates = GetCandidateGridCoordinates(
                root,
                cell => !AreEqual(cell, candidateCell)
                    && GetCellGeometry(cell)
                            .DistanceTo(position) 
                        < distance);

            extendedCandidates.Add(candidate.Value);

            candidate = GetNearest(
                position,
                extendedCandidates,
                filter);

            if (candidate.HasValue
                    && distanceThreshold.HasValue
                    && GetCellGeometry(candidate.Value)
                            .DistanceTo(position) 
                        > distanceThreshold.Value) {

                return null;
            }

            return candidate;
        }

        public bool Intersects(
                AABox box) {

            return root != null
                && GetIntersectionCandidates(box)
                    .Any(gridCoordinate => GetCellGeometry(gridCoordinate)
                        .Intersects(box));
        }

        public List<Vector3i> Intersect(
                AABox box) {

            if (root == null) {
                return new List<Vector3i>();
            }
            
            return GetIntersectionCandidates(box)
                .Where(gridCoordinate => GetCellGeometry(gridCoordinate)
                    .Intersects(box))
                .ToList();
        }

        public List<Vector3i> Intersect(
                Ray ray) {

            if (root == null) {
                return new List<Vector3i>();
            }

            return GetIntersectionCandidates(ray)
                .Where(gridCoordinate => GetCellGeometry(gridCoordinate)
                    .Intersects(ray))
                .ToList();
        }

        public List<(Intersection intersection, Vector3i gridCoordinate)> GetIntersections(
                Ray ray) {

            if (root == null) {
                return new List<(Intersection intersection, Vector3i gridCoordinate)>();
            }

            return GetIntersectionCandidates(ray)
                .SelectMany(gridCoordinate => GetCellGeometry(gridCoordinate)
                    .Intersect(ray)
                    .Select(intersection => (
                        intersection: intersection,
                        gridCoordinate: gridCoordinate
                    )))
                .ToList();
        }

        public Vector3i? GetFirstIntersectingGridCoordinate(
                Ray ray) {

            if (root == null) {
                return null;
            }

            List<(Cell cell, double distance)> weightedCandidateLeafCells = GetWeightedCandidateLeafCells(
                cell => {

                    List<Intersection> intersections = GetCellGeometry(cell).Intersect(ray);

                    if (intersections.Count == 0) {

                        return (
                            isCandidate: false,
                            weight: 0.0
                        );
                    }

                    return intersections
                        .OrderBy(intersection => intersection.Distance)
                        .Select(intersection => (
                            isCandidate: true,
                            weight: intersection.Distance
                        ))
                        .First();
                });

            if (weightedCandidateLeafCells.Count == 0) {

                return null;
            }

            List<Cell> candidateLeafCells = weightedCandidateLeafCells
                .OrderBy(weightedCell => weightedCell.distance)
                .Select(weightedCell => weightedCell.cell)
                .ToList();

            foreach (Cell candidateLeafCell in candidateLeafCells) {

                Vector3i? result = candidateLeafCell
                    .GridCoordinates
                    .Select(gridCoordinate => {

                        List<Intersection> intersections = GetCellGeometry(gridCoordinate).Intersect(ray);

                        if (intersections.Count == 0) {

                            return ((Vector3i gridCoordinate, double distance)?)null;
                        }

                        return intersections
                            .WhereMin(intersection => intersection.Distance)
                            .Select(intersection => (
                                gridCoordinate: gridCoordinate,
                                distance: intersection.Distance
                            ))
                            .First();
                    })
                    .Where(result => result.HasValue)
                    .OrderBy(result => result.Value.distance)
                    .Select(result => (Vector3i?)result.Value.gridCoordinate)
                    .FirstOr(null);

                if (result.HasValue) {

                    return result.Value;
                }
            }

            return null;
        }

        private List<Vector3i> GetGridCoordinates(
                bool[,,] grid) {

            object @lock = new object();
            List<Vector3i> gridCoordinates = new List<Vector3i>();

            Parallel.For(
                0,
                grid.GetLength(0),
                () => new List<Vector3i>(),
                (x, loopState, localGridCoordinates) => {

                    for (int y = 0; y < grid.GetLength(1); y++) {
                        for (int z = 0; z < grid.GetLength(2); z++) {

                            if (grid[x, y, z]) {

                                localGridCoordinates.Add(
                                    new Vector3i(x, y, z));
                            }
                        }
                    }

                    return localGridCoordinates;
                },
                localGridCoordinates => {

                    lock (@lock) {

                        gridCoordinates.AddRange(localGridCoordinates);
                    }
                });

            return gridCoordinates;
        }

        private Cell GetMinValueLeafCell(
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

            return GetMinValueLeafCell(
                cell.Children[minIndex],
                callback);
        }

        private List<(Cell cell, double weight)> GetWeightedCandidateLeafCells(
                Func<Cell, (bool isCandidate, double weight)> callback) {

            List<(Cell cell, double weight)> weightedCandidateLeafCells = new List<(Cell cell, double weight)>();

            GetWeightedCandidateLeafCells(
                root,
                callback,
                weightedCandidateLeafCells);

            return weightedCandidateLeafCells;
        }

        private void GetWeightedCandidateLeafCells(
                Cell cell,
                Func<Cell, (bool isCandidate, double weight)> callback,
                List<(Cell cell, double weight)> weightedCandidateLeafCells) {

            (bool isCandidate, double weight) result = callback(cell);

            if (result.isCandidate) {

                if (cell.IsLeaf) {

                    weightedCandidateLeafCells.Add((
                        cell: cell,
                        weight: result.weight
                    ));
                }
                else {

                    foreach (Cell childCell in cell.Children) {

                        GetWeightedCandidateLeafCells(
                            childCell,
                            callback,
                            weightedCandidateLeafCells);
                    }
                }
            }
        }

        private Vector3i? GetNearest(
                Vector3d position,
                IEnumerable<Vector3i> candidates,
                Predicate<Vector3i> filter) {

            Vector3i? nearest = null;
            double distance;
            double minDistance = double.MaxValue;

            foreach (Vector3i gridCoordinate in candidates) { 

                if (filter != null
                        && !filter(gridCoordinate)) {
                    continue;
                }

                distance = GetCellGeometry(gridCoordinate)
                    .DistanceTo(position);

                if (distance < minDistance) {

                    minDistance = distance;
                    nearest = gridCoordinate;
                }
            }

            return nearest;
        }

        private List<Vector3i> GetIntersectionCandidates(
                Ray ray) {

            return GetCandidateGridCoordinates(
                root,
                cell => GetCellGeometry(cell)
                    .Intersect(ray)
                    .Count > 0);
        }

        private List<Vector3i> GetIntersectionCandidates(
                AABox box) {

            return GetCandidateGridCoordinates(
                root,
                cell => GetCellGeometry(cell)
                    .Intersects(box));
        }

        private List<Vector3i> GetCandidateGridCoordinates(
                Cell cell,
                Predicate<Cell> cellFilter) {

            return GetCandidateGridCoordinates(
                cell,
                cellFilter,
                gridCoordinate => true);
        }

        private List<Vector3i> GetCandidateGridCoordinates(
                Cell cell,
                Predicate<Cell> cellFilter,
                Predicate<Vector3i> gridCoordinateFilter) {

            List<Vector3i> candidates = new List<Vector3i>();

            if (!cellFilter(cell)) {
                return candidates;
            }

            if (cell.IsLeaf) {

                candidates.AddRange(cell.GridCoordinates);

                return candidates;
            }

            foreach (Cell childCell in cell.Children) {

                candidates.AddRange(
                    GetCandidateGridCoordinates(
                        childCell,
                        cellFilter,
                        gridCoordinateFilter));
            }

            return candidates
                .Where(geometry => gridCoordinateFilter(geometry))
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
        private AABox GetCellGeometry(
                Cell cell) {

            return GetCellGeometry(
                cell.Min,
                cell.Max);
        }

        private AABox GetCellGeometry(
                Vector3i gridCoordinate) {

            return GetCellGeometry(
                gridCoordinate,
                gridCoordinate);
        }

        private AABox GetCellGeometry(
                Vector3i min,
                Vector3i max) {

            return new AABox(
                this.minCoordinate + resolution * min.ToVector3d() - 0.5 * new Vector3d(resolution),
                this.minCoordinate + resolution * max.ToVector3d() + 0.5 * new Vector3d(resolution));
        }

        private bool AreEqual(
                Cell cell1,
                Cell cell2) {

            return cell1.Min.Equals(cell2.Min)
                && cell1.Max.Equals(cell2.Max);
        }
    }
}