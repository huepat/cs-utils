using HuePat.Util.Colors;
using HuePat.Util.IO;
using HuePat.Util.IO.PLY.Reading;
using HuePat.Util.IO.PLY.Writing;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.Processing.PoseTransformation;
using HuePat.Util.Math.Geometry.Processing.Properties;
using HuePat.Util.Object.Properties;
using System;
using System.IO;
using System.Linq;
using static HuePat.Util.Time.TimeUtils;

namespace HuePat.Util.Image.RGBD {
    public static class RGBD_WorkAround {
        public static void Export3D() {

            bool exportSingleFrames = true;
            double axisLength = 0.2;
            double pointDistance = 0.005;
            string inputDirectory = @"C:\Users\phuebner\data\tmp\orbslam_test";
            string outputDirectory = @"C:\Users\phuebner\data\tmp\orbslam_test\output";
            string framesDirectory = $"{outputDirectory}/frames";

            if (!Directory.Exists(inputDirectory)) {
                throw new ArgumentException();
            }

            if (Directory.Exists(outputDirectory)) {
                FileSystemUtils.CleanDirectory(outputDirectory);
            }

            Directory.CreateDirectory(outputDirectory);

            if (exportSingleFrames) {
                Directory.CreateDirectory(framesDirectory);
            }

            string[] pointCloudFrameFiles = LoadInputFilePaths(inputDirectory, "ply");
            string[] poseFiles = LoadInputFilePaths(inputDirectory, "txt");

            if (pointCloudFrameFiles.Length != poseFiles.Length) {
                throw new ArgumentException();
            }

            PlyReader reader = new PlyReader() {
                PointFormat = new ColoredPointFormat()
            };

            using (PlyStreamWriter 
                    posesPLYWriter = new PlyStreamWriter($"{outputDirectory}/poses.ply") {
                        PointFormat = new PointFormat() {
                            PropertyDescriptor = new PropertyDescriptor()
                                .AddColor()
                                .AddFloatProperty("t")
                        }
                    },
                    pointsPLYWriter = new PlyStreamWriter($"{outputDirectory}/points.ply") {
                        PointFormat = new ColoredPointFormat()
                    }) {

                using (StreamWriter posesTextWriter = new StreamWriter($"{outputDirectory}/poses.txt")) {

                    for (int i = 0; i < pointCloudFrameFiles.Length; i++) {

                        Timestamped<Pose> pose = ReadPose(poseFiles[i]);

                        PointCloud points = reader.ReadPointCloud(pointCloudFrameFiles[i]);

                        points.Transform(
                            pose.Element.Inverted());

                        PointCloud visualizationPoints = pose.Element.CreateVisualizationPointCloud(
                            axisLength,
                            pointDistance);

                        visualizationPoints.SetFloatProperty("t", (float)i);

                        visualizationPoints.PropagatePropertiesToPoints();

                        posesTextWriter.WriteLine(
                            $"{pose.Timestamp} " +
                            $"{pose.Element.X} {pose.Element.Y} {pose.Element.Z} " +
                            $"{pose.Element.Quaternion.X} {pose.Element.Quaternion.Y} {pose.Element.Quaternion.Z} {pose.Element.Quaternion.W}");

                        posesPLYWriter.Write(visualizationPoints);

                        pointsPLYWriter.Write(points);

                        if (exportSingleFrames) {

                            pose.Element.ExportPointsPly(
                                $"{framesDirectory}/pose_{i}.ply",
                                axisLength,
                                pointDistance);

                            points.ExportPointsPly(
                                $"{framesDirectory}/points_{i}.ply");
                        }
                    }
                }
            }
        }

        private static string[] LoadInputFilePaths(
                string inputDirectory,
                string fileExtension) {
            
            string[] inputFiles = Directory.GetFiles(inputDirectory, $"*.{fileExtension}");
            string[] result = new string[inputFiles.Length];

            foreach (string inputFile in inputFiles) {

                result[
                    int.Parse(Path
                        .GetFileNameWithoutExtension(inputFile)
                        .Split("_")[1])
                ] = inputFile;
            }

            return result;
        }

        private static Timestamped<Pose> ReadPose(
                string poseFile) {
            
            double[] values = File
                .ReadAllLines(poseFile)[0]
                .Split(" ")
                .Select(double.Parse)
                .ToArray();

            return new Timestamped<Pose>(
                double.Parse(Path
                    .GetFileNameWithoutExtension(poseFile)
                    .Split("_")[2]),
                new Pose(
                    values[0], values[1], values[2], values[3],
                    values[4], values[5], values[6], values[7],
                    values[8], values[9], values[10], values[11]));
        }
    }
}
