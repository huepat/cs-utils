using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HuePat.Util.Math.Geometry.Processing.Voxelization {
    public static class Extension {
        private static readonly object LOCK = new object();

        public static bool[,,] GetOccupancyGrid<T>(
                this IEnumerable<T> geometries,
                double resolution,
                Vector3d? min,
                Vector3d? max,
                bool useParallel = false)
                    where T : IFiniteGeometry {

            bool[,,] grid = null;

            geometries.Voxelize(
                resolution,
                min,
                max,
                gridSize => {
                    grid = new bool[
                        gridSize.Item1,
                        gridSize.Item2,
                        gridSize.Item3];
                },
                (voxel, voxelGeometries) => {
                    if (voxelGeometries.Count > 0) {
                        grid[
                            voxel.Item1,
                            voxel.Item2,
                            voxel.Item3] = true;
                    }
                },
                useParallel);

            return grid;
        }

        public static void Voxelize<T>(
                this IEnumerable<T> geometries,
                double resolution,
                Vector3d? min,
                Vector3d? max,
                Action<(int, int, int)> gridInitializationCallback,
                Action<(int, int, int), List<T>> voxelCallback,
                bool useParallel = false,
                bool useParallelForPostProcessing = true) 
                    where T : IFiniteGeometry {

            (int, int, int) gridSize = geometries.GetGridSize(
                useParallel,
                resolution,
                min,
                max,
                out Vector3d offset);

            gridInitializationCallback(gridSize);

            if (useParallel) {
                geometries.Voxelize_Parallel(
                    resolution,
                    gridSize, 
                    offset,
                    voxelCallback,
                    useParallelForPostProcessing);
            }
            else {
                geometries.Voxelize_Sequential(
                    resolution,
                    gridSize,
                    offset,
                    voxelCallback);
            }
        }

        private static void Voxelize_Sequential<T>(
                this IEnumerable<T> geometries,
                double resolution,
                (int, int, int) gridSize,
                Vector3d offset,
                Action<(int, int, int), List<T>> voxelCallback)
                    where T : IFiniteGeometry {

            int x, y, z;
            (int, int, int) gridMin, gridMax;
            AABox bBox;
            Dictionary<(int, int, int), List<T>> voxelGeometries = new Dictionary<(int, int, int), List<T>>();

            foreach (T geometry in geometries) {

                bBox = geometry.BBox;
                gridMin = bBox.Min.ToGridCoordinate(resolution, gridSize, offset, true);
                gridMax = bBox.Max.ToGridCoordinate(resolution, gridSize, offset, false);

                for (x = gridMin.Item1; x <= gridMax.Item1; x++) {
                    for (y = gridMin.Item2; y <= gridMax.Item2; y++) {
                        for (z = gridMin.Item3; z <= gridMax.Item3; z++) {

                            if (geometry.Intersects(GetVoxelGeometry(x, y, z, resolution, offset))) {

                                voxelGeometries.BucketAdd(
                                    (x, y, z),
                                    geometry);
                            }
                        }
                    }
                }
            }

            foreach ((int, int, int) voxel in voxelGeometries.Keys) {
                voxelCallback(voxel, voxelGeometries[voxel]);
            }
        }

        private static void Voxelize_Parallel<T>(
                this IEnumerable<T> geometries,
                double resolution,
                (int, int, int) gridSize,
                Vector3d offset,
                Action<(int, int, int), List<T>> voxelCallback,
                bool useParallelForPostProcessing)
                    where T : IFiniteGeometry {

            Dictionary<(int, int, int), List<T>> voxelGeometries = new Dictionary<(int, int, int), List<T>>();

            Parallel.ForEach(
                geometries,
                () => new Dictionary<(int, int, int), List<T>>(),
                (geometry, loopState, localVoxelGeometries) => {

                    AABox bBox = geometry.BBox;
                    (int, int, int) gridMin = bBox.Min.ToGridCoordinate(resolution, gridSize, offset, true);
                    (int, int, int) gridMax = bBox.Max.ToGridCoordinate(resolution, gridSize, offset, false);

                    for (int x = gridMin.Item1; x <= gridMax.Item1; x++) {
                        for (int y = gridMin.Item2; y <= gridMax.Item2; y++) {
                            for (int z = gridMin.Item3; z <= gridMax.Item3; z++) {

                                if (geometry.Intersects(GetVoxelGeometry(x, y, z, resolution, offset))) {

                                    localVoxelGeometries.BucketAdd(
                                        (x, y, z),
                                        geometry);
                                }
                            }
                        }
                    }
                    return localVoxelGeometries;
                },
                localVoxelGeometries => {
                    lock (LOCK) {
                        voxelGeometries.BucketAdd(localVoxelGeometries);
                    }
                });

            if (useParallelForPostProcessing) {

                Parallel.ForEach(
                    voxelGeometries.Keys,
                    voxel => voxelCallback(
                        voxel,
                        voxelGeometries[voxel]));
            }
            else {
                foreach ((int, int, int) voxel in voxelGeometries.Keys) {
                    voxelCallback(voxel, voxelGeometries[voxel]);
                }
            }
        }

        private static AABox GetVoxelGeometry(
                int x,
                int y,
                int z,
                double resolution,
                Vector3d offset) {

            return AABox.FromCenterAndSize(
                new Vector3d(
                    offset.X + y * resolution,
                    offset.Y + x * resolution,
                    offset.Z + z * resolution),
                new Vector3d(resolution));
        }

        private static (int, int, int) ToGridCoordinate(
                this Vector3d position,
                double resolution,
                (int, int, int) gridSize,
                Vector3d offset,
                bool isMin) {

            (int, int, int) result;

            double[] gridCoordinate = new double[] {
                (position.Y - offset.Y) / resolution,
                (position.X - offset.X) / resolution,
                (position.Z - offset.Z) / resolution
            };

            if (isMin) {
                result = (
                    (int)gridCoordinate[0].Floor(),
                    (int)gridCoordinate[1].Floor(),
                    (int)gridCoordinate[2].Floor());
            }
            else {
                result = (
                    (int)gridCoordinate[0].Ceil(),
                    (int)gridCoordinate[1].Ceil(),
                    (int)gridCoordinate[2].Ceil());
            }

            if (result.Item1 < 0) {
                result.Item1++;
            }
            if (result.Item1 >= gridSize.Item1) {
                result.Item1--;
            }
            if (result.Item2 < 0) {
                result.Item2++;
            }
            if (result.Item2 >= gridSize.Item2) {
                result.Item2--;
            }
            if (result.Item3 < 0) {
                result.Item3++;
            }
            if (result.Item3 >= gridSize.Item3) {
                result.Item3--;
            }

            return result;
        }

        private static (int, int, int) GetGridSize<T>(
                this IEnumerable<T> geometries,
                bool useParallel,
                double resolution,
                Vector3d? min,
                Vector3d? max,
                out Vector3d offset)
                    where T : IFiniteGeometry {

            Vector3d _min, _max;

            if (min.HasValue && max.HasValue) {

                _min = min.Value;
                _max = max.Value;
            }
            else {

                geometries.GetMinMax(
                    out Vector3d _min2,
                    out Vector3d _max2,
                    useParallel);

                _min = min.HasValue ? min.Value : _min2;
                _max = max.HasValue ? max.Value : _max2;
            }

            _min -= new Vector3d(resolution);
            _max += new Vector3d(resolution);

            offset = _min;

            (int, int, int) size = (
                (int)((_max.Y - _min.Y) / resolution).Ceil(),
                (int)((_max.X - _min.X) / resolution).Ceil(),
                (int)((_max.Z - _min.Z) / resolution).Ceil());

            if (size.Item1 == 0) {
                size.Item1 = 1;
            }
            if (size.Item2 == 0) {
                size.Item2 = 1;
            }
            if (size.Item3 == 0) {
                size.Item3 = 1;
            }

            return size;
        }
    }
}