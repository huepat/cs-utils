using HuePat.Util.IO.PLY.Writing;
using HuePat.Util.IO;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.Projection;
using HuePat.Util.Math.Statistics;
using OpenCvSharp;
using System.IO;
using System.Linq;
using System;
using HuePat.Util.Image;
using System.Collections.Generic;
using OpenTK.Platform.Windows;

namespace HuePat.Util.Photogrammetry {
    public static class CameraCalibrationUtils {
        public static InnerOrientation CalibrateManually(
                string imageDirectory,
                Point3f[] points3D,
                out double reprojectionError) {

            if (!Directory.Exists(imageDirectory)) {
                throw new ArgumentException($"Directory {imageDirectory} does not exist.");
            }

            bool first = true;
            double[] distortion = new double[5];
            double[,] cameraMatrix = new double[3, 3];
            Size size = new Size(0, 0);
            List<Point2f[]> points2D = new List<Point2f[]>();

            //StreamWriter writer = new StreamWriter(@"C:\Users\phuebner\data\tmp\flir_registration\points_flir.txt");

            List<Point2f> points = File
                .ReadLines(@"C:\Users\phuebner\data\tmp\flir_registration\points_flir.txt")
                .Select(line => line.Split(", "))
                .Select(values => new Point2f(
                    float.Parse(values[0]),
                    float.Parse(values[1]))).ToList();

            int i = 0;

            foreach (string imageFile in Directory.EnumerateFiles(imageDirectory)) {

                using (Mat image = new Mat(imageFile)) {

                    if (first) {

                        cameraMatrix[0, 0] = 1.0;
                        cameraMatrix[1, 1] = 1.0;
                        cameraMatrix[0, 2] = image.Width / 2;
                        cameraMatrix[1, 2] = image.Height / 2;
                        cameraMatrix[2, 2] = 1.0;

                        size = image.Size();

                        first = false;
                    }

                    if (image.Height != size.Height
                            || image.Width != size.Width) {

                        throw new ArgumentException("All images need to have same size.");
                    }

                    Point2f[] p = new Point2f[points3D.Length];
                    for (int j = 0; j < points3D.Length; j++) {
                        p[j] = points[i * points3D.Length + j];
                    }

                    //var p = image.QueryPoints(points3D.Length);

                    //foreach (var p_ in p) {
                    //    writer.WriteLine($"{p_.X}, {p_.Y}");
                    //}

                    points2D.Add(p);
                }
            }

            //writer.Dispose();

            reprojectionError = Cv2.CalibrateCamera(
                Enumerable
                    .Range(0, points2D.Count)
                    .Select(i => points3D)
                    .ToArray(),
                points2D,
                size,
                cameraMatrix,
                distortion,
                out _,
                out _,
                CalibrationFlags.UseIntrinsicGuess, 
                new TermCriteria(CriteriaTypes.MaxIter, 1000, double.Epsilon));

            PerspectiveProjection projection = new PerspectiveProjection(size, cameraMatrix, distortion);

            return new InnerOrientation(projection);
        }

        public static PoseStatistics GetRelativeOrientationManually(
                string imageDirectory1,
                string imageDirectory2,
                InnerOrientation innerOrientation1,
                InnerOrientation innerOrientation2,
                Point3f[] points3D,
                string visualizationExportDirectory = null) {

            if (!Directory.Exists(imageDirectory1)) {
                throw new ArgumentException($"Directory {imageDirectory1} does not exist.");
            }
            if (!Directory.Exists(imageDirectory2)) {
                throw new ArgumentException($"Directory {imageDirectory2} does not exist.");
            }

            if (visualizationExportDirectory != null) {

                FileSystemUtils.CleanDirectory(visualizationExportDirectory);
                Directory.CreateDirectory(visualizationExportDirectory);

                points3D
                    .Select(point => point.ToVectord())
                    .ExportPointsPly($"{visualizationExportDirectory}/points.ply");
            }

            PerspectiveProjection projection1 = new PerspectiveProjection(innerOrientation1);
            PerspectiveProjection projection2 = new PerspectiveProjection(innerOrientation2);

            PnpParams pnpParams = new PnpParams(SolvePnPFlags.Iterative);

            PoseStatistics poseStatistics = new PoseStatistics();

            PlyStreamWriter pose1Writer = null;
            PlyStreamWriter pose2Writer = null;

            if (visualizationExportDirectory != null) {

                pose1Writer = new PlyStreamWriter(
                        $"{visualizationExportDirectory}/poses1.ply") {
                    PointFormat = new ColoredPointFormat()
                };

                pose2Writer = new PlyStreamWriter(
                        $"{visualizationExportDirectory}/poses2.ply") {
                    PointFormat = new ColoredPointFormat()
                };
            }

            foreach (string imageFile1 in Directory.EnumerateFiles(imageDirectory1)) {

                string imageFile2 = $"{imageDirectory2}/{Path.GetFileName(imageFile1)}";

                if (!File.Exists(imageFile2)) {
                    throw new ArgumentException($"File {imageFile2} does not exist.");
                }

                using (Mat
                        image1 = new Mat(imageFile1),
                        image2 = new Mat(imageFile2)) {

                    Point2f[] points1 = image1.QueryPoints(points3D.Length);
                    Point2f[] points2 = image2.QueryPoints(points3D.Length);

                    Pose pose1 = projection1.GetModelPoseInCameraFrame(points3D, points1, pnpParams);
                    Pose pose2 = projection2.GetModelPoseInCameraFrame(points3D, points2, pnpParams);

                    if (visualizationExportDirectory != null) {

                        pose1Writer.Write(
                            pose1.CreateVisualizationPointCloud(0.2, 0.005));

                        pose2Writer.Write(
                            pose2.CreateVisualizationPointCloud(0.2, 0.005));
                    }

                    poseStatistics.Update(
                        pose2 * pose1.Inverted());
                }
            }

            if (visualizationExportDirectory != null) {

                pose1Writer.Dispose();
                pose2Writer.Dispose();
            }

            return poseStatistics;
        }
    }
}
