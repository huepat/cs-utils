using HuePat.Util.Colors;
using HuePat.Util.IO;
using HuePat.Util.IO.PLY.Writing;
using HuePat.Util.Math;
using HuePat.Util.Math.Geometry;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuePat.Util.Photogrammetry {
    public static class ColmapTools {

        public static void ExportTrajectoryPly(
                string file,
                double axesLength,
                double pointDistance) {

            LoadTrajectory(file)
                .CreateVisualizationPointCloud(
                    axesLength, 
                    pointDistance)
                .ExportPointsPly(
                    FileSystemUtils.GetWithNewExtension(
                        file,
                        "ply"));
        }

        public static void ExportPointsPly(
                string file) {

            LoadPoints(file)
                .ExportPointsPly(
                    FileSystemUtils.GetWithNewExtension(
                        file,
                        "ply"));
        }

        public static List<Pose> LoadTrajectory(
                string imagesFile,
                string camerasFile = null) {

            int i = 0;
            string[] values;
            List<Pose> trajectory = new List<Pose>();

            foreach (string line in File.ReadLines(imagesFile)) {

                if (line.StartsWith("#")) {
                    continue;
                }

                i++;
                if (i % 2 == 0) {
                    continue;
                }

                values = line.Split(' ');

                Pose pose = new Pose() {
                    Position = new Vector3d(
                            double.Parse(values[5]),
                            double.Parse(values[6]),
                            double.Parse(values[7])),
                    Quaternion = new Quaterniond(
                            double.Parse(values[2]),
                            double.Parse(values[3]),
                            double.Parse(values[4]),
                            double.Parse(values[1]))
                };

                pose.SetCameraId(
                    int.Parse(values[8]));

                pose.SetImageFileName(values[9]);

                pose.Position = pose.OrientationMatrix.Transposed().Multiply(-1.0) * pose.Position;
                pose.OrientationMatrix = pose.OrientationMatrix.Inverted();

                trajectory.Add(pose);
            }

            if (camerasFile == null) {

                return trajectory;
            }

            Dictionary<int, InnerOrientation> innerOrientations = LoadInnerOrientations(camerasFile);

            foreach (Pose pose in trajectory) {

                int cameraId = pose.GetCameraId();

                if (!innerOrientations.ContainsKey(cameraId)) {

                    throw new ApplicationException(
                        $"No inner orientation found for pose '{pose.GetImageFileName()}'.");
                }

                pose.SetInnerOrientation(
                    innerOrientations[cameraId]);
            }

            return trajectory;
        }

        private static Dictionary<int, InnerOrientation> LoadInnerOrientations(
                string file) {

            Dictionary<int, InnerOrientation> innerOrientations = new Dictionary<int, InnerOrientation>();

            foreach (string line in File
                    .ReadLines(file)
                    .Where(line => !line.StartsWith("#"))
                    .Where(line => line.Length != 0)) {

                string[] values = line.Split(' ');

                int cameraId = int.Parse(values[0]);

                InnerOrientation innerOrientation = ParseInnerOrientation(values);

                innerOrientations.Add(
                    cameraId,
                    innerOrientation);
            }

            return innerOrientations;
        }

        private static InnerOrientation ParseInnerOrientation(
                string[] values) {

            if (values[1] == "SIMPLE_RADIAL") {

                return new InnerOrientation(
                        int.Parse(values[3]),
                        int.Parse(values[2]),
                        double.Parse(values[4]));
            }

            if (values[1] == "PINHOLE") {

                return new InnerOrientation(
                        int.Parse(values[3]),
                        int.Parse(values[2]),
                        double.Parse(values[4]),
                        double.Parse(values[5])) {

                    PrincipalPointX = double.Parse(values[6]),
                    PrincipalPointY = double.Parse(values[7]),
                };
            }

            if (values[1] == "OPENCV") {
                return new InnerOrientation(
                        int.Parse(values[3]),
                        int.Parse(values[2]),
                        double.Parse(values[4]),
                        double.Parse(values[5])) {

                    PrincipalPointX = double.Parse(values[6]),
                    PrincipalPointY = double.Parse(values[7]),
                    K1 = double.Parse(values[8]),
                    K2 = double.Parse(values[9]),
                    P1 = double.Parse(values[10]),
                    P2 = double.Parse(values[11])
                };
            }

            throw new ApplicationException("Unsupported camera format.");
        }

        private static PointCloud LoadPoints(
                string file) {

            string[] values;
            List<Point> points = new List<Point>();

            foreach (string line in File.ReadAllLines(file)) {

                if (line.StartsWith("#")) {
                    continue;
                }

                values = line.Split(' ');

                points.Add(
                    new Point(
                            double.Parse(values[1]),
                            double.Parse(values[2]),
                            double.Parse(values[3]))
                        .SetColor(
                            new Color(
                                byte.Parse(values[4]),
                                byte.Parse(values[5]),
                                byte.Parse(values[6]))));
            }

            return new PointCloud(points);
        }
    }
}