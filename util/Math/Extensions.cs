using OpenTK.Mathematics;
using System.Collections.Generic;

namespace HuePat.Util.Math {
    public static class Extensions {
        private const double EPSILON = 10E-5;

        public static bool HasSameSign(
                this double d1, 
                double d2) {

            return (d1 < 0.0 && d2 < 0.0)
                || (d1 >= 0.0 && d2 >= 0.0);
        }

        public static bool HasSameSign(
                this Vector3d vector,
                Vector3d otherVector) {

            return vector.X.HasSameSign(otherVector.X) &&
                 vector.Y.HasSameSign(otherVector.Y) &&
                 vector.Z.HasSameSign(otherVector.Z);
        }

        public static bool ApproximateEquals(
                this float d1,
                float d2,
                float epsilon = float.Epsilon) {

            return (d2 - d1).Abs() < epsilon;
        }

        public static bool ApproximateEquals(
                this double d1, 
                double d2, 
                double epsilon = EPSILON) {

            return (d2 - d1).Abs() < epsilon;
        }

        public static bool ApproximateEquals(
                this Vector3d vector,
                double x,
                double y,
                double z) {

            return vector.X.ApproximateEquals(x)
                && vector.Y.ApproximateEquals(y)
                && vector.Z.ApproximateEquals(z);
        }

        public static bool ApproximateEquals(
                this Vector3d vector,
                Vector3d otherVector) {

            return vector.X.ApproximateEquals(otherVector.X)
                && vector.Y.ApproximateEquals(otherVector.Y)
                && vector.Z.ApproximateEquals(otherVector.Z);
        }

        public static int Abs(
                this int value) {

            return System.Math.Abs(value);
        }

        public static double Abs(
                this double value) {

            return System.Math.Abs(value);
        }

        public static float Abs(
                this float value) {

            return System.Math.Abs(value);
        }

        public static Vector3d Abs(
                this Vector3d vector) {

            return new Vector3d(
                vector.X.Abs(),
                vector.Y.Abs(),
                vector.Z.Abs());
        }

        public static Matrix3d Abs(
                this Matrix3d matrix) {

            return new Matrix3d(
                matrix[0, 0].Abs(), matrix[0, 1].Abs(), matrix[0, 2].Abs(),
                matrix[1, 0].Abs(), matrix[1, 1].Abs(), matrix[1, 2].Abs(),
                matrix[2, 0].Abs(), matrix[2, 1].Abs(), matrix[2, 2].Abs());
        }

        public static float Round(
                this float value) {

            return (float)System.Math.Round(value);
        }

        public static double Round(
                this double value) {

            return System.Math.Round(value);
        }

        public static Vector3d Round(
                this Vector3d vector) {

            return new Vector3d(
                vector.X.Round(),
                vector.Y.Round(),
                vector.Z.Round());
        }

        public static double Ceil(
                this double value) {

            return System.Math.Ceiling(value);
        }

        public static Vector3d Ceil(
                this Vector3d vector) {

            return new Vector3d(
                vector.X.Ceil(),
                vector.Y.Ceil(),
                vector.Z.Ceil());
        }

        public static double Floor(
                this double value) {

            return System.Math.Floor(value);
        }

        public static Vector3d Floor(
                this Vector3d vector) {

            return new Vector3d(
                vector.X.Floor(),
                vector.Y.Floor(),
                vector.Z.Floor());
        }

        public static double Log10(
                this double value) {

            return System.Math.Log10(value);
        }

        public static float Sqrt(
                this float value) {

            return (float)System.Math.Sqrt(value);
        }

        public static double Sqrt(
                this double value) {

            return System.Math.Sqrt(value);
        }

        public static float Squared(
                this float value) {

            return value * value;
        }

        public static double Squared(
                this double value) {

            return value * value;
        }

        public static Vector3d Sqrt(
                this Vector3d vector) {

            return new Vector3d(
                vector.X.Sqrt(),
                vector.Y.Sqrt(),
                vector.Z.Sqrt());
        }

        public static float Pow(
                this float value,
                float exponent) {

            return (float)System.Math.Pow(
                value, 
                exponent);
        }

        public static double Pow(
                this double value,
                double exponent) {

            return System.Math.Pow(
                value, 
                exponent);
        }

        public static double Log(
                this double value) {

            return System.Math.Log(value);
        }

        public static float Sin(
                this float value) {

            return (float)System.Math.Sin(value);
        }

        public static double Sin(
                this double value) {

            return System.Math.Sin(value);
        }

        public static float ASin(
                this float value) {

            return (float)System.Math.Asin(value);
        }

        public static double ASin(
                this double value) {

            return System.Math.Asin(value);
        }

        public static float Cos(
                this float value) {

            return (float)System.Math.Cos(value);
        }

        public static double Cos(
                this double value) {

            return System.Math.Cos(value);
        }

        public static float Acos(
                this float value) {

            return (float)System.Math.Acos(value);
        }

        public static double Acos(
                this double value) {

            return System.Math.Acos(value);
        }

        public static float RadianToDegree(
                this float radian) {

            return radian * 180f / (float)System.Math.PI;
        }

        public static float DegreeToRadian(
                this float degree) {

            return degree * (float)System.Math.PI / 180f;
        }

        public static double DegreeToRadian(
                this double degree) {

            return degree * System.Math.PI / 180.0;
        }

        public static Vector3d DegreeToRadian(
                this Vector3d degree) {

            return new Vector3d(
                degree.X.RadianToDegree(),
                degree.Y.RadianToDegree(),
                degree.Z.RadianToDegree());
        }

        public static double RadianToDegree(
                this double radian) {

            return radian * 180.0 / System.Math.PI;
        }

        public static Vector3d RadianToDegree(
                this Vector3d radian) {

            return new Vector3d(
                radian.X.RadianToDegree(),
                radian.Y.RadianToDegree(),
                radian.Z.RadianToDegree());
        }

        public static double Max(
                this Vector3d vector) {

            return System.Math.Max(
                vector.X, 
                System.Math.Max(
                    vector.Y,
                    vector.Z));
        }

        public static void GetMinMax(
                this IEnumerable<(int, int)> values,
                out (int, int) min,
                out (int, int) max) {

            min = (int.MaxValue, int.MaxValue);
            max = (int.MinValue, int.MinValue);

            foreach ((int, int) value in values) {

                if (value.Item1 < min.Item1) {
                    min.Item1 = value.Item1;
                }
                if (value.Item2 < min.Item2) {
                    min.Item2 = value.Item2;
                }
                if (value.Item1 > max.Item1) {
                    max.Item1 = value.Item1;
                }
                if (value.Item2 > max.Item2) {
                    max.Item2 = value.Item2;
                }
            }
        }

        public static void GetMinMax(
                this IEnumerable<Vector3d> vectors,
                out Vector3d min,
                out Vector3d max) {

            min = new Vector3d(
                double.MaxValue,
                double.MaxValue,
                double.MaxValue);

            max = new Vector3d(
                double.MinValue,
                double.MinValue,
                double.MinValue);

            foreach (Vector3d vector in vectors) {

                if (vector.X < min.X) {
                    min.X = vector.X;
                }
                if (vector.X > max.X) {
                    max.X = vector.X;
                }
                if (vector.Y < min.Y) {
                    min.Y = vector.Y;
                }
                if (vector.Y > max.Y) {
                    max.Y = vector.Y;
                }
                if (vector.Z < min.Z) {
                    min.Z = vector.Z;
                }
                if (vector.Z > max.Z) {
                    max.Z = vector.Z;
                }
            }
        }

        public static Matrix3d OuterProduct(
                this Vector3d vector1,
                Vector3d vector2) {

            return new Matrix3d(
                vector1[0] * vector2[0], vector1[0] * vector2[1], vector1[0] * vector2[2],
                vector1[1] * vector2[0], vector1[1] * vector2[1], vector1[1] * vector2[2],
                vector1[2] * vector2[0], vector1[2] * vector2[1], vector1[2] * vector2[2]);
        }

        public static bool IsIdentity(
                this Matrix3d matrix) {

            return
                matrix[0, 0].ApproximateEquals(1, EPSILON) &&
                matrix[0, 1].ApproximateEquals(0, EPSILON) &&
                matrix[0, 2].ApproximateEquals(0, EPSILON) &&
                matrix[1, 0].ApproximateEquals(0, EPSILON) &&
                matrix[1, 1].ApproximateEquals(1, EPSILON) &&
                matrix[1, 2].ApproximateEquals(0, EPSILON) &&
                matrix[1, 0].ApproximateEquals(0, EPSILON) &&
                matrix[2, 1].ApproximateEquals(0, EPSILON) &&
                matrix[2, 2].ApproximateEquals(1, EPSILON);
        }

        public static Matrix2d Transposed(
                this Matrix2d matrix) {

            Matrix2d temp = matrix.Clone();

            temp.Transpose();

            return temp;
        }

        public static Matrix3d Multiply(
                this Matrix3d matrix,
                double factor) {

            return new Matrix3d(
                matrix[0, 0] * factor, matrix[0, 1] * factor, matrix[0, 2] * factor,
                matrix[1, 0] * factor, matrix[1, 1] * factor, matrix[1, 2] * factor,
                matrix[2, 0] * factor, matrix[2, 1] * factor, matrix[2, 2] * factor);
        }

        public static Vector3d Multiply(
                this Matrix3d matrix,
                Vector3d vector) {

            return new Vector3d(
                matrix[0, 0] * vector.X + matrix[0, 1] * vector.Y + matrix[0, 2] * vector.Z,
                matrix[1, 0] * vector.X + matrix[1, 1] * vector.Y + matrix[1, 2] * vector.Z,
                matrix[2, 0] * vector.X + matrix[2, 1] * vector.Y + matrix[2, 2] * vector.Z);
        }

        public static Matrix3d Transposed(
                this Matrix3d matrix) {

            Matrix3d temp = matrix.Clone();

            temp.Transpose();

            return temp;
        }
    }
}