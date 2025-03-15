#if OCTTREE_DEBUG_MODE
using HuePat.Util.IO.PLY.Writing;
#endif
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.Math.Geometry.SpatialIndices.OctTree {
    public enum OctTreeCellFilter { ALL, LEAF, CONTAINS_GEOMETRIES }

    public class OctTree<T> : ISpatialIndex<T> where T : IFiniteGeometry {
        public const int DEFAULT_MAX_OCCUPATION = 50;
        public const int DEFAULT_CLUMPING_DEPTH_THRESHOLD = 5;
        public const int DEFAULT_MIN_OCCUPATION_FOR_PARALLEL = 10000;

        private readonly bool useParallel;
        private Cell<T> root;

        public bool UseBBoxIntersects { private get; set; }
        public int MaxOccupation { private get; set; }
        public int ClumpingDepthThreshold { private get; set; }
        public int MinOccupationForParallel { private get; set; }
        public Dictionary<string, IProperty> Properties { get; set; }

        public AABox BBox {
            get {
                return root;
            }
        }

        public Mesh Mesh => throw new NotImplementedException();

        public OctTree(
                bool useParallel) {

            this.useParallel = useParallel;

            UseBBoxIntersects = true;
            MaxOccupation = DEFAULT_MAX_OCCUPATION;
            ClumpingDepthThreshold = DEFAULT_CLUMPING_DEPTH_THRESHOLD;
            MinOccupationForParallel = DEFAULT_MIN_OCCUPATION_FOR_PARALLEL;
        }

        public ISpatialIndex<T> CopyEmpty() {

            return new OctTree<T>(useParallel) {

                MaxOccupation = MaxOccupation,
                ClumpingDepthThreshold = ClumpingDepthThreshold,
                MinOccupationForParallel = MinOccupationForParallel,
                Properties = Properties
            };
        }

        public void Load(
                IEnumerable<T> geometries) {

            if (!geometries.Any()) {

                return;
            }

            root = new Cell<T>( 
                useParallel,
                UseBBoxIntersects,
                MaxOccupation,
                ClumpingDepthThreshold,
                MinOccupationForParallel,
                geometries.ToList());
        }

        public Mesh GetCellsMesh(
                OctTreeCellFilter cellFilter = OctTreeCellFilter.ALL) {

            if (root == null) {

                return new Mesh();
            }

            switch (cellFilter) {
                default:
                case OctTreeCellFilter.ALL:
                    return GetCellsMesh(cell => true);
                case OctTreeCellFilter.LEAF:
                    return GetCellsMesh(cell => cell.IsLeaf);
                case OctTreeCellFilter.CONTAINS_GEOMETRIES:
                    return GetCellsMesh(cell => cell.ContainsGeometries);
            }
        }

        public void UpdateBBox() {
            // nothing to do
        }

        public double DistanceTo(
                Vector3d position) {

            if (root == null) {
                throw new ApplicationException();
            }

            T geometry = GetNearest(
                position,
                null,
                (double?)null);

            if (geometry == null) {
                throw new ApplicationException();
            }

            return geometry.DistanceTo(position);
        }

        public T GetNearest(
                Vector3d position,
                Predicate<T> filter,
                double? distanceThreshold) {

            if (root == null) {
                return default;
            }

            Cell<T> candidateCell = GetMinValueLeaf(
                root, 
                cell => cell.DistanceTo(position));

            T candidate = GetNearest(
                position, 
                candidateCell.Geometries, 
                filter);

            if (candidate != null
                    && distanceThreshold.HasValue 
                    && candidate.DistanceTo(position) > distanceThreshold.Value) {

                return default(T);
            }

            while (candidate == null) {

                candidateCell = candidateCell.Parent;

                candidate = GetNearest(
                    position, 
                    candidateCell.Geometries, 
                    filter);
            }

            double distance = candidate.DistanceTo(position);

            List<T> extendedCandidates = GetCandidateGeometries(
                root, 
                cell => !cell.ApproximateEquals(candidateCell) 
                    && cell.DistanceTo(position) < distance);

            extendedCandidates.Add(candidate);

            candidate = GetNearest(
                position, 
                extendedCandidates, 
                filter);

            if (candidate != null
                    && distanceThreshold.HasValue
                    && candidate.DistanceTo(position) > distanceThreshold.Value) {

                return default;
            }

            return candidate;
        }

        public bool Intersects(
                AABox box) {

            return root != null
                && GetIntersectionCandidates(box)
                    .Intersects(box);
        }

        public List<T> Intersect(
                AABox box) {

            if (root == null) {
                return new List<T>();
            }

            return GetIntersectionCandidates(box)
                .Intersect(box);
        }

        public List<MultiGeometryIntersection<T>> Intersect(
                Ray ray) {

            if (root == null) {
                return new List<MultiGeometryIntersection<T>>();
            }

            return Intersect(ray, null);
        }

        public List<MultiGeometryIntersection<T>> Intersect(
                Ray ray,
                double? distanceTheshold) {

            if (root == null) {
                return new List<MultiGeometryIntersection<T>>();
            }

            return GetIntersectionCandidates(
                    ray,
                    distanceTheshold)
                .Intersect(
                    ray,
                    distanceTheshold);
        }

        public T GetFirstIntersectingGridCoordinate(
                Ray ray) {

            if (root == null) {
                return default;
            }

            List<(Cell<T> cell, double distance)> weightedCandidateLeafCells = GetWeightedCandidateLeafCells(
                cell => {

                    List<Intersection> intersections = cell.Intersect(ray);

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

                return default;
            }

            List<Cell<T>> candidateLeafCells = weightedCandidateLeafCells
                .OrderBy(weightedCell => weightedCell.distance)
                .Select(weightedCell => weightedCell.cell)
                .ToList();

            foreach (Cell<T> candidateLeafCell in candidateLeafCells) {

                T result = candidateLeafCell
                    .Geometries
                    .Select(geometry => {

                        List<Intersection> intersections = geometry.Intersect(ray);

                        if (intersections.Count == 0) {

                            return ((T geometry, double distance)?)null;
                        }

                        return intersections
                            .WhereMin(intersection => intersection.Distance)
                            .Select(intersection => (
                                geometry: geometry,
                                distance: intersection.Distance
                            ))
                            .First();
                    })
                    .Where(result => result.HasValue)
                    .OrderBy(result => result.Value.distance)
                    .Select(result => result.Value.geometry)
                    .FirstOrDefault();

                if (result != null) {

                    return result;
                }
            }

            return default;
        }

        List<Intersection> IGeometry.Intersect(
                Ray ray) {

            if (root == null) {
                return new List<Intersection>();
            }

            return (GetIntersectionCandidates(ray, null) as IFiniteGeometry)
                .Intersect(ray);
        }

#if OCTTREE_DEBUG_MODE
        public void ExportCell<U>(
                string id,
                string directory) where U : class, IFiniteGeometry {

            Cell<T> cell = null; 

            ForEachCell(
                root,
                searchCell => {
                    if (searchCell.Id == id) {
                        cell = searchCell;
                    }
                });

            cell.ExportMeshPly($"{directory}/Cell_{id}.ply");

            cell
                .Geometries
                .Select(geometry => geometry as U)
                .ExportMeshPly($"{directory}/Cell_{id}_Geometries.ply");
        }
#endif
        private List<Cell<T>> GetCells(
                Predicate<Cell<T>> filter) {

            List<Cell<T>> cells = new List<Cell<T>>();

            ForEachCell(
                root,
                cell => {
                    if (filter(cell)) {
                        cells.Add(cell);
                    }
                });

            return cells;
        }

        private Cell<T> GetMinValueLeaf(
                Cell<T> cell, 
                Func<Cell<T>, double> callback) {

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

        private List<(Cell<T> cell, double weight)> GetWeightedCandidateLeafCells(
                Func<Cell<T>, (bool isCandidate, double weight)> callback) {

            List<(Cell<T> cell, double weight)> weightedCandidateLeafCells 
                = new List<(Cell<T> cell, double weight)>();

            GetWeightedCandidateLeafCells(
                root,
                callback,
                weightedCandidateLeafCells);

            return weightedCandidateLeafCells;
        }

        private void GetWeightedCandidateLeafCells(
                Cell<T> cell,
                Func<Cell<T>, (bool isCandidate, double weight)> callback,
                List<(Cell<T> cell, double weight)> weightedCandidateLeafCells) {

            (bool isCandidate, double weight) result = callback(cell);

            if (result.isCandidate) {

                if (cell.IsLeaf) {

                    weightedCandidateLeafCells.Add((
                        cell: cell,
                        weight: result.weight
                    ));
                }
                else {

                    foreach (Cell<T> childCell in cell.Children) {

                        GetWeightedCandidateLeafCells(
                            childCell,
                            callback,
                            weightedCandidateLeafCells);
                    }
                }
            }
        }

        private T GetNearest(
                Vector3d position, 
                IEnumerable<T> candidates,
                Predicate<T> filter) {

            BruteForceIndex<T> bruteForceIndex = new BruteForceIndex<T>();

            bruteForceIndex.Load(candidates);

            return bruteForceIndex.GetNearest(
                position, 
                filter,
                null);
        }

        private BruteForceIndex<T> GetIntersectionCandidates(
                Ray ray,
                double? distanceTheshold) {

            List<Intersection> intersections;
            BruteForceIndex<T> bruteForceIndex = new BruteForceIndex<T>();

            bruteForceIndex.Load(
                GetCandidateGeometries(
                    root, 
                    cell => {

                        intersections = cell.Intersect(ray);

                        if (intersections.Count == 0) {
                            return false;
                        }

                        if (distanceTheshold.HasValue
                                && !cell.Contains(ray.Origin)) {

                            return intersections.Any(
                                intersection => intersection.Distance <= distanceTheshold.Value);
                        }

                        return true;
                    }));

            return bruteForceIndex;
        }

        private BruteForceIndex<T> GetIntersectionCandidates(
                AABox box) {

            BruteForceIndex<T> bruteForceIndex = new BruteForceIndex<T>();

            bruteForceIndex.Load(
                GetCandidateGeometries(
                    root, 
                    cell => cell.Intersects(box)));

            return bruteForceIndex;
        }

        private List<T> GetCandidateGeometries(
                Cell<T> cell,
                Predicate<Cell<T>> cellFilter) {

            return GetCandidateGeometries(
                cell, 
                cellFilter, 
                geometry => true);
        }

        private List<T> GetCandidateGeometries(
                Cell<T> cell, 
                Predicate<Cell<T>> cellFilter,
                Predicate<T> geometryFilter) {

            List<T> candidates = new List<T>();

            if (!cellFilter(cell)) {
                return candidates;
            }

            if (cell.IsLeaf) {

                candidates.AddRange(cell.Geometries);

                return candidates;
            }

            foreach (Cell<T> childCell in cell.Children) {

                candidates.AddRange(
                    GetCandidateGeometries(
                        childCell, 
                        cellFilter, 
                        geometryFilter));
            }

            return candidates
                .Where(geometry => geometryFilter(geometry))
                .ToList();
        }

        private Mesh GetCellsMesh(
                Predicate<Cell<T>> filterCallback) {

            MeshCreator creator = new MeshCreator();

            ForEachCell(
                root, 
                cell => {
                    if (filterCallback(cell)) {
                        creator.Add(cell.Mesh);
                    }
                });

            return creator.Create();
        }

        private void ForEachCell(
                Cell<T> cell, 
                Action<Cell<T>> callback) {

            callback(cell);

            if (cell.IsLeaf) {
                return;
            }

            foreach (Cell<T> childCell in cell.Children) {

                ForEachCell(
                    childCell, 
                    callback);
            }
        }
    }
}