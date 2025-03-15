using HuePat.Util.Colors;
using HuePat.Util.Image;
using HuePat.Util.IO;
using HuePat.Util.IO.PLY.Writing;
using HuePat.Util.Math;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Object;
using HuePat.Util.Object.Properties;
using HuePat.Util.Photogrammetry;
using OpenCvSharp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Point = HuePat.Util.Math.Geometry.Point;

namespace HuePat.Util.Photogrammetry.IO_Old
{
    public static class BundlerTools {
        private const string POSE_INDICES_PROPERTY_KEY = "pose_indices";

        public class PoseIndices : List<int>, IProperty {

            public PoseIndices(IEnumerable<int> poseIndices) :
                    base(poseIndices) {
            }

            public IProperty Clone() {
                return new PoseIndices(this);
            }
        }

        public static PoseIndices GetPoseIndices(
                this Point point) {

            return point.GetProperty(POSE_INDICES_PROPERTY_KEY) as PoseIndices;
        }

        public static Point SetPoseIndices(
                this Point point,
                PoseIndices poseIndices) {

            point.SetProperty(
                POSE_INDICES_PROPERTY_KEY,
                poseIndices);

            return point;
        }

        public static void Visualize(
                string bundlerFile,
                string outputDirectory,
                string imageDirectory,
                double axisLength = 1.0,
                double pointDistance = 0.002) {

            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory);
            }

            Import(
                bundlerFile,
                imageDirectory,
                out List<Pose> poses,
                out PointCloud pointCloud);

            poses.ExportPly(
                $"{outputDirectory}/poses.ply",
                1,
                axisLength,
                pointDistance);

            new PlyWriter() {
                PointFormat = new ColoredPointFormat()
            }.Write(
                $"{outputDirectory}/points.ply",
                pointCloud);
        }

        public static void Export(
                string outputFile,
                List<Pose> poses,
                PointCloud pointCloud) {

            Color color;
            Vector3d position;
            Matrix3d orientation;
            InnerOrientation innerOrientation;

            using (StreamWriter writer = new StreamWriter(outputFile)) {

                writer.WriteLine("# Bundle file v0.3");
                writer.WriteLine($"{poses.Count} {pointCloud.Count}");

                foreach (Pose pose in poses) {

                    innerOrientation = pose.GetInnerOrientation();
                    orientation = pose.OrientationMatrix.Transposed();
                    position = orientation.Multiply(-1.0) * pose.Position;

                    writer.WriteLine($"{innerOrientation.FocalLength} {innerOrientation.K1} {innerOrientation.K2}");
                    writer.WriteLine($"{orientation[0, 0]} {orientation[0, 1]} {orientation[0, 2]}");
                    writer.WriteLine($"{orientation[1, 0]} {orientation[1, 1]} {orientation[1, 2]}");
                    writer.WriteLine($"{orientation[2, 0]} {orientation[2, 1]} {orientation[2, 2]}");
                    writer.WriteLine($"{position[0]} {position[1]} {position[2]}");
                }

                foreach (Point point in pointCloud) {

                    color = point.GetColor();

                    writer.WriteLine($"{point.X} {point.Y} {point.Z}");
                    writer.WriteLine($"{color.R} {color.G} {color.B}");
                    writer.WriteLine($"");
                }
            }
        }

        public static void ExportColmap(
                string bundlerFile,
                string imageDirectory,
                string outputDirectory,
                bool orderImagesDescending = false) {

            Import(
                bundlerFile,
                imageDirectory,
                out List<Pose> poses,
                out _);

            ColmapTools.Export(
                outputDirectory,
                imageDirectory,
                poses,
                orderImagesDescending);
        }

        public static void Import(
                string bundlerFile,
                string imageDirectory,
                out List<Pose> poses,
                out PointCloud pointCloud,
                bool orderImagesDescending = false,
                bool checkImageSizeConsistency = false) {

            bool isReadingCounts = true;
            bool isReadingPoses = false;
            bool isReadingCameraParameters = false;
            bool isReadingOrientation = false;
            bool isReadingPosition = false;
            bool isReadingPoints = false;
            bool isReadingColor = false;
            bool isReadingPoseIndices = false;
            int poseIndex = 0;
            int poseCount = 0;
            int pointCount;
            int orientationLineIndex = 0;
            int height;
            int width;
            double focalLength = 0f;
            double k1 = 0f;
            double k2 = 0f;
            string[] values;
            Color color = null;
            Vector3d position = new Vector3d();
            Matrix3d orientation = new Matrix3d();
            List<Point> points = new List<Point>();
            List<string> imageFileNames;
            Dictionary<(int, int), List<string>> imageSizes;

            poses = new List<Pose>();

            if (!Directory.Exists(imageDirectory)) {
                throw new ArgumentException("Image directory does not exist.");
            }

            if (checkImageSizeConsistency) {

                imageSizes = ImageUtils.CheckImageSizes(imageDirectory);

                if (imageSizes.Count > 1) {
                    throw new ArgumentException("Images have different sizes.");
                }
            }

            imageFileNames = ImageUtils.GetImageFileNames(
                imageDirectory,
                orderImagesDescending);

            foreach (string line in File.ReadAllLines(bundlerFile)) {

                if (line.StartsWith('#')) {
                    continue;
                }

                if (isReadingCounts) {

                    values = line.Split(' ');
                    poseCount = int.Parse(values[0]);
                    pointCount = int.Parse(values[1]);
                    isReadingCounts = false;
                    isReadingPoses = true;
                    isReadingCameraParameters = true;

                    if (poseCount != imageFileNames.Count) {
                        throw new ArgumentException(
                            $"Image count {imageFileNames.Count} does not match pose count {poseCount}.");
                    }
                }
                else if (isReadingPoses) {

                    if (isReadingCameraParameters) {

                        values = line.Split(' ');
                        focalLength = double.Parse(values[0]);
                        k1 = double.Parse(values[1]);
                        k2 = double.Parse(values[2]);
                        isReadingCameraParameters = false;
                        isReadingOrientation = true;
                        orientationLineIndex = 0;
                        orientation = new Matrix3d();
                    }
                    else if (isReadingOrientation) {

                        values = line.Split(' ');
                        orientation[orientationLineIndex, 0] = double.Parse(values[0]);
                        orientation[orientationLineIndex, 1] = double.Parse(values[1]);
                        orientation[orientationLineIndex, 2] = double.Parse(values[2]);

                        if (orientationLineIndex == 2) {

                            orientation.Transpose();
                            isReadingOrientation = false;
                            isReadingPosition = true;
                        }
                        else {
                            orientationLineIndex++;
                        }
                    }
                    else if (isReadingPosition) {

                        values = line.Split(' ');

                        position = new Vector3d(
                            double.Parse(values[0]),
                            double.Parse(values[1]),
                            double.Parse(values[2]));

                        position = orientation.Multiply(-1.0) * position;

                        ImageUtils.GetImageSize(
                            $"{imageDirectory}/{imageFileNames[poseIndex]}",
                            out height,
                            out width);

                        poses.Add(
                            new Pose() { 
                                OrientationMatrix = orientation,
                                Position = position
                            }.SetInnerOrientation(
                                new InnerOrientation(
                                        width,
                                        height,
                                        focalLength) { 
                                    K1 = k1,
                                    K2 = k2
                                })
                            .SetImageFileName(imageFileNames[poseIndex]));

                        poseIndex++;

                        if (poses.Count == poseCount) {

                            isReadingPoses = false;
                            isReadingPoints = true;
                        }
                        else {
                            isReadingPosition = false;
                            isReadingCameraParameters = true;
                        }
                    }
                }
                else if (isReadingPoints) {

                    if (isReadingPosition) {

                        values = line.Split(' ');
                        position = new Vector3d(
                            double.Parse(values[0]),
                            double.Parse(values[1]),
                            double.Parse(values[2]));
                        isReadingPosition = false;
                        isReadingColor = true;
                    }
                    else if (isReadingColor) {

                        values = line.Split(' ');
                        color = new Color(
                            byte.Parse(values[0]),
                            byte.Parse(values[1]),
                            byte.Parse(values[2]));
                        isReadingColor = false;
                        isReadingPoseIndices = true;
                    }
                    else if (isReadingPoseIndices) {

                        points.Add(
                            new Point(position)
                                .SetColor(color)
                                .SetPoseIndices(
                                    new PoseIndices(
                                        ParsePoseIndices(line))));
                        isReadingPoseIndices = false;
                        isReadingPosition = true;
                    }
                }
            }

            pointCloud = new PointCloud(points);
        }

        public static void EnforceImageSizeConsistency(
                string bundlerFile,
                string imageDirectory,
                bool orderImagesDescending = false,
                double? downsampleFactor = null) {

            int poseIndex;
            (int, int) minSize;
            (int, int) minPosition;
            (int, int) maxPosition;
            (int, int) sizeOverhead;
            string[] files;
            InnerOrientation innerOrientation;
            PointCloud pointCloud;
            List<Pose> poses;
            Dictionary<(int, int), List<string>> imageSizes;

            files = Directory.GetFiles(imageDirectory);

            imageSizes = ImageUtils.CheckImageSizes(imageDirectory);

            minSize = imageSizes.Keys.MinTuple();

            minSize = (2195, 3657);

            Import(
                bundlerFile,
                imageDirectory,
                out poses,
                out pointCloud,
                orderImagesDescending);

            for (int i = 0; i < files.Length; i++) {

                poseIndex = orderImagesDescending ?
                    files.Length - i - 1 :
                    i;

                try {
                    using (Mat 
                            image = Cv2.ImRead(files[i]),
                            croppedImage = new Mat(
                                minSize.Item1,
                                minSize.Item2,
                                MatType.CV_8UC3)) {

                        sizeOverhead = (
                            image.Height - minSize.Item1,
                            image.Width - minSize.Item2
                        );

                        minPosition = (
                            sizeOverhead.Item1 / 2,
                            sizeOverhead.Item2 / 2
                        );

                        maxPosition = (
                            image.Height - 1 - sizeOverhead.Item1 / 2,
                            image.Width - 1 - sizeOverhead.Item2 / 2
                        );

                        if (sizeOverhead.Item1 % 2 != 0) {
                            maxPosition.Item1--;
                        }

                        if (sizeOverhead.Item2 % 2 != 0) {
                            maxPosition.Item2--;
                        }

                        for (int r = minPosition.Item1; r <= maxPosition.Item1; r++) {
                            for (int c = minPosition.Item2; c <= maxPosition.Item2; c++) {

                                croppedImage.Set(
                                    r - minPosition.Item1,
                                    c - minPosition.Item2,
                                    image.Get<Vec3b>(r, c));
                            }
                        }

                        innerOrientation = poses[poseIndex].GetInnerOrientation();

                        if (downsampleFactor.HasValue) {

                            using (Mat downsampled = new Mat()) {

                                Cv2.Resize(
                                    croppedImage,
                                    downsampled,
                                    new Size(0, 0),
                                    1 / downsampleFactor.Value,
                                    1 / downsampleFactor.Value,
                                    InterpolationFlags.Nearest);

                                downsampled.CopyTo(croppedImage);
                            }

                            innerOrientation = new InnerOrientation(
                                    croppedImage.Height,
                                    croppedImage.Width,
                                    innerOrientation.FocalLength / downsampleFactor.Value) {
                                PrincipalPointX = (innerOrientation.PrincipalPointX - minPosition.Item1) / downsampleFactor.Value,
                                PrincipalPointY = (innerOrientation.PrincipalPointY - minPosition.Item2) / downsampleFactor.Value,
                                K1 = innerOrientation.K1,
                                K2 = innerOrientation.K2
                            };
                        }
                        else {
                            innerOrientation = new InnerOrientation(
                                    croppedImage.Height,
                                    croppedImage.Width,
                                    innerOrientation.FocalLength) {
                                PrincipalPointX = innerOrientation.PrincipalPointX - minPosition.Item1,
                                PrincipalPointY = innerOrientation.PrincipalPointY - minPosition.Item2,
                                K1 = innerOrientation.K1,
                                K2 = innerOrientation.K2
                            };
                        }

                        poses[poseIndex].SetInnerOrientation(innerOrientation);

                        Cv2.ImWrite(
                            files[i],
                            croppedImage);
                    }
                }
                catch (OpenCVException) {
                    Debug.WriteLine($"Cannot open image '{files[i]}'.");
                }
            }

            Export(
                bundlerFile,
                poses,
                pointCloud);
        }

        private static PoseIndices ParsePoseIndices(
                string line) {

            int count;
            string[] values;
            List<int> poseIndices = new List<int>();

            if (line.Length == 0) {
                return new PoseIndices(poseIndices);
            }
            
            values = line.Split(' ');
            count = int.Parse(values[0]);

            for (int i = 1; i < 4 * count ; i += 4) {

                poseIndices.Add(
                    int.Parse(values[i]));
            }

            return new PoseIndices(poseIndices);
        }
    }
}