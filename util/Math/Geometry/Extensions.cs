//#define DEBUG_BBOX_FITTING

using Accord.Math.Decompositions;
using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry.Raytracing;
#if DEBUG_BBOX_FITTING
using HuePat.Util.IO.PLY.Writing;
#endif
using OpenCvSharp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HuePat.Util.Math.Geometry {
    public static class Extensions {
        private const double EPSILON = 10E-5;
        private static readonly object LOCK = new object();

        public static Plane FitPlane(
                this IList<Vector3d> points) {

            Vector3d origin = new Vector3d(0, 0, 0);
            Vector3d center = points.GetCentroid();
            double[,] matrix = new double[points.Count, 3];

            for (int i = 0; i < points.Count; i++) {

                matrix[i, 0] = points[i].X - center.X;
                matrix[i, 1] = points[i].Y - center.Y;
                matrix[i, 2] = points[i].Z - center.Z;
            }

            SingularValueDecomposition svd = new SingularValueDecomposition(matrix);
            double[,] singularVectors = svd.RightSingularVectors;

            Vector3d normal = new Vector3d(
                singularVectors[0, 2],
                singularVectors[1, 2],
                singularVectors[2, 2]).Normalized();

            double distance = origin.DistanceTo(center);

            if (distance < origin.DistanceTo(center + distance / 2 * normal)) {
                normal = -1.0 * normal;
            }

            return new Plane(center, normal);
        }

        public static Vector3d GetCentroid(
                this IList<Vector3d> points,
                bool useParallel = false) {

            if (useParallel) {

                return points.GetCentroid_Parallel();
            }

            return points.GetCentroid_Sequential();
        }

        public static Vector3d GetCentroid(
                this IShape shape,
                bool useParallel = false) {

            if (useParallel) {

                return shape
                    .GetPoints()
                    .AsParallel()
                    .Select(point => point.Position)
                    .ToList()
                    .GetCentroid_Parallel();
            }

            return shape
                .GetPoints()
                .Select(point => point.Position)
                .ToList()
                .GetCentroid_Sequential();
        }

        public static Mesh ToMesh<T>(
                this IEnumerable<T> geometries) 
                    where T : IFiniteGeometry {

            MeshCreator creator = new MeshCreator();

            foreach (T geometry in geometries) {
                creator.Add(geometry.Mesh);
            }

            return creator.Create();
        }

        public static AABox GetBBox<T>(
                this IList<T> geometries) 
                    where T : IFiniteGeometry {

            return AABox.FromContainedGeometries(geometries);
        }

        public static Mesh Merge(
                this IEnumerable<Mesh> meshes) {

            return Mesh.From(meshes);
        }

        public static Mesh Merge(
                this IEnumerable<IFiniteGeometry> geometries) {

            return Mesh.From(geometries);
        }

        public static IEnumerable<AABox> Clip(
                this IEnumerable<AABox> boxes) {

            return boxes.Clip(
                (box1, box2) => box1.ClipOn(box2),
                (box1, box2) => box1.Overlaps(box2));
        }

        public static IEnumerable<AARectangle> Clip(
                this IEnumerable<AARectangle> rectangles) {

            return rectangles.Clip(
                (rectangle1, rectangle2) => rectangle1.ClipOn(rectangle2),
                (rectangle1, rectangle2) => rectangle1.Overlaps(rectangle2));
        }

        public static IEnumerable<AARectangle> ClipOn(
                this IEnumerable<AARectangle> rectangles,
                IEnumerable<AABox> boxes) {

            return rectangles.SelectMany(rectangle => {

                List<AARectangle> toClip = new List<AARectangle> { rectangle };
                List<AARectangle> result = new List<AARectangle>();

                foreach (AABox box in boxes) {

                    foreach (AARectangle rectangleToClip in toClip) {

                        if (rectangleToClip.Overlaps(box)) {

                            result.AddRange(rectangleToClip.ClipOn(box));
                        }
                        else {
                            result.Add(rectangleToClip);
                        }
                    }

                    toClip = new List<AARectangle>(result);
                    result = new List<AARectangle>();
                }

                return toClip;
            });
        }

        public static void GetMinMax<T>(
                this IEnumerable<T> geometries,
                out Vector3d min,
                out Vector3d max,
                bool useParallel = false) 
                    where T : IFiniteGeometry {

            if (useParallel) {
            
                geometries.GetMinMax_Parallel(
                    out min,
                    out max);
            }
            else {
                geometries.GetMinMax_Sequential(
                    out min,
                    out max);
            }
        }

        public static void GetMinMax(
                this IEnumerable<Vector3d> vectors,
                Dimension dimension,
                out double min,
                out double max) {

            min = double.MaxValue;
            max = double.MinValue;

            double value;

            foreach (Vector3d vector in vectors) {

                value = vector.Get(dimension);

                if (value < min) {
                    min = value;
                }

                if (value > max) {
                    max = value;
                }
            }
        }

        public static void GetMinMax(
                this IEnumerable<Vector3d> vectors,
                Dimension dimension1,
                Dimension dimension2,
                out Vector2d min,
                out Vector2d max) {

            min = new Vector2d(double.MaxValue, double.MaxValue);
            max = new Vector2d(double.MinValue, double.MinValue);

            double value;

            foreach (Vector3d vector in vectors) {

                value = vector.Get(dimension1);

                if (value < min.X) {
                    min.X = value;
                }

                if (value > max.X) {
                    max.X = value;
                }

                value = vector.Get(dimension2);

                if (value < min.Y) {
                    min.Y = value;
                }

                if (value > max.Y) {
                    max.Y = value;
                }
            }
        }

        public static void CenterAndScaleToAABox(
                this IList<Pose> poses,
                AABox targetBBox) {

            Vector3d scaling;
            AABox bBox;

            bBox = AABox.FromContainedGeometries(
                poses
                    .Select(pose => new Point(pose.Position))
                    .ToList(),
                true);

#if DEBUG_BBOX_FITTING
            string outputDirectory = @"C:\Users\phuebner\data\test";

            poses
                .Select(pose => new Point(pose.Position))
                .ToList()
                .ExportPointsPly(
                    $"{outputDirectory}/positions_org.ply",
                    Color.Black);

            bBox.ExportMeshPly(
                $"{outputDirectory}/bbox_org.ply",
                Color.Black);
#endif

            scaling = new Vector3d(
                targetBBox.Size.X / bBox.Size.X,
                targetBBox.Size.Y / bBox.Size.Y,
                targetBBox.Size.Z / bBox.Size.Z);

            scaling *= 4.0;

            Parallel.For(
                0,
                poses.Count,
                i => {
                    poses[i].Position = (poses[i].Position - bBox.Center) 
                        * scaling + targetBBox.Center;
                });

#if DEBUG_BBOX_FITTING
            poses
                .Select(pose => new Point(pose.Position))
                .ToList()
                .ExportPointsPly(
                    $"{outputDirectory}/positions_fitted.ply",
                    Color.Red);

            targetBBox.ExportMeshPly(
                $"{outputDirectory}/bbox_fitted.ply",
                Color.Red);
#endif
        }

        public static Vector3d RotationMatrixToRodriguesElements(
                this Matrix3d rotationMatrix) {

            Cv2.Rodrigues(
                rotationMatrix.To2DArray(),
                out double[] rodriguesElements,
                out _);

            return new Vector3d(
                rodriguesElements[0],
                rodriguesElements[1],
                rodriguesElements[2]);
        }

        public static Matrix3d RodriguesElementsToRotationMatrix(
                this Vector3d rodriguesElements) {

            Cv2.Rodrigues(
                rodriguesElements.ToArray(),
                out double[,] orientation,
                out _);

            return new Matrix3d(
                orientation[0, 0], orientation[0, 1], orientation[0, 2],
                orientation[1, 0], orientation[1, 1], orientation[1, 2],
                orientation[2, 0], orientation[2, 1], orientation[2, 2]);
        }

        public static Vector3d RotationMatrixToEulerAngles(
                this Matrix3d rotationMatrix) {

            return new Vector3d(
                System.Math.Atan2(
                    rotationMatrix[2, 1], 
                    rotationMatrix[2, 2]),
                System.Math.Atan2(
                    -rotationMatrix[2, 0],
                    (rotationMatrix[2, 1].Squared() + rotationMatrix[2, 2].Squared()).Sqrt()),
                System.Math.Atan2(
                    rotationMatrix[1, 0],
                    rotationMatrix[0, 0]));
        }

        public static Matrix3d EulerAnglesToRotationMatrix(
                this Vector3d eulerAngles) {

            double cPitch = eulerAngles.X.Cos();
            double sPitch = eulerAngles.X.Sin();
            double cYaw = eulerAngles.Y.Cos();
            double sYaw = eulerAngles.Y.Sin();
            double cRoll = eulerAngles.Z.Cos();
            double sRoll = eulerAngles.Z.Sin();

            return new Matrix3d(
                cYaw * cRoll, 
                    -cYaw * sRoll * cPitch + sYaw * sPitch, 
                    cYaw * sRoll * sPitch + sYaw * cPitch,
                sRoll, 
                    cRoll * cPitch, 
                    -cRoll * sPitch,
                -sYaw * cRoll, 
                    sYaw * sRoll * cPitch + cYaw * sPitch, 
                    -sYaw * sRoll * sPitch + cYaw * cPitch);
        }

        public static Matrix3d ToRotationMatrix(
                this Quaterniond quaternion) {

            quaternion = quaternion.Normalized();

            return new Matrix3d(
                1 - 2 * quaternion.Y.Squared() - 2 * quaternion.Z.Squared(),
                2 * quaternion.X * quaternion.Y - 2 * quaternion.Z * quaternion.W,
                2 * quaternion.X * quaternion.Z + 2 * quaternion.Y * quaternion.W,
                2 * quaternion.X * quaternion.Y + 2 * quaternion.Z * quaternion.W,
                1 - 2 * quaternion.X.Squared() - 2 * quaternion.Z.Squared(),
                2 * quaternion.Y * quaternion.Z - 2 * quaternion.X * quaternion.W,
                2 * quaternion.X * quaternion.Z - 2 * quaternion.Y * quaternion.W,
                2 * quaternion.Y * quaternion.Z + 2 * quaternion.X * quaternion.W,
                1 - 2 * quaternion.X.Squared() - 2 * quaternion.Y.Squared());
        }

        public static double AngleTo(
                this Vector2d vector1,
                Vector2d vector2) {

            return Vector2d.Dot(
                    vector1.Normalized(), 
                    vector2.Normalized())
                .Acos();
        }

        public static Vector3d GetOrthogonalVector(
                this Vector3d vector) {

            if (vector.ApproximateEquals(-1.0, 1.0, 0.0)) {
                return new Vector3d(-1.0);
            }

            return new Vector3d(
                vector.Z,
                vector.Z,
                -vector.X - vector.Y);
        }

        public static bool IsOrthogonalTo(
                this Vector3d vector,
                Vector3d otherVector,
                double epsilon = EPSILON) {

            return Vector3d.Dot(
                    vector, 
                    otherVector) 
                <= epsilon;
        }

        public static double DistanceTo(
                this Vector3d v1,
                Vector3d v2) {

            return (v2 - v1).Length;
        }

        public static double AngleTo(
                this Vector3d v1,
                Vector3d v2) {

            return Vector3d.CalculateAngle(v1, v2);
        }

        public static double AngleTo(
                this Vector3d v1,
                Vector3d v2,
                Vector3d axis) {

            return System.Math.Atan2(
                Vector3d.Dot(
                    axis,
                    Vector3d.Cross(v1, v2)),
                Vector3d.Dot(v1, v2));
        }

        public static double AngleTo(
                this Vector3d v1,
                Vector3d v2,
                out Vector3d axis) {

            axis = Vector3d.Cross(v1, v2).Normalized();

            return Vector3d.Dot(
                    v1.Normalized(), 
                    v2.Normalized())
                .Acos();
        }

        public static Vector3d RotateX(
                this Vector3d vector, 
                double angle) {

            return Matrix3d
                .CreateRotationX(angle)
                .Multiply(vector);
        }

        public static Vector3d RotateY(
                this Vector3d vector, 
                double angle) {

            return Matrix3d
                .CreateRotationY(angle)
                .Multiply(vector);
        }

        public static Vector3d RotateZ(
                this Vector3d vector, 
                double angle) {

            return Matrix3d
                .CreateRotationZ(angle)
                .Multiply(vector);
        }

        public static Vector3d RotateDirection(
                this Vector3d direction,
                Matrix3d rotation) {

            return rotation * direction;
        }

        public static Vector3d RotateCoordinate(
                this Vector3d coordinate,
                Matrix3d rotation) {

            return coordinate.RotateCoordinate(
                rotation,
                new Vector3d(0.0));
        }

        public static Vector3d RotateCoordinate(
                this Vector3d coordinate,
                Matrix3d rotation,
                Vector3d anchorPoint) {

            return rotation * (coordinate - anchorPoint) + anchorPoint;
        }

        public static Matrix3d GetRotation(
                this Matrix4d matrix) {

            return new Matrix3d(
                matrix[0, 0], matrix[0, 1], matrix[0, 2],
                matrix[1, 0], matrix[1, 1], matrix[1, 2],
                matrix[2, 0], matrix[2, 1], matrix[2, 2]);
        }

        public static Matrix3d RotationTo(
                this Vector3d v1, 
                Vector3d v2,
                bool invertAngle = false) {

            double angle = v1.AngleTo(
                v2, 
                out Vector3d axis);

            if (invertAngle) {
                angle *= -1;
            }

            return axis.GetRotationAround(angle);
        }

        public static Matrix3d GetRotationAround(
                this Vector3d axis, 
                double angle) {

            double c = angle.Cos();
            double s = angle.Sin();
            double t = 1.0 - c;

            Vector3d axisNormalized = axis.Normalized();

            double tmp1 = axisNormalized.X * axisNormalized.Y * t;
            double tmp2 = axisNormalized.Z * s;
            double tmp5 = axisNormalized.Y * axisNormalized.Z * t;
            double tmp6 = axisNormalized.X * s;
            double tmp3 = axisNormalized.X * axisNormalized.Z * t;
            double tmp4 = axisNormalized.Y * s;

            return new Matrix3d(
                c + axisNormalized.X * axisNormalized.X * t, tmp1 - tmp2, tmp3 + tmp4,
                tmp1 + tmp2, c + axisNormalized.Y * axisNormalized.Y * t, tmp5 - tmp6,
                tmp3 - tmp4, tmp5 + tmp6, c + axisNormalized.Z * axisNormalized.Z * t);
        }

        public static Vector3d OrthogonalProject(
                this Vector3d vector,
                Vector3d axis) {

            return vector - Vector3d.Dot(vector, axis) * axis;
        }

        // incl. mirrored rotation matrices (for left-hand coordinate frames)
        public static bool IsRotation(
                this Matrix3d matrix) {

            return (matrix * matrix.Transposed()).Abs().IsIdentity() 
                && matrix.Determinant.Abs().ApproximateEquals(1, EPSILON);
        }

        public static double Get(
                this Vector3d coordinate,
                Dimension dimension) {

            switch (dimension) {
                case Dimension.X:
                    return coordinate.X;
                case Dimension.Y:
                    return coordinate.Y;
                case Dimension.Z:
                    return coordinate.Z;
                default:
                    return 0.0;
            }
        }

        public static Vector3d CopySet(
                this Vector3d coordinate,
                Dimension dimension,
                double value) {

            switch (dimension) {
                case Dimension.X:
                    return new Vector3d(value, coordinate.Y, coordinate.Z);
                case Dimension.Y:
                    return new Vector3d(coordinate.X, value, coordinate.Z);
                case Dimension.Z:
                    return new Vector3d(coordinate.X, coordinate.Y, value);
                default:
                    return new Vector3d();
            }
        }

        public static Vector2d To2D(
                this Vector3d vector,
                Dimension thirdDimension) {

            switch (thirdDimension) {
                case Dimension.X:
                    return new Vector2d(vector.Y, vector.Z);
                case Dimension.Y:
                    return new Vector2d(vector.X, vector.Z);
                case Dimension.Z:
                    return new Vector2d(vector.X, vector.Y);
                default:
                    return new Vector2d();
            }
        }

        public static Vector3d To3D(
                this Vector2d vector,
                Dimension thirdDimension,
                double thirdDimensionCoordinate) {

            switch (thirdDimension) {
                case Dimension.X:
                    return new Vector3d(thirdDimensionCoordinate, vector[0], vector[1]);
                case Dimension.Y:
                    return new Vector3d(vector[0], thirdDimensionCoordinate, vector[1]);
                case Dimension.Z:
                    return new Vector3d(vector[0], vector[1], thirdDimensionCoordinate);
                default:
                    return new Vector3d();
            }
        }

        public static PointCloud CreateVisualizationPointCloud(
                this IEnumerable<Pose> trajectory,
                double axisLength,
                double pointDistance,
                int everyXPose = 1) {

            int poseIndex = 0;
            List<Point> points = new List<Point>();

            foreach (Pose pose in trajectory) {

                if (++poseIndex % everyXPose == 0) {

                    points.AddRange(
                        pose.CreateVisualizationPoints(
                            axisLength,
                            pointDistance));
                }
            }

            return new PointCloud(points);
        }

        public static PointCloud CreateVisualizationPointCloud(
                this Pose pose,
                double axisLength,
                double pointDistance) {

            List<Point> points = pose.CreateVisualizationPoints(
                axisLength,
                pointDistance);

            return new PointCloud(points);
        }

        public static PointCloud CreateVisualizationPointCloud(
                this Ray ray,
                double length,
                double pointDistance) {

            return ray.CreateVisualizationPointCloud(
                length, 
                pointDistance,
                Color.Gray);
        }

        public static PointCloud CreateVisualizationPointCloud(
                this Ray ray,
                double length,
                double pointDistance,
                Color color) {

            List<Point> points = new List<Point>();

            Vector3d direction = ray.Direction.Normalized();

            for (double d = pointDistance; d < length; d += pointDistance) {

                points.Add(
                    new Point(
                            ray.Origin + d * direction)
                        .SetColor(color));
            }

            return new PointCloud(points);
        }

        private static Vector3d GetCentroid_Parallel(
                this IList<Vector3d> points) {

            return points
                    .AsParallel()
                    .Aggregate((position1, position2) => position1 + position2)
                / points.Count;
        }

        private static Vector3d GetCentroid_Sequential(
                this IList<Vector3d> points) {

            long counter = 0;
            Vector3d centroid = new Vector3d(0.0);

            foreach (Vector3d point in points) {

                centroid += (point - centroid) / ++counter;
            }

            return centroid;
        }

        private static IEnumerable<T> Clip<T>(
                this IEnumerable<T> geometries,
                Func<T, T, IEnumerable<T>> clipCallback,
                Func<T, T, bool> overlapCallback) 
                    where T : IFiniteGeometry {

            List<T> toClip = geometries.ToList();
            List<T> result = new List<T>();
            bool didClip = false;
            int iStart = 0;

            do {

                if (didClip) {
                    toClip = new List<T>(result);
                    result = new List<T>();
                }

                didClip = false;

                for (int i = 0; i < iStart; i++) {
                    result.Add(toClip[i]);
                }

                for (int i = iStart; i < toClip.Count; i++) {
                    for (int j = 0; j < toClip.Count; j++) {

                        if (i == j) {
                            continue;
                        }

                        if (!overlapCallback(toClip[i], toClip[j])) {
                            continue;
                        }

                        IEnumerable<T> clipResult = clipCallback(toClip[i], toClip[j]);

                        result.AddRange(clipResult);

                        for (int k = iStart + 1; k < toClip.Count; k++) {
                            result.Add(toClip[k]);
                        }

                        iStart = i;
                        didClip = true;

                        break;
                    }

                    if (didClip) {
                        break;
                    }

                    result.Add(toClip[i]);

                    iStart++;
                }

            } while (didClip);

            return result;
        }

        private static void GetMinMax_Sequential<T>(
                this IEnumerable<T> geometries,
                out Vector3d min,
                out Vector3d max)
                    where T : IFiniteGeometry {

            Vector3d _min, _max;
            AABox bBox;
            min = new Vector3d(double.MaxValue);
            max = new Vector3d(double.MinValue);

            foreach (T geometry in geometries) {

                bBox = geometry.BBox;
                _min = bBox.Min;
                _max = bBox.Max;

                if (_min.X < min.X) {
                    min.X = _min.X;
                }
                if (_min.Y < min.Y) {
                    min.Y = _min.Y;
                }
                if (_min.Z < min.Z) {
                    min.Z = _min.Z;
                }
                if (_max.X > max.X) {
                    max.X = _max.X;
                }
                if (_max.Y > max.Y) {
                    max.Y = _max.Y;
                }
                if (_max.Z > max.Z) {
                    max.Z = _max.Z;
                }
            }
        }

        private static void GetMinMax_Parallel<T>(
                this IEnumerable<T> geometries,
                out Vector3d min,
                out Vector3d max)
                    where T : IFiniteGeometry {

            Vector3d _min = new Vector3d(double.MaxValue);
            Vector3d _max = new Vector3d(double.MinValue);

            Parallel.ForEach(
                geometries,
                () => (
                    new Vector3d(double.MaxValue),
                    new Vector3d(double.MinValue)),
                (geometry, loopState, localMinMax) => {

                    AABox bBox = geometry.BBox;
                    Vector3d _min2 = bBox.Min;
                    Vector3d _max2 = bBox.Max;

                    if (_min2.X < localMinMax.Item1.X) {
                        localMinMax.Item1.X = _min2.X;
                    }
                    if (_min2.Y < localMinMax.Item1.Y) {
                        localMinMax.Item1.Y = _min2.Y;
                    }
                    if (_min2.Z < localMinMax.Item1.Z) {
                        localMinMax.Item1.Z = _min2.Z;
                    }
                    if (_max2.X > localMinMax.Item2.X) {
                        localMinMax.Item2.X = _max2.X;
                    }
                    if (_max2.Y > localMinMax.Item2.Y) {
                        localMinMax.Item2.Y = _max2.Y;
                    }
                    if (_max2.Z > localMinMax.Item2.Z) {
                        localMinMax.Item2.Z = _max2.Z;
                    }

                    return localMinMax;
                },
                localMinMax => {

                    lock (LOCK) {
                        if (localMinMax.Item1.X < _min.X) {
                            _min.X = localMinMax.Item1.X;
                        }
                        if (localMinMax.Item1.Y < _min.Y) {
                            _min.Y = localMinMax.Item1.Y;
                        }
                        if (localMinMax.Item1.Z < _min.Z) {
                            _min.Z = localMinMax.Item1.Z;
                        }
                        if (localMinMax.Item2.X > _max.X) {
                            _max.X = localMinMax.Item2.X;
                        }
                        if (localMinMax.Item2.Y > _max.Y) {
                            _max.Y = localMinMax.Item2.Y;
                        }
                        if (localMinMax.Item2.Z > _max.Z) {
                            _max.Z = localMinMax.Item2.Z;
                        }
                    }
                });

            min = _min;
            max = _max;
        }

        private static List<Point> CreateVisualizationPoints(
                this Pose pose,
                double axisLength,
                double pointDistance) {

            List<Point> points = new List<Point> {
                new Point(pose.Position)
                    .SetColor(Color.Black)
            };

            for (double d = pointDistance; d < axisLength; d += pointDistance) {

                points.Add(
                    new Point(pose * new Vector3d(d, 0, 0))
                        .SetColor(Color.Red));

                points.Add(
                    new Point(pose * new Vector3d(0, d, 0))
                        .SetColor(Color.Green));

                points.Add(
                    new Point(pose * new Vector3d(0, 0, d))
                        .SetColor(Color.Blue));
            }

            return points;
        }
    }
}