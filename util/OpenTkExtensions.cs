using OpenTK.Mathematics;
using System;
using System.IO;

namespace HuePat.Util {
    public static class OpenTkExtensions {
        public static Vector3i ToVector3i(
                this Vector3d vector) {

            return new Vector3i(
                (int)vector.X,
                (int)vector.Y,
                (int)vector.Z);
        }

        public static Vector2d ToVector2d(
                this double[] values) {

            if (values.Length != 2) {
                throw new ArgumentException(
                    $"Number of values must be 2 but was {values.Length}.");
            }

            return new Vector2d(
                values[0], 
                values[1]);
        }

        public static Vector3d ToVector3d(
                this double[] values) {

            if (values.Length != 3) {
                throw new ArgumentException(
                    $"Number of values must be 3 but was {values.Length}.");
            }

            return new Vector3d(
                values[0],
                values[1],
                values[2]);
        }

        public static Vector3d ToVector3d(
                this Vector3i vector) {

            return new Vector3d(vector);
        }

        public static Matrix2d ToMatrix2d(
                this double[,] values) {

            if (values.GetLength(0) != 2
                    || values.GetLength(1) != 2) {
                throw new ArgumentException(
                    $"Dimension of values must be 2x2 but was {values.GetLength(0)}x{values.GetLength(1)}.");
            }

            return new Matrix2d(
                values[0, 0], values[0, 1],
                values[1, 0], values[1, 1]);
        }

        public static Matrix3d ToMatrix3d(
                this double[,] values) {

            if (values.GetLength(0) != 3
                    || values.GetLength(1) != 3) {

                throw new ArgumentException(
                    $"Dimension of values must be 3x3 but was {values.GetLength(0)}x{values.GetLength(1)}.");
            }

            return new Matrix3d(
                values[0, 0], values[0, 1], values[0, 2],
                values[1, 0], values[1, 1], values[1, 2],
                values[2, 0], values[2, 1], values[2, 2]);
        }

        public static Vector2d ToVector2d(
                this System.Drawing.Size size) {

            return new Vector2d(
                size.Width, 
                size.Height);
        }

        public static Vector3i ToIntegerVector(
                this Vector3d vector) {

            return new Vector3i(
                (int)vector.X,
                (int)vector.Y,
                (int)vector.Z);
        }

        public static Vector2 ToFloatVector(
                this Vector2d vector) {

            return new Vector2(
                (float)vector.X, 
                (float)vector.Y);
        }

        public static Vector3 ToFloatVector(
                this Vector3d vector) {

            return new Vector3(
                (float)vector.X,
                (float)vector.Y,
                (float)vector.Z);
        }

        public static Matrix4 ToFloatMatrix(
                this Matrix4d matrix) {

            return new Matrix4(
                (float)matrix[0, 0], (float)matrix[0, 1], (float)matrix[0, 2], (float)matrix[0, 3],
                (float)matrix[1, 0], (float)matrix[1, 1], (float)matrix[1, 2], (float)matrix[1, 3],
                (float)matrix[2, 0], (float)matrix[2, 1], (float)matrix[2, 2], (float)matrix[2, 3],
                (float)matrix[3, 0], (float)matrix[3, 1], (float)matrix[3, 2], (float)matrix[3, 3]);
        }

        public static Vector3d ToDoubleVector(
                this Vector3 vector) {

            return new Vector3d(
                vector.X, 
                vector.Y, 
                vector.Z);
        }

        public static double[] ToArray(
                this Vector3d vector) {

            return new double[] {
                vector.X, vector.Y, vector.Z
            };
        }

        public static double[,] To2DArray(
                this Matrix2d matrix) {

            return new double[,] {
                { matrix[0, 0], matrix[0, 1] },
                { matrix[1, 0], matrix[1, 1] }
            };
        }

        public static double[,] To2DArray(
                this Matrix3d matrix) {

            return new double[,] {
                { matrix[0, 0], matrix[0, 1], matrix[0, 2] },
                { matrix[1, 0], matrix[1, 1], matrix[1, 2] },
                { matrix[2, 0], matrix[2, 1], matrix[2, 2] }
            };
        }

        public static Matrix2d Clone(
                this Matrix2d matrix) {

            return new Matrix2d(
                matrix[0, 0], matrix[0, 1],
                matrix[1, 0], matrix[1, 1]);
        }

        public static Matrix3d Clone(
                this Matrix3d matrix) {

            return new Matrix3d(
                matrix[0, 0], matrix[0, 1], matrix[0, 2],
                matrix[1, 0], matrix[1, 1], matrix[1, 2],
                matrix[2, 0], matrix[2, 1], matrix[2, 2]);
        }

        public static void Write(
                this BinaryWriter writer,
                Vector3d vector) {

            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
        }
    }
}