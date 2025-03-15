//#define DEBUG_NERF_EXPORT

using HuePat.Util.Colors;
using HuePat.Util.Image;
using HuePat.Util.IO;
using HuePat.Util.IO.PLY.Writing;
using HuePat.Util.Math;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Photogrammetry;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuePat.Util.Photogrammetry.IO_Old {
    public static class ColmapTools {
        public static void AddPositionOffsetToPoses(
                string imagesFile,
                string outputImagesFile,
                Vector3d offset) {

            string[] values;

            using (StreamWriter writer = new StreamWriter(outputImagesFile)) {
                foreach (string line in File.ReadLines(imagesFile)) {

                    values = line.Split(' ');
                    values[5] = (double.Parse(values[5]) + offset[0]).ToString();
                    values[6] = (double.Parse(values[6]) + offset[1]).ToString();
                    values[7] = (double.Parse(values[7]) + offset[2]).ToString();
                    writer.WriteLine(values.Join(" "));
                }
            }
        }

        public static void RearrangeIds(
                string imagesFile,
                string pointsFile,
                string outputDirectory) {

            int i = 0;
            int id;
            int newId = 1;
            string newTrack;
            string[] values;
            Dictionary<int, int> idMapping = new Dictionary<int, int>();

            if (Directory.Exists(outputDirectory)) {
                FileSystemUtils.CleanDirectory(outputDirectory);
            }
            else {
                Directory.CreateDirectory(outputDirectory);
            }

            using (StreamWriter writer = new StreamWriter($"{outputDirectory}/images.txt")) {

                foreach (string line in File.ReadLines(imagesFile)) {

                    if (line.StartsWith("#")) {

                        writer.WriteLine(line);
                        continue;
                    }

                    i++;

                    if (i % 2 == 0) {

                        writer.WriteLine(line);
                        continue;
                    }

                    values = line.Split(' ');

                    id = int.Parse(values[0]);

                    idMapping.Add(
                        id, 
                        newId);

                    writer.WriteLine(
                        $"{newId} {values.Skip(1).Join(" ")}");

                    newId++;
                }
            }

            using (StreamWriter writer = new StreamWriter($"{outputDirectory}/points3D.txt")) {

                foreach (string line in File.ReadLines(pointsFile)) {

                    if (line.StartsWith("#")) {

                        writer.WriteLine(line);
                        continue;
                    }

                    values = line.Split(' ');

                    newTrack = "";

                    for (i = 8; i < values.Length; i += 2) {

                        newTrack = $"{newTrack} {idMapping[int.Parse(values[i])]} {values[i + 1]}";
                    }

                    writer.WriteLine(
                        $"{values.Take(8).Join(" ")}{newTrack}");
                }
            }
        }

        public static List<int> GetImageIds(
                string imagesFile) {

            int i = 0;
            List<int> imageIds = new List<int>();

            foreach (string line in File.ReadLines(imagesFile)) {

                if (line.StartsWith("#")) {
                    continue;
                }

                i++;
                if (i % 2 == 0) {
                    continue;
                }

                imageIds.Add(
                    int.Parse(
                        line.Split(' ')[0]));
            }

            return imageIds;
        }

        public static void Visualize(
                string camerasFile,
                string imagesFile,
                string pointsFile,
                string outputDirectory,
                double axisLength = 1.0,
                double pointDistance = 0.002) {

            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory);
            }

            Import(
                camerasFile,
                imagesFile,
                pointsFile,
                out List<Pose> poses,
                out PointCloud points);

            poses.ExportPly(
                $"{outputDirectory}/poses.ply",
                1,
                axisLength,
                pointDistance);

            if (points != null) {
                new PlyWriter() {
                    PointFormat = new ColoredPointFormat()
                }.Write(
                    $"{outputDirectory}/points.ply",
                    points);
            }
        }

        public static void Import(
                string camerasFile,
                string imagesFile,
                string pointsFile,
                out List<Pose> poses,
                out PointCloud pointCloud) {

            poses = ReadPoses(imagesFile);

            if (camerasFile != null) {

                InnerOrientation innerOrientation = ReadCameraParameters(camerasFile);

                foreach (Pose pose in poses) {
                    pose.SetInnerOrientation(innerOrientation);
                }
            }

            if (pointsFile == null) {
                pointCloud = null;   
            }
            else {
                pointCloud = ReadPoints(pointsFile);
            }
        }

        public static void Export(
                string outputDirectory,
                string imageDirectory,
                List<Pose> poses,
                bool orderImagesDescending = false) {

            int height;
            int width;
            Vector3d position;
            Quaterniond quaternion;
            Matrix3d orientation;
            InnerOrientation innerOrientation;
            List<string> imageFileNames;

            if (!Directory.Exists(imageDirectory)) {
                throw new ArgumentException("Image directory does not exist.");
            }

            imageFileNames = ImageUtils.GetImageFileNames(
                imageDirectory,
                orderImagesDescending);

            if (poses.Count != imageFileNames.Count) {
                throw new ArgumentException(
                    $"Image count {imageFileNames.Count} does not match pose count {poses.Count}.");
            }

            using (StreamWriter
                    cameraWriter = new StreamWriter($"{outputDirectory}/cameras.txt"),
                    imageWriter = new StreamWriter($"{outputDirectory}/images.txt")) {

                for (int i = 0; i < poses.Count; i++) {

                    innerOrientation = poses[i].GetInnerOrientation();

                    ImageUtils.GetImageSize(
                        $"{imageDirectory}/{imageFileNames[i]}",
                        out height,
                        out width);

                    cameraWriter.WriteLine(
                        $"{i} RADIAL {width} {height} "
                            + $"{innerOrientation.FocalLengthX} {width / 2.0} {height / 2.0} "
                            + $"{innerOrientation.K1} {innerOrientation.K2}");

                    orientation = poses[i].OrientationMatrix.Transposed();
                    position = orientation.Multiply(-1.0) * poses[i].Position;
                    quaternion = Quaterniond.FromMatrix(orientation);

                    imageWriter.WriteLine(
                        $"{i} {quaternion.W} {quaternion.X} {quaternion.Y} {quaternion.Z} "
                            + $"{position.X} {position.Y} {position.Z} "
                            + $"{i} {imageFileNames[i]}");

                    imageWriter.WriteLine($" ");
                }
            }
        }

        public static InnerOrientation ReadCameraParameters(
                string camerasFile) {

            string[] lines;
            string[] values;

            lines = File
                .ReadLines(camerasFile)
                .Where(line => !line.StartsWith("#"))
                .Where(line => line.Length != 0)
                .ToArray();

            if (lines.Length > 1) {
                throw new ApplicationException(
                    "Currently, we expect to have exactly on camera for the whole scene.");
            }

            values = lines[0].Split(' ');

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

        public static List<Pose> ReadPoses(
                string file) {

            int i = 0;
            string[] values;
            List<Pose> poses = new List<Pose>();

            foreach (string line in File.ReadLines(file)) {

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

                pose.SetImageFileName(values[9]);

                pose.Position = pose.OrientationMatrix.Transposed().Multiply(-1.0) * pose.Position;
                pose.OrientationMatrix = pose.OrientationMatrix.Inverted();

                poses.Add(pose);
            }

            return poses;
        }

        public static PointCloud ReadPoints(
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