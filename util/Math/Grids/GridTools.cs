using HuePat.Util.Colors;
using OpenCvSharp;
using OpenTK.Mathematics;
using System;

namespace HuePat.Util.Math.Grids {
    public static class GridTools {
        public static T[,,] Create<T>(
                Vector3i gridSize) {

            return new T[
                gridSize.X,
                gridSize.Y,
                gridSize.Z];
        }

        public static Vector3i GetGridSize(
                double resolution,
                Vector3d minCoordinate,
                Vector3d maxCoordinate) {

            return ((maxCoordinate - minCoordinate) / resolution)
                .Ceil()
                .ToIntegerVector() + new Vector3i(1);
        }

        public static Vector3i GetGridCoordinate(
                double resolution,
                Vector3d coordinate,
                Vector3d minCoordinate) {

            return ((1.0 / resolution * (coordinate - minCoordinate)) + new Vector3d(0.5))
                .ToVector3i();
        }

        public static void ExportSlices(
                string directory,
                Vector3i gridSize,
                Func<Vector3i, Color> colorizationCallback) {

            for (int x = 0; x < gridSize.X; x++) {

                using (Mat image = new Mat(
                        gridSize.Y,
                        gridSize.Z,
                        MatType.CV_8SC3)) {

                    using (Mat<Vec3b> _image = new Mat<Vec3b>(image)) {

                        MatIndexer<Vec3b> imageData = _image.GetIndexer();

                        for (int y = 0; y < gridSize.Y; y++) {
                            for (int z = 0; z < gridSize.Z; z++) {

                                imageData[y, z] = colorizationCallback(
                                        new Vector3i(x, y, z))
                                    .ToOpenCV();
                            }
                        }
                    }

                    image.ImWrite($"{directory}/x_{x}.tiff");
                }
            }

            for (int y = 0; y < gridSize.Y; y++) {

                using (Mat image = new Mat(
                        gridSize.X,
                        gridSize.Z,
                        MatType.CV_8SC3)) {

                    using (Mat<Vec3b> _image = new Mat<Vec3b>(image)) {

                        MatIndexer<Vec3b> imageData = _image.GetIndexer();

                        for (int x = 0; x < gridSize.X; x++) {
                            for (int z = 0; z < gridSize.Z; z++) {

                                imageData[x, z] = colorizationCallback(
                                        new Vector3i(x, y, z))
                                    .ToOpenCV();
                            }
                        }
                    }

                    image.ImWrite($"{directory}/y_{y}.tiff");
                }
            }

            for (int z = 0; z < gridSize.Z; z++) {

                using (Mat image = new Mat(
                        gridSize.X,
                        gridSize.Y,
                        MatType.CV_8SC3)) {

                    using (Mat<Vec3b> _image = new Mat<Vec3b>(image)) {

                        MatIndexer<Vec3b> imageData = _image.GetIndexer();

                        for (int x = 0; x < gridSize.X; x++) {
                            for (int y = 0; y < gridSize.Y; y++) {

                                imageData[x, y] = colorizationCallback(
                                        new Vector3i(x, y, z))
                                    .ToOpenCV();
                            }
                        }
                    }

                    image.ImWrite($"{directory}/z_{z}.tiff");
                }
            }
        }
    }
}
