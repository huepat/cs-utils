using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.Processing.Properties;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.IO.PLY.Writing {
    public static class Extensions {

        private const string INDEX_PROPERTY_KEY = "idx";

        public static void ExportPositionsPly(
                this IEnumerable<Pose> poses,
                string file,
                Color color) {

            poses
                .Select(pose => pose.Position)
                .ExportPointsPly(file, color);
        }

        public static void ExportPositionsPly(
                this IEnumerable<Pose> poses,
                string file) {

            poses
                .Select(pose => pose.Position)
                .ExportPointsPly(file);
        }

        public static void ExportPositionPly(
                this Pose pose,
                string file,
                Color color) {

            pose.Position.ExportPointPly(file, color);
        }

        public static void ExportPositionPly(
                this Pose pose,
                string file) {

            pose.Position.ExportPointPly(file);
        }

        public static void ExportPly(
                this IList<Pose> poses,
                string file,
                int everyXPose = 1,
                double axisLength = 1,
                double resolution = 0.001) {

            PointCloud points;

            using (PlyStreamWriter writer = new PlyStreamWriter(file) {
                PointFormat = new PointFormat() {
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddColor()
                        .AddIntegerProperty(INDEX_PROPERTY_KEY)
                }
            }) {
                for (int i = 0; i < poses.Count; i++) {

                    if (i % everyXPose != 0) {
                        continue;
                    }

                    points = poses[i].CreateVisualizationPointCloud(
                        axisLength,
                        resolution);

                    points.SetIntegerProperty(
                        INDEX_PROPERTY_KEY,
                        i);

                    points.PropagatePropertiesToPoints();

                    writer.Write(points);
                }
            }
        }

        public static void ExportPly(
                this Pose pose,
                string file,
                double axisLength = 1,
                double resolution = 0.001) {

            pose
                .GetPoints(axisLength, resolution)
                .ExportPointsPly(file);
        }

        public static void ExportPointPly(
                this Vector3d vector,
                string file,
                Color color) {

            new Point(vector)
                .ExportPointPly(file, color);
        }

        public static void ExportPointPly(
                this Vector3d vector,
                string file) {

            new Point(vector)
                .ExportPointPly(file);
        }

        public static void ExportPointsPly(
                this IEnumerable<Vector3d> vectors,
                string file,
                Color color) {

            vectors
                .Select(vector => new Point(vector))
                .ExportPointsPly(file, color);
        }

        public static void ExportPointsPly(
                this IEnumerable<Vector3d> vectors,
                string file) {

            vectors
                .Select(vector => new Point(vector)
                    .SetColor(Color.Gray))
                .ExportPointsPly(file);
        }

        public static void ExportPointPly(
                this Point point,
                string file,
                Color color) {

            point.SetColor(color);
            point.ExportPointPly(file);
        }

        public static void ExportPointPly(
                this Point point,
                string file) {

            new Point[] { point }.ExportPointsPly(file);
        }

        public static void ExportPointsPly(
                this Pose pose,
                string file,
                double axesLength,
                double pointDistance) {

            pose
                .CreateVisualizationPointCloud(axesLength, pointDistance)
                .ExportPointsPly(file);                
        }

        public static void ExportPointsPly(
                this IEnumerable<Point> points,
                string file,
                Color color) {

            new PlyWriter() { 
                PointFormat = new ColoredPointFormat()
            }.Write(
                file,
                new PointCloud(points)
                    .SetColor(color));
        }

        public static void ExportPointsPly(
                this IEnumerable<Point> points,
                string file) {

            new PlyWriter() { 
                PointFormat = new ColoredPointFormat()
            }.Write(
                file, 
                new PointCloud(points));
        }

        public static void ExportPointsPly(
                this IEnumerable<Pose> trajectory,
                string file,
                int everyXPose,
                double poseAxesLength,
                double posePointDistance) {

            trajectory
                .ThinOut(everyXPose)
                .SelectMany(pose => pose.CreateVisualizationPointCloud(
                    poseAxesLength,
                    posePointDistance))
                .ExportPointsPly(file);
        }

        public static void ExportMeshPly(
                this IEnumerable<IFiniteGeometry> geometries,
                string file,
                Color color) {

            Mesh
                .From(geometries)
                .ExportMeshPly(file, color);
        }

        public static void ExportMeshPly(
                this IEnumerable<IFiniteGeometry> geometries,
                string file) {

            Mesh
                .From(geometries)
                .ExportMeshPly(file);
        }

        public static void ExportMeshPly(
                this IFiniteGeometry geometry,
                string file,
                Color color) {

            geometry.Mesh.ExportMeshPly(file, color);
        }

        public static void ExportMeshPly(
                this IFiniteGeometry geometry,
                string file) {

            geometry.Mesh.ExportMeshPly(file);
        }

        public static void ExportMeshPly(
                this IEnumerable<Mesh> meshes,
                string file,
                Color color) {

            Mesh
                .From(meshes)
                .ExportMeshPly(file, color);
        }

        public static void ExportMeshPly(
                this IEnumerable<Mesh> meshes,
                string file) {

            Mesh
                .From(meshes)
                .ExportMeshPly(file);
        }

        public static void ExportMeshPly(
                this Mesh mesh,
                string file,
                Color color) {

            mesh.SetColor(color);

            new PlyWriter() {
                PointFormat = new PointFormat() {
                    PropertyDescriptor = new PropertyDescriptor().AddColor()
                }
            }.Write(file, mesh);
        }

        public static void ExportMeshPly(
                this Mesh mesh,
                string file) {

            new PlyWriter()
                .Write(file, mesh);
        }

        private static IEnumerable<Pose> ThinOut(
                this IEnumerable<Pose> poses,
                int everyXPose) {

            long poseCount = 0;

            foreach (Pose pose in poses) {

                if (poseCount++ % everyXPose == 0) {
                    yield return pose;
                }
            }
        }

        private static IEnumerable<Point> GetPoints(
                this Pose pose,
                double axisLength = 1,
                double resolution = 0.001) {

            for (double i = resolution; i < axisLength; i += resolution) {
                yield return new Point(pose * new Vector3d(i, 0, 0))
                    .SetColor(Color.Red);
            }

            for (double i = resolution; i < axisLength; i += resolution) {
                yield return new Point(pose * new Vector3d(0, i, 0))
                    .SetColor(Color.Green);
            }

            for (double i = resolution; i < axisLength; i += resolution) {
                yield return new Point(pose * new Vector3d(0, 0, i))
                    .SetColor(Color.Blue);
            }
        }
    }
}