//#define DEBUG_MODE
//#define FULL_GRID_EXPORT
//#define EXPORT_VERTICAL_NORMALS
//#define EXPORT_CLUSTER_GEOMETRIES
//#define LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL

#if DEBUG_MODE
using HuePat.Util.Colors;
using HuePat.Util.IO;
using HuePat.Util.IO.PLY.Writing;
#endif
using HuePat.Util.Math.Geometry.Processing.Rotating;
using HuePat.Util.Math.Statistics;
#if DEBUG_MODE
using HuePat.Util.Object.Properties;
#endif
using OpenTK.Mathematics;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if DEBUG_MODE
using System.IO;
#endif
using System.Linq;
using System.Threading.Tasks;

namespace HuePat.Util.Math.Geometry.Processing.Alignment {
    public static class Extensions {
        private const double VERTICAL_PEAK_RATIO = 0.75;
        private const double HORIZONTAL_PEAK_RATIO = 0.75;
        private const double UNAMBIGUOUS_ALIGN_SIZE_FRACTION = 0.1;
        private static readonly double DEGREE_45 = 45.0.DegreeToRadian();
        private static readonly double DEGREE_90 = 90.0.DegreeToRadian();
        private static readonly double DEGREE_180 = 180.0.DegreeToRadian();
        private static readonly double VERTICAL_RESOLUTION = 1.0.DegreeToRadian();
        private static readonly double HORIZONTAL_RESOLUTION = 1.0.DegreeToRadian();
        private static readonly double VERTICAL_ALIGNMENT_ANGLE_RADIUS = 40.0.DegreeToRadian();
        private static readonly double VERTICAL_AXIS_REFINEMENT_ANGLE_RADIUS = 5.0.DegreeToRadian();
        private static readonly double HORIZONTAL_ANGLE_REFINEMENT_ANGLE_RADIUS = 5.0.DegreeToRadian();
        private static readonly double HORIZONTAL_ALIGNMENT_VERTICAL_ANGLE_MIN_THRESHOLD = 45.0.DegreeToRadian();
        private static readonly double HORIZONTAL_ALIGNMENT_VERTICAL_ANGLE_MAX_THRESHOLD = 135.0.DegreeToRadian();

        public static void Align(
                this IShape shape,
                bool normalizeAlignment,
                Vector3d upAxis,
                Vector3d horizontalAxis,
                out Matrix3d rotation) {

            Vector3d centroid = shape.GetCentroid(true);
            IReadOnlyList<double> weights = shape.GetSizeWeights();

            rotation = shape.AlignVertically(
                centroid,
                upAxis,
                horizontalAxis,
                weights);

#if DEBUG_MODE
            (shape as Mesh).ExportMeshPly(@"C:\Users\phuebner\data\test\alignment\aligned_vertically.ply");
#endif

            rotation = shape.AlignHorizontally(
                    centroid,
                    upAxis,
                    horizontalAxis,
                    weights)
                * rotation;

#if DEBUG_MODE
            (shape as Mesh).ExportMeshPly(@"C:\Users\phuebner\data\test\alignment\aligned_horizontally.ply");
#endif

            if (normalizeAlignment) {

                shape.NormalizeAlignment(
                    centroid,
                    upAxis,
                    horizontalAxis);
#if DEBUG_MODE
                (shape as Mesh).ExportMeshPly(@"C:\Users\phuebner\data\test\alignment\aligned_horizontally_normalized.ply");
#endif

            }
        }

        private static Matrix3d AlignVertically(
                this IShape shape,
                Vector3d centroid,
                Vector3d upAxis,
                Vector3d horizontalAxis,
                IReadOnlyList<double> weights) {
            Matrix3d rotation;
            double[,] verticalGrid;
            IReadOnlyList<Vector3d> normals = shape.GetNormals();
            Dictionary<(int, int), List<(Vector3d, double)>> weightedNormalsPerVerticalGridCell;
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
            Dictionary<(int, int), List<(IFiniteGeometry, Vector3d)>> geometriesPerVerticalGridCell;
#endif
            verticalGrid = CreateVerticalGrid(
                upAxis,
                horizontalAxis,
                weights,
                normals,
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
                shape.GetGeometries(),
                out geometriesPerVerticalGridCell,
#endif
                out weightedNormalsPerVerticalGridCell);
            RemoveSecondaryNormalClustersPerGridCell(
                verticalGrid,
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
                geometriesPerVerticalGridCell,
#endif
                weightedNormalsPerVerticalGridCell);
            rotation = GetAlignedUpAxis(
                    upAxis,
#if DEBUG_MODE
                    centroid,
#endif
#if DEBUG_MODE && EXPORT_CLUSTER_GEOMETRIES
                    horizontalAxis,
                    shape,
#endif
                    verticalGrid,
                    weights,
                    normals,
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
                    geometriesPerVerticalGridCell,
#endif
                    weightedNormalsPerVerticalGridCell)
                .RotationTo(upAxis);
            shape.Rotate(
                new Rotation(rotation) {
                    UseParallel = true,
                    Anchor = centroid,
                    RotateNormals = shape.Type == ShapeType.POINT_CLOUD
                });
            return rotation;
        }

        private static double[,] CreateVerticalGrid(
                Vector3d upAxis,
                Vector3d horizontalAxis,
                IReadOnlyList<double> weights,
                IReadOnlyList<Vector3d> normals,
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
                IReadOnlyList<IFiniteGeometry> geometries,
                out Dictionary<(int, int), List<(IFiniteGeometry, Vector3d)>> geometriesPerVerticalGridCell,
#endif
                out Dictionary<(int, int), List<(Vector3d, double)>> weightedNormalsPerVerticalGridCell) {
            object @lock = new object();
            Vector3d horizontalAxis2 = Vector3d.Cross(upAxis, horizontalAxis);
            double[,] verticalGrid = new double[
                (int)(DEGREE_90 / VERTICAL_RESOLUTION).Ceil(),
                (int)(VERTICAL_ALIGNMENT_ANGLE_RADIUS / VERTICAL_RESOLUTION).Ceil()];
#if DEBUG_MODE && FULL_GRID_EXPORT
            double[,] fullVerticalGrid = new double[
                (int)(360.0.DegreeToRadian() / VERTICAL_RESOLUTION).Ceil(),
                (int)(DEGREE_180 / VERTICAL_RESOLUTION).Ceil()];
#endif
            Dictionary<(int, int), List<(Vector3d, double)>> cellNormals
                = new Dictionary<(int, int), List<(Vector3d, double)>>();
            Parallel.ForEach(
                Partitioner.Create(0, normals.Count),
                () => (
                    new double[
                        verticalGrid.GetLength(0),
                        verticalGrid.GetLength(1)],
                    new Dictionary<(int, int), List<(Vector3d, double)>>()
#if DEBUG_MODE && FULL_GRID_EXPORT
                    , new double[
                        fullVerticalGrid.GetLength(0),
                        fullVerticalGrid.GetLength(1)]
#endif
                    ),
                (partition, loopState, localState) => {
                    int a, i;
                    double azimuth, inclination;
                    for (int j = partition.Item1; j < partition.Item2; j++) {
                        if (double.IsNaN(normals[j].X)
                                || double.IsNaN(normals[j].Y)
                                || double.IsNaN(normals[j].Z)
                                || double.IsNaN(weights[j])) {
                            continue;
                        }
#if DEBUG_MODE && FULL_GRID_EXPORT
                        azimuth = System.Math.Atan2(
                            Vector3d.Dot(normals[j], horizontalAxis),
                            Vector3d.Dot(normals[j], horizontalAxis2)) + DEGREE_180;
                        inclination = Vector3d.Dot(normals[j], upAxis).Acos();
                        a = (int)(azimuth / VERTICAL_RESOLUTION);
                        i = (int)(inclination / VERTICAL_RESOLUTION);
                        if (a == localState.Item3.GetLength(0)) {
                            a = 0;
                        }
                        localState.Item3[a, i] += weights[j];
#endif
                        inclination = DEGREE_90 - (Vector3d.Dot(normals[j], upAxis).Acos() - DEGREE_90).Abs();
                        if (double.IsNaN(inclination)
                                || inclination.Abs() > VERTICAL_ALIGNMENT_ANGLE_RADIUS) {
                            continue;
                        }
                        azimuth = (System.Math.Atan2(
                                Vector3d.Dot(normals[j], horizontalAxis),
                                Vector3d.Dot(normals[j], horizontalAxis2)).Abs()
                            - DEGREE_90).Abs();
                        a = (int)(azimuth / VERTICAL_RESOLUTION);
                        i = (int)(inclination / VERTICAL_RESOLUTION);
                        if (a == localState.Item1.GetLength(0)) {
                            a = 0;
                        }
                        localState.Item1[a, i] += weights[j];
                        localState.Item2.BucketAdd(
                            (a, i),
                            (normals[j], weights[j]));
                    }
                    return localState;
                },
                localState => {
                    int a, i;
                    lock (@lock) {
                        cellNormals.BucketAdd(localState.Item2);
                        for (a = 0; a < verticalGrid.GetLength(0); a++) {
                            for (i = 0; i < verticalGrid.GetLength(1); i++) {
                                verticalGrid[a, i] += localState.Item1[a, i];
                            }
                        }
#if DEBUG_MODE && FULL_GRID_EXPORT
                        for (a = 0; a < fullVerticalGrid.GetLength(0); a++) {
                            for (i = 0; i < fullVerticalGrid.GetLength(1); i++) {
                                fullVerticalGrid[a, i] += localState.Item3[a, i];
                            }
                        }
#endif
                    }
                });
            weightedNormalsPerVerticalGridCell = cellNormals;
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
            geometriesPerVerticalGridCell = new Dictionary<(int, int), List<(IFiniteGeometry, Vector3d)>>();
            {
                int a, i;
                double azimuth, inclination;
                for (int j = 0; j < geometries.Count; j++) {
                    if (double.IsNaN(normals[j].X)
                                || double.IsNaN(normals[j].Y)
                                || double.IsNaN(normals[j].Z)
                                || double.IsNaN(weights[j])) {
                        continue;   
                    }
                    inclination = DEGREE_90 - (Vector3d.Dot(normals[j], upAxis).Acos() - DEGREE_90).Abs();
                    if (double.IsNaN(inclination)
                            || inclination.Abs() > VERTICAL_ALIGNMENT_ANGLE_RADIUS) {
                        continue;
                    }
                    azimuth = (System.Math.Atan2(
                            Vector3d.Dot(normals[j], horizontalAxis),
                            Vector3d.Dot(normals[j], horizontalAxis2)).Abs()
                        - DEGREE_90).Abs();
                    a = (int)(azimuth / VERTICAL_RESOLUTION);
                    i = (int)(inclination / VERTICAL_RESOLUTION);
                    if (a == verticalGrid.GetLength(0)) {
                        a = 0;
                    }
                    geometriesPerVerticalGridCell.BucketAdd(
                        (a, i),
                        (geometries[j], normals[j]));
                }
            }
#endif
            for (int a = 1; a < verticalGrid.GetLength(0); a++) {
                verticalGrid[0, 0] += verticalGrid[a, 0];
                verticalGrid[a, 0] = 0;
                if (weightedNormalsPerVerticalGridCell.ContainsKey((a, 0))) {
                    weightedNormalsPerVerticalGridCell.BucketAdd(
                        (0, 0),
                        weightedNormalsPerVerticalGridCell[(a, 0)]);
                    weightedNormalsPerVerticalGridCell.Remove((a, 0));
                }
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
                if (geometriesPerVerticalGridCell.ContainsKey((a, 0))) {
                    geometriesPerVerticalGridCell.BucketAdd(
                        (0, 0),
                        geometriesPerVerticalGridCell[(a, 0)]);
                    geometriesPerVerticalGridCell.Remove((a, 0));
                }
#endif
            }
#if DEBUG_MODE && FULL_GRID_EXPORT
            for (int a = 1; a < fullVerticalGrid.GetLength(0); a++) {
                fullVerticalGrid[0, 0] += fullVerticalGrid[a, 0];
                fullVerticalGrid[a, 0] = 0;
            }
            fullVerticalGrid.ExportPly(
                0,
                "Test_Vertical_Full",
                null);
#endif
            return verticalGrid;
        }

        private static void RemoveSecondaryNormalClustersPerGridCell(
                double[,] verticalGrid,
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
                Dictionary<(int, int), List<(IFiniteGeometry, Vector3d)>> geometriesPerVerticalGridCell,
#endif
                Dictionary<(int, int), List<(Vector3d, double)>> weightedNormalsPerVerticalGridCell) {
            List<(int, int)> verticalGridCellKeys = weightedNormalsPerVerticalGridCell.Keys.ToList();
            Parallel.For(
                0,
                verticalGridCellKeys.Count,
                j => {
                    bool found;
                    int k, l;
                    double angle;
                    List<Vector3d> finalClusterCenters = new List<Vector3d>();
                    List<VectorStatistics> clusterCenters = new List<VectorStatistics>();
                    List<List<(Vector3d, double)>> clusters = new List<List<(Vector3d, double)>>();
                    List<List<(Vector3d, double)>> finalClusters = new List<List<(Vector3d, double)>>();
                    foreach ((Vector3d, double) weightedNormal in weightedNormalsPerVerticalGridCell[verticalGridCellKeys[j]]) {
                        found = false;
                        for (k = 0; k < clusterCenters.Count; k++) {
                            angle = weightedNormal.Item1.AngleTo(clusterCenters[k].Mean);
                            if (angle < 2 * VERTICAL_RESOLUTION) {
                                found = true;
                                clusters[k].Add(weightedNormal);
                                clusterCenters[k].Update(weightedNormal.Item1);
                                break;
                            }
                        }
                        if (!found) {
                            clusters.Add(
                                new List<(Vector3d, double)> {
                                    weightedNormal
                                });
                            clusterCenters.Add(
                                new VectorStatistics());
                            clusterCenters[clusterCenters.Count - 1]
                                .Update(weightedNormal.Item1);
                        }
                    }
                    for (k = 0; k < clusters.Count; k++) {
                        found = false;
                        for (l = 0; l < finalClusters.Count; l++) {
                            if ((clusterCenters[k].Mean.AngleTo(finalClusterCenters[l]) - DEGREE_180).Abs() < 2 * VERTICAL_RESOLUTION) {
                                found = true;
                                finalClusters[l].AddRange(clusters[k]);
                                break;
                            }
                        }
                        if (!found) {
                            finalClusters.Add(clusters[k]);
                            finalClusterCenters.Add(clusterCenters[k].Mean);
                        }
                    }
                    if (finalClusters.Count > 1) {
                        List<(Vector3d, double)> mainClusterNormals = finalClusters
                            .WhereMax(cluster => cluster.Count)
                            .SelectMany(cluster => cluster)
                            .ToList();
                        verticalGrid[
                                verticalGridCellKeys[j].Item1,
                                verticalGridCellKeys[j].Item2]
                            = mainClusterNormals.Sum(weightedNormal => weightedNormal.Item2);
                        weightedNormalsPerVerticalGridCell[verticalGridCellKeys[j]] = mainClusterNormals;
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
                        List<Vector3d> mainClustersCenters = Enumerable
                            .Range(0, finalClusters.Count)
                            .WhereMax(m => finalClusters[m].Count)
                            .Select(m => finalClusterCenters[m])
                            .ToList();
                        geometriesPerVerticalGridCell[verticalGridCellKeys[j]]
                            = geometriesPerVerticalGridCell[verticalGridCellKeys[j]]
                                .Where(geometry => mainClustersCenters
                                    .Any(mainClustersCenter => {
                                        angle = mainClustersCenter.AngleTo(geometry.Item2);
                                        return angle.Abs() < 2 * VERTICAL_RESOLUTION
                                            || (angle - DEGREE_180).Abs() < 2 * VERTICAL_RESOLUTION;
                                    }))
                                .ToList();
#endif
                    }
                });
        }

        private static Vector3d GetAlignedUpAxis(
                Vector3d upAxis,
#if DEBUG_MODE
                Vector3d centroid,
#endif
#if DEBUG_MODE && EXPORT_CLUSTER_GEOMETRIES
                Vector3d horizontalAxis,
                IShape shape,
#endif
                double[,] verticalGrid,
                IReadOnlyList<double> weights,
                IReadOnlyList<Vector3d> normals,
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
                Dictionary<(int, int), List<(IFiniteGeometry, Vector3d)>> geometriesPerVerticalGridCell,
#endif
                Dictionary<(int, int), List<(Vector3d, double)>> weightedNormalsPerVerticalGridCell) {
            int da, a, a2, di, i, i2;
            int maxSumClusterIndex;
            double clusterSum;
            double threshold = double.MinValue;
            Vector3d alignedUpAxis;
            (int, int) candidate;
            bool[,] alreadyClustered = new bool[
                verticalGrid.GetLength(0),
                verticalGrid.GetLength(1)];
            Queue<(int, int)> candidates = new Queue<(int, int)>();
            List<double> clusterSums = new List<double>();
            List<(int, int)> gridCellsToCheckNeighbours;
            List<List<(int, int)>> clusters = new List<List<(int, int)>>();
            for (a = 0; a < verticalGrid.GetLength(0); a++) {
                for (i = 0; i < verticalGrid.GetLength(1); i++) {
                    if (verticalGrid[a, i] > threshold) {
                        threshold = verticalGrid[a, i];
                    }
                }
            }
            threshold *= VERTICAL_PEAK_RATIO;
            for (a = 0; a < verticalGrid.GetLength(0); a++) {
                for (i = 0; i < verticalGrid.GetLength(1); i++) {
                    if (alreadyClustered[a, i]
                            || verticalGrid[a, i] < threshold) {
                        continue;
                    }
                    clusterSum = 0.0;
                    List<(int, int)> cluster = new List<(int, int)>();
                    candidates.Enqueue((a, i));
                    do {
                        candidate = candidates.Dequeue();
                        if (alreadyClustered[
                                candidate.Item1,
                                candidate.Item2]) {
                            continue;
                        }
                        alreadyClustered[
                            candidate.Item1,
                            candidate.Item2] = true;
                        cluster.Add(candidate);
                        clusterSum += verticalGrid[
                            candidate.Item1,
                            candidate.Item2];
                        gridCellsToCheckNeighbours = new List<(int, int)> {
                            candidate
                        };
                        if (candidate.Item1 == 0
                                && candidate.Item2 == 0) {
                            for (a2 = 1; a2 < verticalGrid.GetLength(1); a2++) {
                                alreadyClustered[a2, 0] = true;
                                cluster.Add((a2, 0));
                                gridCellsToCheckNeighbours.Add((a2, 0));
                            }
                        }
                        foreach ((int, int) position in gridCellsToCheckNeighbours) {
                            for (da = -1; da <= 1; da++) {
                                for (di = -1; di <= 1; di++) {
                                    if (da == 0 && di == 0) {
                                        continue;
                                    }
                                    a2 = position.Item1 + da;
                                    i2 = position.Item2 + di;
                                    if (i2 < 0
                                            || i2 >= verticalGrid.GetLength(1)) {
                                        continue;
                                    }
                                    if (a2 == -1) {
                                        a2 = verticalGrid.GetLength(0) - 1;
                                    }
                                    if (a2 == verticalGrid.GetLength(0)) {
                                        a2 = 0;
                                    }
                                    if (!alreadyClustered[a2, i2]
                                            && verticalGrid[a2, i2] >= threshold) {
                                        candidates.Enqueue((a2, i2));
                                    }
                                }
                            }
                        }
                    } while (candidates.Count > 0);
                    clusters.Add(cluster);
                    clusterSums.Add(clusterSum);
                }
            }
            maxSumClusterIndex = Enumerable
                .Range(0, clusters.Count)
                .WhereMax(j => clusterSums[j])
                .First();
#if DEBUG_MODE
            verticalGrid.ExportPly(
                maxSumClusterIndex,
                "Test_Vertical",
                clusters);
#endif
#if DEBUG_MODE && EXPORT_CLUSTER_GEOMETRIES
            shape.ExportMainClusterGeometries(
                maxSumClusterIndex,
                upAxis,
                horizontalAxis,
                clusters);
#endif
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
            geometriesPerVerticalGridCell.Export(
                new (int, int)[] { 
                    (47, 22),
                    (67, 31)
                });
#endif
            alignedUpAxis = clusters[maxSumClusterIndex]
                .Where(gridPosition => weightedNormalsPerVerticalGridCell.ContainsKey(gridPosition))
                .SelectMany(gridPosition => weightedNormalsPerVerticalGridCell[gridPosition])
                .Select(weightedNormal => (
                    Vector3d.Dot(upAxis, weightedNormal.Item1) < 0.0 ? -weightedNormal.Item1 : weightedNormal.Item1,
                    weightedNormal.Item2
                ))
                .WeightedMean()
                .Normalized();
#if DEBUG_MODE

            PointCloud
                .GetLine(
                    centroid,
                    centroid + 10.0 * upAxis,
                    0.01)
                .ExportPointsPly(
                    $"{AlignmentEvaluation.DIRECTORY}/NewUpAxis.ply",
                    Color.Red);

            PointCloud
                .GetLine(
                    centroid,
                    centroid + 10.0 * alignedUpAxis,
                    0.01)
                .ExportPointsPly(
                    $"{AlignmentEvaluation.DIRECTORY}/NewUpAxis1.ply",
                    Color.Red);
#endif
#if DEBUG_MODE && EXPORT_VERTICAL_NORMALS
            using (PlyStreamWriter writer = new PlyStreamWriter(
                    $"{AlignmentEvaluation.DIRECTORY}/verticalNormals1.ply") {
                PointFormat = new ColoredPointFormat()
            }) {
                foreach (Vector3d normal in clusters[maxSumClusterIndex]
                        .Where(gridPosition => weightedNormalsPerVerticalGridCell.ContainsKey(gridPosition))
                        .SelectMany(gridPosition => weightedNormalsPerVerticalGridCell[gridPosition])
                        .Select(weightedNormal => weightedNormal.Item1)) {
                    foreach (Point point in PointCloud
                            .GetLine(
                                centroid,
                                centroid + 10.0 * normal,
                                0.1)) {
                        point.SetColor(Color.Yellow);
                        writer.Write(point);
                    }

                }
            }
#endif
            alignedUpAxis = Enumerable
                .Range(0, normals.Count)
                .AsParallel()
                .Where(j => {
                    if (double.IsNaN(weights[j])) {
                        return false;
                    }
                    double angle = normals[j].AngleTo(alignedUpAxis).Abs();
                    return angle < VERTICAL_AXIS_REFINEMENT_ANGLE_RADIUS
                        || (angle - DEGREE_180).Abs() < VERTICAL_AXIS_REFINEMENT_ANGLE_RADIUS;
                })
                .Select(j => (
                    Vector3d.Dot(upAxis, normals[j]) < 0.0 ? -normals[j] : normals[j],
                    weights[j]
                ))
                .WeightedMedian()
                .Normalized();
#if DEBUG_MODE
            PointCloud
                .GetLine(
                    centroid,
                    centroid + 10.0 * alignedUpAxis,
                    0.01)
                .ExportPointsPly(
                    $"{AlignmentEvaluation.DIRECTORY}/NewUpAxis2.ply",
                    Color.Red);
#endif
#if DEBUG_MODE && EXPORT_VERTICAL_NORMALS
            using (PlyStreamWriter writer = new PlyStreamWriter(
                    $"{AlignmentEvaluation.DIRECTORY}/verticalNormals2.ply") {
                PointFormat = new ColoredPointFormat()
            }) {
                foreach (Vector3d normal in Enumerable
                        .Range(0, normals.Count)
                        .Where(j => {
                            if (double.IsNaN(weights[j])) {
                                return false;
                            }
                            double angle = normals[j].AngleTo(alignedUpAxis).Abs();
                            return angle < VERTICAL_AXIS_REFINEMENT_ANGLE_RADIUS
                                || (angle - DEGREE_180).Abs() < VERTICAL_AXIS_REFINEMENT_ANGLE_RADIUS;
                        })
                        .Select(j => normals[j])) {
                    foreach (Point point in PointCloud
                            .GetLine(
                                centroid,
                                centroid + 10.0 * normal,
                                0.1)) {
                        point.SetColor(Color.Yellow);
                        writer.Write(point);
                    }

                }
            }
#endif
            return alignedUpAxis;
        }

        private static Matrix3d AlignHorizontally(
                this IShape shape,
                Vector3d centroid,
                Vector3d upAxis,
                Vector3d horizontalAxis,
                IReadOnlyList<double> weights) {
            Matrix3d rotation;
            List<(double, double)> weightedAngles = shape.GetWeightedAngles(
                upAxis,
                horizontalAxis,
                weights);
#if DEBUG_MODE && FULL_GRID_EXPORT
            object @lock = new object();
            double[] fullHorizontalGrid = new double[
                (int)(360.0.DegreeToRadian() / HORIZONTAL_RESOLUTION).Ceil()];
            IReadOnlyList<double> normalWeights = shape.GetSizeWeights();
            IReadOnlyList<Vector3d> normals = shape.GetNormals();
            Parallel.ForEach(
                Partitioner.Create(0, normals.Count),
                () => new double[fullHorizontalGrid.Length],
                (partition, loopState, localGrid) => {
                    int a;
                    double angle;
                    for (int j = partition.Item1; j < partition.Item2; j++) {
                        if (double.IsNaN(normals[j].X)
                                || double.IsNaN(normals[j].Y)
                                || double.IsNaN(normals[j].Z)
                                || double.IsNaN(weights[j])) {
                            continue;
                        }
                        angle = normals[j].AngleTo(upAxis);
                        if (angle < HORIZONTAL_ALIGNMENT_VERTICAL_ANGLE_MIN_THRESHOLD
                                || angle > HORIZONTAL_ALIGNMENT_VERTICAL_ANGLE_MAX_THRESHOLD) {
                            continue;
                        }
                        angle = normals[j]
                             .OrthogonalProject(upAxis)
                             .AngleTo(horizontalAxis, upAxis) + DEGREE_180;
                        a = (int)(angle / HORIZONTAL_RESOLUTION);
                        if (a == localGrid.Length) {
                            a = 0;
                        }
                        localGrid[a] += normalWeights[j];
                    }
                    return localGrid;
                },
                localGrid => {
                    lock (@lock) {
                        for (int j = 0; j < fullHorizontalGrid.Length; j++) {
                            fullHorizontalGrid[j] += localGrid[j];
                        }
                    }
                });
            fullHorizontalGrid.Export(
                0,
                "Test_Horizontal_Full",
                null);
#endif
            rotation = upAxis.GetRotationAround(
                CreateHorizontalGrid(weightedAngles)
                    .GetHorizontalAlignmentAngle(
#if DEBUG_MODE && EXPORT_CLUSTER_GEOMETRIES
                        shape,
                        upAxis,
                        horizontalAxis,
#endif
                        weightedAngles));
            shape.Rotate(
                new Rotation(rotation) {
                    UseParallel = true,
                    UpdateBBox = true,
                    Anchor = centroid,
                    RotateNormals = shape.Type == ShapeType.POINT_CLOUD
                });
            return rotation;
        }

        private static List<(double, double)> GetWeightedAngles(
                this IShape shape,
                Vector3d upAxis,
                Vector3d horizontalAxis,
                IReadOnlyList<double> weights) {
            IReadOnlyList<Vector3d> normals = shape.GetNormals();
            return Enumerable
                .Range(0, normals.Count)
                .AsParallel()
                .Where(j => {
                    if (double.IsNaN(normals[j].X)
                            || double.IsNaN(normals[j].Y)
                            || double.IsNaN(normals[j].Z)
                            || double.IsNaN(weights[j])) {
                        return false;
                    }
                    double angle = normals[j].AngleTo(upAxis).Abs();
                    return angle >= HORIZONTAL_ALIGNMENT_VERTICAL_ANGLE_MIN_THRESHOLD
                        && angle <= HORIZONTAL_ALIGNMENT_VERTICAL_ANGLE_MAX_THRESHOLD;
                })
                .Select(j => {
                    double angle = normals[j]
                         .OrthogonalProject(upAxis)
                         .AngleTo(horizontalAxis, upAxis);
                    if (angle < 0.0) {
                        angle += DEGREE_180;
                    }
                    if (angle > DEGREE_90) {
                        angle -= DEGREE_90;
                    }
                    return (angle, weights[j]);
                })
                .ToList();
        }

        private static double[] CreateHorizontalGrid(
                List<(double, double)> weightedAngles) {
            object @lock = new object();
            double[] horizontalGrid = new double[
                (int)(DEGREE_90 / HORIZONTAL_RESOLUTION).Ceil()];
            Parallel.ForEach(
                Partitioner.Create(0, weightedAngles.Count),
                () => new double[horizontalGrid.Length],
                (partition, loopState, localGrid) => {
                    int a;
                    for (int j = partition.Item1; j < partition.Item2; j++) {
                        a = (int)(weightedAngles[j].Item1 / HORIZONTAL_RESOLUTION);
                        if (a == localGrid.Length) {
                            a = 0;
                        }
                        localGrid[a] += weightedAngles[j].Item2;
                    }
                    return localGrid;
                },
                localGrid => {
                    lock (@lock) {
                        for (int j = 0; j < horizontalGrid.Length; j++) {
                            horizontalGrid[j] += localGrid[j];
                        }
                    }
                });
            return horizontalGrid;
        }

        private static double GetHorizontalAlignmentAngle(
                this double[] horizontalGrid,
#if DEBUG_MODE && EXPORT_CLUSTER_GEOMETRIES
                IShape shape,
                Vector3d upAxis,
                Vector3d horizontalAxis,
#endif
                List<(double, double)> weightedAngles) {
            int da, a2;
            int candidate;
            int maxSumClusterIndex;
            double clusterSum;
            double angle;
            double firstAngle;
            double threshold = HORIZONTAL_PEAK_RATIO * horizontalGrid.Max();
            bool[] alreadyClustered = new bool[horizontalGrid.Length];
            Queue<int> candidates = new Queue<int>();
            List<double> clusterSums = new List<double>();
            List<List<int>> clusters = new List<List<int>>();
            for (int a = 0; a < horizontalGrid.Length; a++) {
                if (alreadyClustered[a]
                        || horizontalGrid[a] < threshold) {
                    continue;
                }
                clusterSum = 0.0;
                List<int> cluster = new List<int>();
                candidates.Enqueue(a);
                do {
                    candidate = candidates.Dequeue();
                    if (alreadyClustered[candidate]) {
                        continue;
                    }
                    alreadyClustered[candidate] = true;
                    cluster.Add(candidate);
                    clusterSum += horizontalGrid[candidate];
                    for (da = -1; da <= 1; da += 2) {
                        a2 = candidate + da;
                        if (a2 == -1) {
                            a2 = horizontalGrid.Length - 1;
                        }
                        if (a2 == horizontalGrid.Length) {
                            a2 = 0;
                        }
                        if (!alreadyClustered[a2]
                                && horizontalGrid[a2] >= threshold) {
                            candidates.Enqueue(a2);
                        }
                    }
                } while (candidates.Count > 0);
                clusters.Add(cluster);
                clusterSums.Add(clusterSum);
            }
            maxSumClusterIndex = Enumerable
                .Range(0, clusters.Count)
                .WhereMax(j => clusterSums[j])
                .First();

            maxSumClusterIndex = 0;

#if DEBUG_MODE
            horizontalGrid.Export(
                maxSumClusterIndex,
                "Test_Horizontal",
                clusters);
#endif
#if DEBUG_MODE && EXPORT_CLUSTER_GEOMETRIES
            shape.ExportMainClusterGeometries(
                maxSumClusterIndex,
                upAxis,
                horizontalAxis,
                clusters);
#endif
            firstAngle = clusters[maxSumClusterIndex].First() * HORIZONTAL_RESOLUTION;
            angle = clusters[maxSumClusterIndex]
                .Select(a => {
                    double diff = a * HORIZONTAL_RESOLUTION - firstAngle;
                    if (diff.Abs() > DEGREE_45) {
                        if (diff < 0.0) {
                            diff += DEGREE_90;
                        }
                        else {
                            diff -= DEGREE_90;
                        }
                    }
                    return ((firstAngle + diff), horizontalGrid[a]);
                })
                .WeightedMean();
            angle = weightedAngles
                .AsParallel()
                .Where(a => {
                    double diff = (angle - a.Item1).Abs();
                    if (diff > DEGREE_45) {
                        diff -= DEGREE_90;
                    }
                    return diff.Abs() < HORIZONTAL_ANGLE_REFINEMENT_ANGLE_RADIUS;
                })
                .Select(a => {
                    double diff = a.Item1 - angle;
                    if (diff.Abs() > DEGREE_45) {
                        if (diff < 0) {
                            diff += DEGREE_90;
                        }
                        else {
                            diff -= DEGREE_90;
                        }
                    }
                    return (angle + diff, a.Item2);
                })
                .WeightedMedian();
            return angle;
        }

        public static void NormalizeAlignment(
                this IShape shape,
                Vector3d centroid,
                Vector3d upAxis,
                Vector3d horizontalAxis) {
            object @lock = new object();
            double size;
            (int, int) counters = (0, 0);
            (double, double) bounds;
            IReadOnlyList<Point> points;
            if (!((horizontalAxis.X.Abs().ApproximateEquals(1.0)
                            && horizontalAxis.Y.Abs().ApproximateEquals(0.0)
                            && horizontalAxis.Z.Abs().ApproximateEquals(0.0))
                        || (horizontalAxis.X.Abs().ApproximateEquals(0.0)
                            && horizontalAxis.Y.Abs().ApproximateEquals(1.0)
                            && horizontalAxis.Z.Abs().ApproximateEquals(0.0))
                        || (horizontalAxis.X.Abs().ApproximateEquals(0.0)
                            && horizontalAxis.Y.Abs().ApproximateEquals(0.0)
                            && horizontalAxis.Z.Abs().ApproximateEquals(1.0)))
                    || !((upAxis.X.Abs().ApproximateEquals(1.0)
                            && upAxis.Y.Abs().ApproximateEquals(0.0)
                            && upAxis.Z.Abs().ApproximateEquals(0.0))
                        || (upAxis.X.Abs().ApproximateEquals(0.0)
                            && upAxis.Y.Abs().ApproximateEquals(1.0)
                            && upAxis.Z.Abs().ApproximateEquals(0.0))
                        || (upAxis.X.Abs().ApproximateEquals(0.0)
                            && upAxis.Y.Abs().ApproximateEquals(0.0)
                            && upAxis.Z.Abs().ApproximateEquals(1.0)))) {
                return;
            }
            points = shape.GetPoints();
            size = Vector3d.Dot(
                shape.BBox.Size,
                horizontalAxis);
            if (size < Vector3d.Dot(
                    shape.BBox.Size,
                    Vector3d.Cross(upAxis, horizontalAxis))) {
                shape.Rotate(
                    new Rotation(DEGREE_90, upAxis) {
                        UseParallel = true,
                        UpdateBBox = true,
                        Anchor = centroid,
                        RotateNormals = shape.Type == ShapeType.POINT_CLOUD
                    });
            }
            bounds = (
                Vector3d.Dot(shape.BBox.Min, horizontalAxis) + UNAMBIGUOUS_ALIGN_SIZE_FRACTION * size,
                Vector3d.Dot(shape.BBox.Max, horizontalAxis) - UNAMBIGUOUS_ALIGN_SIZE_FRACTION * size);
            Parallel.ForEach(
                Partitioner.Create(0, points.Count),
                () => (0, 0),
                (partition, loopState, localCounters) => {
                    double value;
                    for (int i = partition.Item1; i < partition.Item2; i++) {
                        value = Vector3d.Dot(points[i].Position, horizontalAxis);
                        if (value <= bounds.Item1) {
                            localCounters.Item1++;
                        }
                        else if (value >= bounds.Item2) {
                            localCounters.Item2++;
                        }
                    }
                    return localCounters;
                },
                localCounters => {
                    lock (@lock) {
                        counters.Item1 += localCounters.Item1;
                        counters.Item2 += localCounters.Item2;
                    }
                });
            if (counters.Item1 > counters.Item2) {
                shape.Rotate(
                    new Rotation(DEGREE_180, upAxis) {
                        UseParallel = true,
                        UpdateBBox = true,
                        Anchor = centroid,
                        RotateNormals = shape.Type == ShapeType.POINT_CLOUD
                    });
            }
        }

#if DEBUG_MODE
        private static void Export(
                this double[] grid,
                int maxCountIndex,
                string fileName,
                List<List<int>> clusters) {
            float isCluster, isMaxCluster;
            double angle;
            string csvLine;
            Point point;
            PropertyDescriptor propertyDescriptor = new PropertyDescriptor()
                .AddFloatProperty("azimuth")
                .AddFloatProperty("count");
            if (clusters != null) {
                propertyDescriptor
                    .AddFloatProperty("isCluster")
                    .AddFloatProperty("isMaxCluster");
            }
            using (StreamWriter csvWriter = new StreamWriter(
                    $"{AlignmentEvaluation.DIRECTORY}/{fileName}.csv")) {
                csvLine = "azimuth;count";
                if (clusters != null) {
                    csvLine += ";isCluster;isMaxCluster";
                }
                csvWriter.WriteLine(csvLine);
                using (PlyStreamWriter plyWriter = new PlyStreamWriter(
                                $"{AlignmentEvaluation.DIRECTORY}/{fileName}.ply") {
                    PointFormat = new PointFormat() {
                        PropertyDescriptor = propertyDescriptor
                    }
                }) {
                    for (int j = 0; j < grid.Length; j++) {
                        angle = j * HORIZONTAL_RESOLUTION;
                        point = new Point(
                            angle.Cos(),
                            0.0,
                            angle.Sin());
                        if (clusters == null) {
                            angle -= DEGREE_180;
                        }
                        angle = angle.RadianToDegree();
                        csvLine = $"{angle};{grid[j]}";
                        point.SetFloatProperty("azimuth", (float)angle);
                        point.SetFloatProperty("count", (float)grid[j]);
                        if (clusters != null) {
                            isCluster = clusters.Any(cluster => cluster.Contains(j)) ? 1f : 0f;
                            isMaxCluster = clusters[maxCountIndex].Contains(j) ? 1f : 0f;
                            csvLine += $";{isCluster};{isMaxCluster}";
                            point.SetFloatProperty("isCluster", isCluster);
                            point.SetFloatProperty("isMaxCluster", isMaxCluster);
                        }
                        csvWriter.WriteLine(csvLine);
                        plyWriter.Write(point);
                    }
                }
            }
        }

        private static void ExportPly(
                this double[,] grid,
                int maxWeightClusterIndex,
                string fileName,
                List<List<(int, int)>> clusters) {
            int a, i;
            float isCluster;
            float isMaxCluster;
            double weight;
            double azimuth;
            double inclination;
            string csvLine;
            Point point;
            PropertyDescriptor propertyDescriptor = new PropertyDescriptor()
                .AddFloatProperty("azimuth")
                .AddFloatProperty("inclination")
                .AddFloatProperty("weight");
            if (clusters != null) {
                propertyDescriptor
                    .AddFloatProperty("isCluster")
                    .AddFloatProperty("isMaxCluster");
            }
            using (StreamWriter csvWriter = new StreamWriter(
                    $"{AlignmentEvaluation.DIRECTORY}/{fileName}.csv")) {
                csvLine = "azimuth;inclination;weight";
                if (clusters != null) {
                    csvLine += ";isCluster;isMaxCluster";
                }
                csvWriter.WriteLine(csvLine);
                using (PlyStreamWriter plyWriter = new PlyStreamWriter(
                        $"{AlignmentEvaluation.DIRECTORY}/{fileName}.ply") {
                    PointFormat = new PointFormat() {
                        PropertyDescriptor = propertyDescriptor
                    }
                }) {
                    for (a = 0; a < grid.GetLength(0); a++) {
                        for (i = 0; i < grid.GetLength(1); i++) {
                            if (i == 0 && a > 0) {
                                continue;
                            }
                            weight = grid[a, i];
                            azimuth = a * VERTICAL_RESOLUTION;
                            inclination = i * VERTICAL_RESOLUTION;
                            point = new Point(
                                inclination.Sin() * azimuth.Cos(),
                                inclination.Cos(),
                                inclination.Sin() * azimuth.Sin());
                            azimuth = azimuth.RadianToDegree();
                            inclination = inclination.RadianToDegree();
                            csvLine = $"{azimuth};{inclination};{weight}";
                            point.SetFloatProperty("azimuth", (float)azimuth);
                            point.SetFloatProperty("inclination", (float)inclination);
                            point.SetFloatProperty("weight", (float)weight);
                            if (clusters != null) {
                                isCluster = clusters.Any(cluster => cluster.Contains((a, i))) ? 1f : 0f;
                                isMaxCluster = clusters[maxWeightClusterIndex].Contains((a, i)) ? 1f : 0f;
                                csvLine += $";{isCluster};{isMaxCluster}";
                                point.SetFloatProperty("isCluster", isCluster);
                                point.SetFloatProperty("isMaxCluster", isMaxCluster);
                            }
                            csvWriter.WriteLine(csvLine);
                            plyWriter.Write(point);
                        }
                    }
                }
            }
        }

        private static void ExportFaces(
                this IShape pointCloud,
                int maxAzimuthGridSize,
                Vector3d upAxis,
                Vector3d horizontalAxis,
                Vector3d horizontalAxis2,
                HashSet<(int, int)> orientations) {
            int a, i;
            Dictionary<(int, int), PlyStreamWriter> writers = new Dictionary<(int, int), PlyStreamWriter>();
            foreach (Face face in pointCloud.Mesh) {
                if (double.IsNaN(face.Geometry.Normal.X)
                        || double.IsNaN(face.Geometry.Normal.Y)
                        || double.IsNaN(face.Geometry.Normal.Z)) {
                    continue;
                }
                double inclination = (Vector3d.Dot(
                        face.Geometry.Normal,
                        upAxis)
                    .Acos() - 90.0.DegreeToRadian()).Abs();
                double inclination2 = 90.0.DegreeToRadian() - inclination;
                if (double.IsNaN(inclination2)
                        || inclination2.Abs() > VERTICAL_ALIGNMENT_ANGLE_RADIUS) {
                    continue;
                }
                double azimuth = System.Math.Atan2(
                    Vector3d.Dot(face.Geometry.Normal, horizontalAxis),
                    Vector3d.Dot(face.Geometry.Normal, horizontalAxis2));
                double azimuth2 = (azimuth.Abs() - 90.0.DegreeToRadian()).Abs();
                a = (int)(azimuth2 / VERTICAL_RESOLUTION);
                i = (int)(inclination2 / VERTICAL_RESOLUTION);
                if (a == maxAzimuthGridSize) {
                    a = 0;
                }
                if (!orientations.Contains((a, i))) {
                    continue;
                }
                if (!writers.ContainsKey((a, i))) {
                    writers.Add(
                        (a, i),
                        new PlyStreamWriter($"{AlignmentEvaluation.DIRECTORY}/HorizontalFaces_({a},{i}).ply"));
                }
                writers[(a, i)].Write(face.Geometry);
            }
            foreach (PlyStreamWriter writer in writers.Values) {
                writer.Dispose();
            }
        }
#endif
#if DEBUG_MODE && EXPORT_CLUSTER_GEOMETRIES

        private static void ExportMainClusterGeometries(
                this IShape shape,
                int maxSumClusterIndex,
                Vector3d upAxis,
                Vector3d horizontalAxis,
                List<List<int>> clusters) {
            int a;
            double angle;
            Mesh mesh;
            Color color;
            HashSet<int> mainClusterAngleIndices = clusters[maxSumClusterIndex].ToHashSet();
            IReadOnlyList<Vector3d> normals = shape.GetNormals();
            IReadOnlyList<IFiniteGeometry> geometries = shape.GetGeometries();
            using (PlyStreamWriter writer = new PlyStreamWriter(
                    $"{AlignmentEvaluation.DIRECTORY}/Horizontal_MainClusterGeometries.ply") { 
                PointFormat = new ColoredPointFormat()
            }) {
                for (int j = 0; j < normals.Count; j++) {
                    if (double.IsNaN(normals[j].X)
                            || double.IsNaN(normals[j].Y)
                            || double.IsNaN(normals[j].Z)) {
                        continue;
                    }
                    angle = normals[j].AngleTo(upAxis);
                    if (angle < HORIZONTAL_ALIGNMENT_VERTICAL_ANGLE_MIN_THRESHOLD
                            || angle > HORIZONTAL_ALIGNMENT_VERTICAL_ANGLE_MAX_THRESHOLD) {
                        continue;
                    }
                    angle = normals[j]
                         .OrthogonalProject(upAxis)
                         .AngleTo(horizontalAxis, upAxis);
                    if (angle < 0.0) {
                        angle += DEGREE_180;
                    }
                    if (angle > DEGREE_90) {
                        angle -= DEGREE_90;
                    }
                    a = (int)(angle / HORIZONTAL_RESOLUTION);
                    color = mainClusterAngleIndices.Contains(a) ?
                        Color.Red :
                        Color.Gray;
                    mesh = geometries[j].Mesh;
                    foreach (Point point in mesh.Vertices) {
                        point.SetColor(color);
                    }
                    writer.Write(mesh);
                }
            }
        }

        private static void ExportMainClusterGeometries(
                this IShape shape,
                int maxSumClusterIndex,
                Vector3d upAxis,
                Vector3d horizontalAxis,
                List<List<(int, int)>> clusters) {
            int a, i;
            double azimuth, inclination;
            Mesh mesh;
            Color color;
            Vector3d horizontalAxis2 = Vector3d.Cross(upAxis, horizontalAxis);
            HashSet<(int, int)> mainClusterIndices = clusters[maxSumClusterIndex].ToHashSet();
            IReadOnlyList<Vector3d> normals = shape.GetNormals();
            IReadOnlyList<IFiniteGeometry> geometries = shape.GetGeometries();
            using (PlyStreamWriter writer = new PlyStreamWriter(
                    $"{AlignmentEvaluation.DIRECTORY}/Vertical_MainClusterGeometries.ply") {
                PointFormat = new ColoredPointFormat()
            }) {
                for (int j = 0; j < normals.Count; j++) {
                    if (double.IsNaN(normals[j].X)
                            || double.IsNaN(normals[j].Y)
                            || double.IsNaN(normals[j].Z)) {
                        continue;
                    }
                    inclination = DEGREE_90 - (Vector3d.Dot(normals[j], upAxis).Acos() - DEGREE_90).Abs();
                    if (double.IsNaN(inclination)
                            || inclination.Abs() > VERTICAL_ALIGNMENT_ANGLE_RADIUS) {
                        continue;
                    }
                    azimuth = (System.Math.Atan2(
                            Vector3d.Dot(normals[j], horizontalAxis),
                            Vector3d.Dot(normals[j], horizontalAxis2)).Abs()
                        - DEGREE_90).Abs();
                    a = (int)(azimuth / VERTICAL_RESOLUTION);
                    i = (int)(inclination / VERTICAL_RESOLUTION);
                    if (a == (int)(DEGREE_90 / VERTICAL_RESOLUTION).Ceil()) {
                        a = 0;
                    }
                    color = mainClusterIndices.Contains((a, i)) ?
                        Color.Red :
                        Color.Gray;
                    mesh = geometries[j].Mesh;
                    foreach (Point point in mesh.Vertices) {
                        point.SetColor(color);
                    }
                    writer.Write(mesh);
                }
            }
        }
#endif
#if DEBUG_MODE && LOG_GEOMETRIES_PER_VERTICAL_GRID_CELL
        private static void Export(
                this Dictionary<(int, int), List<(IFiniteGeometry, Vector3d)>> geometriesPerVerticalGridCell,
                IEnumerable<(int, int)> gridPositions) {
            foreach ((int, int) gridPosition in gridPositions) {
                geometriesPerVerticalGridCell[gridPosition]
                    .Select(geometry => geometry.Item1.Mesh)
                    .ExportMeshPly(
                        $"{AlignmentEvaluation.DIRECTORY}/Geometries_({gridPosition.Item1}, {gridPosition.Item2}).ply");
            }
        }
#endif
    }
}