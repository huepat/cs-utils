using HuePat.Util.Colors;
using OpenCvSharp;
using OpenTK.Mathematics;
using System;
using static OpenCvSharp.Mat;

namespace HuePat.Util {
    public static class OpenCvExtensions {
        public static bool EqualsColor(
                this Vec3b value,
                Color color) {

            return value.Item0 == color.B
                && value.Item1 == color.G
                && value.Item2 == color.R;
        }

        public static float Max(
                this Vec3f vector) {

            return System.Math.Max(
                vector[0],
                System.Math.Max(
                    vector[1], 
                    vector[2]));
        }

        public static Color ToColor(
                this Vec3b value) {

            return new Color(
                value.Item2,
                value.Item1,
                value.Item0);
        }

        public static Vec3b ToOpenCV(
                this Color color) {

            return new Vec3b(
                color.B, 
                color.G, 
                color.R);
        }

        public static Vector3d ToOpenTKVector3d(
                this Vec3b value) {

            return new Vector3d(
                value[0] / 255.0,
                value[1] / 255.0,
                value[2] / 255.0);
        }

        public static Vec3b ToOpenCVVec3b(
                this Vector3d value) {

            return new Vec3b(
                (byte)(value[0] * 255.0),
                (byte)(value[1] * 255.0),
                (byte)(value[2] * 255.0));
        }

        public static Vector2d ToVectord(
                this Point2f point) {

            return new Vector2d(
                point.X, 
                point.Y);
        }

        public static Vector3d ToVectord(
                this Point3f point) {

            return new Vector3d(
                point.X, 
                point.Y, 
                point.Z);
        }

        public static Point3f ToPoint3f(
                this Vector3d vector) {

            return new Point3f(
                (float)vector.X, 
                (float)vector.Y, 
                (float)vector.Z);
        }

        public static Mat ToMat(
                this Point3f[] points) {

            return new Mat(
                points.Length, 
                1, 
                MatType.CV_32FC3, 
                points);
        }

        public static Mat ToMat(
                this Point2f[] points) {

            return new Mat(
                points.Length, 
                1, 
                MatType.CV_32FC2, 
                points);
        }

        public static double[] ToDoubleArray(
                this Mat matrix) {

            if (matrix.Channels() == 1 
                    && !(matrix.Width == 1 
                        && matrix.Height == 1)) {

                return SingleChannelLineMatrixToDoubleArray(matrix);
            }

            if (matrix.Channels() > 1 
                    && matrix.Width == 1 
                    && matrix.Height == 1) {

                return MultiChannelMatrixElementToDoubleArray(matrix);
            }

            throw new ArgumentException("Can't convert matrix.");
        }

        public static double[,] ToDoubleArray2D(
                this Mat matrix) {

            if (matrix.Channels() > 1) {
                throw new ArgumentException("Can't convert matrix.");
            }

            double[,] converted = new double[matrix.Rows, matrix.Cols];

            using (Mat<double> _matrix = new Mat<double>(matrix)) {

                MatIndexer<double> indexer = _matrix.GetIndexer();

                for (int r = 0; r < matrix.Rows; r++) {
                    for (int c = 0; c < matrix.Cols; c++) {

                        converted[r, c] = indexer[r, c];
                    }
                }
            }

            return converted;
        }

        private static double[] SingleChannelLineMatrixToDoubleArray(
                Mat matrix) {

            int i = 0;
            double[] converted = new double[matrix.Rows * matrix.Cols];

            using (Mat<double> _matrix = new Mat<double>(matrix)) {

                MatIndexer<double> indexer = _matrix.GetIndexer();

                for (int r = 0; r < matrix.Rows; r++) {
                    for (int c = 0; c < matrix.Cols; c++) {

                        converted[i] = indexer[r, c];
                        i++;
                    }
                }
            }

            return converted;
        }

        private static double[] MultiChannelMatrixElementToDoubleArray(
                Mat matrix) {

            switch (matrix.Channels()) {
                case 2:
                    return ToDoubleArray(matrix.Get<Vec2d>(0, 0));
                case 3:
                    return ToDoubleArray(matrix.Get<Vec3d>(0, 0));
                case 4:
                    return ToDoubleArray(matrix.Get<Vec4d>(0, 0));
                default:
                    throw new ArgumentException("Can't convert matrix.");
            }
        }

        private static double[] ToDoubleArray(
                Vec2d vector) {

            return new double[] {
                vector[0], vector[1]
            };
        }

        private static double[] ToDoubleArray(
                Vec3d vector) {

            return new double[] {
                vector[0], vector[1], vector[2]
            };
        }

        private static double[] ToDoubleArray(
                Vec4d vector) {

            return new double[] {
                vector[0], 
                vector[1], 
                vector[2], 
                vector[3]
            };
        }
    }
}