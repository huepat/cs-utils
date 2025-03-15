//#define DEBUG_RGBD_ALIGNMENT
//#define DEBUG_CHECKERBOARD_EVALUATION
//#define DEBUG_CHECKERBOARD_REFERENCE_DISTANCE
//#define DEBUG_CHECKERBOARD_DATASET_POSES
//#define DEBUG_REFERENCE_POINTCLOUD
//#define EXPORT_RAYS

using HuePat.Util.Colors;
using HuePat.Util.Image.Marker.Checkerboard;
using HuePat.Util.IO;
using HuePat.Util.IO.PLY.Reading;
using HuePat.Util.IO.PLY.Writing;
using HuePat.Util.Math;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.Processing.PoseTransformation;
using HuePat.Util.Math.Geometry.Processing.Properties;
using HuePat.Util.Math.Geometry.Projection;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Math.Geometry.SpatialIndices.OctTree;
using HuePat.Util.Math.Statistics;
using HuePat.Util.Object.Properties;
using HuePat.Util.Photogrammetry;
using HuePat.Util.Photogrammetry.IO_Old;
using HuePat.Util.Time;
using OpenCvSharp;
using OpenTK.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using static HuePat.Util.Time.TimeUtils;
using ColmapTools = HuePat.Util.Photogrammetry.IO_Old.ColmapTools;
using Point = HuePat.Util.Math.Geometry.Point;
using SmallPoint = HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals.Point;
using SmallPointPLYReader = HuePat.Util.IO.PLY.Reading.MemoryEfficiency.PointsWithNormals.PlyReader;

namespace HuePat.Util.Image.RGBD
{
    public static class RGBDUtils {
        private static readonly double DEGREE_180 = 180.0.DegreeToRadian();

#if DEBUG_RGBD_ALIGNMENT
        const string RGBD_ALIGNMENT_DEBUG_OUTPUT_DIRECTORY = @"C:\Users\phuebner\data\tmp\realsense_test\debug\rgbd_alignment";
#endif

        public class RGBDFrame {
            public double Timestamp { get; private set; }
            public string ColorImageFilePath { get; private set; }
            public string DepthImageFilePath { get; private set; }
            public Pose Pose { get; private set; }

            public RGBDFrame(
                    double timestamp,
                    string colorImageFilePath,
                    string depthImageFilePath) :
                        this(
                            timestamp,
                            colorImageFilePath,
                            depthImageFilePath,
                            null) {
            }

            public RGBDFrame(
                    double timestamp,
                    string colorImageFilePath,
                    string depthImageFilePath,
                    Pose pose) {

                Timestamp = timestamp;
                ColorImageFilePath = colorImageFilePath;
                DepthImageFilePath = depthImageFilePath;
                Pose = pose;
            }
        }

        public static Mat LoadDepthImage(
                this RGBDFrame frame,
                double depthScale) {

            Mat convertedDepthImage = new Mat();

            using (Mat depthImage = new Mat(
                    frame.DepthImageFilePath,
                    ImreadModes.Unchanged)) {

                depthImage.ConvertTo(
                    convertedDepthImage,
                    MatType.CV_64FC1,
                    depthScale);
            }

            return convertedDepthImage;
        }

        public static List<RGBDFrame> LoadTrajectoryFromROSBagData(
                string poseFile,
                string colorImageDirectory,
                string depthImageDirectory,
                string[] supportedImageFormats,
                double maxDifference = 0.02,
                bool evaluationOutput = false) {

            List<Timestamped<Pose>> poses = LoadPoses(poseFile);

            List <Associated<string, string>> associatedImageFilePaths = GetAssociatedImageFilePaths(
                colorImageDirectory,
                depthImageDirectory,
                supportedImageFormats,
                maxDifference);

            if (evaluationOutput) {
                System.Console.WriteLine("RGB TO DEPTH ASSOCIATIONS:");

                associatedImageFilePaths.Print();
            }

            List<Associated<Pose, Associated<string, string>>> associatedPoses = Associate(
                poses,
                associatedImageFilePaths.ToTimestamped(),
                maxDifference);

            if (evaluationOutput) {
                System.Console.WriteLine("");
                System.Console.WriteLine("POSE TO RGBD ASSOCIATIONS:");

                associatedPoses.Print();
            }            

            return associatedPoses
                .Where(poseAssociation => poseAssociation.Element2 != null
                    && poseAssociation.Element2.Element.Element2 != null)
                .Select(poseAssociation => new RGBDFrame(
                    poseAssociation.Element1.Timestamp,
                    poseAssociation.Element2.Element.Element1.Element,
                    poseAssociation.Element2.Element.Element2.Element,
                    poseAssociation.Element1.Element))
                .ToList();
        }

        public static List<RGBDFrame> LoadTrajectoryFromROSBagData(
                string colorImageDirectory,
                string depthImageDirectory,
                string[] supportedImageFormats,
                double maxDifference = 0.02) {

            return GetAssociatedImageFilePaths(
                    colorImageDirectory,
                    depthImageDirectory,
                    supportedImageFormats,
                    maxDifference)
                .Select(associatedImageFilePaths => new RGBDFrame(
                    associatedImageFilePaths.Element1.Timestamp,
                    associatedImageFilePaths.Element1.Element,
                    associatedImageFilePaths.Element2.Element))
                .ToList();
        }

        public static List<RGBDFrame> LoadTrajectoryFromColmap(
                string posesFile,
                string colorImageDirectory,
                string depthImageDirectory,
                string[] supportedImageFormats,
                Pose poseTransformation,
                double maxDifference = 0.02) {

            List<Pose> poses = ColmapTools.ReadPoses(posesFile);

            if (poseTransformation != null) {

                poses.Transform(poseTransformation);
            }

            List<Associated<string, string>> associatedImageFilePaths = GetAssociatedImageFilePaths(
                colorImageDirectory,
                depthImageDirectory,
                supportedImageFormats,
                maxDifference);

            return poses
                .Select((Pose pose) => {

                    string imageFileName = pose.GetImageFileName();

                    Associated<string, string> imageFilePaths = associatedImageFilePaths
                        .Where(paths => Path.GetFileName(paths.Element1.Element) == imageFileName)
                        .FirstOr(null);

                    if (imageFilePaths == null) {
                        return null;
                    }

                    return new RGBDFrame(
                        imageFilePaths.Element1.Timestamp,
                        imageFilePaths.Element1.Element,
                        imageFilePaths.Element2.Element,
                        pose);
                })
                .Where(rgbdFrame => rgbdFrame != null)
                .ToList();
        }

        public static void VisualizeFrames(
                this List<RGBDFrame> trajectory,
                double depthScale,
                int visualizationDelay = 0,
                bool doAlignment = false,
                InnerOrientation colorImageInnerOrientation = null,
                InnerOrientation depthImageInnerOrientation = null,
                Pose colorToDepthPose = null,
                (double, double)? depthVisualizationRange = null) {

            if (doAlignment
                    && (colorImageInnerOrientation == null
                        || depthImageInnerOrientation == null
                        || colorToDepthPose == null)) {

                throw new ArgumentException("Inner orientations and relative pose must be given for alignment.");
            }

#if DEBUG_RGBD_ALIGNMENT
            if (doAlignment) {

                if (!Directory.Exists(RGBD_ALIGNMENT_DEBUG_OUTPUT_DIRECTORY)) {
                    Directory.CreateDirectory(RGBD_ALIGNMENT_DEBUG_OUTPUT_DIRECTORY);
                }

                FileSystemUtils.CleanDirectory(RGBD_ALIGNMENT_DEBUG_OUTPUT_DIRECTORY);
            }
#endif

            int frameIndex = 0;

            double? min = 0.001;
            double? max = null;

            if (depthVisualizationRange.HasValue) {

                min = depthVisualizationRange.Value.Item1;
                max = depthVisualizationRange.Value.Item2;
            }

            Window colorWindow = new Window("color");
            Window depthWindow = new Window("depth");
            Window blendedWindow = new Window("blended");

            foreach (RGBDFrame frame in trajectory) {

                Cv2.SetWindowTitle("color", $"color_{frameIndex}");
                Cv2.SetWindowTitle("depth", $"depth_{frameIndex}");
                Cv2.SetWindowTitle("blended", $"blended_{frameIndex}");

                frameIndex++;

                using (Mat 
                        colorImage = new Mat(frame.ColorImageFilePath),
                        depthImage = frame.LoadDepthImage(depthScale)) {

                    if (doAlignment) {

                        using (Mat alignedDepthImage = depthImage.AlignTo(
                                colorImage,
                                colorImageInnerOrientation,
                                depthImageInnerOrientation,
                                colorToDepthPose)) {

                            Visualize(
                                visualizationDelay,
                                min,
                                max,
                                colorImage,
                                alignedDepthImage,
                                colorWindow,
                                depthWindow,
                                blendedWindow);
                        }
                    }
                    else {
                        Visualize(
                            visualizationDelay,
                            min,
                            max,
                            colorImage,
                            depthImage,
                            colorWindow,
                            depthWindow,
                            blendedWindow);
                    }
                }
            }

            colorWindow.Close();
            depthWindow.Close();
            blendedWindow.Close();
        }

        public static void Export3D(
                this List<RGBDFrame> trajectory,
                string outputDirectory,
                double depthScale,
                InnerOrientation innerOrientation,
                Pose transformation,
                bool exportPerFrameData = false,
                int everyXPose = 1,
                double poseAxesLength = 0.2,
                double posePointDistance = 0.005,
                (double, double)? depthRange = null) {

            string framesDirectory = $"{outputDirectory}/frames";
            string framePosesDirectory = $"{framesDirectory}/poses";
            string framePointsDirectory = $"{framesDirectory}/points";

            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory);
            }

            FileSystemUtils.CleanDirectory(outputDirectory);

            if (exportPerFrameData) {
                Directory.CreateDirectory(framePosesDirectory);
                Directory.CreateDirectory(framePointsDirectory);
            }

            if (transformation == null) {
                transformation = Pose.Identity;
            }

            new PlyWriter() {
                PointFormat = new PointFormat() {
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddColor()
                        .AddFloatProperty("t")
                }
            }.Write(
                $"{outputDirectory}/trajectory.ply",
                Enumerable
                    .Range(0, trajectory.Count)
                    .SelectMany(i => {

                        Pose pose = transformation * trajectory[i].Pose;

                        PointCloud points = pose.CreateVisualizationPointCloud(
                            poseAxesLength, 
                            posePointDistance);

                        points.SetFloatProperty("t", i);

                        points.PropagatePropertiesToPoints();

                        return points;
                    }));

            if (exportPerFrameData) {

                for (int i = 0; i < trajectory.Count; i++) {

                    (transformation * trajectory[i].Pose).ExportPointsPly(
                        $"{framePosesDirectory}/pose_{i}.ply",
                        poseAxesLength,
                        posePointDistance);
                }
            }

            using (PlyStreamWriter writer = new PlyStreamWriter($"{outputDirectory}/point_cloud.ply") {
                PointFormat = new ColoredPointFormat()
            }) {

                List<Point> points;

                for (int i = 0; i < trajectory.Count; i++) {

                    points = trajectory[i].GetPoints(
                        depthScale,
                        innerOrientation,
                        transformation,
                        depthRange);

                    writer.Write(points);

                    if (exportPerFrameData) {
                        points.ExportPointsPly($"{framePointsDirectory}/points_{i}.ply");
                    }
                }
            }
        }

        public static (double, double) GetDepthRange(
                this List<RGBDFrame> trajectory,
                double depthScale) {

            object @lock = new object();
            (double min, double max) range = (double.MaxValue, double.MinValue);

            Parallel.ForEach(
                Partitioner.Create(0, trajectory.Count),
                (Func<(double min, double max)>)(() => (double.MaxValue, double.MinValue)),
                (partition, loopState, partitionRange) => {

                    for (int i = partition.Item1; i < partition.Item2; i++) {

                        using (Mat depthImage = trajectory[i].LoadDepthImage(depthScale)) {

                            for (int r = 0; r < depthImage.Height; r++) {
                                for (int c = 0; c < depthImage.Width; c++) {

                                    double depth = depthImage.Get<double>(r, c);

                                    if (depth < partitionRange.min) {
                                        partitionRange.min = depth;
                                    }

                                    if (depth > partitionRange.max) {
                                        partitionRange.max = depth;
                                    }
                                }
                            }
                        }
                    }

                    return partitionRange;
                },
                partitionRange => {

                    lock (@lock) {

                        if (partitionRange.min < range.min) {
                            range.min = partitionRange.min;
                        }

                        if (partitionRange.max > range.max) {
                            range.max = partitionRange.max;
                        }
                    }
                });

            return range;
        }

        public static Histogram GetDepthHistogram(
                this List<RGBDFrame> trajectory,
                double depthScale,
                double binSize,
                double origin = 0.0) {
            
            object @lock = new object();
            Histogram histogram = new Histogram(binSize, origin);

            Parallel.ForEach(
                Partitioner.Create(0, trajectory.Count),
                () => new Histogram(binSize, origin),
                (partition, loopState, partitionHistogram) => {

                    for (int i = partition.Item1; i < partition.Item2; i++) {

                        using (Mat depthImage = trajectory[i].LoadDepthImage(depthScale)) {

                            for (int r = 0; r < depthImage.Height; r++) {
                                for (int c = 0; c < depthImage.Width; c++) {

                                    partitionHistogram.Add(
                                        depthImage.Get<double>(r, c));
                                }
                            }
                        } 
                    }

                    return partitionHistogram;
                },
                partitionHistogram => {

                    lock (@lock) {
                        histogram.Add(partitionHistogram);
                    }
                });

            return histogram;
        }

        public static void ShowCheckerboardEvaluationImagesPerDataset(
                string inputDirectory) {

            foreach (List<RGBDFrame> dataset in LoadCheckerboardDatasets(inputDirectory)) {

                System.Diagnostics.Trace.WriteLine(dataset.Count);
            }
        }

        public static void EvaluateCheckerboardDetection(
                string inputDirectory,
                Size checkerboardSize) {

            foreach (List<RGBDFrame> dataset in LoadCheckerboardDatasets(inputDirectory)) {

                int correctDetections = dataset
                    .Select(frame => {

                        Point2f[] checkerboardPoints;

                        using (Mat image = new Mat(frame.ColorImageFilePath)) {

                            checkerboardPoints = image.DetectCheckerboardPoints(checkerboardSize);
                        }

                        return checkerboardPoints;
                    })
                    .Count();

                System.Diagnostics.Trace.WriteLine($"{correctDetections}/{dataset.Count} " +
                    $"({Path.GetDirectoryName(Path.GetDirectoryName(dataset[0].ColorImageFilePath))})");
            }
        }

        public static void ShowCheckerboardMasks(
                string inputDirectory,
                int visualizationDelay,
                Size checkerboardSize) {

            foreach (List<RGBDFrame> dataset in LoadCheckerboardDatasets(inputDirectory)) {

                foreach (RGBDFrame frame in dataset) {

                    using (Mat image = new Mat(frame.ColorImageFilePath)) {

                        Point2f[] checkerboardPoints = image.DetectCheckerboardPoints(
                            checkerboardSize,
                            true);

                        using (Mat
                                checkerboardMask = CheckerboardUtils.CreateMaskFromCheckerboardPoints(
                                    image.Size(),
                                    checkerboardSize,
                                    checkerboardPoints),
                                red = new Mat(
                                    image.Size(),
                                    MatType.CV_8UC3,
                                    Scalar.Red),
                                blue = new Mat(
                                    image.Size(),
                                    MatType.CV_8UC3,
                                    Scalar.Blue),
                                checkerboardPointVisualization = image.Clone(),
                                maskedVisualization = image.ApplyMask(checkerboardMask),
                                whiteMask = maskedVisualization.Binarize(),
                                blackMask = checkerboardMask.XOr(whiteMask),
                                whiteMaskBlue = blue.ApplyMask(blackMask),
                                blackMaskRed = red.ApplyMask(whiteMask),
                                checkerboardRedBlue = whiteMaskBlue.Or(blackMaskRed),
                                checkerboardMaskVisualization = ImageUtils.Blend(
                                    image,
                                    checkerboardRedBlue,
                                    0.5)) {

                            checkerboardPointVisualization.DrawCheckerboardPoints(
                                checkerboardSize,
                                checkerboardPoints);

                            Cv2.ImShow("checkerboard points", checkerboardPointVisualization);
                            Cv2.ImShow("masked", maskedVisualization);
                            Cv2.ImShow("masked blended", checkerboardMaskVisualization);

                            Cv2.WaitKey(visualizationDelay);
                        }
                    }
                }
            }
        }

        public static void EvaluateAgainstCheckerboard(
                string inputDirectory,
                string outputDirectory,
                int frameCountThreshold,
                float checkerboardSquareSize,
                double depthScale,
                double heatmapDistanceResolution,
                double heatmapAngleResolution,
                Size checkerboardSize,
                InnerOrientation innerOrientation,
                bool exportPerFramePoses = false,
                bool exportPerFrameImages = false,
                bool visualizePerFrameImages = false,
                int visualizationDelay = 1) {

            if (Directory.Exists(outputDirectory)) {

                FileSystemUtils.CleanDirectory(outputDirectory);
            }
            else {
                Directory.CreateDirectory(outputDirectory);
            }

            int globalPoseIndex = 0;

            Point3f[] checkerboardPoints3D = CheckerboardUtils.GetCheckerboardPoints3D(
                checkerboardSize,
                checkerboardSquareSize);

            checkerboardPoints3D
                .Select(point => point.ToVectord())
                .ExportPointsPly($"{outputDirectory}/checkerboardpoints.ply");

            PnpParams pnpParams = new PnpParams();

            PerspectiveProjection projection = new PerspectiveProjection(
                new Size(
                    innerOrientation.Width,
                    innerOrientation.Height),
                innerOrientation.GetCameraMatrixOpenCV(),
                innerOrientation.GetDistortionCoefficientsOpenCV());

            PlyStreamWriter perFramePoseWriter = null;

            PlyStreamWriter perDatasetPoseWriter = new PlyStreamWriter($"{outputDirectory}/cameraposes_per_dataset.ply") {
                PointFormat = new PointFormat() {
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddFloatProperty("stddev_position")
                        .AddFloatProperty("stddev_orientation")
                }
            };

#if DEBUG_CHECKERBOARD_REFERENCE_DISTANCE
            PlyStreamWriter rayWriter = new PlyStreamWriter($"{outputDirectory}/rays.ply") {
                PointFormat = new PointFormat() {
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddFloatProperty("distance")
                        .AddFloatProperty("angle")
                        .AddFloatProperty("index")
                }
            };
#endif

            if (exportPerFramePoses) {

                perFramePoseWriter = new PlyStreamWriter($"{outputDirectory}/cameraposes_per_frame.ply") {
                    PointFormat = new PointFormat() {
                        PropertyDescriptor = new PropertyDescriptor()
                        .AddColor()
                        .AddFloatProperty("distance")
                        .AddFloatProperty("angle")
                        .AddFloatProperty("index")
                    }
                };
            }

            using (StreamWriter 
                    whiteResultsWriter = CreateResultsWriter($"{outputDirectory}/results_white.csv"),
                    blackResultsWriter = CreateResultsWriter($"{outputDirectory}/results_black.csv")) {

                foreach (List<RGBDFrame> dataset in LoadCheckerboardDatasets(inputDirectory)) {

                    int i = 0;

                    string directory2 = Directory.GetParent(
                        Path.GetDirectoryName(dataset[0].ColorImageFilePath)
                    ).FullName;

                    string directory1 = Directory.GetParent(directory2).Name;

                    directory2 = new DirectoryInfo(directory2).Name;

                    // ignoring outlier measurement
                    if (directory1 == "03" && directory2 == "20230606_165027") {
                        continue;
                    }

                    string datasetOutputDirectory = $"{outputDirectory}/{directory1}/{directory2}";

                    Directory.CreateDirectory(datasetOutputDirectory);

                    DoubleStatistics whiteNoisePerDatasetStatistics = new DoubleStatistics();
                    DoubleStatistics blackNoisePerDatasetStatistics = new DoubleStatistics();
                    DoubleStatistics whiteAccuracyPerDatasetStatistics = new DoubleStatistics();
                    DoubleStatistics blackAccuracyPerDatasetStatistics = new DoubleStatistics();
                    DoubleStatistics referenceDistancePerDatasetStatistics = new DoubleStatistics();
                    AngleStatistics referenceAnglePerDatasetStatistics = new AngleStatistics();
                    PoseStatistics poseStatistics = new PoseStatistics();

#if DEBUG_CHECKERBOARD_DATASET_POSES
                    PlyStreamWriter datasetPoseWriter = new PlyStreamWriter(
                            $"{outputDirectory}/poses_{directory1}_{directory2}.ply") { 
                        PointFormat = new ColoredPointFormat()
                    };
#endif

                    using (StreamWriter
                            whitePerDatasetResultsWriter = CreatePerDatasetResultsWriter($"{datasetOutputDirectory}/results_white.csv"),
                            blackPerDatasetResultsWriter = CreatePerDatasetResultsWriter($"{datasetOutputDirectory}/results_black.csv")) {

                        foreach (RGBDFrame frame in dataset) {

                            if (i == frameCountThreshold) {
                                break;
                            }

                            double timestamp = double.Parse(
                                Path.GetFileNameWithoutExtension(frame.DepthImageFilePath));

                            DoubleStatistics whiteNoisePerFrameStatistics = new DoubleStatistics();
                            DoubleStatistics blackNoisePerFrameStatistics = new DoubleStatistics();
                            DoubleStatistics whiteAccuracyPerFrameStatistics = new DoubleStatistics();
                            DoubleStatistics blackAccuracyPerFrameStatistics = new DoubleStatistics();

                            Mat noiseImage = null;
                            Mat accuracyImage = null;
                            Mat<double> _noiseImage = null;
                            Mat<double> _accurayImage = null;
                            MatIndexer<double> noiseImageIndexer = null;
                            MatIndexer<double> accuracyImageIndexer = null;

                            using (Mat
                                    rgb = new Mat(frame.ColorImageFilePath),
                                    depth = frame.LoadDepthImage(depthScale)) {

                                if (exportPerFrameImages || visualizePerFrameImages) {

                                    noiseImage = new Mat(rgb.Size(), MatType.CV_64FC1, new Scalar(-1.0));
                                    accuracyImage = new Mat(rgb.Size(), MatType.CV_64FC1, new Scalar(-1.0));

                                    _noiseImage = new Mat<double>(noiseImage);
                                    _accurayImage = new Mat<double>(accuracyImage);

                                    noiseImageIndexer = _noiseImage.GetIndexer();
                                    accuracyImageIndexer = _accurayImage.GetIndexer();
                                }

                                using (Mat<double> _depth = new Mat<double>(depth)) {

                                    MatIndexer<double> depthIndexer = _depth.GetIndexer();

                                    Point2f[] checkerboardPoints = rgb.DetectCheckerboardPoints(
                                        checkerboardSize,
                                        doSubPixelRefinement: true,
                                        doDisambiguation: true);

                                    if (checkerboardPoints.Length == 0) {
                                        continue;
                                    }

                                    rgb.GetCheckerboardMasks(
                                        checkerboardSize,
                                        checkerboardPoints,
                                        out Mat whiteMask,
                                        out Mat blackMask);

                                    Pose checkerboardPose = projection.GetModelPoseInCameraFrame(
                                        checkerboardPoints3D,
                                        checkerboardPoints,
                                        pnpParams);

                                    Vector3d[] checkerboardPointsInCameraFrame = checkerboardPoints3D
                                        .Select(point => checkerboardPose * point)
                                        .Select(point => point.ToVectord())
                                        .ToArray();

                                    Plane checkerboardPointsPlane = checkerboardPointsInCameraFrame.FitPlane();

#if DEBUG_CHECKERBOARD_EVALUATION
                                    checkerboardPointsInCameraFrame.ExportPointsPly(
                                        $"{datasetOutputDirectory}/{i}_checkerboard_points.ply",
                                        Colors.Color.Green);

                                    checkerboardPointsPlane
                                        .GetPose()
                                        .CreateVisualizationPointCloud(
                                            0.2,
                                                0.005)
                                        .ExportPointsPly($"{datasetOutputDirectory}/{i}_checkerboard_plane_pose.ply");
#endif

                                    depthIndexer.Evaluate(
                                        exportPerFrameImages || visualizePerFrameImages,
#if DEBUG_CHECKERBOARD_EVALUATION
                                        i,
                                        datasetOutputDirectory,
                                        "white",
#endif
                                        innerOrientation,
                                        checkerboardPointsPlane,
                                        whiteNoisePerDatasetStatistics,
                                        whiteAccuracyPerDatasetStatistics,
                                        whiteNoisePerFrameStatistics,
                                        whiteAccuracyPerFrameStatistics,
                                        whiteMask,
                                        noiseImageIndexer,
                                        accuracyImageIndexer);

                                    depthIndexer.Evaluate(
                                        exportPerFrameImages || visualizePerFrameImages,
#if DEBUG_CHECKERBOARD_EVALUATION
                                        i,
                                        datasetOutputDirectory,
                                        "black",
#endif
                                        innerOrientation,
                                        checkerboardPointsPlane,
                                        blackNoisePerDatasetStatistics,
                                        blackAccuracyPerDatasetStatistics,
                                        blackNoisePerFrameStatistics,
                                        blackAccuracyPerFrameStatistics,
                                        blackMask,
                                        noiseImageIndexer,
                                        accuracyImageIndexer);

                                    GetReferenceDistanceAndAngle(
                                        checkerboardSize,
                                        innerOrientation,
                                        checkerboardPointsPlane,
#if DEBUG_CHECKERBOARD_REFERENCE_DISTANCE
                                        globalPoseIndex,
                                        checkerboardPose.Inverted(),
                                        rayWriter,
#endif
                                        checkerboardPoints,
                                        out double referenceDistance,
                                        out double referenceAngle);

                                    referenceAngle = referenceAngle.RadianToDegree();

                                    // to filter out some outliers in pose determination
                                    if (referenceAngle > 100.0) {
                                        continue;
                                    }

                                    referenceDistancePerDatasetStatistics.Update(referenceDistance);
                                    referenceAnglePerDatasetStatistics.UpdateDegree(referenceAngle);

                                    poseStatistics.Update(
                                        checkerboardPose.Inverted());

                                    whitePerDatasetResultsWriter.WritePerFrameResults(
                                        timestamp,
                                        referenceDistance,
                                        referenceAngle,
                                        whiteNoisePerFrameStatistics,
                                        whiteAccuracyPerFrameStatistics);

                                    blackPerDatasetResultsWriter.WritePerFrameResults(
                                        timestamp,
                                        referenceDistance,
                                        referenceAngle,
                                        blackNoisePerFrameStatistics,
                                        blackAccuracyPerFrameStatistics);

#if DEBUG_CHECKERBOARD_DATASET_POSES
                                    datasetPoseWriter.Write(
                                        checkerboardPose
                                            .Inverted()
                                            .CreateVisualizationPointCloud(0.2, 0.005));
#endif

                                    if (exportPerFramePoses) {

                                        PointCloud poseVisualization = checkerboardPose
                                            .Inverted()
                                            .CreateVisualizationPointCloud(0.2, 0.005);

                                        poseVisualization.SetFloatProperty("distance", (float)referenceDistance);
                                        poseVisualization.SetFloatProperty("angle", (float)referenceAngle);
                                        poseVisualization.SetFloatProperty("index", globalPoseIndex);
                                        poseVisualization.PropagatePropertiesToPoints();

                                        perFramePoseWriter.Write(poseVisualization);
                                    }

                                    if (exportPerFrameImages || visualizePerFrameImages) {

                                        _noiseImage.Dispose();
                                        _accurayImage.Dispose();

                                        Colorize(
                                            visualizePerFrameImages,
                                            exportPerFrameImages,
                                            "noise",
                                            $"{datasetOutputDirectory}/{timestamp}_noise.png",
                                            rgb,
                                            noiseImage,
                                            whiteMask,
                                            blackMask);

                                        Colorize(
                                            visualizePerFrameImages,
                                            exportPerFrameImages,
                                            "accuracy",
                                            $"{datasetOutputDirectory}/{timestamp}_accuracy.png",
                                            rgb,
                                            accuracyImage,
                                            whiteMask,
                                            blackMask);

                                        if (visualizePerFrameImages) {

                                            Cv2.WaitKey(visualizationDelay);
                                        }

                                        noiseImage.Dispose();
                                        accuracyImage.Dispose();
                                    }

                                    whiteMask.Dispose();
                                    blackMask.Dispose();

                                    i++;
                                    globalPoseIndex++;
                                }
                            }

                            if (i == frameCountThreshold) {
                                break;
                            }
                        }
                    }

                    whiteResultsWriter.WritePerDatasetResults(
                        $"{directory1}_{directory2}",
                        whiteNoisePerDatasetStatistics,
                        whiteAccuracyPerDatasetStatistics,
                        referenceDistancePerDatasetStatistics,
                        referenceAnglePerDatasetStatistics);

                    blackResultsWriter.WritePerDatasetResults(
                        $"{directory1}_{directory2}",
                        blackNoisePerDatasetStatistics,
                        blackAccuracyPerDatasetStatistics,
                        referenceDistancePerDatasetStatistics,
                        referenceAnglePerDatasetStatistics);

#if DEBUG_CHECKERBOARD_DATASET_POSES
                    datasetPoseWriter.Dispose();
#endif

                    {
                        PointCloud poseVisualization = poseStatistics.Mean.CreateVisualizationPointCloud(0.2, 0.005);

                        poseVisualization.SetFloatProperty(
                            "stddev_position",
                            (float)poseStatistics.StandardDeviation.Position.Length);

                        poseVisualization.SetFloatProperty(
                            "stddev_orientation",
                            (float)poseStatistics.StandardDeviation.EulerAngles.Length.RadianToDegree());

                        poseVisualization.PropagatePropertiesToPoints();

                        perDatasetPoseWriter.Write(poseVisualization);
                    }
                }
            }

#if DEBUG_CHECKERBOARD_REFERENCE_DISTANCE
            rayWriter.Dispose();
#endif

            perDatasetPoseWriter.Dispose();

            if (exportPerFramePoses) {
                perFramePoseWriter.Dispose();
            }
        }

        public static void EvaluateAgainstReferenceMesh(
                this List<RGBDFrame> trajectory,
                string outputDirectory,
                string referenceMeshFile,
                double depthScale,
                double heatmapDistanceResolution,
                double heatmapAngleResolution,
                InnerOrientation innerOrientation,
                Pose rgbdToReferencePose,
                bool exportVisualizations = false,
		        int resumeIndex = 0,
		        int? resumeNDigits = null) {

            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory);
            }

            FileSystemUtils.CleanDirectory(outputDirectory);

            object @lock = new object();

            int nDigits = resumeNDigits.HasValue ?
		        resumeNDigits.Value :
		        trajectory.Count.GetNumberOfDigits();

            string frameNumberLabel = "";

            (double, double) resolution = (
                heatmapDistanceResolution,
                heatmapAngleResolution
            );

            StatisticsHeatmap heatmap = new StatisticsHeatmap(resolution);

            Mat noReferenceImage = null;
            Mat referenceDepthImage = null;
            Mat angleImage = null;
            Mat depthAccuracyImage = null;

            Mat<byte> _noReferenceImage = null;
            Mat<double> _referenceDepthImage = null;
            Mat<double> _angleImage = null;
            Mat<double> _depthAccuracyImage = null;

            MatIndexer<byte> noReferenceIndexer = null;
            MatIndexer<double> referenceDepthIndexer = null;
            MatIndexer<double> angleIndexer = null;
            MatIndexer<double> depthAccuracyIndexer = null;

            Mesh referenceMesh = new PlyReader()
                .ReadMesh(referenceMeshFile);

            OctTree<Face> referenceOctTree = new OctTree<Face>(true);

            referenceOctTree.Load(referenceMesh);

            System.Console.WriteLine("Finished creating OctTree.");

            for (int i = 0; i < trajectory.Count; i++) {

                Pose pose = rgbdToReferencePose * trajectory[i].Pose;
                StatisticsHeatmap frameHeatmap = new StatisticsHeatmap(resolution);

                using (Mat
                        colorImage = new Mat(trajectory[i].ColorImageFilePath),
                        depthImage = trajectory[i].LoadDepthImage(depthScale)) {

                    Size size = depthImage.Size();

                    if (exportVisualizations) {

                        frameNumberLabel = (i + resumeIndex)
                            .ToString()
                            .PadLeft(nDigits, '0');

                        colorImage.ImWrite($"{outputDirectory}/{frameNumberLabel}_00_color.png");

                        depthImage.ImWrite($"{outputDirectory}/{frameNumberLabel}_01_depth.tif");

                        noReferenceImage = new Mat(
                            depthImage.Size(),
                            MatType.CV_8UC1,
                            Scalar.Black);

                        referenceDepthImage = new Mat(
                            depthImage.Size(),
                            MatType.CV_64FC1);

                        angleImage = new Mat(
                            depthImage.Size(),
                            MatType.CV_64FC1);

                        depthAccuracyImage = new Mat(
                            depthImage.Size(),
                            MatType.CV_64FC1);

                        _noReferenceImage = new Mat<byte>(noReferenceImage);
                        _referenceDepthImage = new Mat<double>(referenceDepthImage);
                        _angleImage = new Mat<double>(angleImage);
                        _depthAccuracyImage = new Mat<double>(depthAccuracyImage);
                    }

                    using (Mat<double> _depthImage = new Mat<double>(depthImage)) {

                        MatIndexer<double> depthIndexer = _depthImage.GetIndexer();

                        if (exportVisualizations) {
                            
                            noReferenceIndexer = _noReferenceImage.GetIndexer();
                            referenceDepthIndexer = _referenceDepthImage.GetIndexer();
                            angleIndexer = _angleImage.GetIndexer();
                            depthAccuracyIndexer = _depthAccuracyImage.GetIndexer();
                        }

#if DEBUG_REFERENCE_POINTCLOUD
                        PointFormat pointFormat = new PointFormat() {
                            PropertyDescriptor = new PropertyDescriptor()
                                .AddDoubleProperty("r")
                                .AddDoubleProperty("c")
                                .AddDoubleProperty("hit")
                        };

                        using (PlyStreamWriter 
                                writer = new PlyStreamWriter($"{outputDirectory}/points.ply") { 
                                    PointFormat = pointFormat
                                },
                                referenceWriter = new PlyStreamWriter($"{outputDirectory}/reference_points.ply") {
                                    PointFormat = pointFormat
                                }) {

                            for (int r = 0; r < size.Height; r++) {

                                Evaluate(
                                    exportVisualizations,
                                    r,
                                    size,
                                    rgbdToReferencePose * trajectory[i].Pose,
                                    innerOrientation,
                                    heatmap,
                                    writer,
                                    referenceWriter,
                                    noReferenceIndexer,
                                    depthIndexer,
                                    referenceDepthIndexer,
                                    angleIndexer,
                                    depthAccuracyIndexer,
                                    referenceOctTree);
                            }
                        }                       
#endif
#if EXPORT_RAYS
                        using (PlyStreamWriter 
                                rayWriter = new PlyStreamWriter($"{outputDirectory}/{frameNumberLabel}_rays.ply") { 
                                    PointFormat = new ColoredPointFormat()
                                },
                                poseWriter = new PlyStreamWriter($"{outputDirectory}/{frameNumberLabel}_poses.ply") {
                                    PointFormat = new ColoredPointFormat()
                                }) {

                            poseWriter.Write(
                                (rgbdToReferencePose * trajectory[i].Pose).CreateVisualizationPointCloud(
                                    0.2,
                                    0.005));

                            for (int r = 0; r < size.Height; r++) {

                                Evaluate(
                                    exportVisualizations,
                                    r,
                                    size,
                                    rgbdToReferencePose * trajectory[i].Pose,
                                    innerOrientation,
                                    heatmap,
                                    rayWriter,
                                    noReferenceIndexer,
                                    depthIndexer,
                                    referenceDepthIndexer,
                                    angleIndexer,
                                    depthAccuracyIndexer,
                                    referenceOctTree);
                            }
                        }
#endif
#if !DEBUG_REFERENCE_POINTCLOUD && !EXPORT_RAYS
                        Parallel.ForEach(
                            Partitioner.Create(
                                0,
                                size.Height),
                            () => new StatisticsHeatmap(resolution),
                            (partition, loopState, partitionHeatmap) => {

                                for (int r = partition.Item1; r < partition.Item2; r++) {

                                    Evaluate(
                                        exportVisualizations,
                                        r,
                                        size,
                                        rgbdToReferencePose * trajectory[i].Pose,
                                        innerOrientation,
                                        partitionHeatmap,
                                        noReferenceIndexer,
                                        depthIndexer,
                                        referenceDepthIndexer,
                                        angleIndexer,
                                        depthAccuracyIndexer,
                                        referenceOctTree);
                                }

                                return partitionHeatmap;
                            },
                            partitionHeatmap => {

                                lock (@lock) {
                                    frameHeatmap.Add(partitionHeatmap);
                                }
                            });
#endif
                    }

                    if (exportVisualizations) {

                        noReferenceImage.ImWrite(
                            $"{outputDirectory}/{frameNumberLabel}_02_no_reference.png");

                        referenceDepthImage.ImWrite(
                            $"{outputDirectory}/{frameNumberLabel}_03_reference_depth.tif");

                        angleImage.ImWrite(
                            $"{outputDirectory}/{frameNumberLabel}_04_angle.tif");

                        depthAccuracyImage.ImWrite(
                            $"{outputDirectory}/{frameNumberLabel}_05_depth_accuracy.tif");
                    }
                }

                if (exportVisualizations) {

                    noReferenceImage.Dispose();
                    referenceDepthImage.Dispose();
                    angleImage.Dispose();
                    depthAccuracyImage.Dispose();

                    _noReferenceImage.Dispose();
                    _referenceDepthImage.Dispose();
                    _angleImage.Dispose();
                    _depthAccuracyImage.Dispose();
                }

		        frameHeatmap
                    .GetMeanHeatmap()
                    .Export(
                        $"{outputDirectory}/{frameNumberLabel}_result_mean_accuracy.csv",
                        4,
                        "distance_m; angle_deg; mean_accuracy_m");

                frameHeatmap
                    .GetStandardDeviationHeatmap()
                    .Export(
                        $"{outputDirectory}/{frameNumberLabel}_result_stddev_accuracy.csv",
                        4,
                        "distance_m; angle_deg; stddev_accuracy_m");

                frameHeatmap
                    .GetCountHeatmap()
                    .Export(
                        $"{outputDirectory}/{frameNumberLabel}_result_count_accuracy.csv",
                        4,
                        "distance_m; angle_deg; count");

                heatmap.Add(frameHeatmap);

                System.Console.WriteLine($"Finished evaluating frame {i}.");
            }

            heatmap
                .GetMeanHeatmap()
                .Export(
                    $"{outputDirectory}/result_mean_accuracy.csv",
                    4,
                    "distance_m; angle_deg; mean_accuracy_m");

            heatmap
                .GetStandardDeviationHeatmap()
                .Export(
                    $"{outputDirectory}/result_stddev_accuracy.csv",
                    4,
                    "distance_m; angle_deg; stddev_accuracy_m");

            heatmap
                .GetCountHeatmap()
                .Export(
                    $"{outputDirectory}/result_count_accuracy.csv",
                    4,
                    "distance_m; angle_deg; count");
        }

        // something's wrong here! this does not work as intended!
        public static Mat AlignTo(
                this Mat depthImage,
                Mat colorImage,
                InnerOrientation colorImageInnerOrientation,
                InnerOrientation depthImageInnerOrientation,
                Pose colorToDepthPose) {

            Mat alignedDepthImage = new Mat(
                depthImage.Size(),
                depthImage.Type());

            Pose depthToColorPose = colorToDepthPose/*.Inverted()*/;

#if DEBUG_RGBD_ALIGNMENT
            Point3d[,] debugExportPoints = new Point3d[
                depthImage.Height,
                depthImage.Width]; 
#endif

            //using (Mat
            //        colorImageCameraMatrix = colorImageInnerOrientation.GetCameraMatrixOpenCV(),
            //        depthImageCameraMatrix = depthImageInnerOrientation.GetCameraMatrixOpenCV(),
            //        colorImageDistortionCoefficients = colorImageInnerOrientation.GetDistortionCoefficientsOpenCV(),
            //        depthImageDistortionCoefficients = depthImageInnerOrientation.GetDistortionCoefficientsOpenCV(),
            //        identitiy_rotation = Mat.FromArray(Pose.Identity.RodriguesElements.ToArray()),
            //        identitiy_translation = Mat.FromArray(Pose.Identity.Position.ToArray())
            //        ) {

                for (int r = 0; r < depthImage.Height; r++) {


                    //Parallel.For(
                    //    0,
                    //    depthImage.Height,
                    //    r => {

                    //Point2d[] rowCoordinates2D = Enumerable
                    //    .Range(0, depthImage.Width)
                    //    .Select(c => new Point2d(r, c))
                    //    .ToArray();
                    Point3d[] rowCoordinates3D;

                    //using (Mat
                    //        rowCoordinateMatrix = Mat.FromArray(rowCoordinates2D),
                    //        undistortedRowCoordinates = new Mat()) {

                        //Cv2.UndistortPoints(
                        //    rowCoordinateMatrix,
                        //    undistortedRowCoordinates,
                        //    depthImageCameraMatrix,
                        //    depthImageDistortionCoefficients);

                        rowCoordinates3D = Enumerable
                            .Range(0, depthImage.Width)
                            .Select(c => {

                                double depth = depthImage.Get<double>(r, c);

                                //Point2d point = undistortedRowCoordinates.Get<Point2d>(c);

                                Point2d point = new Point2d(
                                    (r - depthImageInnerOrientation.PrincipalPointX) / depthImageInnerOrientation.FocalLengthX,
                                    (c - depthImageInnerOrientation.PrincipalPointY) / depthImageInnerOrientation.FocalLengthY);

                                return new Vector3d(
                                    point.X * depth,
                                    point.Y * depth,
                                    depth);
                            })
                            .Select(point => depthToColorPose * point)
                            .Select(point => new Point3d(
                                point.X,
                                point.Y,
                                point.Z))
                            .ToArray();

#if DEBUG_RGBD_ALIGNMENT
                                                for (int c = 0; c < depthImage.Width; c++) {

                                                    debugExportPoints[r, c] = rowCoordinates3D[c];
                                                }
#endif
                    //}

                    //using (Mat
                    //        rowCoordinateMatrix = Mat.FromArray(rowCoordinates3D),
                    //        colorImageCoordinates = new Mat()) {

                    //    Cv2.ProjectPoints(
                    //        rowCoordinateMatrix,
                    //        identitiy_rotation,
                    //        identitiy_translation,
                    //        colorImageCameraMatrix,
                    //        colorImageDistortionCoefficients,
                    //        colorImageCoordinates);

                        for (int c = 0; c < depthImage.Width; c++) {

                            //Point2d point = colorImageCoordinates.Get<Point2d>(c);

                            Point2d point = new Point2d(
                                colorImageInnerOrientation.FocalLengthX * rowCoordinates3D[c].X / rowCoordinates3D[c].Z + colorImageInnerOrientation.PrincipalPointX,
                                colorImageInnerOrientation.FocalLengthY * rowCoordinates3D[c].Y / rowCoordinates3D[c].Z + colorImageInnerOrientation.PrincipalPointY);

                            if (point.X >= 0
                                    && point.Y >= 0
                                    && point.X < depthImage.Height
                                    && point.Y < depthImage.Width) {

                                alignedDepthImage.Set(
                                    (int)point.X.Round(),
                                    (int)point.Y.Round(),
                                    depthImage.Get<double>(r, c));
                            }
                        }
                    //}

                    //});

                    //for (int c = 0; c < depthImage.Width; c++) {

                    //    alignedDepthImage.Set(r, c, depthImage.Get<double>(r, c));
                    //}
                }
            //}

#if DEBUG_RGBD_ALIGNMENT
            Enumerable
                .Range(0, depthImage.Height)
                .SelectMany(r => Enumerable
                    .Range(0, depthImage.Width)
                    .Select(c => (r, c)))
                .Select(index => debugExportPoints[index.r, index.c])
                .Select(point => new Vector3d(
                    point.X,
                    point.Y,
                    point.Z))
                .ExportPointsPly($"{RGBD_ALIGNMENT_DEBUG_OUTPUT_DIRECTORY}/depth_points.ply");
#endif

            return alignedDepthImage;
        }

        public static List<Point> GetPoints(
                this RGBDFrame frame,
                double depthScale,
                InnerOrientation innerOrientation,
                Pose transformation,
                (double, double)? depthRange = null) {

            List<Point> points = new List<Point>();

            if (transformation == null) {
                transformation = Pose.Identity;
            }

            using (Mat
                    colorImage = new Mat(frame.ColorImageFilePath),
                    depthImage = frame.LoadDepthImage(depthScale)) {

                for (int r = 0; r < depthImage.Height; r++) {
                    for (int c = 0; c < depthImage.Width; c++) {

                        double depth = depthImage.Get<double>(r, c);

                        if (depthRange.HasValue
                                && (depth < depthRange.Value.Item1
                                    || depth > depthRange.Value.Item2)) {

                            continue;
                        }

                        points.Add(
                            new Point(
                                    GetPoint(
                                        r,
                                        c,
                                        depth,
                                        transformation * frame.Pose,
                                        innerOrientation))
                                .SetColor(colorImage
                                    .Get<Vec3b>(r, c)
                                    .ToColor()));
                    }
                }
            }

            return points;
        }

        public static void MaskVisualizationImage(
                string visualizationFile,
                string depthImageFile,
                string noReferenceMaskFile,
                Color backgroundColor) {

            Mat noReferenceMaskImage = null;
            Mat<byte> _noReferenceMaskImage = null;
            MatIndexer<byte> noReferenceMask = null;
            Vec3b _backgroundColor = backgroundColor.ToOpenCV();

            if (noReferenceMaskFile != null) {

                noReferenceMaskImage = new Mat(noReferenceMaskFile);
                _noReferenceMaskImage = new Mat<byte>(noReferenceMaskImage);
                noReferenceMask = _noReferenceMaskImage.GetIndexer();
            }

            using (Mat 
                    image = new Mat(visualizationFile),
                    depthImage = new Mat(depthImageFile, ImreadModes.Unchanged)) {

                using (Mat<Vec3b> _image = new Mat<Vec3b>(image)) {

                    using (Mat<double> _depthImage = new Mat<double>(depthImage)) {

                        MatIndexer<Vec3b> imageData = _image.GetIndexer();
                        MatIndexer<double> depth = _depthImage.GetIndexer();

                        for (int r = 0; r < image.Height; r++) {
                            for (int c = 0; c < image.Width; c++) {

                                if (depth[r, c].ApproximateEquals(0.0)
                                        || depth[r, c] > 6.0
                                        || (noReferenceMaskFile != null
                                            && noReferenceMask[r, c] == 255)) {

                                    imageData[r, c] = _backgroundColor;
                                }
                            }
                        }
                    }
                }

                Cv2.ImWrite(
                    FileSystemUtils.GetFileWithPostfix(
                        visualizationFile,
                        "_masked"),
                    image);
            }

            if (noReferenceMaskFile != null) {

                noReferenceMaskImage.Dispose();
                _noReferenceMaskImage.Dispose();
            }
        }

        private static Vector3d GetPoint(
                int r,
                int c,
                double depth,
                Pose pose,
                InnerOrientation innerOrientation) {

            return pose * new Vector3d(
                (c - innerOrientation.PrincipalPointX)
                    / innerOrientation.FocalLengthX * depth,
                (r - innerOrientation.PrincipalPointY)
                    / innerOrientation.FocalLengthY * depth,
                depth);
        }

        private static List<Associated<string, string>> GetAssociatedImageFilePaths(
                string colorImageDirectory,
                string depthImageDirectory,
                string[] supportedImageFormats,
                double maxDifference = 0.02) {

            HashSet<string> _supportedImageFormats = new HashSet<string>(supportedImageFormats);

            List<Timestamped<string>> colorImageFilePaths = LoadImageFilePaths(
                colorImageDirectory,
                _supportedImageFormats);
            List<Timestamped<string>> depthImageFilePaths = LoadImageFilePaths(
                depthImageDirectory,
                _supportedImageFormats);

            return Associate(
                colorImageFilePaths,
                depthImageFilePaths,
                maxDifference);
        }

        private static List<Timestamped<string>> LoadImageFilePaths(
                string imageDirectory,
                HashSet<string> supportedImageFormats) {

            List<Timestamped<string>> imageFilePaths = new List<Timestamped<string>>();

            foreach (string imageFilePath in Directory.EnumerateFiles(imageDirectory)) {

                if (supportedImageFormats.Contains(Path
                        .GetExtension(imageFilePath)
                        .Split(".")[1])) {

                    imageFilePaths.Add(
                        new Timestamped<string>(
                            double.Parse(
                                Path.GetFileNameWithoutExtension(imageFilePath)),
                            imageFilePath));
                }
            }

            return imageFilePaths;
        }

        private static List<Timestamped<Pose>> LoadPoses(
                string poseFile) {

            string[] values;
            List<Timestamped<Pose>> poses = new List<Timestamped<Pose>>();

            foreach (string line in File.ReadLines(poseFile)) {

                values = line.Split(" ");

                poses.Add(
                    new Timestamped<Pose>(
                        double.Parse(values[0]),
                        new Pose() {
                            Position = new Vector3d(
                                double.Parse(values[1]),
                                double.Parse(values[2]),
                                double.Parse(values[3])),
                            Quaternion = new Quaterniond(
                                double.Parse(values[4]),
                                double.Parse(values[5]),
                                double.Parse(values[6]),
                                double.Parse(values[7]))
                        }));
            }

            return poses;
        }

        private static void Visualize(
                int visualizationDelay,
                double? min,
                double? max,
                Mat colorImage,
                Mat depthImage,
                Window colorWindow,
                Window depthWindow,
                Window blendedWindow) {

            using (Mat colorizedDepthImage = depthImage.Colorize(
                    min,
                    max,
                    Colors.Color.Black)) {

                colorWindow.ShowImage(colorImage);
                depthWindow.ShowImage(colorizedDepthImage);

                using (Mat blended = ImageUtils.Blend(
                        colorImage,
                        colorizedDepthImage,
                        0.5)) {

                    blendedWindow.ShowImage(blended);
                }
            }

            Cv2.WaitKey(visualizationDelay);
        }

        private static void SaveColorizedValueImage(
                Mat valueImage,
                string imageFilePath) {

            using (Mat colorizedDepthImage = valueImage.Colorize(
                    null,
                    null,
                    Colors.Color.Black)) {

                colorizedDepthImage.ImWrite(imageFilePath);
            }
        }

        private static IEnumerable<List<RGBDFrame>> LoadCheckerboardDatasets(
                string inputDirectory) {

            if (!Directory.Exists(inputDirectory)) {

                throw new ArgumentException(
                    $"Input directory '{inputDirectory}' does not exist.");
            }

            foreach (string subDirectory in Directory.EnumerateDirectories(inputDirectory)) {
                foreach (string subSubDirectory in Directory.EnumerateDirectories(subDirectory)) {

                    string depthDirectory = $"{subSubDirectory}/d";
                    string rgbDirectory = $"{subSubDirectory}/rgb";

                    if (!Directory.Exists(depthDirectory)) {

                        throw new ArgumentException(
                            $"Input directory '{depthDirectory}' does not exist.");
                    }

                    if (!Directory.Exists(rgbDirectory)) {

                        throw new ArgumentException(
                            $"Input directory '{rgbDirectory}' does not exist.");
                    }

                    yield return LoadTrajectoryFromROSBagData(
                        rgbDirectory,
                        depthDirectory,
                        new string[] { "png" });
                }
            }
        }

        private static StreamWriter CreateResultsWriter(
                string file) {

            StreamWriter writer = new StreamWriter(file);

            writer.WriteLine($"timestamp; n_pixels; " +
                $"mean_reference_distance_m; mean_reference_angle_deg; " +
                $"stddev_reference_distance_m; stddev_reference_angle_deg; " +
                $"spread_reference_distance_m; spread_reference_angle_deg; " +
                $"mean_noise_m; mean_accuracy_m; " +
                $"stddev_noise_m; stddev_accuracy_m; " +
                $"min_noise_m; min_accuracy_m; " +
                $"max_noise_m; max_accuracy_m");

            return writer;
        }

        private static StreamWriter CreatePerDatasetResultsWriter(
                string file) {

            StreamWriter writer = new StreamWriter(file);

            writer.WriteLine($"timestamp; n_pixels; " +
                $"reference_distance_m; reference_angle_deg; " +
                $"mean_noise_m; mean_accuracy_m; " +
                $"stddev_noise_m; stddev_accuracy_m" +
                $"min_noise_m; min_accuracy_m; " +
                $"max_noise_m; max_accuracy_m");

            return writer;
        }

        private static void WritePerFrameResults(
                this StreamWriter whitePerDatasetResultsWriter,
                double timestamp,
                double referenceDistance, 
                double referenceAngle,
                DoubleStatistics noiseStatistics,
                DoubleStatistics accuracyStatistics) {

            whitePerDatasetResultsWriter.WriteLine(
                $"{timestamp}; {noiseStatistics.Counter}; " +
                $"{referenceDistance:0.0000}; {referenceAngle:0.0000}; " +
                Format(noiseStatistics, accuracyStatistics));
        }

        private static void WritePerDatasetResults(
                this StreamWriter whitePerDatasetResultsWriter,
                string datasetName,
                DoubleStatistics noiseStatistics,
                DoubleStatistics accuracyStatistics,
                DoubleStatistics referenceDistanceStatistics,
                AngleStatistics referenceAngleStatistics) {

            whitePerDatasetResultsWriter.WriteLine(
                $"{datasetName}; {noiseStatistics.Counter}; " +
                $"{referenceDistanceStatistics.Mean:0.0000}; {referenceAngleStatistics.Mean:0.0000}; " +
                $"{referenceDistanceStatistics.StandardDeviation:0.0000}; {referenceAngleStatistics.StandardDeviation:0.0000}; " +
                $"{referenceDistanceStatistics.Max - referenceDistanceStatistics.Min:0.0000}; " +
                $"{Angles.SubstractDegree(referenceAngleStatistics.Max, referenceAngleStatistics.Min):0.0000}; " +
                Format(noiseStatistics, accuracyStatistics));
        }

        private static string Format(
                DoubleStatistics noiseStatistics,
                DoubleStatistics accuracyStatistics) {

            return $"{noiseStatistics.Mean:0.0000}; {accuracyStatistics.Mean:0.0000}; " +
                $"{noiseStatistics.StandardDeviation:0.0000}; {accuracyStatistics.StandardDeviation:0.0000}; " +
                $"{noiseStatistics.Min:0.0000}; {accuracyStatistics.Min:0.0000}; " +
                $"{noiseStatistics.Max:0.0000}; {accuracyStatistics.Max:0.0000}";
        }

        private static void GetCheckerboardMasks(
                this Mat image,
                Size checkerboardSize,
                Point2f[] checkerboardPoints,
                out Mat whiteMask,
                out Mat blackMask) {

            using (Mat
                    checkerboardMask = CheckerboardUtils.CreateMaskFromCheckerboardPoints(
                        image.Size(),
                        checkerboardSize,
                        checkerboardPoints),
                    maskedImage = image.ApplyMask(checkerboardMask)) {

                whiteMask = maskedImage.Binarize();
                blackMask = checkerboardMask.XOr(whiteMask);
            }
        }

        private static void Evaluate(
                this MatIndexer<double> depthImage,
                bool exportImages,
#if DEBUG_CHECKERBOARD_EVALUATION
                int frameIndex,
                string outputDirectory,
                string label,
#endif
                InnerOrientation innerOrientation,
                Plane checkerboardPlane,
                DoubleStatistics noisePerDatasetStatistics,
                DoubleStatistics accuracyPerDatasetStatistics,
                DoubleStatistics noisePerFrameStatistics,
                DoubleStatistics accuracyPerFrameStatistics,
                Mat mask,
                MatIndexer<double> noiseImage,
                MatIndexer<double> accuracyImage) {

            using (Mat maskPixels = mask.GetMaskPixels()) {
                using (Mat<OpenCvSharp.Point> _maskPixels = new Mat<OpenCvSharp.Point>(maskPixels)) {

                    MatIndexer<OpenCvSharp.Point> maskPixelIndexer = _maskPixels.GetIndexer();

                    List<Vector3d?> points = depthImage.GetCheckerboardPoints(
                        maskPixels.Height,
                        maskPixelIndexer,
                        innerOrientation);

                    Plane depthPlane = points
                        .Where(point => point.HasValue)
                        .Select(point => point.Value)
                        .ToList()
                        .FitPlane();

#if DEBUG_CHECKERBOARD_EVALUATION
                    points
                        .Where(point => point.HasValue)
                        .Select(point => point.Value)
                        .ExportPointsPly(
                            $"{outputDirectory}/{frameIndex}_depth_points_{label}.ply",
                            Colors.Color.Red);

                    depthPlane
                        .GetPose()
                        .CreateVisualizationPointCloud(
                            0.2,
                            0.005)
                        .ExportPointsPly($"{outputDirectory}/{frameIndex}_depth_plane_pose_{label}.ply");
#endif

                    points.Evaluate(
                        exportImages,
                        checkerboardPlane,
                        depthPlane,
                        noisePerDatasetStatistics,
                        accuracyPerDatasetStatistics,
                        noisePerFrameStatistics,
                        accuracyPerFrameStatistics,
                        noiseImage,
                        accuracyImage,
                        maskPixelIndexer);
                }
            }
        }

        private static List<Vector3d?> GetCheckerboardPoints(
                this MatIndexer<double> depthImage,
                int maskPixelCount,
                MatIndexer<OpenCvSharp.Point> maskPixels,
                InnerOrientation innerOrientation) {

            List<Vector3d?> points = new List<Vector3d?>();

            for (int i = 0; i < maskPixelCount; i++) {

                OpenCvSharp.Point maskPixel = maskPixels[i];

                double depth = depthImage[
                    maskPixel.Y,
                    maskPixel.X];

                Vector3d? point = depth.ApproximateEquals(0.0) ?
                    null :
                    GetPoint(
                        maskPixel.Y,
                        maskPixel.X,
                        depth,
                        Pose.Identity,
                        innerOrientation);

                points.Add(point);
            }

            return points;
        }

        private static void Evaluate(
                this List<Vector3d?> points,
                bool exportImages,
                Plane checkerboardPlane,
                Plane depthPlane,
                DoubleStatistics noisePerDatasetStatistics,
                DoubleStatistics accuracyPerDatasetStatistics,
                DoubleStatistics noisePerFrameStatistics,
                DoubleStatistics accuracyPerFrameStatistics,
                MatIndexer<double> noiseImage,
                MatIndexer<double> accuracyImage,
                MatIndexer<OpenCvSharp.Point> maskPixels) {

            for (int i = 0; i < points.Count; i++) {

                OpenCvSharp.Point pixel = maskPixels[i];

                if (!points[i].HasValue) {

                    if (exportImages) {
                        noiseImage[
                            pixel.Y,
                            pixel.X] = -1.0;

                        accuracyImage[
                            pixel.Y,
                            pixel.X] = -1.0;
                    }

                    continue;
                }

                Vector3d point = points[i].Value;
                
                double noise = depthPlane.DistanceTo(point);
                double accuracy = checkerboardPlane.DistanceTo(point);

                noisePerDatasetStatistics.Update(noise);
                accuracyPerDatasetStatistics.Update(accuracy);
                noisePerFrameStatistics.Update(noise);
                accuracyPerFrameStatistics.Update(accuracy);

                if (exportImages) {

                    noiseImage[
                        pixel.Y,
                        pixel.X] = noise;

                    accuracyImage[
                        pixel.Y,
                        pixel.X] = accuracy;
                }
            }
        }

        private static void GetReferenceDistanceAndAngle(
                Size checkerboardSize,
                InnerOrientation innerOrientation,
                Plane checkerboardPlane,
#if DEBUG_CHECKERBOARD_REFERENCE_DISTANCE
                int index,
                Pose pose,
                PlyStreamWriter rayWriter,
#endif
                Point2f[] checkerboardPoints,
                out double distance,
                out double angle) {

            Point2f centerPoint = CheckerboardUtils.GetCenterFromCheckerboardPoints(
                checkerboardSize,
                checkerboardPoints);

            Ray ray = new Ray(
                new Vector3d(0.0),
                new Vector3d(
                    (centerPoint.X - innerOrientation.PrincipalPointX)
                        / innerOrientation.FocalLengthX,
                    (centerPoint.Y - innerOrientation.PrincipalPointY)
                        / innerOrientation.FocalLengthY,
                    1.0));

            distance = checkerboardPlane
                .Intersect(ray)[0]
                .Distance;

            angle = (checkerboardPlane.Normal.AngleTo(ray.Direction) - DEGREE_180).Abs();

#if DEBUG_CHECKERBOARD_REFERENCE_DISTANCE

            PointCloud rayVisualization = ray.CreateVisualizationPointCloud(
                distance,
                0.005);

            rayVisualization.SetFloatProperty("distance", (float)distance);
            rayVisualization.SetFloatProperty("angle", (float)angle.RadianToDegree());
            rayVisualization.SetFloatProperty("index", index);
            rayVisualization.PropagatePropertiesToPoints();
            rayVisualization.Transform(pose);

            rayWriter.Write(rayVisualization);
#endif
        }

        private static void Colorize(
                bool visualize,
                bool export,
                string label,
                string outputFile,
                Mat image,
                Mat valueImage,
                Mat whiteMask,
                Mat blackMask) {

            double? min = null;
            double? max = null;

            using (Mat
                    mask = whiteMask.Or(blackMask),
                    backgroundMask = mask.InvertMask(),
                    colorizedValueImage = valueImage.Colorize(
                        ref min,
                        ref max,
                        backGroundColor : Color.Black,
                        doInvert : true),
                    maskedImage = image.ApplyMask(backgroundMask),
                    final = colorizedValueImage.Or(maskedImage)) {

                if (export) {
                    Cv2.ImWrite(outputFile, final);
                }

                if (visualize) {
                    Cv2.ImShow(label, final);
                }
            }
        }

        private static void Evaluate(
                bool exportVisualizations,
                int r,
                Size size,
                Pose pose,
                InnerOrientation innerOrientation,
                StatisticsHeatmap heatmap,
#if DEBUG_REFERENCE_POINTCLOUD
                PlyStreamWriter writer,
                PlyStreamWriter referenceWriter,
#endif
#if EXPORT_RAYS
                PlyStreamWriter rayWriter,
#endif
                MatIndexer<byte> noReferenceImage,
                MatIndexer<double> depthImage,
                MatIndexer<double> referenceDepthImage,
                MatIndexer<double> angleImage,
                MatIndexer<double> depthAccuracyImage,
                OctTree<Face> referenceOctTree) {

            for (int c = 0; c < size.Width; c++) {

                double depth = depthImage[r, c];

                if (depth.ApproximateEquals(0.0)) {

                    continue;
                }

                Vector3d point = GetPoint(
                    r,
                    c,
                    depth,
                    pose,
                    innerOrientation);

                Ray ray = new Ray(
                    pose.Position,
                    (point - pose.Position).Normalized());

                MultiGeometryIntersection<Face> intersection = referenceOctTree
                    .Intersect(ray)
                    .WhereMin(intersectionCandidate => intersectionCandidate.Distance)
                    .FirstOrDefault();

#if EXPORT_RAYS
                PointCloud rayVisualization = null;
#endif

                if (intersection == null) {
#if DEBUG_REFERENCE_POINTCLOUD
                    Point _point = new Point(point);

                    _point.SetDoubleProperty("r", r);
                    _point.SetDoubleProperty("c", c);
                    _point.SetDoubleProperty("hit", 0.0);

                    writer.Write(_point);
#endif
#if EXPORT_RAYS
                    rayVisualization = ray.CreateVisualizationPointCloud(
                        1.0,
                        0.005);

                    rayVisualization.SetColor(Colors.Color.Red);
#endif

                    if (exportVisualizations) {
                        noReferenceImage[r, c] = 255;
                    }
                }
                else {

#if DEBUG_REFERENCE_POINTCLOUD
                    Point _point = new Point(point);
                    Point _referencePoint = new Point(intersection.Position);

                    _point.SetDoubleProperty("r", r);
                    _point.SetDoubleProperty("c", c);
                    _point.SetDoubleProperty("hit", 1.0);
                    _referencePoint.SetDoubleProperty("r", r);
                    _referencePoint.SetDoubleProperty("c", c);
                    _referencePoint.SetDoubleProperty("hit", 1.0);

                    writer.Write(_point);
                    referenceWriter.Write(_referencePoint);
#endif
#if EXPORT_RAYS
                    rayVisualization = ray.CreateVisualizationPointCloud(
                        intersection.Distance,
                        0.005);

                    rayVisualization.SetColor(Colors.Color.Green);
#endif

                    double referenceDepth = intersection.Position.DistanceTo(pose.Position);
                    double accuracy = (referenceDepth - depth).Abs();

                    double referenceAngle = ray.Direction
                        .AngleTo(intersection.IntersectingGeometry.Geometry.Normal)
                        .RadianToDegree();

                    if (referenceAngle > 90.0) {
                        referenceAngle = (referenceAngle - 180.0).Abs();
                    }

                    heatmap.Add(
                        (referenceDepth, referenceAngle),
                        accuracy);

                    if (exportVisualizations) {

                        referenceDepthImage[r, c] = referenceDepth;
                        angleImage[r, c] = referenceAngle;
                        depthAccuracyImage[r, c] = accuracy;
                    }
                }
#if EXPORT_RAYS
                rayVisualization.PropagatePropertiesToPoints();
                rayWriter.Write(rayVisualization);
#endif
            }
        }
    }
}
