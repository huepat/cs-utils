//#define DEBUG_CANNY_FILTERING
//#define DEBUG_SHAPE_CROSSSECTION_EXPORT
//#define DEBUG_MESH_TRANSFORMATION
//#define DEBUG_GRID_POINTS
//#define DEBUG_CULLING
//#define DEBUG_CULLING_EXPORT_RAYS
//#define DEBUG_CULLING_EXPORT_RAYCASTING_POINTS
//#define DEBUG_CULLING_EXPORT_INITIALIZED_RAYMARCHING_DIRECTIONS_AS_PLY
//#define DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
//#define DEBUG_COLMAP_DEPTH_IMAGE_RENDERING

#define DGPF_EVAL

#define PARALLELIZE_CULLING_OVER_POSES
//#define PARALLELIZE_CULLING_OVER_RAYS

using HuePat.Util.Colors;
using HuePat.Util.Image;
using HuePat.Util.IO;
using HuePat.Util.IO.JSON;
using HuePat.Util.IO.PLY.Reading;
using HuePat.Util.IO.PLY.Writing;
using HuePat.Util.Math;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.Processing.PoseTransformation;
using HuePat.Util.Math.Geometry.Processing.Rotating;
using HuePat.Util.Math.Geometry.Processing.Scaling;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Math.Geometry.SpatialIndices.OctTree;
using HuePat.Util.Math.Geometry.SpatialIndices.OctTree.Grid;
using HuePat.Util.Math.Grids;
using HuePat.Util.Math.Statistics;
using HuePat.Util.Object.Properties;
using HuePat.Util.Photogrammetry;
using HuePat.Util.Processing.Transformations;
using HuePat.Util.Time;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;
using NumSharp;
using Plotly.NET;
using OpenCvSharp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Color = HuePat.Util.Colors.Color;
using Point = HuePat.Util.Math.Geometry.Point;
using System.Diagnostics;
using static Plotly.NET.StyleParam;
using Plotly.NET.LayoutObjects;
using Microsoft.FSharp.Core;
using Accord.Math;
using HuePat.Util.Math.Geometry.Processing.Properties;

namespace HuePat.Util.Photogrammetry {
    public static class NeRFTools {

        public static readonly Pose FICUS_TO_NERF_TRANSFORMATION = new Pose(
                0.999932706356, 0.010878063738, -0.004035292659, 0.000067247041,
                -0.010890754871, 0.999935746193, -0.003136525163, 0.007612653077,
                0.004000913817, 0.003180261469, 0.999986946583, -0.010281534865)
            * new Pose(
                2.277933597565, 0.014178439043, 0.030160712078, -0.007774857339,
                0.030232883990, -0.011549953371, -2.277947425842, 0.002694868250,
                -0.014024079777, 2.278103828430, -0.011736867949, -1.022780060768);

        public enum RenderMode {
            GREYSCALE,
            COLOR,
            SDF_INSIDE_OUTSIDE,
            SDF_ZERO_CROSSING,
            RAW_DATA_IMAGE
        }

        public class ExportConfig {
            public bool Invert { get; set; }
            public bool AreCoordinatesFloat { get; set; }
            public bool ExportSectionPlanes { get; set; }
            public double Resolution { get; private set; }
            public string TransformationFile { get; set; }
            public string RegistrationFile { get; set; }
            public Vector3d CropMinCoordinate { get; private set; }
            public Vector3d CropMaxCoordinate { get; private set; }
            public Matrix3d? CropOrientation { get; private set; }

            public ExportConfig(
                    double resolution,
                    Vector3d cropMinCoordinate,
                    Vector3d cropMaxCoordinate) :
                        this(resolution) {

                CropMinCoordinate = cropMinCoordinate;
                CropMaxCoordinate = cropMaxCoordinate;
            }

            public ExportConfig(
                    double resolution,
                    string cropFile) :
                        this(resolution) {

                ReadCrop(
                    cropFile,
                    out Vector3d center,
                    out Vector3d size,
                    out Matrix3d orientation);

                CropMinCoordinate = center - 0.5 * size;
                CropMaxCoordinate = center + 0.5 * size;
                CropOrientation = orientation;
            }

            private ExportConfig(
                    double resolution) {

                AreCoordinatesFloat = true;
                Resolution = resolution;
            }
        }

        private class Quad : IFiniteGeometry {
            private Triangle triangle1;
            private Triangle triangle2;

            public Vector3i GridCoordinate { get; private set; }
            public Dictionary<string, IProperty> Properties { get; set; }

            public Mesh Mesh {
                get {

                    return Mesh.From(
                        new Mesh[] {
                            triangle1.Mesh,
                            triangle2.Mesh
                        });
                }
            }

            public AABox BBox {
                get {

                    return AABox.FromContainedGeometries(
                        new Triangle[] {
                            triangle1,
                            triangle2
                        });
                }
            }

            public Quad(
                    Vector3i gridCoordinate,
                    Triangle triangle1,
                    Triangle triangle2) {

                GridCoordinate = gridCoordinate;

                this.triangle1 = triangle1;
                this.triangle2 = triangle2;
            }

            public double DistanceTo(
                    Vector3d position) {

                return System.Math.Min(
                    triangle1.DistanceTo(position),
                    triangle2.DistanceTo(position));
            }

            public List<Intersection> Intersect(
                    Ray ray) {

                List<Intersection> intersections = new List<Intersection>();

                intersections.AddRange(
                    triangle1.Intersect(ray));

                intersections.AddRange(
                    triangle2.Intersect(ray));

                return intersections;
            }

            public bool Intersects(
                    AABox box) {

                return triangle1.Intersects(box)
                    || triangle2.Intersects(box);
            }

            public void UpdateBBox() {

                // nothing to do
            }
        }

        public static void PrintDensityRange(
                string densityGridFile) {

            double[,,] densityGrid = Load(densityGridFile);

            (double min, double max) range = densityGrid.GetRange();

            System.Console.WriteLine($"min: {range.min:0.00}, max: {range.max:0.00}");
        }

        public static void ShowDensityHistogram(
                string densityGridFile,
                double binSize) {

            double[,,] densityGrid = Load(densityGridFile);

            densityGrid
                .GetHistogram(binSize)
                .CreateChart("densities")
                .Show();
        }

        public static void PrintDensityHistogram(
                string densityGridFile,
                double binSize) {

            double[,,] densityGrid = Load(densityGridFile);

            Histogram histogram = densityGrid.GetHistogram(binSize);

            histogram.GetData(
                out double[] densities,
                out double[] counts);

            for (int i = 0; i < densities.Length; i++) {

                System.Console.WriteLine(
                    $"density {densities[i]}: {counts[i]}");
            }
        }

        public static void ExportPointCloudByDensityThreshold(
                string densityGridFile,
                string cropFile,
                double resolution,
                double densityThreshold,
                bool exportAllSlices = false,
                string posesFile = null,
                string transformationFile = null) {

            double[,,] densityGrid = Load(densityGridFile);

            ReadCrop(
                cropFile,
                out Vector3d cropCenter,
                out Vector3d cropSize,
                out Matrix3d cropOrientation);

            bool[,,] occupancyGrid = densityGrid.ApplyDensityThreshold(densityThreshold);

            if (posesFile != null) {

                List<Pose> poses = LoadPoses(posesFile);

                if (transformationFile != null) {

                    poses.TransformToNeRFStudio(transformationFile);
                }

                occupancyGrid = occupancyGrid.CullInvisibleCells(
#if DEBUG_CULLING
                    densityGridFile,
#endif
                    resolution,
                    cropCenter,
                    cropSize,
                    cropOrientation,
                    poses);
            }

            if (exportAllSlices) {

                string directory = $"{Path.GetDirectoryName(densityGridFile)}"
                    + $"/{Path.GetFileNameWithoutExtension(densityGridFile)}_densityThreshold{densityThreshold}_SLICES";

                if (Directory.Exists(directory)) {

                    FileSystemUtils.CleanDirectory(directory);
                }
                else {
                    Directory.CreateDirectory(directory);
                }

                occupancyGrid.ExportSlices(
                    directory);
            }

            Export(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_densityThreshold{densityThreshold}",
                    "ply"),
                resolution,
                cropCenter,
                cropSize,
                cropOrientation,
                occupancyGrid,
                densityGrid);
        }

        public static void ExportDebugGradients(
                string densityGridFile,
                int gaussianKernelSize,
                double gaussianKernelSigma,
                double visualizationDensityThreshold) {

            double[,,] densityGrid = Load(densityGridFile);

            densityGrid = densityGrid.ApplyGaussianFilter(
                gaussianKernelSize,
                gaussianKernelSigma);

            densityGrid.GetDebugGradients(
                out double[,,] gradientXGrid,
                out double[,,] gradientYGrid,
                out double[,,] gradientZGrid);

            gradientXGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_DEBUG_GRADIENT_X",
                        "tiff"),
                    null,
                    null,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

            gradientXGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_DEBUG_GRADIENT_X",
                    "tiff"),
                null,
                visualizationDensityThreshold,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);

            gradientYGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_DEBUG_GRADIENT_Y",
                    "tiff"),
                null,
                null,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);

            gradientYGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_DEBUG_GRADIENT_Y",
                    "tiff"),
                null,
                visualizationDensityThreshold,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);

            gradientZGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_DEBUG_GRADIENT_Z",
                    "tiff"),
                null,
                null,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);

            gradientZGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_DEBUG_GRADIENT_Z",
                    "tiff"),
                null,
                visualizationDensityThreshold,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);
        }

        public static void ExportPointCloudByCannyFiltering(
                string densityGridFile,
                string cropFile,
                int gaussianKernelSize,
                double gaussianKernelSigma,
                double lowerThreshold,
                double upperThreshold,
                double resolution,
                string posesFile = null,
                string transformationFile = null) {

            double[,,] densityGrid = Load(densityGridFile);

            ReadCrop(
                cropFile,
                out Vector3d cropCenter,
                out Vector3d cropSize,
                out Matrix3d cropOrientation);

#if DEBUG_CANNY_FILTERING
            const double VISUALIZATION_DENSITY_THRESHOLD = 50.0;

            densityGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_CANNYDEBUG_01_DENSITY",
                    "tiff"),
                null,
                null,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);

            densityGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_CANNYDEBUG_01_DENSITY",
                    "tiff"),
                null,
                VISUALIZATION_DENSITY_THRESHOLD,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);

            densityGrid.Export(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_CANNYDEBUG_01_DENSITY",
                    "ply"),
                VISUALIZATION_DENSITY_THRESHOLD,
                resolution,
                cropCenter,
                cropSize,
                cropOrientation);
#endif

            bool[,,] edgeGrid = densityGrid.ApplyCannyFilter(
                gaussianKernelSize,
                gaussianKernelSigma,
                lowerThreshold,
                upperThreshold
#if DEBUG_CANNY_FILTERING
                , densityGridFile
                , VISUALIZATION_DENSITY_THRESHOLD
                , resolution
                , cropCenter
                , cropSize
                , cropOrientation
#endif
            );

#if DEBUG_CANNY_FILTERING
            edgeGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_CANNYDEBUG_05_EDGES",
                    "tiff"),
                false);
#endif

            if (posesFile != null) {

                List<Pose> poses = LoadPoses(posesFile);

                if (transformationFile != null) {

                    poses.TransformToNeRFStudio(transformationFile);
                }

                edgeGrid = edgeGrid.CullInvisibleCells(
#if DEBUG_CULLING
                    densityGridFile,
#endif
                    resolution,
                    cropCenter,
                    cropSize,
                    cropOrientation,
                    poses);
            }

            edgeGrid.Export(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    //$"_Canny(kernel{gaussianKernelSize}, sigma{gaussianKernelSigma}, thresholds({lowerThreshold}, {upperThreshold}))",
                    "_canny",
                    "ply"),
                resolution,
                cropCenter,
                cropSize,
                cropOrientation);
        }

        public static void ExportCrossSections(
                string densityGridFile,
                double? min,
                double? max,
                RenderMode renderMode,
                bool invertScale = false,
                bool useAbsoluteValues = false,
                ColormapTypes colorMap = ColormapTypes.Jet) {

            double[,,] densityGrid = Load(densityGridFile);

            if (useAbsoluteValues) {

                for (int x = 0; x < densityGrid.GetLength(0); x++) {
                    for (int y = 0; y < densityGrid.GetLength(1); y++) {
                        for (int z = 0; z < densityGrid.GetLength(2); z++) {

                            densityGrid[x, y, z] = densityGrid[x, y, z].Abs();
                        }
                    }
                }
            }

            densityGrid.ExportCrossSections(
                densityGridFile,
                min,
                max,
                renderMode,
                colorMap,
                invertScale);
        }

        public static void ExportCrossSectionsFromShape(
                string file,
                ExportConfig exportConfig) {

            ReadShape(
                    file,
                    exportConfig)
                .ExportCrossSections(
                    file,
                    exportConfig);
        }

        public static void ExportGridPoints(
                string cropFile,
                double resolution) {

            ReadCrop(
                cropFile,
                out Vector3d center,
                out Vector3d size,
                out Matrix3d orientation);

            Vector3d minCoordinate = center - 0.5 * size;
            Vector3d maxCoordinate = center + 0.5 * size;

#if DEBUG_GRID_POINTS
            new PointCloud(new Vector3d[] { center })
                .ExportPointsPly(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        cropFile,
                        "_center",
                        "ply"),
                    Color.Gray);

            new PointCloud(new Vector3d[] { 
                minCoordinate,
                maxCoordinate
            })
                .ExportPointsPly(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        cropFile,
                        "_bounds",
                        "ply"),
                    Color.Gray);

            new PointCloud(new Vector3d[] {
                orientation * (minCoordinate - center) + center,
                orientation * (maxCoordinate - center) + center,
            })
                .ExportPointsPly(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        cropFile,
                        "_bounds_rotated",
                        "ply"),
                    Color.Gray);
#endif

            Vector3i gridSize = GridTools.GetGridSize(
                resolution,
                minCoordinate,
                maxCoordinate);

            using (PlyStreamWriter writer = new PlyStreamWriter(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        cropFile,
                        "_gridPoints",
                        "ply")) {
                PointFormat = new ColoredPointFormat()
            }) {

                for (int x = 0; x < gridSize.X; x++) {
                    for (int y = 0; y < gridSize.Y; y++) {
                        for (int z = 0; z < gridSize.Z; z++) {

                            Point point = new Point(
                                new Vector3i(x, y, z)
                                    .Transform(
                                        resolution,
                                        minCoordinate,
                                        center,
                                        orientation));

                            point.SetColor(Color.Gray);

                            writer.Write(point);
                        }
                    }
                }
            }
        }

        public static void TransformPointCloudToNeRFStudio(
                string pointCloudFileFile,
                string transformationFile,
                bool withColors = true) {

            PointFormat pointFormat = withColors ?
                new ColoredPointFormat() :
                new PointFormat();

            PointCloud pointCloud = new PlyReader() {
                PointFormat = pointFormat
            }.ReadPointCloud(pointCloudFileFile);

            pointCloud.ApplyTransformationFile(transformationFile);

            new PlyWriter() {
                PointFormat = pointFormat
            }.Write(
                FileSystemUtils.GetFileWithPostfix(
                    pointCloudFileFile,
                    "_toNeRFStudio"),
                pointCloud);
        }

        public static void ExportCropToJson(
                string outputFile,
                Vector3d center,
                Vector3d size,
                Matrix3d orientation) {

            using (JSONWriter writer = new JSONWriter(outputFile)) {

                writer.Write("center", center);
                writer.Write("size", size);
                writer.Write("orientation", orientation);
            }
        }

        public static void ConvertNeRFStudioShapeToColmap(
                string pointCloudFile,
                string colmapToNeRFStudioTransformationFile,
                bool hasColor = true) {

            Mesh mesh = new PlyReader() {
                PointFormat = hasColor ?
                    new ColoredPointFormat() :
                    new PointFormat()
            }.ReadMesh(pointCloudFile);

            JToken jsonData = JSONTools.Read(colmapToNeRFStudioTransformationFile);

            mesh.Scale(
                1.0 / jsonData.ReadDouble("json_to_nerfstudio_scale_factor"),
                true);

            mesh.Transform(
                jsonData
                    .ReadPose("json_to_nerfstudio_transformation")
                    .Inverted(),
                true);

            mesh.Transform(
                jsonData
                    .ReadPose("source_to_json_transformation")
                    .Inverted(),
                true);

            new PlyWriter() {
                PointFormat = hasColor ?
                    new ColoredPointFormat() :
                    new PointFormat()
            }.Write(
                FileSystemUtils.GetFileWithPostfix(
                    pointCloudFile,
                    "_toColmap"),
                mesh);
        }

        public static void ConvertNeRFStudioPointCloudToSDFStudio(
                string pointCloudFile,
                string colmapToNeRFStudioTransformationFile,
                string nerfStudioToSDFStudioTransformationFile,
                bool hasColor = true) {

            Mesh mesh = new PlyReader() {
                PointFormat = hasColor ?
                    new ColoredPointFormat() :
                    new PointFormat()
            }.ReadMesh(pointCloudFile);

            if (colmapToNeRFStudioTransformationFile != null) {

                JToken jsonData = JSONTools.Read(colmapToNeRFStudioTransformationFile);

                mesh.Scale(
                    1.0 / jsonData.ReadDouble("json_to_nerfstudio_scale_factor"),
                    true);

                mesh.Transform(
                    jsonData
                        .ReadPose("json_to_nerfstudio_transformation")
                        .Inverted(),
                    true);
            }

            foreach (Pose transformation in ReadRegistrationTransformations(
                    nerfStudioToSDFStudioTransformationFile)) {

                mesh.Transform(transformation);
            }

            new PlyWriter() {
                PointFormat = hasColor ?
                    new ColoredPointFormat() :
                    new PointFormat()
            }.Write(
                FileSystemUtils.GetFileWithPostfix(
                    pointCloudFile,
                    "_toSDFStudio"),
                mesh);
        }

        public static void ConvertCropToSDFStudio(
                string cropFile,
                string colmapToNeRFStudioTransformationFile,
                string nerfStudioToSDFStudioTransformationFile,
                string outputFile) {

            ReadCrop(
                cropFile,
                out Vector3d center,
                out Vector3d size,
                out Matrix3d orientation);

            JToken jsonData = JSONTools.Read(colmapToNeRFStudioTransformationFile);

            double scale = jsonData.ReadDouble("json_to_nerfstudio_scale_factor");

            center /= scale;
            size /= scale;

            Pose nerfstudioTransformation = jsonData
                .ReadPose("json_to_nerfstudio_transformation")
                .Inverted();

            center = nerfstudioTransformation * center;
            orientation = nerfstudioTransformation.OrientationMatrix * orientation;

            foreach (Pose transformation in ReadRegistrationTransformations(
                    nerfStudioToSDFStudioTransformationFile)) {

                center = transformation * center;
                orientation = transformation.OrientationMatrix * orientation;
            }

            ExportCropToJson(
                outputFile,
                center,
                size,
                orientation);
        }

        public static void ExportCropToMesh(
                string cropFile) {

            ReadCrop(
                cropFile,
                out Vector3d center,
                out Vector3d size,
                out Matrix3d orientation);

            Mesh mesh = AABox
                .FromCenterAndSize(center, size)
                .Mesh;

            mesh.Rotate(
                new Rotation(orientation));

            mesh.ExportMeshPly(
                FileSystemUtils.GetWithNewExtension(
                    cropFile,
                    "ply"));
        }

        public static void ExportPosesToPLY(
                string posesFile,
                double axisLength,
                double pointResolution,
                string transformationFile = null,
                bool toColmap = false) {

            string outputFile = FileSystemUtils.GetWithNewExtension(
                posesFile,
                "ply");

            List<Pose> poses = LoadPoses(posesFile);

            if (transformationFile != null) {

                if (toColmap) {

                    poses.TransformToColmap(transformationFile);

                    outputFile = FileSystemUtils.GetFileWithPostfix(
                        outputFile,
                        "_toColmap");
                }
                else {
                    poses.TransformToNeRFStudio(transformationFile);

                    outputFile = FileSystemUtils.GetFileWithPostfix(
                        outputFile,
                        "_toNeRFStudio");
                }
            }
            else if (toColmap) {

                throw new ApplicationException(
                    "ToColmap flag can only be used when transformation file is given.");
            }

            poses.ExportPly(
                outputFile,
                1,
                axisLength,
                pointResolution);
        }

        public static void TestGridCoordinateConversion(
                string cropFile,
                int testCount,
                double resolution,
                bool onlyOnGridSurface = false) {

            ReadCrop(
                cropFile,
                out Vector3d cropCenter,
                out Vector3d cropSize,
                out _);

            Vector3d cropMinCoordinate = cropCenter - 0.5 * cropSize;
            Vector3d cropMaxCoordinate = cropCenter + 0.5 * cropSize;

            Vector3i gridSize = GridTools.GetGridSize(
                resolution,
                cropMinCoordinate,
                cropMaxCoordinate);

            for (int i = 0; i < testCount; i++) {

                Vector3i gridCoordinate;
                Vector3d coordinate;

                gridCoordinate = Random.GetVector3i(
                    new Vector3i(0),
                    gridSize - new Vector3i(1));

                if (onlyOnGridSurface) {

                    switch (Random.GetInteger(0, 5)) {

                        case 0:
                            gridCoordinate.X = 0;
                            break;

                        case 1:
                            gridCoordinate.X = gridSize.X - 1;
                            break;

                        case 2:
                            gridCoordinate.Y = 0;
                            break;

                        case 3:
                            gridCoordinate.Y = gridSize.Y - 1;
                            break;

                        case 4:
                            gridCoordinate.Z = 0;
                            break;

                        case 5:
                            gridCoordinate.Z = gridSize.Z - 1;
                            break;

                        default:
                            break;
                    }
                }

                AABox box = AABox.FromCenterAndSize(
                    cropMinCoordinate + resolution * gridCoordinate.ToVector3d(),
                    new Vector3d(resolution));

                coordinate = Random.GetVector3d(
                        box.Min,
                        box.Max);

                if (onlyOnGridSurface) {

                    if (gridCoordinate.X == 0) {

                        coordinate.X = box.Min.X;
                    }
                    else if (gridCoordinate.X == gridSize.X - 1) {

                        coordinate.X = box.Max.X;
                    }

                    if (gridCoordinate.Y == 0) {

                        coordinate.Y = box.Min.Y;
                    }
                    else if (gridCoordinate.Y == gridSize.Y - 1) {

                        coordinate.Y = box.Max.Y;
                    }

                    if (gridCoordinate.Z == 0) {

                        coordinate.Z = box.Min.Z;
                    }
                    else if (gridCoordinate.Z == gridSize.Z - 1) {

                        coordinate.Z = box.Max.Z;
                    }
                }

                Vector3i gridCoordinate2 = GridTools.GetGridCoordinate(
                    resolution,
                    coordinate,
                    cropMinCoordinate);

                if (gridCoordinate2.X < 0) {

                    gridCoordinate2.X += 1;
                }
                if (gridCoordinate2.Y < 0) {

                    gridCoordinate2.Y += 1;
                }
                if (gridCoordinate2.Z < 0) {

                    gridCoordinate2.Z += 1;
                }
                if (gridCoordinate2.X >= gridSize.X) {

                    gridCoordinate2.X -= 1;
                }
                if (gridCoordinate2.Y >= gridSize.Y) {

                    gridCoordinate2.Y -= 1;
                }
                if (gridCoordinate2.Z >= gridSize.Z) {

                    gridCoordinate2.Z -= 1;
                }

                if (gridCoordinate != gridCoordinate2) {

                    throw new ApplicationException();
                }
            }
        }

        public static void CompareCrossSections(
                string crossSectionFileTemplate1,
                string crossSectionFileTemplate2,
                bool inverted = false) {

            foreach (string postfix in new string[] { 
                "_x", "_y", "_z"
            }) {

                string crossSectionFile1 = FileSystemUtils.GetFileWithPostfix(
                    crossSectionFileTemplate1,
                    postfix);

                string crossSectionFile2 = FileSystemUtils.GetFileWithPostfix(
                    crossSectionFileTemplate2,
                    postfix);

                if (!File.Exists(crossSectionFile1)) {
                    throw new ApplicationException($"File '{crossSectionFile1}' does not exist.");
                }

                if (!File.Exists(crossSectionFile2)) {
                    throw new ApplicationException($"File '{crossSectionFile2}' does not exist.");
                }

                CompareCrossSectionFiles(
                    crossSectionFile1,
                    crossSectionFile2,
                    inverted);
            }
        }

        public static void RenderReferenceDepthImagesInColmapSpace(
                string colmapDirectory,
                string meshFile,
                string colmapToNeRFStudioTransformationFile,
                string inputImageDirectory = null,
                bool useBBoxIntersectsForOctTree = true) {

            const double BLEND_FACTOR = 0.5;

            string outputDirectory = Path.GetDirectoryName(meshFile) 
                + $"/{Path.GetFileNameWithoutExtension(meshFile)}_depth";

            if (Directory.Exists(outputDirectory)) {

                FileSystemUtils.CleanDirectory(outputDirectory);
            }
            else {
                Directory.CreateDirectory(outputDirectory);
            }

            string rawOutputDirectory = $"{outputDirectory}/raw";
            string overlayOutputDirectory = $"{outputDirectory}/overlay";

            Directory.CreateDirectory(rawOutputDirectory);
            Directory.CreateDirectory(overlayOutputDirectory);

            Mesh mesh = new PlyReader().ReadMesh(meshFile);

            JToken jsonData = JSONTools.Read(colmapToNeRFStudioTransformationFile);

            mesh.Scale(
                1.0 / jsonData.ReadDouble("json_to_nerfstudio_scale_factor"),
                true);

            mesh.Transform(
                jsonData
                    .ReadPose("json_to_nerfstudio_transformation")
                    .Inverted(),
                true);

            mesh.Transform(
                jsonData
                    .ReadPose("source_to_json_transformation")
                    .Inverted(),
                true);

            OctTree<Face> octTree = new OctTree<Face>(true) {
                UseBBoxIntersects = useBBoxIntersectsForOctTree
            };

            octTree.Load(mesh);

            List<Pose> poses = ColmapTools.LoadTrajectory(
                $"{colmapDirectory}/images.txt",
                $"{colmapDirectory}/cameras.txt");

#if DEBUG_COLMAP_DEPTH_IMAGE_RENDERING

            int poseIndex = 0;

            mesh.ExportMeshPly(
                $"{outputDirectory}/mesh.ply");

            poses
                .CreateVisualizationPointCloud(0.2, 0.002)
                .ExportPointsPly($"{outputDirectory}/poses.ply");

            PlyStreamWriter raysWriter = new PlyStreamWriter(
                    $"{outputDirectory}/rays.ply") {
                PointFormat = new PointFormat {
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddColor()
                        .AddFloatProperty("poseIndex")
                }
            };
#endif
            foreach (Pose pose in poses) {

                string colorImageFileName = pose.GetImageFileName();

                string outputFileName = FileSystemUtils.GetWithNewExtension(
                    colorImageFileName,
                    "tif");

                InnerOrientation innerOrientation = pose.GetInnerOrientation();

                using (Mat depthImage = new Mat(
                        innerOrientation.Height,
                        innerOrientation.Width,
                        MatType.CV_64FC1,
                        new Scalar(0.0))) {

                    using (Mat<double> _depthImage = new Mat<double>(depthImage)) {

                        MatIndexer<double> depthImageData = _depthImage.GetIndexer();

#if DEBUG_COLMAP_DEPTH_IMAGE_RENDERING
                        {
                            for (int r = 0; r < innerOrientation.Height; r++) {
#else
                        Parallel.For(
                            0,
                            innerOrientation.Height,
                            r => {
#endif
                                for (int c = 0; c < innerOrientation.Width; c++) {

                                    Ray ray = new Ray(
                                        pose.Position,
                                        pose.OrientationMatrix * new Vector3d(
                                            (c - innerOrientation.PrincipalPointX)
                                                / innerOrientation.FocalLengthX,
                                            (r - innerOrientation.PrincipalPointY)
                                                / innerOrientation.FocalLengthY,
                                            1.0));

#if DEBUG_COLMAP_DEPTH_IMAGE_RENDERING
                                    if (r == (int)(innerOrientation.Height / 2.0)
                                            && c == (int)(innerOrientation.Width / 2.0)) {

                                        PointCloud rayPoints = ray.CreateVisualizationPointCloud(5.0, 0.01);
                                        rayPoints.SetColor(Color.Gray);
                                        rayPoints.SetFloatProperty("poseIndex", poseIndex);
                                        rayPoints.PropagatePropertiesToPoints();
                                        raysWriter.Write(rayPoints);
                                    }
#endif

                                    List<MultiGeometryIntersection<Face>> intersections = octTree.Intersect(ray);

                                    if (intersections.Count == 0) {
                                        continue;
                                    }

                                    double depth = intersections
                                        .WhereMin(intersection => intersection.Distance)
                                        .First()
                                        .Distance;

                                    depthImageData[r, c] = depth;
                                }
#if DEBUG_COLMAP_DEPTH_IMAGE_RENDERING
                            }

                            poseIndex++;
                        }
#else
                            });
#endif
                    }

                    depthImage.ImWrite($"{rawOutputDirectory}/{outputFileName}");

                    using (Mat overlayImage = depthImage.Colorize(
                            min : null,
                            max : null,
                            rangeDeterminationMin: double.Epsilon,
                            backGroundColor : Color.Black)) {

                        if (inputImageDirectory != null
                                && Directory.Exists(inputImageDirectory)) {

                            string colorImageFile = $"{inputImageDirectory}/{colorImageFileName}";

                            if (File.Exists(colorImageFile)) {

                                using (Mat colorImage = new Mat(colorImageFile)) {

                                    using (Mat<double> _depthImage = new Mat<double>(depthImage)) {

                                        MatIndexer<double> depthImageData = _depthImage.GetIndexer();

                                        using (Mat<Vec3b>
                                            _overlayImage = new Mat<Vec3b>(overlayImage),
                                            _colorImage = new Mat<Vec3b>(colorImage)) {

                                            MatIndexer<Vec3b> overlayImageData = _overlayImage.GetIndexer();
                                            MatIndexer<Vec3b> colorImageData = _colorImage.GetIndexer();

                                            Parallel.For(
                                                0,
                                                depthImage.Height,
                                                r => {

                                                    for (int c = 0; c < depthImage.Width; c++) {

                                                        if (depthImageData[r, c] == 0.0) {

                                                            overlayImageData[r, c] = colorImageData[r, c];
                                                        }
                                                        else {

                                                            overlayImageData[r, c] = OpenCvExtensions.ToOpenCVVec3b(
                                                                (1.0 - BLEND_FACTOR) * colorImageData[r, c].ToOpenTKVector3d()
                                                                    + BLEND_FACTOR * overlayImageData[r, c].ToOpenTKVector3d());
                                                        }
                                                    }
                                                });
                                        }
                                    }
                                }
                            }
                        }

                        overlayImage.ImWrite($"{overlayOutputDirectory}/{outputFileName}");
                    }
                }
            }
#if DEBUG_COLMAP_DEPTH_IMAGE_RENDERING
            raysWriter.Dispose();
#endif
        }

        private static double[,,] Load(
                string densityGridFile) {

            string splitDirectory = Directory
                .GetDirectories(
                    Path.GetDirectoryName(densityGridFile))
                .Where(FileSystemUtils.IsDirectory)
                .Where(directory => directory.Contains("_split_"))
                .FirstOrDefault();

            if (splitDirectory == null) {

                return (double[,,])np
                    .load(densityGridFile)
                    .ToMuliDimArray<double>();
            }

            int[] splitValues = splitDirectory
                .Split("_split_")[1]
                .Split("_")
                .Select(int.Parse)
                .ToArray();

            if (splitValues.Length != 4) {

                throw new ArgumentException(
                    "Split directory path is expected to contain 4 values.");
            }

            int splitBatchStartIndex = 0;

            double[,,] densityGrid = new double[
                splitValues[0],
                splitValues[1],
                splitValues[2]];

            for (int i = 0; i < splitValues[3]; i++) {

                double[,,] splitBatch = (double[,,])np
                    .load($"{splitDirectory}/split_{i}.npy")
                    .ToMuliDimArray<double>();

                Parallel.For(
                    0,
                    splitBatch.GetLength(0),
                    x => {

                        for (int y = 0; y < densityGrid.GetLength(1); y++) {
                            for (int z = 0; z < densityGrid.GetLength(2); z++) {

                                densityGrid[
                                    splitBatchStartIndex + x,
                                    y,
                                    z
                                ] = splitBatch[x, y, z];
                            }
                        }
                    });

                splitBatchStartIndex += splitBatch.GetLength(0);
            }

            return densityGrid;
        }

        private static (double min, double max) GetRange(
                this double[,,] grid) {

            return grid.ParallelIterate(
                () => (min: double.MaxValue, max: double.MinValue),
                (localRange, density) => {

                    if (density < localRange.min) {
                        localRange.min = density;
                    }
                    if (density > localRange.max) {
                        localRange.max = density;
                    }

                    return localRange;
                },
                (range, localRange) => {

                    if (localRange.min < range.min) {
                        range.min = localRange.min;
                    }
                    if (localRange.max > range.max) {
                        range.max = localRange.max;
                    }

                    return range;
                });
        }

        private static T ParallelIterate<T>(
                this double[,,] densityGrid,
                Func<T> localStateCreationCallback,
                Func<T, double, T> localStateUpdateCallback,
                Func<T, T, T> stateAggregationCallback) {

            object @lock = new object();
            T state = localStateCreationCallback();

            Parallel.For(
                0,
                densityGrid.GetLength(0),
                () => localStateCreationCallback(),
                (x, loopState, localState) => {

                    for (int y = 0; y < densityGrid.GetLength(1); y++) {
                        for (int z = 0; z < densityGrid.GetLength(2); z++) {

                            localState = localStateUpdateCallback(
                                localState,
                                densityGrid[x, y, z]);
                        }
                    }

                    return localState;
                },
                localState => {

                    lock (@lock) {

                        state = stateAggregationCallback(
                            state,
                            localState);
                    }
                });

            return state;
        }

        private static Histogram GetHistogram(
                this double[,,] densityGrid,
                double binSize) {

            return densityGrid
                .ParallelIterate(
                    () => new Histogram(binSize),
                    (localHistogram, density) => {

                        localHistogram.Add(density);

                        return localHistogram;
                    },
                    (histogram, localHistogram) => {

                        histogram.Add(localHistogram);

                        return histogram;
                    });
        }

        private static double[,,] Normalize(
                this double[,,] grid) {

            (double min, double max) rangeBounds = grid.GetRange();

            double range = rangeBounds.max - rangeBounds.min;

            double[,,] result = new double[
                grid.GetLength(0),
                grid.GetLength(1),
                grid.GetLength(2)];

            Parallel.For(
                0,
                grid.GetLength(0),
                x => {

                    for (int y = 0; y < grid.GetLength(1); y++) {
                        for (int z = 0; z < grid.GetLength(2); z++) {

                            result[x, y, z] = (grid[x, y, z] - rangeBounds.min) / range;
                        }
                    }
                });

            return result;
        }

        private static bool[,,] ApplyDensityThreshold(
                this double[,,] densityGrid,
                double densityThreshold) {

            bool[,,] result = new bool[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];

            Parallel.For(
                0,
                densityGrid.GetLength(0),
                x => {

                    for (int y = 0; y < densityGrid.GetLength(1); y++) {
                        for (int z = 0; z < densityGrid.GetLength(2); z++) {

                            if (densityGrid[x, y, z] > densityThreshold) {

                                result[x, y, z] = true;
                            }
                        }
                    }
                });

            return result;
        }

        private static void Export(
                string file,
                double resolution,
                Vector3d cropCenter,
                Vector3d cropSize,
                Matrix3d cropOrientation,
                bool[,,] occupancyGrid,
                double[,,] densityGrid) {

            Vector3d cropMinCoordinate = cropCenter - 0.5 * cropSize;

            using (PlyStreamWriter writer = new PlyStreamWriter(file) {
                PointFormat = new PointFormat() {

                    PropertyDescriptor = new PropertyDescriptor()
                        .AddFloatProperty("density")
                }
            }) {
                for (int x = 0; x < densityGrid.GetLength(0); x++) {
                    for (int y = 0; y < densityGrid.GetLength(1); y++) {
                        for (int z = 0; z < densityGrid.GetLength(2); z++) {

                            if (occupancyGrid[x, y, z]) {

                                Point point = new Point(
                                    new Vector3i(x, y, z)
                                        .Transform(
                                            resolution,
                                            cropMinCoordinate,
                                            cropCenter,
                                            cropOrientation));

                                point.SetFloatProperty("density", (float)densityGrid[x, y, z]);

                                writer.Write(point);
                            }
                        }
                    }
                }
            }
        }

        private static Vector3d Transform(
                this Vector3i coordinate,
                double resolution,
                Vector3d cropMinCoordinate,
                Vector3d cropCenter,
                Matrix3d cropOrientation) {

            return (cropMinCoordinate + resolution * coordinate.ToVector3d())
                .RotateCoordinate(
                    cropOrientation,
                    cropCenter);
        }

        private static bool[,,] CullInvisibleCells(
                this bool[,,] occupancyGrid,
#if DEBUG_CULLING
                string densityGridFile,
#endif
                double resolution,
                Vector3d cropCenter,
                Vector3d cropSize,
                Matrix3d cropOrientation,
                List<Pose> poses) {

            //// TEST
            //poses = poses.Take(100).ToList();
            //// TEST

#if DEBUG_CULLING
            string debugDirectory = Path.GetDirectoryName(densityGridFile)
                + $"/{Path.GetFileNameWithoutExtension(densityGridFile)}_DEBUG_CULLING";

            string rayMarchingDebugDirectory = $"{debugDirectory}/rayMarching";

            if (Directory.Exists(debugDirectory)) {
                
                FileSystemUtils.CleanDirectory(debugDirectory);
            }
            else {
                Directory.CreateDirectory(debugDirectory);
            }

            Directory.CreateDirectory(rayMarchingDebugDirectory);

            poses.ExportPly(
                $"{debugDirectory}/poses.ply",
                1,
                0.1,
                0.001);
#endif
            Vector3i gridSize = occupancyGrid.GetSize();

            Vector3d cropMinCoordinate = cropCenter - 0.5 * cropSize;

            InitializeGridDirections(
                out Dictionary<byte, Vector3i> gridDirections,
                out Dictionary<Vector3i, byte> gridDirectionCodes);

#if DEBUG_CULLING
            gridDirections.ExportPly(
                $"{debugDirectory}/gridDirections.ply");
#endif

            Timer timer = Timer.Instance;

            long t1 = timer.Timestamp;

            Dictionary<Vector3i, HashSet<byte>> rayMarchingCells = InitializeRayMarchingCells(
#if DEBUG_CULLING
                debugDirectory,
#endif
                resolution,
                gridSize,
                cropMinCoordinate,
                cropCenter,
                cropSize,
                cropOrientation,
                poses,
                gridDirectionCodes);

            long t2 = timer.Timestamp;

            System.Console.WriteLine("Cells Initialized!");
            System.Console.WriteLine(HuePat.Util.Time.Extensions.FormatMilliseconds((double)(t2 - t1)));

            return rayMarchingCells
                .March(
#if DEBUG_CULLING
                    rayMarchingDebugDirectory,
                    resolution,
                    cropMinCoordinate,
                    cropCenter,
                    cropOrientation,
#endif
                    occupancyGrid,
                    gridDirections)
                .ToOccupancyGrid(
                    occupancyGrid.GetSize());
        }

#if DEBUG_CULLING
        private static void ExportPly(
                this Dictionary<byte, Vector3i> gridDirections,
                string file) {

            using (PlyStreamWriter writer = new PlyStreamWriter(file) { 
                PointFormat = new PointFormat() {
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddColor()
                        .AddFloatProperty("gridDirectionCode")
                }
            }) {
                foreach (byte gridDirectionCode in gridDirections.Keys) {

                    Ray ray = new Ray(
                        new Vector3d(0.0),
                        gridDirections[gridDirectionCode].ToVector3d());

                    PointCloud rayVisualizationPoints = ray.CreateVisualizationPointCloud(
                        1.0,
                        0.01);

                    rayVisualizationPoints.SetColor(Color.Gray);

                    rayVisualizationPoints.SetFloatProperty(
                        "gridDirectionCode",
                        gridDirectionCode);

                    rayVisualizationPoints.PropagatePropertiesToPoints();

                    writer.Write(rayVisualizationPoints);
                }
            }
        }
#endif

        private static Dictionary<Vector3i, HashSet<byte>> InitializeRayMarchingCells(
#if DEBUG_CULLING
                string debugDirectory,
#endif
                double resolution,
                Vector3i gridSize,
                Vector3d cropMinCoordinate,
                Vector3d cropCenter,
                Vector3d cropSize,
                Matrix3d cropOrientation,
                List<Pose> poses,
                Dictionary<Vector3i, byte> gridDirectionCodes) {

#if (PARALLELIZE_CULLING_OVER_POSES && PARALLELIZE_CULLING_OVER_RAYS) || (!PARALLELIZE_CULLING_OVER_POSES && !PARALLELIZE_CULLING_OVER_RAYS)
            throw new ApplicationException();
#endif

            object @lock = new object();
            double angleThreshold = (22.5).DegreeToRadian();

            AABox cropBox = AABox.FromCenterAndSize(
                cropCenter,
                cropSize);

#if DEBUG_CULLING
            Mesh mesh = cropBox.Mesh;

            mesh.Rotate(new Rotation(cropOrientation) {
                Anchor = cropCenter
            });

            mesh.ExportMeshPly(
                $"{debugDirectory}/crop.ply");
#endif

            Dictionary<Vector3i, Vector3d> directions = new Dictionary<Vector3i, Vector3d>();
            Dictionary<Vector3i, HashSet<byte>> rayMarchingCells = new Dictionary<Vector3i, HashSet<byte>>();

            foreach (Vector3i gridDirection in gridDirectionCodes.Keys) {

                directions.Add(
                    gridDirection,
                    gridDirection.ToVector3d());
            }

#if (DEBUG_CULLING && (DEBUG_CULLING_EXPORT_RAYS || DEBUG_CULLING_EXPORT_RAYCASTING_POINTS)) || PARALLELIZE_CULLING_OVER_RAYS
            Dictionary<Vector3i, HashSet<byte>> localRayMarchingCells = new Dictionary<Vector3i, HashSet<byte>>();

            for (int i = 0; i < poses.Count; i++) {
                {
#else
            Parallel.For(
                0,
                poses.Count,
                () => new Dictionary<Vector3i, HashSet<byte>>(),
                (i, loopState, localRayMarchingCells) => {
#endif

                    bool found;
                    byte minAngleGridDirectionCode = 0;
                    double angle;
                    double minAngle;
                    Vector3i lastGridDirection = gridDirectionCodes.Keys.Last();

                    Pose pose = poses[i].Rotated(
                        cropOrientation.Inverted(),
                        cropCenter);

                    InnerOrientation innerOrientation = pose.GetInnerOrientation();

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS
                   PlyStreamWriter rayWriter = new PlyStreamWriter(
                             $"{debugDirectory}/pose{i}_rays.ply") {
                        PointFormat = new ColoredPointFormat()
                    };
#endif

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYCASTING_POINTS
                    PlyStreamWriter pointWriter = new PlyStreamWriter(
                             $"{debugDirectory}/pose{i}_points.ply") {
                        PointFormat = new ColoredPointFormat()
                    };
#endif

#if PARALLELIZE_CULLING_OVER_POSES
                    for (int r = 0; r < innerOrientation.Height; r++) {
                        {
#endif
#if PARALLELIZE_CULLING_OVER_RAYS
                    Parallel.For(
                        0,
                        innerOrientation.Height,
                        () => new Dictionary<Vector3i, HashSet<byte>>(),
                        (r, loopState, localRayMarchingCells) => {
#endif

                            for (int c = 0; c < innerOrientation.Width; c++) {

                                Ray ray = new Ray(
                                    pose.Position,
                                    pose.OrientationMatrix * new Vector3d(
                                        (c - innerOrientation.PrincipalPointX)
                                            / innerOrientation.FocalLengthX,
                                        (r - innerOrientation.PrincipalPointY)
                                            / innerOrientation.FocalLengthY,
                                        -1.0));

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS
                                if (r % 10 == 0 && c % 10 == 0) {

                                    PointCloud rayVisualizationPoints = ray.CreateVisualizationPointCloud(
                                        1.0,
                                        0.01);

                                    rayVisualizationPoints.Rotate(
                                        new Rotation(cropOrientation) {
                                            Anchor = cropCenter
                                        });

                                    rayWriter.Write(rayVisualizationPoints);
                                }
#endif

                                List<Intersection> intersections = cropBox.Intersect(ray);

                                if (intersections.Count == 0) {
                                    continue;
                                }

                                Vector3d coordinate = intersections
                                    .WhereMin(intersection => intersection.Distance)
                                    .Select(intersection => intersection.Position)
                                    .First();

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYCASTING_POINTS
                                pointWriter.Write(
                                    new Point(
                                            coordinate.RotateCoordinate(
                                                cropOrientation,
                                                cropCenter))
                                        .SetColor(Color.Red));
#endif

                                Vector3i gridCoordinate = GridTools.GetGridCoordinate(
                                    resolution,
                                    coordinate,
                                    cropMinCoordinate);

                                if (gridCoordinate.X < 0) {

                                    gridCoordinate.X += 1;
                                }
                                if (gridCoordinate.Y < 0) {

                                    gridCoordinate.Y += 1;
                                }
                                if (gridCoordinate.Z < 0) {

                                    gridCoordinate.Z += 1;
                                }
                                if (gridCoordinate.X >= gridSize.X) {

                                    gridCoordinate.X = gridSize.X - 1;
                                }
                                if (gridCoordinate.Y >= gridSize.Y) {

                                    gridCoordinate.Y = gridSize.Y - 1;
                                }
                                if (gridCoordinate.Z >= gridSize.Z) {

                                    gridCoordinate.Z = gridSize.Z - 1;
                                }

                                if (ray.Direction
                                            .AngleTo(lastGridDirection)
                                            .Abs()
                                        < angleThreshold) {

                                    localRayMarchingCells.BucketAdd(
                                        gridCoordinate,
                                        gridDirectionCodes[lastGridDirection]);

                                    continue;
                                }

                                found = false;
                                minAngle = double.MaxValue;

                                foreach (Vector3i gridDirection in gridDirectionCodes.Keys) {

                                    angle = ray.Direction
                                        .AngleTo(directions[gridDirection])
                                        .Abs();

                                    if (angle < angleThreshold) {

                                        localRayMarchingCells.BucketAdd(
                                            gridCoordinate,
                                            gridDirectionCodes[gridDirection]);

                                        found = true;

                                        break;
                                    }

                                    if (angle < minAngle) {

                                        minAngle = angle;
                                        minAngleGridDirectionCode = gridDirectionCodes[gridDirection];
                                    }
                                }

                                if (!found) {

                                    localRayMarchingCells.BucketAdd(
                                        gridCoordinate,
                                        minAngleGridDirectionCode);
                                }

                            }

#if DEBUG_CULLING && (DEBUG_CULLING_EXPORT_RAYS || DEBUG_CULLING_EXPORT_RAYCASTING_POINTS)
                        }
                    }
#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS

                    rayWriter.Dispose();
#endif
#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYCASTING_POINTS
                    pointWriter.Dispose();
#endif

                }
            }

            rayMarchingCells = localRayMarchingCells;
#else
#if PARALLELIZE_CULLING_OVER_POSES
                        }
                    }
#endif
                    return localRayMarchingCells;
                },
                localRayMarchingCells => {

                    lock (@lock) {

                        rayMarchingCells.BucketAdd(localRayMarchingCells);
                    }
                });
#if PARALLELIZE_CULLING_OVER_RAYS
                }
            }
#endif
#endif
            return rayMarchingCells;
        }

        private static Dictionary<Vector3i, HashSet<byte>> InitializeRayMarchingCells_OLD(
#if DEBUG_CULLING
                string debugDirectory,
#endif
                double resolution,
                Vector3d cropMinCoordinate,
                Vector3d cropCenter,
                Vector3d cropSize,
                Matrix3d cropOrientation,
                bool[,,] occupancyGrid,
                List<Pose> poses,
                Dictionary<Vector3i, byte> gridDirectionCodes) {

            List<Quad> boundingQuads = occupancyGrid.GetBoundingQuads_OLD(
                resolution,
                cropMinCoordinate);

#if DEBUG_CULLING
            {
                Mesh boundingQuadsVisualization = Mesh.From(boundingQuads);

                boundingQuadsVisualization.Rotate(
                    new Rotation(
                            cropOrientation) {
                        Anchor = cropCenter,
                        UseParallel = true
                    });

                boundingQuadsVisualization.ExportMeshPly(
                    $"{debugDirectory}/boundingQuads.ply");
            }
#endif

            OctTree<Quad> octTree = new OctTree<Quad>(true);

            octTree.Load(boundingQuads);

            return InitializeRayMarchingCells_OLD(
#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS
                debugDirectory,
#endif
                cropCenter,
                cropOrientation,
                octTree,
                poses,
                gridDirectionCodes);
        }

        private static List<Quad> GetBoundingQuads_OLD<T>(
                this T[,,] grid,
                double resolution,
                Vector3d minCoordinate) {

            object @lock = new object();
            List<Quad> boundingQuads = new List<Quad>();

            Parallel.For(
                0,
                grid.GetLength(0),
                () => new List<Quad>(),
                (x, loopState, localBoundingQuads) => {

                    int[] signs = new int[] {
                        -1,
                        1
                    };

                    int[] zs = new int[] {
                        0,
                        grid.GetLength(2) - 1
                    };

                    int[] ys = new int[] {
                        0,
                        grid.GetLength(1) - 1
                    };

                    for (int y = 0; y < grid.GetLength(1); y++) {
                        for (int i = 0; i < 2; i++) {

                            double z = zs[i] * resolution + signs[i] * 0.5 * resolution;

                            Vector3d[] cornerCoordinates = new Vector3d[4] {
                                new Vector3d(
                                    x * resolution - 0.5 * resolution,
                                    y * resolution - 0.5 * resolution,
                                    z),
                                new Vector3d(
                                    x * resolution + 0.5 * resolution,
                                    y * resolution - 0.5 * resolution,
                                    z),
                                new Vector3d(
                                    x * resolution + 0.5 * resolution,
                                    y * resolution + 0.5 * resolution,
                                    z),
                                new Vector3d(
                                    x * resolution - 0.5 * resolution,
                                    y * resolution + 0.5 * resolution,
                                    z)
                            };

                            for (int j = 0; j < cornerCoordinates.Length; j++) {

                                cornerCoordinates[j] += minCoordinate;
                            }

                            localBoundingQuads.Add(
                                new Quad(
                                    new Vector3i(x, y, zs[i]),
                                    new Triangle(
                                        cornerCoordinates[0],
                                        cornerCoordinates[1],
                                        cornerCoordinates[3]),
                                    new Triangle(
                                        cornerCoordinates[1],
                                        cornerCoordinates[2],
                                        cornerCoordinates[3])));
                        }
                    }

                    for (int z = 0; z < grid.GetLength(2); z++) {
                        for (int i = 0; i < 2; i++) {

                            double y = ys[i] * resolution + signs[i] * 0.5 * resolution;

                            Vector3d[] cornerCoordinates = new Vector3d[4] {
                                new Vector3d(
                                    x * resolution - 0.5 * resolution,
                                    y,
                                    z * resolution - 0.5 * resolution),
                                new Vector3d(
                                    x * resolution + 0.5 * resolution,
                                    y,
                                    z * resolution - 0.5 * resolution),
                                new Vector3d(
                                    x * resolution + 0.5 * resolution,
                                    y,
                                    z * resolution + 0.5 * resolution),
                                new Vector3d(
                                    x * resolution - 0.5 * resolution,
                                    y,
                                    z * resolution + 0.5 * resolution)
                            };

                            for (int j = 0; j < cornerCoordinates.Length; j++) {

                                cornerCoordinates[j] += minCoordinate;
                            }

                            localBoundingQuads.Add(
                                new Quad(
                                    new Vector3i(x, ys[i], z),
                                    new Triangle(
                                        cornerCoordinates[0],
                                        cornerCoordinates[1],
                                        cornerCoordinates[3]),
                                    new Triangle(
                                        cornerCoordinates[1],
                                        cornerCoordinates[2],
                                        cornerCoordinates[3])));
                        }
                    }

                    return localBoundingQuads;
                },
                localBoundingQuads => {

                    lock (@lock) {

                        boundingQuads.AddRange(localBoundingQuads);
                    }
                });

            Parallel.For(
                0,
                grid.GetLength(1),
                () => new List<Quad>(),
                (y, loopState, localBoundingQuads) => {

                    int[] signs = new int[] {
                        -1,
                        1
                    };

                    int[] xs = new int[] {
                        0,
                        grid.GetLength(0) - 1
                    };

                    for (int z = 0; z < grid.GetLength(2); z++) {
                        for (int i = 0; i < 2; i++) {

                            double x = xs[i] * resolution + signs[i] * 0.5 * resolution;

                            Vector3d[] cornerCoordinates = new Vector3d[4] {
                                new Vector3d(
                                    x,
                                    y * resolution - 0.5 * resolution,
                                    z * resolution - 0.5 * resolution),
                                new Vector3d(
                                    x,
                                    y * resolution + 0.5 * resolution,
                                    z * resolution - 0.5 * resolution),
                                new Vector3d(
                                    x,
                                    y * resolution + 0.5 * resolution,
                                    z * resolution + 0.5 * resolution),
                                new Vector3d(
                                    x,
                                    y * resolution - 0.5 * resolution,
                                    z * resolution + 0.5 * resolution)
                            };

                            for (int j = 0; j < cornerCoordinates.Length; j++) {

                                cornerCoordinates[j] += minCoordinate;
                            }

                            localBoundingQuads.Add(
                                new Quad(
                                    new Vector3i(xs[i], y, z),
                                    new Triangle(
                                        cornerCoordinates[0],
                                        cornerCoordinates[1],
                                        cornerCoordinates[3]),
                                    new Triangle(
                                        cornerCoordinates[1],
                                        cornerCoordinates[2],
                                        cornerCoordinates[3])));
                        }
                    }

                    return localBoundingQuads;
                },
                localBoundingQuads => {

                    lock (@lock) {

                        boundingQuads.AddRange(localBoundingQuads);
                    }
                });

            return boundingQuads;
        }

        private static void InitializeGridDirections(
                out Dictionary<byte, Vector3i> gridDirections,
                out Dictionary<Vector3i, byte> gridDirectionCodes) {

            gridDirections = new Dictionary<byte, Vector3i>();
            gridDirectionCodes = new Dictionary<Vector3i, byte>();

            byte code = 0;

            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    for (int z = -1; z <= 1; z++) {

                        if (x != 0
                                || y != 0
                                || z != 0) {

                            Vector3i gridDirection = new Vector3i(x, y, z);

                            gridDirections.Add(
                                code,
                                gridDirection);

                            gridDirectionCodes.Add(
                                gridDirection,
                                code);

                            code++;
                        }
                    }
                }
            }
        }

        private static Dictionary<Vector3i, HashSet<byte>> InitializeRayMarchingCells_OLD(
#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS
                string debugDirectory,
#endif
                Vector3d cropCenter,
                Matrix3d cropOrientation,
                OctTree<Quad> octTree,
                List<Pose> poses,
                Dictionary<Vector3i, byte> gridDirectionCodes) {

            object @lock = new object();
            double angleThreshold = (22.5).DegreeToRadian();
            Dictionary<Vector3i, Vector3d> directions = new Dictionary<Vector3i, Vector3d>();
            Dictionary<Vector3i, HashSet<byte>> rayMarchingCells = new Dictionary<Vector3i, HashSet<byte>>();

            foreach (Vector3i gridDirection in gridDirectionCodes.Keys) {

                directions.Add(
                    gridDirection,
                    gridDirection.ToVector3d());
            }

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS
            Dictionary<Vector3i, HashSet<byte>> localRayMarchingCells = new Dictionary<Vector3i, HashSet<byte>>();

            for (int i = 0; i < poses.Count; i++) {
                {
#else
            Parallel.For(
                0,
                poses.Count,
                () => new Dictionary<Vector3i, HashSet<byte>>(),
                (i, loopState, localRayMarchingCells) => {
#endif

                    Pose pose = poses[i].Rotated(
                        cropOrientation.Inverted(),
                        cropCenter);

                    InnerOrientation innerOrientation = pose.GetInnerOrientation();

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS

                    PlyStreamWriter writer = new PlyStreamWriter(
                             $"{debugDirectory}/pose{i}_rays.ply") {
                        PointFormat = new ColoredPointFormat()
                    };
#endif

                    for (int r = 0; r < innerOrientation.Height; r++) {
                        for (int c = 0; c < innerOrientation.Width; c++) {

                            Ray ray = GetRay(
                                r,
                                c,
                                pose,
                                innerOrientation);

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS
                            if (r % 10 == 0 && c % 10 == 0) {

                                PointCloud rayVisualizationPoints = ray.CreateVisualizationPointCloud(
                                    1.0,
                                    0.01);

                                rayVisualizationPoints.Rotate(
                                    new Rotation(cropOrientation) {
                                        Anchor = cropCenter
                                    });

                                writer.Write(rayVisualizationPoints);
                            }
#endif

                            Quad quad = octTree.GetFirstIntersectingGridCoordinate(ray);

                            if (quad == null) {
                                continue;
                            }

                            foreach (Vector3i gridDirection in gridDirectionCodes.Keys) {

                                if (directions[gridDirection]
                                            .AngleTo(ray.Direction)
                                            .Abs()
                                        < angleThreshold) {

                                    localRayMarchingCells.BucketAdd(
                                        quad.GridCoordinate,
                                        gridDirectionCodes[gridDirection]);

                                    break;
                                }
                            }
                        }
                    }
#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYS

                    writer.Dispose();
                }
            }

            rayMarchingCells = localRayMarchingCells;
#else
                    return localRayMarchingCells;
                },
                localRayMarchingCells => {

                    lock (@lock) {

                        rayMarchingCells.BucketAdd(localRayMarchingCells);
                    }
                });
#endif

            return rayMarchingCells;
        }

        private static Ray GetRay(
                int r,
                int c,
                Pose pose,
                InnerOrientation innerOrientation) {

            return new Ray(
                pose.Position,
                pose.OrientationMatrix * new Vector3d(
                    (c - innerOrientation.PrincipalPointX)
                        / innerOrientation.FocalLengthX,
                    (r - innerOrientation.PrincipalPointY)
                        / innerOrientation.FocalLengthY,
                    -1.0));
        }

        private static List<Vector3i> March(
                this Dictionary<Vector3i, HashSet<byte>> rayMarchingCells,
#if DEBUG_CULLING
                string debugDirectory,
                double resolution,
                Vector3d cropMinCoordinate,
                Vector3d cropCenter,
                Matrix3d cropOrientation,
#endif
                bool[,,] occupancyGrid,
                Dictionary<byte, Vector3i> gridDirections) {

            object @lock = new object();
            Vector3i gridSize = occupancyGrid.GetSize();
            HashSet<Vector3i> hitCoordinates = new HashSet<Vector3i>();
            List<Vector3i> rayMarchingSourceGridCoordinates = rayMarchingCells.Keys.ToList();

#if DEBUG_CULLING
            occupancyGrid.RenderCrossSections(
                true,
                out Mat xCrossSection,
                out Mat yCrossSection,
                out Mat zCrossSection);

            rayMarchingCells.ExportPly(
                debugDirectory,
                resolution,
                cropMinCoordinate,
                cropCenter,
                cropOrientation,
                gridDirections);

            rayMarchingCells.ExportDebugVisualization(
                $"{debugDirectory}/initialization_x.tiff",
                $"{debugDirectory}/initialization_y.tiff",
                $"{debugDirectory}/initialization_z.tiff",
                occupancyGrid.GetSize(),
                xCrossSection,
                yCrossSection,
                zCrossSection);
#endif

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
            string overviewVisualizationDirectory = $"{debugDirectory}/overview";
            bool[,,] initializationGrid = GridTools.Create<bool>(gridSize);
            bool[,,] traversedFreeSpaceGrid = GridTools.Create<bool>(gridSize);
            bool[,,] hitGrid = GridTools.Create<bool>(gridSize);
            HashSet<Vector3i> localHitCoordinates = new HashSet<Vector3i>();

            Vector3i? gridCoordinateToHighlight = new Vector3i(0, 177, 63);
            HashSet<Vector3i> candidateTraversedGridCoordinatesToHighlight = null;
            HashSet<Vector3i> traversedGridCoordinatesToHighlight = null;

            if (gridCoordinateToHighlight.HasValue) {

                traversedGridCoordinatesToHighlight = new HashSet<Vector3i>();
            }
            
            Directory.CreateDirectory(overviewVisualizationDirectory);

            for (int i = 0; i < rayMarchingSourceGridCoordinates.Count; i++) {
                {
#else
            Parallel.For(
                0,
                rayMarchingSourceGridCoordinates.Count,
                () => new HashSet<Vector3i>(),
                (i, loopState, localHitCoordinates) => {
#endif

                    Vector3i gridCoordinate = rayMarchingSourceGridCoordinates[i];
                    Vector3i gridCoordinate2;
                    Vector3i gridDirection;

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
                    initializationGrid.Set(
                        gridCoordinate,
                        true);
#endif

                    if (occupancyGrid.Get(gridCoordinate)) {
#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
                        continue;
#else
                        return localHitCoordinates;
#endif
                    }

                    foreach (byte gridDirectionCode in rayMarchingCells[gridCoordinate]) {

                        gridDirection = gridDirections[gridDirectionCode];

                        gridCoordinate2 = gridCoordinate;

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
                        if (gridCoordinateToHighlight.HasValue) {
                            candidateTraversedGridCoordinatesToHighlight = new HashSet<Vector3i>();
                        }
#endif

                        while (true) {

                            gridCoordinate2 += gridDirection;

                            if (!gridCoordinate2.IsWithinBounds(gridSize)) {
                                break;
                            }

                            if (occupancyGrid.Get(gridCoordinate2)) {

                                if (gridDirection.IsGridDirectionDiagonal()
                                        && occupancyGrid.IsDiagonalHit(
                                            gridCoordinate2,
                                            gridDirection,
                                            gridSize)) {

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
                                    traversedFreeSpaceGrid.Set(
                                        gridCoordinate2,
                                        true);

                                    if (gridCoordinateToHighlight.HasValue) {

                                        candidateTraversedGridCoordinatesToHighlight.Add(gridCoordinate2);
                                    }
#endif

                                    break;
                                }

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
                                hitGrid.Set(
                                    gridCoordinate2,
                                    true);

                                if (gridCoordinateToHighlight.HasValue) {

                                    candidateTraversedGridCoordinatesToHighlight.Add(gridCoordinate2);
                                }
#endif

                                localHitCoordinates.Add(gridCoordinate2);

                                break;
                            }

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
                            traversedFreeSpaceGrid.Set(
                                gridCoordinate2,
                                true);

                            if (gridCoordinateToHighlight.HasValue) {

                                candidateTraversedGridCoordinatesToHighlight.Add(gridCoordinate2);
                            }
#endif

                        }

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
                        if (gridCoordinateToHighlight.HasValue
                                && candidateTraversedGridCoordinatesToHighlight.Contains(gridCoordinateToHighlight.Value)) {

                            candidateTraversedGridCoordinatesToHighlight.Remove(gridCoordinateToHighlight.Value);

                            traversedGridCoordinatesToHighlight.AddRange(candidateTraversedGridCoordinatesToHighlight);
                        }
#endif

                    }
#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW
                }
            }

            hitCoordinates.AddRange(localHitCoordinates);
#else
                    return localHitCoordinates;
                },
                localHitCoordinates => {

                    lock (@lock) {
                        hitCoordinates.AddRange(localHitCoordinates);
                    }
                });
#endif

#if DEBUG_CULLING && DEBUG_CULLING_EXPORT_RAYMARCHING_OVERVIEW

            GridTools.ExportSlices(
                overviewVisualizationDirectory,
                gridSize,
                gridCoordinate => {

                    if (gridCoordinateToHighlight.HasValue
                            && gridCoordinate == gridCoordinateToHighlight) {

                        return Color.Magenta;
                    }

                    if (gridCoordinateToHighlight.HasValue
                            && traversedGridCoordinatesToHighlight.Contains(gridCoordinate)) {

                        return Color.Cyan;
                    }

                    if (initializationGrid.Get(gridCoordinate)) {

                        return Color.Red;
                    }

                    if (traversedFreeSpaceGrid.Get(gridCoordinate)) {

                        return Color.Gray;
                    }

                    if (hitGrid.Get(gridCoordinate)) {

                        return Color.Green;
                    }

                    if (occupancyGrid.Get(gridCoordinate)) {

                        return Color.Black;
                    }

                    return Color.White;
                });
#endif

            return hitCoordinates.ToList();
        }

        private static bool IsDiagonalHit(
                this bool[,,] occupancyGrid,
                Vector3i gridCoordinate,
                Vector3i gridDirection,
                Vector3i gridSize) {

            Vector3i gridDirection2;
            Vector3i gridCoordinate3;
            Vector3i gridCoordinate2 = gridCoordinate - gridDirection;

            bool allOccupied = true;

            for (int j = 0; j < 3; j++) {

                if (gridDirection[j] != 0) {

                    gridDirection2 = new Vector3i(0);

                    gridDirection2[j] = gridDirection[j];

                    gridCoordinate3 = gridCoordinate2 + gridDirection2;

                    if (!gridCoordinate3.IsWithinBounds(gridSize)
                            || !occupancyGrid.Get(gridCoordinate3)) {

                        allOccupied = false;

                        break;
                    }
                }
            }

            return allOccupied;
        }

#if DEBUG_CULLING
        private static void ExportPly(
                this Dictionary<Vector3i, HashSet<byte>> rayMarchingCells,
                string debugDirectory,
                double resolution,
                Vector3d cropMinCoordinate,
                Vector3d cropCenter,
                Matrix3d cropOrientation,
                Dictionary<byte, Vector3i> gridDirections) {

            PointFormat pointFormat = new ColoredPointFormat();

            using (PlyStreamWriter
                    pointWriter = new PlyStreamWriter($"{debugDirectory}/rayMarchingCellsInitialization.ply")
#if DEBUG_CULLING_EXPORT_INITIALIZED_RAYMARCHING_DIRECTIONS_AS_PLY
                    , rayWriter = new PlyStreamWriter($"{debugDirectory}/rayMarchingCellsInitialization_Directions.ply")
#endif
                    ) {

                pointWriter.PointFormat = pointFormat;

#if DEBUG_CULLING_EXPORT_INITIALIZED_RAYMARCHING_DIRECTIONS_AS_PLY
                rayWriter.PointFormat = pointFormat;
#endif

                foreach (Vector3i gridCoordinate in rayMarchingCells.Keys) {

                    Vector3d coordinate = gridCoordinate.Transform(
                        resolution,
                        cropMinCoordinate,
                        cropCenter,
                        cropOrientation);

                    pointWriter.Write(
                        new Point(coordinate)
                            .SetColor(Color.Red));

#if DEBUG_CULLING_EXPORT_INITIALIZED_RAYMARCHING_DIRECTIONS_AS_PLY
                    foreach (byte gridDirectionCode in rayMarchingCells[gridCoordinate]) {

                        Ray ray = new Ray(
                            coordinate,
                            gridDirections[gridDirectionCode]
                                .ToVector3d()
                                .RotateDirection(cropOrientation));

                        PointCloud rayVisualizationPoints = ray.CreateVisualizationPointCloud(
                            1.0,
                            0.01);

                        rayVisualizationPoints.SetColor(Color.Blue);

                        rayVisualizationPoints.PropagatePropertiesToPoints();

                        rayWriter.Write(rayVisualizationPoints);
                    }
#endif
                }
            }
        }

        private static void ExportDebugVisualization(
                this Dictionary<Vector3i, HashSet<byte>> rayMarchingCells,
                string outputFileX,
                string outputFileY,
                string outputFileZ,
                Vector3i gridSize,
                Mat xCrossSection,
                Mat yCrossSection,
                Mat zCrossSection) {

            int xCrossSectionIndex = (int)(gridSize.X / 2.0).Floor();
            int yCrossSectionIndex = (int)(gridSize.Y / 2.0).Floor();
            int zCrossSectionIndex = (int)(gridSize.Z / 2.0).Floor();

            Vec3b colorRed = Color.Red.ToOpenCV();

            Mat _xCrossSection = xCrossSection.Clone();
            Mat _yCrossSection = yCrossSection.Clone();
            Mat _zCrossSection = zCrossSection.Clone();

            List<Vector3i> gridCoordinates = rayMarchingCells.Keys.ToList();

            using (Mat<Vec3b>
                    __xCrossSection = new Mat<Vec3b>(_xCrossSection),
                    __yCrossSection = new Mat<Vec3b>(_yCrossSection),
                    __zCrossSection = new Mat<Vec3b>(_zCrossSection)) {

                MatIndexer<Vec3b> xCrossSectionData = __xCrossSection.GetIndexer();
                MatIndexer<Vec3b> yCrossSectionData = __yCrossSection.GetIndexer();
                MatIndexer<Vec3b> zCrossSectionData = __zCrossSection.GetIndexer();

                Parallel.For(
                    0,
                    gridCoordinates.Count,
                    i => {

                        if (gridCoordinates[i].X == xCrossSectionIndex) {

                            xCrossSectionData[
                                gridCoordinates[i].Y,
                                gridCoordinates[i].Z] = colorRed;
                        }

                        if (gridCoordinates[i].Y == yCrossSectionIndex) {

                            yCrossSectionData[
                                gridCoordinates[i].X,
                                gridCoordinates[i].Z] = colorRed;
                        }

                        if (gridCoordinates[i].Z == zCrossSectionIndex) {

                            zCrossSectionData[
                                gridCoordinates[i].X,
                                gridCoordinates[i].Y] = colorRed;
                        }
                    });
            }

            _xCrossSection.ImWrite(outputFileX);
            _yCrossSection.ImWrite(outputFileY);
            _zCrossSection.ImWrite(outputFileZ);
        }
#endif

        private static bool[,,] ToOccupancyGrid(
                this List<Vector3i> gridCoordinates,
                Vector3i gridSize) {

            bool[,,] result = new bool[
                gridSize.X,
                gridSize.Y,
                gridSize.Z];

            Parallel.For(
                0,
                gridCoordinates.Count,
                i => {

                    result.Set(
                        gridCoordinates[i],
                        true);
                });

            return result;
        }

        private static bool[,,] CullInvisibleCells_OLD(
                this bool[,,] occupancyGrid,
                double resolution,
                Vector3d cropCenter,
                Vector3d cropSize,
                Matrix3d cropOrientation,
                List<Pose> poses) {

            Vector3d cropMinCoordinate = cropCenter - 0.5 * cropSize;

            bool[,,] result = new bool[
                occupancyGrid.GetLength(0),
                occupancyGrid.GetLength(1),
                occupancyGrid.GetLength(2)];

            GridOctTree octTree = new GridOctTree(
                occupancyGrid,
                resolution,
                cropMinCoordinate);

            Parallel.For(
                0,
                poses.Count,
                i => {

                    Pose pose = poses[i].Rotated(
                        cropOrientation.Inverted(),
                        cropCenter);

                    foreach (Ray ray in pose.EnumerateRays()) {

                        Vector3i? gridCoordinate = octTree.GetFirstIntersectingGridCoordinate(ray);

                        if (gridCoordinate.HasValue) {

                            result.Set(
                                gridCoordinate.Value,
                                true);
                        }
                    }
                });

            return result;
        }

        private static IEnumerable<Ray> EnumerateRays(
                this Pose pose) {

            InnerOrientation innerOrientation = pose.GetInnerOrientation();

            for (int r = 0; r < innerOrientation.Height; r++) {
                for (int c = 0; c < innerOrientation.Width; c++) {

                    yield return new Ray(
                        pose.Position,
                        pose * new Vector3d(
                            (c - innerOrientation.PrincipalPointX)
                                / innerOrientation.FocalLengthX,
                            (r - innerOrientation.PrincipalPointY)
                                / innerOrientation.FocalLengthY,
                            -1.0));
                }
            }
        }

        private static void ExportSlices(
                this bool[,,] occupancyGrid,
                string directory) {

            GridTools.ExportSlices(
                directory,
                occupancyGrid.GetSize(),
                gridCoordinate => occupancyGrid.Get(gridCoordinate) ?
                    Color.Black :
                    Color.White);
        }

        private static void GetDebugGradients(
                this double[,,] densityGrid,
                out double[,,] gradientXGrid,
                out double[,,] gradientYGrid,
                out double[,,] gradientZGrid) {

            double[,,] _gradientXGrid = new double[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];
            double[,,] _gradientYGrid = new double[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];
            double[,,] _gradientZGrid = new double[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];

            Parallel.For(
                1,
                densityGrid.GetLength(0) - 1,
                x => {

                    for (int y = 1; y < densityGrid.GetLength(1) - 1; y++) {
                        for (int z = 1; z < densityGrid.GetLength(2) - 1; z++) {

                            _gradientXGrid[x, y, z] = (densityGrid[x + 1, y, z] - densityGrid[x - 1, y, z]).Abs();
                            _gradientYGrid[x, y, z] = (densityGrid[x, y + 1, z] - densityGrid[x, y - 1, z]).Abs();
                            _gradientZGrid[x, y, z] = (densityGrid[x, y, z + 1] - densityGrid[x, y, z - 1]).Abs();
                        }
                    }
                });

            gradientXGrid = _gradientXGrid;
            gradientYGrid = _gradientYGrid;
            gradientZGrid = _gradientZGrid;
        }

        private static bool[,,] ApplyCannyFilter(
                this double[,,] densityGrid,
                int gaussianKernelSize,
                double gaussianKernelSigma,
                double lowerThreshold,
                double upperThreshold
#if DEBUG_CANNY_FILTERING
                , string densityGridFile
                , double visualizationDensityThreshold
                , double resolution
                , Vector3d cropCenter
                , Vector3d cropSize
                , Matrix3d cropOrientation
#endif
            ) {

            densityGrid = densityGrid.ApplyGaussianFilter(
                gaussianKernelSize,
                gaussianKernelSigma);

            //// TEST
            //densityGrid = new double[100, 100, 100];

            //for (int x = 0; x < 100; x++) {
            //    for (int y = 0; y < 100; y++) {

            //        densityGrid[x, y, 50] = 100.0;
            //    }
            //}
            //for (int x = 0; x < 100; x++) {
            //    for (int z = 0; z < 100; z++) {

            //        densityGrid[x, 50, z] = 100.0;
            //    }
            //}
            //for (int y = 0; y < 100; y++) {
            //    for (int z = 0; z < 100; z++) {

            //        densityGrid[50, y, z] = 100.0;
            //    }
            //}
            //// TEST

#if DEBUG_CANNY_FILTERING

            densityGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_CANNYDEBUG_02_GAUSSIAN_FILTERED",
                    "tiff"),
                null,
                null,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);

            densityGrid.ExportCrossSections(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_CANNYDEBUG_02_GAUSSIAN_FILTERED",
                    "tiff"),
                null,
                visualizationDensityThreshold,
                RenderMode.COLOR,
                ColormapTypes.Jet,
                false);

            densityGrid.Export(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    densityGridFile,
                    $"_CANNYDEBUG_02_GAUSSIAN_FILTERED",
                    "ply"),
                visualizationDensityThreshold,
                resolution,
                cropCenter,
                cropSize,
                cropOrientation);
#endif

            double[,,] gradientGrid;

            {
                densityGrid.ApplySobelFilter(
                    out double[,,] gradientXGrid,
                    out double[,,] gradientYGrid,
                    out double[,,] gradientZGrid);

#if DEBUG_CANNY_FILTERING

                gradientXGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_X",
                        "tiff"),
                    null,
                    null,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

                gradientXGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_X",
                        "tiff"),
                    null,
                    visualizationDensityThreshold,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

                gradientYGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_Y",
                        "tiff"),
                    null,
                    null,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

                gradientYGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_Y",
                        "tiff"),
                    null,
                    visualizationDensityThreshold,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

                gradientZGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_Z",
                        "tiff"),
                    null,
                    null,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

                gradientZGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_Z",
                        "tiff"),
                    null,
                    visualizationDensityThreshold,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

                gradientXGrid.Export(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_X",
                        "ply"),
                    visualizationDensityThreshold,
                    resolution,
                    cropCenter,
                    cropSize,
                    cropOrientation);

                gradientYGrid.Export(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_Y",
                        "ply"),
                    visualizationDensityThreshold,
                    resolution,
                    cropCenter,
                    cropSize,
                    cropOrientation);

                gradientZGrid.Export(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_03_SOBEL_GRADIENT_Z",
                        "ply"),
                    visualizationDensityThreshold,
                    resolution,
                    cropCenter,
                    cropSize,
                    cropOrientation);
#endif

                gradientGrid = ApplyNonMaximumSuppression(
                    gradientXGrid,
                    gradientYGrid,
                    gradientZGrid);

#if DEBUG_CANNY_FILTERING

                gradientGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_04_NON_MAXIMUM_SUPPRESSION",
                        "tiff"),
                    null,
                    null,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

                gradientGrid.ExportCrossSections(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_04_NON_MAXIMUM_SUPPRESSION",
                        "tiff"),
                    null,
                    visualizationDensityThreshold,
                    RenderMode.COLOR,
                    ColormapTypes.Jet,
                    false);

                gradientGrid.Export(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        densityGridFile,
                        $"_CANNYDEBUG_04_NON_MAXIMUM_SUPPRESSION",
                        "ply"),
                    visualizationDensityThreshold,
                    resolution,
                    cropCenter,
                    cropSize,
                    cropOrientation);
#endif
            }

            return gradientGrid.ApplyThresholds(
                lowerThreshold,
                upperThreshold);
        }

        private static double[,,] ApplyGaussianFilter(
                this double[,,] densityGrid,
                int gaussianKernelSize,
                double gaussianKernelSigma) {

            if (gaussianKernelSize % 2 != 1) {

                throw new ArgumentException("GaussianKernelSize must be odd.");
            }

            int kernelCenterIndexAndKernelHalfSize = (gaussianKernelSize - 1) / 2;

            double[] gaussianKernel1D = Cv2
                .GetGaussianKernel(
                    gaussianKernelSize,
                    gaussianKernelSigma,
                    MatType.CV_64F)
                .ToDoubleArray();

            double[,,] tempGrid1 = new double[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];
            double[,,] tempGrid2 = new double[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];

            Parallel.For(
                0,
                densityGrid.GetLength(0),
                x => {

                    for (int y = 0; y < densityGrid.GetLength(1); y++) {
                        for (int z = 0; z < densityGrid.GetLength(2); z++) {

                            double result = densityGrid[x, y, z] * gaussianKernel1D[kernelCenterIndexAndKernelHalfSize];

                            for (int d = 1; d <= kernelCenterIndexAndKernelHalfSize; d++) {
                                for (int sign = -1; sign <= 1; sign += 2) {

                                    int x2 = x + sign * d;

                                    if (x2 >= 0
                                            && x2 < densityGrid.GetLength(0)) {

                                        result += gaussianKernel1D[kernelCenterIndexAndKernelHalfSize + sign * d]
                                            * densityGrid[x2, y, z];
                                    }
                                }
                            }

                            tempGrid1[x, y, z] = result;
                        }
                    }
                });

            Parallel.For(
                0,
                densityGrid.GetLength(0),
                x => {

                    for (int y = 0; y < densityGrid.GetLength(1); y++) {
                        for (int z = 0; z < densityGrid.GetLength(2); z++) {

                            double result = tempGrid1[x, y, z] * gaussianKernel1D[kernelCenterIndexAndKernelHalfSize];

                            for (int d = 1; d <= kernelCenterIndexAndKernelHalfSize; d++) {
                                for (int sign = -1; sign <= 1; sign += 2) {

                                    int y2 = y + sign * d;

                                    if (y2 >= 0
                                            && y2 < densityGrid.GetLength(1)) {

                                        result += gaussianKernel1D[kernelCenterIndexAndKernelHalfSize + sign * d]
                                            * tempGrid1[x, y2, z];
                                    }
                                }
                            }

                            tempGrid2[x, y, z] = result;
                        }
                    }
                });

            Parallel.For(
                0,
                densityGrid.GetLength(0),
                x => {

                    for (int y = 0; y < densityGrid.GetLength(1); y++) {
                        for (int z = 0; z < densityGrid.GetLength(2); z++) {

                            double result = tempGrid2[x, y, z] * gaussianKernel1D[kernelCenterIndexAndKernelHalfSize];

                            for (int d = 1; d <= kernelCenterIndexAndKernelHalfSize; d++) {
                                for (int sign = -1; sign <= 1; sign += 2) {

                                    int z2 = z + sign * d;

                                    if (z2 >= 0
                                            && z2 < densityGrid.GetLength(2)) {

                                        result += gaussianKernel1D[kernelCenterIndexAndKernelHalfSize + sign * d]
                                            * tempGrid2[x, y, z2];
                                    }
                                }
                            }

                            tempGrid1[x, y, z] = result;
                        }
                    }
                });

            return tempGrid1;
        }

        private static void ApplySobelFilter(
                this double[,,] densityGrid,
                out double[,,] gradientXGrid,
                out double[,,] gradientYGrid,
                out double[,,] gradientZGrid) {

            double[,] kernel = new double[3, 3]{
                { 1.0, 2.0, 1.0 },
                { 2.0, 4.0, 2.0 },
                { 1.0, 2.0, 1.0 },
            };

            double[,,] _gradientXGrid = new double[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];
            double[,,] _gradientYGrid = new double[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];
            double[,,] _gradientZGrid = new double[
                densityGrid.GetLength(0),
                densityGrid.GetLength(1),
                densityGrid.GetLength(2)];

            Parallel.For(
                1,
                densityGrid.GetLength(0) - 1,
                x => {

                    for (int y = 1; y < densityGrid.GetLength(1) - 1; y++) {
                        for (int z = 1; z < densityGrid.GetLength(2) - 1; z++) {

                            double result = 0.0;

                            for (int dy = -1; dy <= 1; dy++) {
                                for (int dz = -1; dz <= 1; dz++) {
                                    for (int dx = -1; dx <= 1; dx += 2) {


                                        result += dx * kernel[1 + dy, 1 + dz]
                                            * densityGrid[
                                                x + dx,
                                                y + dy,
                                                z + dz];
                                    }
                                }
                            }

                            _gradientXGrid[x, y, z] = result.Abs();

                            result = 0.0;

                            for (int dx = -1; dx <= 1; dx++) {
                                for (int dz = -1; dz <= 1; dz++) {
                                    for (int dy = -1; dy <= 1; dy += 2) {

                                        result += dy * kernel[1 + dx, 1 + dz]
                                            * densityGrid[
                                                x + dx,
                                                y + dy,
                                                z + dz];
                                    }
                                }
                            }

                            _gradientYGrid[x, y, z] = result.Abs();

                            result = 0.0;

                            for (int dx = -1; dx <= 1; dx++) {
                                for (int dy = -1; dy <= 1; dy++) {
                                    for (int dz = -1; dz <= 1; dz += 2) {

                                        result += dz * kernel[1 + dx, 1 + dy]
                                            * densityGrid[
                                                x + dx,
                                                y + dy,
                                                z + dz];
                                    }
                                }
                            }

                            _gradientZGrid[x, y, z] = result.Abs();
                        }
                    }
                });

            gradientXGrid = _gradientXGrid;
            gradientYGrid = _gradientYGrid;
            gradientZGrid = _gradientZGrid;
        }

        private static double[,,] ApplyNonMaximumSuppression(
                double[,,] gradientXGrid,
                double[,,] gradientYGrid,
                double[,,] gradientZGrid) {

            double[,,] gradientGrid = new double[
                gradientXGrid.GetLength(0),
                gradientXGrid.GetLength(1),
                gradientXGrid.GetLength(2)];

            Parallel.For(
                1,
                gradientXGrid.GetLength(0) - 1,
                x => {

                    for (int y = 1; y < gradientXGrid.GetLength(1) - 1; y++) {
                        for (int z = 1; z < gradientXGrid.GetLength(2) - 1; z++) {

                            double gradientX = gradientXGrid[x, y, z];
                            double gradientY = gradientYGrid[x, y, z];
                            double gradientZ = gradientZGrid[x, y, z];

                            gradientX = gradientX > gradientXGrid[x - 1, y, z]
                                    && gradientX > gradientXGrid[x + 1, y, z] ?
                                gradientX :
                                0.0;

                            gradientY = gradientY > gradientYGrid[x, y - 1, z]
                                    && gradientY > gradientYGrid[x, y + 1, z] ?
                                gradientY :
                                0.0;

                            gradientY = gradientZ > gradientZGrid[x, y, z - 1]
                                    && gradientZ > gradientZGrid[x, y, z - 1] ?
                                gradientY :
                                0.0;

                            if (gradientX > 0.0
                                    || gradientY > 0.0
                                    || gradientZ > 0.0) {

                                gradientGrid[x, y, z] = System.Math.Sqrt(
                                    gradientX.Squared()
                                        + gradientY.Squared()
                                        + gradientZ.Squared());
                            }
                        }
                    }
                });

            return gradientGrid;
        }

        private static bool[,,] ApplyThresholds(
                this double[,,] gradientGrid,
                double lowerThreshold,
                double upperThreshold) {

            bool[,,] edgeGrid = new bool[
                gradientGrid.GetLength(0),
                gradientGrid.GetLength(1),
                gradientGrid.GetLength(2)];
            bool[,,] edgeCandidateGrid = new bool[
                gradientGrid.GetLength(0),
                gradientGrid.GetLength(1),
                gradientGrid.GetLength(2)];
            Queue<(int x, int y, int z)> edgeCandidateVoxels = new Queue<(int x, int y, int z)>();

            Parallel.For(
                0,
                gradientGrid.GetLength(0),
                x => {

                    for (int y = 0; y < gradientGrid.GetLength(1); y++) {
                        for (int z = 0; z < gradientGrid.GetLength(2); z++) {

                            double gradient = gradientGrid[x, y, z];

                            if (gradient >= lowerThreshold) {

                                if (gradient >= upperThreshold) {

                                    edgeGrid[x, y, z] = true;
                                }
                                else {
                                    edgeCandidateGrid[x, y, z] = true;
                                }
                            }
                        }
                    }
                });

            for (int x = 0; x < gradientGrid.GetLength(0); x++) {
                for (int y = 0; y < gradientGrid.GetLength(1); y++) {
                    for (int z = 0; z < gradientGrid.GetLength(2); z++) {

                        if (!edgeGrid[x, y, z]) {
                            continue;
                        }

                        for (int dx = -1; dx <= 1; dx++) {
                            for (int dy = -1; dy <= 1; dy++) {
                                for (int dz = -1; dz <= 1; dz++) {

                                    int x2 = x + dx;
                                    int y2 = y + dy;
                                    int z2 = z + dz;

                                    if (x2 >= 0
                                            && y2 >= 0
                                            && z2 >= 0
                                            && x2 < gradientGrid.GetLength(0)
                                            && y2 < gradientGrid.GetLength(1)
                                            && z2 < gradientGrid.GetLength(2)
                                            && edgeCandidateGrid[x2, y2, z2]
                                            && !edgeGrid[x2, y2, z2]) {

                                        edgeCandidateVoxels.Enqueue(
                                            (x2, y2, z2));
                                    }
                                }
                            }
                        }

                        while (edgeCandidateVoxels.Count > 0) {

                            (int x, int y, int z) edgeCandidateVoxel = edgeCandidateVoxels.Dequeue();

                            if (edgeGrid[
                                    edgeCandidateVoxel.x,
                                    edgeCandidateVoxel.y,
                                    edgeCandidateVoxel.z]) {

                                continue;
                            }

                            edgeGrid[
                                edgeCandidateVoxel.x,
                                edgeCandidateVoxel.y,
                                edgeCandidateVoxel.z] = true;

                            edgeCandidateGrid[
                                edgeCandidateVoxel.x,
                                edgeCandidateVoxel.y,
                                edgeCandidateVoxel.z] = false;

                            for (int dx = -1; dx <= 1; dx++) {
                                for (int dy = -1; dy <= 1; dy++) {
                                    for (int dz = -1; dz <= 1; dz++) {

                                        int x2 = edgeCandidateVoxel.x + dx;
                                        int y2 = edgeCandidateVoxel.y + dy;
                                        int z2 = edgeCandidateVoxel.z + dz;

                                        if (x2 >= 0
                                                && y2 >= 0
                                                && z2 >= 0
                                                && x2 < gradientGrid.GetLength(0)
                                                && y2 < gradientGrid.GetLength(1)
                                                && z2 < gradientGrid.GetLength(2)
                                                && edgeCandidateGrid[x2, y2, z2]
                                                && !edgeGrid[x2, y2, z2]) {

                                            edgeCandidateVoxels.Enqueue(
                                                (x2, y2, z2));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return edgeGrid;
        }

        private static void Export(
                this bool[,,] edgeGrid,
                string file,
                double resolution,
                Vector3d cropCenter,
                Vector3d cropSize,
                Matrix3d cropOrientation) {

            Vector3d cropMinCoordinate = cropCenter - 0.5 * cropSize;

            using (PlyStreamWriter writer = new PlyStreamWriter(file) {
                PointFormat = new ColoredPointFormat()
            }) {

                for (int x = 0; x < edgeGrid.GetLength(0); x++) {
                    for (int y = 0; y < edgeGrid.GetLength(1); y++) {
                        for (int z = 0; z < edgeGrid.GetLength(2); z++) {

                            if (edgeGrid[x, y, z]) {

                                Point point = new Point(
                                    new Vector3i(x, y, z)
                                        .Transform(
                                            resolution,
                                            cropMinCoordinate,
                                            cropCenter,
                                            cropOrientation));

                                point.SetColor(Color.Gray);

                                writer.Write(point);
                            }
                        }
                    }
                }
            }
        }

        private static void ExportCrossSections(
                this double[,,] grid,
                string file,
                double? min,
                double? max,
                RenderMode renderMode,
                ColormapTypes colorMap,
                bool invertScale) {

            grid.RenderCrossSections(
                ref min,
                ref max,
                renderMode,
                colorMap,
                invertScale,
                out Mat imageX,
                out Mat imageY,
                out Mat imageZ);

            Export(
                FileSystemUtils.GetFileWithPostfix(
                    file,
                    $"_range({min:0.000}, {max:0.000})"),
                imageX,
                imageY,
                imageZ);
        }

        private static IShape ReadShape(
                string file,
                ExportConfig exportConfig) {

            IShape shape = new PlyReader() {
                AreVertexCoordinatesFloat = exportConfig.AreCoordinatesFloat,
                IsVertexNormalVectorFloat = exportConfig.AreCoordinatesFloat,
                IsFaceNormalVectorFloat = exportConfig.AreCoordinatesFloat
            }.ReadShape(file);

            if (exportConfig.RegistrationFile != null) {

                shape.ApplyRegistrationFile(exportConfig.RegistrationFile);

#if DEBUG_SHAPE_CROSSSECTION_EXPORT
                shape.ExportMeshPly(
                    FileSystemUtils.GetFileWithPostfix(
                        file,
                        "_registered"),
                    Color.Gray);
#endif
            }

            if (exportConfig.TransformationFile != null) {

                shape.ApplyTransformationFile(exportConfig.TransformationFile);

#if DEBUG_SHAPE_CROSSSECTION_EXPORT
                shape.ExportMeshPly(
                    FileSystemUtils.GetFileWithPostfix(
                        file,
                        "_transformed"),
                    Color.Gray);
#endif
            }

            if (exportConfig.CropOrientation.HasValue) {

                Vector3d cropCenter = exportConfig.CropMinCoordinate + 0.5
                    * (exportConfig.CropMaxCoordinate - exportConfig.CropMinCoordinate);

                shape.Rotate(
                    new Rotation(exportConfig.CropOrientation.Value.Inverted()) {
                        Anchor = cropCenter,
                        UseParallel = false
                    });
            }

            return shape;
        }

        private static void ExportCrossSections(
                this bool[,,] grid,
                string file,
                bool invert) {

            grid.RenderCrossSections(
                invert,
                out Mat imageX,
                out Mat imageY,
                out Mat imageZ);

            Export(
                file,
                imageX,
                imageY,
                imageZ);
        }

        private static void ExportCrossSections(
                this IShape shape,
                string file,
                ExportConfig exportConfig) {

            shape.RenderCrossSections(
                file,
                exportConfig,
                out Mat imageX,
                out Mat imageY,
                out Mat imageZ);

            Export(
                FileSystemUtils.GetFileWithPostfix(
                    file,
                    $"_res{exportConfig.Resolution:0.000}_cropped"),
                imageX,
                imageY,
                imageZ);
        }

        private static void Export(
                string file,
                Mat imageX,
                Mat imageY,
                Mat imageZ) {

            imageX.ImWrite(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    file,
                    "_x",
                    "tiff"));

            imageY.ImWrite(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    file,
                    "_y",
                    "tiff"));

            imageZ.ImWrite(
                 FileSystemUtils.GetWithPostfixAndNewExtension(
                     file,
                     "_z",
                     "tiff"));
        }

        private static void RenderCrossSections(
                this double[,,] grid,
                ref double? min,
                ref double? max,
                RenderMode renderMode,
                ColormapTypes colorMap,
                bool invertScale,
                out Mat imageX,
                out Mat imageY,
                out Mat imageZ) {

            if (!min.HasValue
                    || !max.HasValue) {

                (double min, double max) range = grid.GetRange();

                if (!min.HasValue) {
                    min = range.min;
                }
                if (!max.HasValue) {
                    max = range.max;
                }
            }

            grid.GetCrossSections(
                out double[,] crossSectionX,
                out double[,] crossSectionY,
                out double[,] crossSectionZ);

            imageX = crossSectionX.Render(
                min,
                max,
                renderMode,
                colorMap,
                invertScale);

            imageY = crossSectionY.Render(
                min,
                max,
                renderMode,
                colorMap,
                invertScale);

            imageZ = crossSectionZ.Render(
                min,
                max,
                renderMode,
                colorMap,
                invertScale);
        }

        private static void RenderCrossSections(
                this bool[,,] grid,
                bool invert,
                out Mat imageX,
                out Mat imageY,
                out Mat imageZ) {

            grid.GetCrossSections(
                out bool[,] crossSectionX,
                out bool[,] crossSectionY,
                out bool[,] crossSectionZ);

            imageX = crossSectionX.Render(invert);
            imageY = crossSectionY.Render(invert);
            imageZ = crossSectionZ.Render(invert);
        }

        private static void RenderCrossSections(
                this IShape shape,
                string file,
                ExportConfig exportConfig,
                out Mat imageX,
                out Mat imageY,
                out Mat imageZ) {

            shape.GetCrossSections(
                file,
                exportConfig,
                out bool[,] crossSectionX,
                out bool[,] crossSectionY,
                out bool[,] crossSectionZ);

            imageX = crossSectionX.Render(exportConfig.Invert);
            imageY = crossSectionY.Render(exportConfig.Invert);
            imageZ = crossSectionZ.Render(exportConfig.Invert);
        }

        private static void GetCrossSections(
                this double[,,] grid,
                out double[,] crossSectionX,
                out double[,] crossSectionY,
                out double[,] crossSectionZ) {

            int xCrossSectionIndex = (int)(grid.GetLength(0) / 2.0).Floor();
            int yCrossSectionIndex = (int)(grid.GetLength(1) / 2.0).Floor();
            int zCrossSectionIndex = (int)(grid.GetLength(2) / 2.0).Floor();

            crossSectionX = new double[
                grid.GetLength(1),
                grid.GetLength(2)];
            crossSectionY = new double[
                grid.GetLength(0),
                grid.GetLength(2)];
            crossSectionZ = new double[
                grid.GetLength(0),
                grid.GetLength(1)];

            for (int y = 0; y < grid.GetLength(1); y++) {
                for (int z = 0; z < grid.GetLength(2); z++) {

                    crossSectionX[y, z] = grid[xCrossSectionIndex, y, z];
                }
            }

            for (int x = 0; x < grid.GetLength(0); x++) {
                for (int z = 0; z < grid.GetLength(2); z++) {

                    crossSectionY[x, z] = grid[x, yCrossSectionIndex, z];
                }
            }

            for (int x = 0; x < grid.GetLength(0); x++) {
                for (int y = 0; y < grid.GetLength(1); y++) {

                    crossSectionZ[x, y] = grid[x, y, zCrossSectionIndex];
                }
            }
        }

        private static void GetCrossSections(
                this bool[,,] grid,
                out bool[,] crossSectionX,
                out bool[,] crossSectionY,
                out bool[,] crossSectionZ) {

            int xCrossSectionIndex = (int)(grid.GetLength(0) / 2.0).Floor();
            int yCrossSectionIndex = (int)(grid.GetLength(1) / 2.0).Floor();
            int zCrossSectionIndex = (int)(grid.GetLength(2) / 2.0).Floor();

            crossSectionX = new bool[
                grid.GetLength(1),
                grid.GetLength(2)];
            crossSectionY = new bool[
                grid.GetLength(0),
                grid.GetLength(2)];
            crossSectionZ = new bool[
                grid.GetLength(0),
                grid.GetLength(1)];

            for (int y = 0; y < crossSectionX.GetLength(0); y++) {
                for (int z = 0; z < crossSectionX.GetLength(1); z++) {

                    crossSectionX[y, z] = grid[xCrossSectionIndex, y, z];
                }
            }

            for (int x = 0; x < crossSectionY.GetLength(0); x++) {
                for (int z = 0; z < crossSectionY.GetLength(1); z++) {

                    crossSectionY[x, z] = grid[x, yCrossSectionIndex, z];
                }
            }

            for (int x = 0; x < crossSectionZ.GetLength(0); x++) {
                for (int y = 0; y < crossSectionZ.GetLength(1); y++) {

                    crossSectionZ[x, y] = grid[x, y, zCrossSectionIndex];
                }
            }
        }

        private static void GetCrossSections(
                this IShape shape,
                string file,
                ExportConfig exportConfig,
                out bool[,] crossSectionX,
                out bool[,] crossSectionY,
                out bool[,] crossSectionZ) {

            object @lock = new object();

            Vector3i gridSize = GridTools.GetGridSize(
                exportConfig.Resolution,
                exportConfig.CropMinCoordinate,
                exportConfig.CropMaxCoordinate);

            int xCrossSectionIndex = (int)(gridSize.X / 2.0).Floor();
            int yCrossSectionIndex = (int)(gridSize.Y / 2.0).Floor();
            int zCrossSectionIndex = (int)(gridSize.Z / 2.0).Floor();

            bool[,] _crossSectionX = new bool[
                gridSize.Y,
                gridSize.Z];
            bool[,] _crossSectionY = new bool[
                gridSize.X,
                gridSize.Z];
            bool[,] _crossSectionZ = new bool[
                gridSize.X,
                gridSize.Y];

#if DEBUG_SHAPE_CROSSSECTION_EXPORT
            using (PlyStreamWriter writer = new PlyStreamWriter(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        file,
                        "_DEBUG_01_x_voxels",
                        "ply"))) {

                for (int y = 0; y < _crossSectionX.GetLength(0); y++) {
                    for (int z = 0; z < _crossSectionX.GetLength(1); z++) {

                        AABox voxel = GetVoxelGeometry(
                            xCrossSectionIndex,
                            y,
                            z,
                            resolution,
                            cropMinCoordinate);

                        writer.Write(voxel);
                    }
                }
            }

            using (PlyStreamWriter writer = new PlyStreamWriter(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        file,
                        "_DEBUG_01_y_voxels",
                        "ply"))) {

                for (int x = 0; x < _crossSectionY.GetLength(0); x++) {
                    for (int z = 0; z < _crossSectionY.GetLength(1); z++) {

                        AABox voxel = GetVoxelGeometry(
                            x,
                            yCrossSectionIndex,
                            z,
                            resolution,
                            cropMinCoordinate);

                        writer.Write(voxel);
                    }
                }
            }

            using (PlyStreamWriter writer = new PlyStreamWriter(
                    FileSystemUtils.GetWithPostfixAndNewExtension(
                        file,
                        "_DEBUG_01_z_voxels",
                        "ply"))) {

                for (int x = 0; x < _crossSectionZ.GetLength(0); x++) {
                    for (int y = 0; y < _crossSectionZ.GetLength(1); y++) {

                        AABox voxel = GetVoxelGeometry(
                            x,
                            y,
                            zCrossSectionIndex,
                            resolution,
                            cropMinCoordinate);

                        writer.Write(voxel);
                    }
                }
            }
#endif

            AABox xSectionBBox = AABox.FromContainedGeometries(new AABox[] {
                GetVoxelGeometry(
                    xCrossSectionIndex,
                    0,
                    0,
                    exportConfig.Resolution,
                    exportConfig.CropMinCoordinate),
                GetVoxelGeometry(
                    xCrossSectionIndex,
                    gridSize.Y,
                    gridSize.Z,
                    exportConfig.Resolution,
                    exportConfig.CropMinCoordinate)
            });

            AABox ySectionBBox = AABox.FromContainedGeometries(new AABox[] {
                GetVoxelGeometry(
                    0,
                    yCrossSectionIndex,
                    0,
                    exportConfig.Resolution,
                    exportConfig.CropMinCoordinate),
                GetVoxelGeometry(
                    gridSize.X,
                    yCrossSectionIndex,
                    gridSize.Z,
                    exportConfig.Resolution,
                    exportConfig.CropMinCoordinate)
            });

            AABox zSectionBBox = AABox.FromContainedGeometries(new AABox[] {
                GetVoxelGeometry(
                    0,
                    0,
                    zCrossSectionIndex,
                    exportConfig.Resolution,
                    exportConfig.CropMinCoordinate),
                GetVoxelGeometry(
                    gridSize.X,
                    gridSize.Y,
                    zCrossSectionIndex,
                    exportConfig.Resolution,
                    exportConfig.CropMinCoordinate)
            });

            if (exportConfig.ExportSectionPlanes
                    && exportConfig.CropOrientation.HasValue) {

                string outputFile = FileSystemUtils.GetWithNewExtension(
                    file,
                    "ply");

                xSectionBBox
                    .Mesh
                    .ExportPLY(
                        FileSystemUtils.GetFileWithPostfix(
                            outputFile,
                            "_sectionX"),
                    exportConfig.CropOrientation.Value);

                ySectionBBox
                    .Mesh
                    .ExportPLY(
                        FileSystemUtils.GetFileWithPostfix(
                            outputFile,
                            "_sectionY"),
                    exportConfig.CropOrientation.Value);

                zSectionBBox
                    .Mesh
                    .ExportPLY(
                        FileSystemUtils.GetFileWithPostfix(
                            outputFile,
                            "_sectionZ"),
                    exportConfig.CropOrientation.Value);
            }

#if DEBUG_SHAPE_CROSSSECTION_EXPORT
            xSectionBBox.ExportMeshPly(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    file,
                    "_DEBUG_02_x_bbox",
                    "ply"));

            ySectionBBox.ExportMeshPly(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    file,
                    "_DEBUG_02_y_bbox",
                    "ply"));

            zSectionBBox.ExportMeshPly(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    file,
                    "_DEBUG_02_z_bbox",
                    "ply"));
#endif

            IReadOnlyList<IFiniteGeometry> geometries = shape.GetGeometries();
            List<IFiniteGeometry> xSectionGeometries = new List<IFiniteGeometry>();
            List<IFiniteGeometry> ySectionGeometries = new List<IFiniteGeometry>();
            List<IFiniteGeometry> zSectionGeometries = new List<IFiniteGeometry>();
            OctTree<IFiniteGeometry> xSectionOctTree = new OctTree<IFiniteGeometry>(true);
            OctTree<IFiniteGeometry> ySectionOctTree = new OctTree<IFiniteGeometry>(true);
            OctTree<IFiniteGeometry> zSectionOctTree = new OctTree<IFiniteGeometry>(true);

            Parallel.For(
                0,
                geometries.Count,
                () => (
                    xSection: new List<IFiniteGeometry>(),
                    ySection: new List<IFiniteGeometry>(),
                    zSection: new List<IFiniteGeometry>()
                ),
                (i, loopState, localSectionGeometries) => {

                    if (geometries[i].Intersects(xSectionBBox)) {
                        localSectionGeometries.xSection.Add(geometries[i]);
                    }
                    if (geometries[i].Intersects(ySectionBBox)) {
                        localSectionGeometries.ySection.Add(geometries[i]);
                    }
                    if (geometries[i].Intersects(zSectionBBox)) {
                        localSectionGeometries.zSection.Add(geometries[i]);
                    }

                    return localSectionGeometries;
                },
                localSectionGeometries => {

                    lock (@lock) {

                        xSectionGeometries.AddRange(localSectionGeometries.xSection);
                        ySectionGeometries.AddRange(localSectionGeometries.ySection);
                        zSectionGeometries.AddRange(localSectionGeometries.zSection);
                    }
                });

#if DEBUG_SHAPE_CROSSSECTION_EXPORT
            xSectionGeometries.ExportMeshPly(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    file,
                    "_DEBUG_03_x_geometries",
                    "ply"),
                Color.Gray);

            ySectionGeometries.ExportMeshPly(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    file,
                    "_DEBUG_03_y_geometries",
                    "ply"),
                Color.Gray);

            zSectionGeometries.ExportMeshPly(
                FileSystemUtils.GetWithPostfixAndNewExtension(
                    file,
                    "_DEBUG_03_z_geometries",
                    "ply"),
                Color.Gray);
#endif

            xSectionOctTree.Load(xSectionGeometries);
            ySectionOctTree.Load(ySectionGeometries);
            zSectionOctTree.Load(zSectionGeometries);

            Parallel.For(
                0,
                _crossSectionX.GetLength(0),
                y => {

                    for (int z = 0; z < _crossSectionX.GetLength(1); z++) {

                        AABox voxel = GetVoxelGeometry(
                            xCrossSectionIndex,
                            y,
                            z,
                            exportConfig.Resolution,
                            exportConfig.CropMinCoordinate);

                        if (xSectionOctTree.Intersects(voxel)) {

                            _crossSectionX[y, z] = true;
                        }
                    }
                });

            Parallel.For(
                0,
                _crossSectionY.GetLength(0),
                x => {

                    for (int z = 0; z < _crossSectionY.GetLength(1); z++) {

                        AABox voxel = GetVoxelGeometry(
                            x,
                            yCrossSectionIndex,
                            z,
                            exportConfig.Resolution,
                            exportConfig.CropMinCoordinate);

                        if (ySectionOctTree.Intersects(voxel)) {

                            _crossSectionY[x, z] = true;
                        }
                    }
                });

            Parallel.For(
                0,
                _crossSectionZ.GetLength(0),
                x => {

                    for (int y = 0; y < _crossSectionZ.GetLength(1); y++) {

                        AABox voxel = GetVoxelGeometry(
                            x,
                            y,
                            zCrossSectionIndex,
                            exportConfig.Resolution,
                            exportConfig.CropMinCoordinate);

                        if (zSectionOctTree.Intersects(voxel)) {

                            _crossSectionZ[x, y] = true;
                        }
                    }
                });

            crossSectionX = _crossSectionX;
            crossSectionY = _crossSectionY;
            crossSectionZ = _crossSectionZ;
        }

        private static void ExportPLY(
                this Mesh mesh,
                string file,
                Matrix3d rotation) {

            mesh.Rotate(
                new Rotation(rotation));

            mesh.ExportMeshPly(file);
        }

        private static AABox GetVoxelGeometry(
                int x,
                int y,
                int z,
                double resolution,
                Vector3d offset) {

            return AABox.FromCenterAndSize(
                new Vector3d(
                    offset.X + x * resolution,
                    offset.Y + y * resolution,
                    offset.Z + z * resolution),
                new Vector3d(resolution));
        }

        private static Mat Render(
                this double[,] grid,
                double? min,
                double? max,
                RenderMode renderMode,
                ColormapTypes colorMap,
                bool invertScale) {

            double[,] grid2 = null;

            if (renderMode == RenderMode.SDF_ZERO_CROSSING) {

                grid2 = grid.Copy();

                for (int x = 0; x < grid.GetLength(0); x++) {
                    for (int y = 0; y < grid.GetLength(1); y++) {

                        grid[x, y] = grid[x, y].Abs();
                    }
                }

                min = grid2.Min();
                max = grid2.Max();
            }

            using (Mat image = new Mat(
                    grid.GetLength(0),
                    grid.GetLength(1),
                    MatType.CV_64FC1,
                    grid)) {

                if (renderMode == RenderMode.GREYSCALE) {

                    Mat result = image.ConvertTo8Bit(
                        ref min,
                        ref max);

                    if (invertScale) {

                        result = result.Invert();
                    }

                    return result;
                }

                if (renderMode == RenderMode.COLOR) {

                    return image.Colorize(
                        min,
                        max,
                        Color.Black,
                        false,
                        invertScale,
                        colorMap);
                }

                if (renderMode == RenderMode.SDF_INSIDE_OUTSIDE) {

                    Mat result = new Mat(
                        image.Size(),
                        MatType.CV_8UC3);

                    for (int r = 0; r < image.Height; r++) {
                        for (int c = 0; c < image.Width; c++) {

                            if (grid[r, c] < 0.0) {

                                result.Set(
                                    r,
                                    c,
                                    Color.Red.ToOpenCV());
                            }
                            else if(grid[r, c] > 0.0) {

                                result.Set(
                                    r,
                                    c,
                                    Color.Green.ToOpenCV());
                            }
                            else {

                                result.Set(
                                    r,
                                    c,
                                    Color.Black.ToOpenCV());
                            }
                        }
                    }

                    return result;
                }

                if (renderMode == RenderMode.SDF_ZERO_CROSSING) {

                    Mat result = image.Colorize(
                        min,
                        max,
                        Color.Black,
                        false,
                        invertScale,
                        colorMap);

                    for (int r = 0; r < image.Height; r++) {
                        for (int c = 0; c < image.Width; c++) {

                            bool isZeroCrossing = false;

                            for (int dr = -1; dr <= 1; dr++) {
                                for (int dc = -1; dc <= 1; dc++) {

                                    int r2 = r + dr;
                                    int c2 = c + dc;

                                    if (r2 < 0
                                            || c2 < 0
                                            || r2 >= image.Height
                                            || c2 >= image.Width) {

                                        continue;
                                    }

                                    if ((grid2[r, c] > 0.0 
                                                && grid2[r2, c2] < 0.0)
                                            || (grid2[r, c] < 0.0 
                                                && grid2[r2, c2] > 0.0)) {

                                        isZeroCrossing = true;
                                        break;
                                    }
                                }

                                if (isZeroCrossing) {
                                    break;
                                }
                            }

                            if (isZeroCrossing) {

                                result.Set(
                                    r,
                                    c,
                                    Color.Black.ToOpenCV());
                            }
                        }
                    }

                    return result;
                }

                if (renderMode == RenderMode.RAW_DATA_IMAGE) {

                    return image.Clone();
                }

                throw new NotImplementedException();
            }
        }

        private static Mat Render(
                this bool[,] grid,
                bool invert) {

            Mat image = new Mat(
                grid.GetLength(0),
                grid.GetLength(1),
                MatType.CV_8UC3);

            using (Mat<Vec3b> _image = new Mat<Vec3b>(image)) {

                MatIndexer<Vec3b> imageData = _image.GetGenericIndexer<Vec3b>();

                for (int r = 0; r < grid.GetLength(0); r++) {
                    for (int c = 0; c < grid.GetLength(1); c++) {

                        imageData[r, c] = grid[r, c] ?
                            invert ?
                                Scalar.Black.ToVec3b() :
                                Scalar.White.ToVec3b() :
                            invert ?
                                Scalar.White.ToVec3b() :
                                Scalar.Black.ToVec3b();
                    }
                }
            }

            return image;
        }

        private static void ApplyRegistrationFile(
                this IShape shape,
                string registrationFile) {

            Pose completeTransformation = Pose.Identity;

            foreach (Pose transformation in ReadRegistrationTransformations(registrationFile)) {

                completeTransformation = transformation * completeTransformation;
            }

            shape.Transform(
                completeTransformation,
                true);
        }

        private static IEnumerable<Pose> ReadRegistrationTransformations(
                string registrationFile) {

            JToken jsonData = JSONTools.Read(registrationFile);

            JArray array = jsonData.ReadProperty("transforms") as JArray;

            for (int i = 0; i < array.Count; i++) {

                bool invert = array[i].ReadBool("invert");

                Pose transformation = array[i].ReadPose("transform");

                if (invert) {
                    transformation.Invert();
                }

                yield return transformation;
            }
        }

        private static void ApplyTransformationFile(
                this IShape shape,
                string transformationFile) {

            JToken jsonData = JSONTools.Read(transformationFile);

            shape.Transform(
                jsonData.ReadPose("source_to_json_transformation"),
                true);

            shape.Transform(
                jsonData.ReadPose("json_to_nerfstudio_transformation"),
                true);

            shape.Scale(
                jsonData.ReadDouble("json_to_nerfstudio_scale_factor"),
                true);
        }

        private static void TransformToNeRFStudio(
                this List<Pose> poses,
                string transformationFile) {

            JToken jsonData = JSONTools.Read(transformationFile);

            double jsonToNeRFStudioScaleFactor = jsonData.ReadDouble("json_to_nerfstudio_scale_factor");
            Pose jsonToNeRFStudioTransformation = jsonData.ReadPose("json_to_nerfstudio_transformation");

            for (int i = 0; i < poses.Count; i++) {

                poses[i] = jsonToNeRFStudioTransformation * poses[i];
                poses[i].Position *= jsonToNeRFStudioScaleFactor;
            }
        }

        private static void TransformToColmap(
                this List<Pose> poses,
                string transformationFile) {

            JToken jsonData = JSONTools.Read(transformationFile);

            Pose jsonToColmapStudioTransformation = jsonData
                .ReadPose("source_to_json_transformation")
                .Inverted();

            for (int i = 0; i < poses.Count; i++) {

                poses[i] = jsonToColmapStudioTransformation * poses[i];
            }
        }

        private static void ReadCrop(
                string cropFile,
                out Vector3d center,
                out Vector3d size,
                out Matrix3d orientation) {

            JToken jsonData = JSONTools.Read(cropFile);

            center = jsonData.ReadVector3d("center");
            size = jsonData.ReadVector3d("size");
            orientation = jsonData.ReadMatrix3d("orientation");
        }

        private static List<Pose> LoadPoses(
                string inputFile) {

            List<Pose> poses = new List<Pose>();

            JToken data = JSONTools.Read(inputFile);

            InnerOrientation innerOrientation = new InnerOrientation(
                    data.ReadInteger("h"),
                    data.ReadInteger("w"),
                    data.ReadDouble("fl_x"),
                    data.ReadDouble("fl_y")) {

                PrincipalPointX = data.ReadDouble("cx"),
                PrincipalPointY = data.ReadDouble("cy")
            };

            foreach (JToken frame in (data as JObject)
                    .Children()
                    .Select(property => property as JProperty)
                    .Where(property => property.Name == "frames")
                    .Select(framesProperty => framesProperty.Value as JArray)
                    .SelectMany(array => array.Children())) {

                List<JProperty> properties = (frame as JObject)
                    .Children()
                    .Select(property => property as JProperty)
                    .ToList();

                JArray poseArray = properties
                    .Where(property => property.Name == "transform_matrix")
                    .Select(poseProperty => poseProperty.Value as JArray)
                    .First();

                string filePath = properties
                    .Where(property => property.Name == "file_path")
                    .Select(property => property.Value.ToString())
                    .First();

                Pose pose = new Pose(
                    (double)(poseArray[0] as JArray)[0],
                        (double)(poseArray[0] as JArray)[1],
                        (double)(poseArray[0] as JArray)[2],
                        (double)(poseArray[0] as JArray)[3],
                    (double)(poseArray[1] as JArray)[0],
                        (double)(poseArray[1] as JArray)[1],
                        (double)(poseArray[1] as JArray)[2],
                        (double)(poseArray[1] as JArray)[3],
                    (double)(poseArray[2] as JArray)[0],
                        (double)(poseArray[2] as JArray)[1],
                        (double)(poseArray[2] as JArray)[2],
                        (double)(poseArray[2] as JArray)[3]);

                pose.SetInnerOrientation(innerOrientation);

                poses.Add(pose);
            }

            return poses;
        }

        private static void CompareCrossSectionFiles(
                string crossSectionFile1,
                string crossSectionFile2,
                bool inverted) {

            Vec3b TRUE_POSITIVE_COLOR = Color.ForestGreen.ToOpenCV();
            Vec3b FALSE_POSITIVE_COLOR = Color.Orange.ToOpenCV();
            Vec3b FALSE_NEGATIVE_COLOR = Color.Pink.ToOpenCV();
            Vec3b TRUE_NEGATIVE_COLOR = Color.White.ToOpenCV();

            Vec3b targetColor = inverted ?
                Color.Black.ToOpenCV() :
                Color.White.ToOpenCV();
            
            using (Mat 
                    crossSectionImage1 = new Mat(crossSectionFile1),
                    crossSectionImage2 = new Mat(crossSectionFile2),
                    result = new Mat(
                        crossSectionImage1.Size(),
                        MatType.CV_8UC3)) {

                using (Mat<Vec3b> 
                        _crossSectionImage1 = new Mat<Vec3b>(crossSectionImage1),
                        _crossSectionImage2 = new Mat<Vec3b>(crossSectionImage2),
                        _result = new Mat<Vec3b>(result)) {

                    MatIndexer<Vec3b> crossSectionData1 = _crossSectionImage1.GetIndexer();
                    MatIndexer<Vec3b> crossSectionData2 = _crossSectionImage2.GetIndexer();
                    MatIndexer<Vec3b> resultData = _result.GetIndexer();

                    Parallel.For(
                        0,
                        crossSectionImage1.Height,
                        r => {

                            for (int c = 0; c < crossSectionImage1.Width; c++) {

                                Vec3b resultColor;

                                bool value1 = crossSectionData1[r, c] == targetColor;
                                bool value2 = crossSectionData2[r, c] == targetColor;

                                if (value1 && value2) {

                                    resultColor = TRUE_POSITIVE_COLOR;
                                }
                                else if (value1) {

                                    resultColor = FALSE_NEGATIVE_COLOR;
                                }
                                else if (value2) {

                                    resultColor = FALSE_POSITIVE_COLOR;
                                }
                                else {
                                    resultColor = TRUE_NEGATIVE_COLOR;
                                }

                                resultData[r, c] = resultColor;
                            }
                        });
                }

                result.ImWrite(
                    FileSystemUtils.GetFileWithPostfix(
                        crossSectionFile1,
                        $"_vs_{Path.GetFileNameWithoutExtension(crossSectionFile2)}"));
            }
        }

#if DGPF_EVAL
        public static void DGPFEval_CheckScaleFactors(
                        string registrationFile) {

            foreach (Pose transformation in ReadRegistrationTransformations(registrationFile)) {

                Debug.WriteLine(
                    $"Scale: {transformation.Scale.X}, {transformation.Scale.X}, {transformation.Scale.X}");
            }
        }

        public static void DGPFEval_ShowHistogramms(
                string directory,
                string registrationFile = null,
                double? additionalScaleFactor = null) {

            DGPFEval_ForAllFiles(
                directory,
                file => DGPFEval_ShowHistogramm(
                    file,
                    registrationFile,
                    additionalScaleFactor));
        }

        public static void DGPFEval_Eval(
                string directory,
                string registrationFile = null,
                double? additionalScaleFactor = null,
                double? visualizationMax = null) {

            DGPFEval_ForAllFiles(
                directory,
                file => DGPFEval_EvalFile(
                    file,
                    registrationFile,
                    additionalScaleFactor,
                    visualizationMax));
        }

        private static void DGPFEval_ForAllFiles(
                string directory,
                Action<string> callback) {

            foreach (string fileName in new string[] {
                "colmap.ply",
                "nerfacto_density.ply",
                "nerfacto_nerfstudio.ply"
            }) {

                callback($"{directory}/{fileName}");
            }
        }

        private static void DGPFEval_ShowHistogramm(
                string file,
                string registrationFile,
                double? additionalScaleFactor) {

            IShape shape = DGPFEval_LoadWithScaledDistances(
                file,
                registrationFile,
                additionalScaleFactor,
                out string propertyIdentifier);

            Histogram histogram = new Histogram(0.001);

            foreach (Point point in shape.GetPoints()) {

                histogram.Add(
                    point.GetFloatProperty(propertyIdentifier));
            }

            histogram
                .CreateChart(
                    Path.GetFileNameWithoutExtension(file))
                .Show();
        }

        private static void DGPFEval_EvalFile(
                string file,
                string registrationFile = null,
                double? additionalScaleFactor = null,
                double? visualizationMax = null) {

            IShape shape = DGPFEval_LoadWithScaledDistances(
                file,
                registrationFile,
                additionalScaleFactor,
                out string propertyIdentifier);

            FloatStatistics statistics = new FloatStatistics();

            shape
                .GetPoints()
                .Select(point => point.GetFloatProperty(propertyIdentifier))
                .ForEach(statistics.Update);

            Debug.WriteLine(file);
            Debug.WriteLine($"Mean {statistics.Mean}");
            Debug.WriteLine($"StdDev {statistics.StandardDeviation}");

            double max = visualizationMax.HasValue ?
                visualizationMax.Value :
                statistics.Max;

            IReadOnlyList<Point> points = shape.GetPoints();

            Color[] colors = points
                .Select(point => (double)point.GetFloatProperty(propertyIdentifier))
                .ToArray()
                .Colorize(
                    0.0,
                    max,
                    Color.Black,
                    useBackgroundColorForOutOfRangePixel : false);

            Parallel.For(
                0,
                points.Count,
                i => {
                    points[i].SetColor(colors[i]);
                });

            new PlyWriter() {
                PointFormat = new ColoredPointFormat()
            }.Write(
                FileSystemUtils.GetFileWithPostfix(
                    file,
                    $"_colorized_{max}"),
                shape.Mesh);
        }

        private static IShape DGPFEval_LoadWithScaledDistances(
                string file,
                string registrationFile,
                double? additionalScaleFactor,
                out string propertyIdentifier) {

           float scaleFactor = 1.0f;
            
            string _propertyIdentifier = null;

            string[] candidatePropertyIdentifiers = new string[] {
                "scalar_C2C_absolute_distances",
                "scalar_C2M_absolute_distances"
            };

            foreach (string line in File.ReadLines(file)) {

                foreach (string candidatePropertyIdentifier in candidatePropertyIdentifiers) {

                    if (line.Contains(candidatePropertyIdentifier)) {

                        _propertyIdentifier = candidatePropertyIdentifier;
                        break;
                    }
                }

                if (line.Contains("end_header")) {
                    break;
                }
            }

            if (_propertyIdentifier == null) {

                throw new ApplicationException("Property Identifier not found.");
            }

            IShape shape = new PlyReader() {
                PointFormat = new PointFormat() {
                    PropertyDescriptor = new PropertyDescriptor()
                        .AddFloatProperty(_propertyIdentifier)
                }
            }.ReadShape(file);

            if (registrationFile != null) {

                foreach (Pose transformation in ReadRegistrationTransformations(registrationFile)) {

                    scaleFactor /= (float)transformation.Scale.Mean();
                }
            }

            if (additionalScaleFactor.HasValue) {

                scaleFactor /= (float)additionalScaleFactor.Value;
            }

            shape
                .GetPoints()
                .AsParallel()
                .ForEach(point => point.SetFloatProperty(
                    _propertyIdentifier,
                    scaleFactor * point.GetFloatProperty(_propertyIdentifier)));

            propertyIdentifier = _propertyIdentifier;

            return shape;
        }
#endif
    }
}