using HuePat.Util.Colors;
using HuePat.Util.IO;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.Projection;
using HuePat.Util.Math.Statistics;
using HuePat.Util.Photogrammetry;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HuePat.Util.Image {
    public enum PixelDirection {
        X, Y
    }

    public enum Channels {
        ONE = 1,
        TWO = 2,
        THREE = 3,
        FOUR = 4
    }

    public enum DataType {
        BYTE, UNSIGNED_SHORT, SHORT, INT, FLOAT, DOUBLE
    }

    public static class ImageUtils {
        public static Point2f[] QueryPoints(
                this Mat image,
                int pointsCount) {

            int i = 0;

            Point2f[] points = new Point2f[pointsCount];

            Window window = new Window($"Select {pointsCount} pixels");

            window.SetMouseCallback(
                (mouseEvent, x, y, flags, userData) => {

                    if (mouseEvent != MouseEventTypes.LButtonDown) {
                        return;
                    }

                    points[i] = new Point2f(x, y);

                    i++;

                    if (i == pointsCount) {
                        window.Close();
                    }
                });

            window.ShowImage(image);

            Cv2.WaitKey(0);

            return points;
        }

        public static Mat Blend(
                Mat image1,
                Mat image2,
                double alpha) {

            Mat blended = new Mat();

            Cv2.AddWeighted(
                image1,
                alpha,
                image2,
                1 - alpha,
                0.0,
                blended);

            return blended;
        }

        public static Mat CreateMaskFromPolygon(
                Size imageSize,
                Point2f[] points) {

            Mat mask = new Mat(
                imageSize,
                MatType.CV_8UC1,
                Scalar.Black);

            Cv2.DrawContours(
                mask,
                new OpenCvSharp.Point[][] {
                    points
                        .Select(point => new OpenCvSharp.Point(
                            (int)point.X,
                            (int)point.Y))
                        .ToArray()
                },
                -1,
                Scalar.White,
                -1,
                LineTypes.Link4);

            return mask;
        }

        public static void ConvertDirectoryJPG2PNG(
                string directory) {

            foreach (string file in Directory.GetFiles(directory)) {

                if (!file.EndsWith(".jpg") && !file.EndsWith(".jpeg")) {
                    continue;
                }

                using (Mat image = Cv2.ImRead(file)) {

                    Cv2.ImWrite(
                        FileSystemUtils.GetWithNewExtension(file, "png"),
                        image);
                }
            }
        }

        public static List<string> GetImageFileNames(
                string imageDirectory,
                bool orderImagesDescending) {

            IEnumerable<string> imageFileNames = Directory
                .EnumerateFiles(imageDirectory)
                .Select(filePath => Path.GetFileName(filePath));

            if (orderImagesDescending) {
                imageFileNames = imageFileNames.Order();
            }
            else {
                imageFileNames = imageFileNames.OrderDescending();
            }

            return imageFileNames.ToList();
        }

        public static Dictionary<(int, int), List<string>> CheckImageSizes(
                string imageDirectory) {

            int height;
            int width;
            Dictionary<(int, int), List<string>> sizeBuckets
                = new Dictionary<(int, int), List<string>>();

            if (!Directory.Exists(imageDirectory)) {
                throw new ArgumentException($"Directory {imageDirectory} does not exist.");
            }

            foreach (string file in Directory.GetFiles(imageDirectory)) {

                try {
                    GetImageSize(
                        file,
                        out height,
                        out width);
                }
                catch (OpenCVException) {
                    Debug.WriteLine($"Cannot open image '{file}'.");
                    continue;
                }

                sizeBuckets.BucketAdd(
                    (height, width),
                    file);
            }

            if (sizeBuckets.Count > 1) {

                Debug.WriteLine("Different image sizes detected:");

                foreach ((int, int) size in sizeBuckets.Keys) {

                    Debug.WriteLine($"   ({size.Item1}, {size.Item2}):");

                    foreach (string file in sizeBuckets[size]) {
                        Debug.WriteLine($"      {Path.GetFileName(file)}");
                    }
                }
            }
            else {
                Debug.WriteLine($"All images have same size: "
                    + $"({sizeBuckets.Keys.First().Item1}, {sizeBuckets.Keys.First().Item2})");
            }

            return sizeBuckets;
        }

        public static void GetImageSize(
                string file,
                out int height,
                out int width) {

            using (Mat image = Cv2.ImRead(file)) {
                height = image.Height;
                width = image.Width;
            }
        }

        public static Mat GetMaskPixels(
                this Mat mask) {

            Mat maskPixels = new Mat();

            Cv2.FindNonZero(
                mask,
                maskPixels);

            return maskPixels;
        }

        public static Mat Binarize(
                this Mat image) {

            Mat binarized = new Mat();

            using (Mat greyscale = image.ToGreyscale()) {

                Cv2.Threshold(
                    greyscale,
                    binarized,
                    128.0,
                    255.0,
                    ThresholdTypes.Otsu);
            }

            return binarized;
        }

        public static Mat ApplyMask(
                this Mat image,
                Mat mask) {

            Mat masked = new Mat();

            Cv2.BitwiseAnd(
                image,
                image,
                masked,
                mask);

            return masked;
        }

        public static Mat InvertMask(
                this Mat mask) {

            Mat inverted = new Mat();

            Cv2.BitwiseNot(
                mask, 
                inverted);

            return inverted;
        }

        public static Mat XOr(
                this Mat image1,
                Mat image2) {

            Mat result = new Mat();

            Cv2.BitwiseXor(
                image1, 
                image2, 
                result);

            return result;
        }

        public static Mat Or(
                this Mat image1,
                Mat image2) {

            return image1.Or(
                image2,
                null);
        }

        public static Mat Or(
                this Mat image1,
                Mat image2,
                Mat mask) {

            Mat result = new Mat();

            Cv2.BitwiseOr(
                image1,
                image2,
                result,
                mask);

            return result;
        }

        public static void CheckDoubleImage(
                this Mat image) {

            if (image.Type() != MatType.CV_64FC1) {

                throw new ArgumentException(
                    $"This Method can only be applied to images of type {MatType.CV_64FC1}");
            }
        }

        public static Histogram GetHistogram(
                this Mat image,
                double binSize,
                double offset = 0.0) {

            if (image.Channels() != 1) {
                throw new ArgumentException(
                    "Currently, only single channel images are supported.");
            }

            if (image.Type() == MatType.CV_8U) {

                return image.GetHistogram<byte>(
                    binSize,
                    offset);
            }

            if (image.Type() == MatType.CV_64F) {

                return image.GetHistogram<double>(
                    binSize,
                    offset);
            }

            throw new NotImplementedException();
        }

        public static Mat Undistort(
                this Mat image,
                InnerOrientation innerOrientation) {

            Mat undistorted = new Mat();

            using (Mat
                    cameraMatrix = innerOrientation.GetCameraMatrixOpenCV(),
                    distortionCoefficients = innerOrientation.GetDistortionCoefficientsOpenCV()) {

                Cv2.Undistort(
                    image,
                    undistorted,
                    cameraMatrix,
                    distortionCoefficients);
            }

            return undistorted;
        }

        public static Mat Colorize(
                this Mat image,
                double? min,
                double? max,
                Color backGroundColor,
                bool useBackgroundColorForOutOfRangePixel = true,
                bool doInvert = false,
                ColormapTypes colormap = ColormapTypes.Jet,
                double? rangeDeterminationMin = null,
                double? rangeDeterminationMax = null) {

            return image.Colorize(
                ref min,
                ref max,
                backGroundColor,
                useBackgroundColorForOutOfRangePixel,
                doInvert,
                colormap,
                rangeDeterminationMin,
                rangeDeterminationMax);
        }

        public static Mat Colorize(
                this Mat image,
                ref double? min,
                ref double? max,
                Color backGroundColor,
                bool useBackgroundColorForOutOfRangePixel = true,
                bool doInvert = false,
                ColormapTypes colormap = ColormapTypes.Jet,
                double? rangeDeterminationMin = null,
                double? rangeDeterminationMax = null) {

            image.CheckDoubleImage();

            using (Mat<double> _image = new Mat<double>(image)) {

                return _image
                    .GetIndexer()
                    .Colorize(
                        image.Height,
                        image.Width,
                        ref min,
                        ref max,
                        rangeDeterminationMin,
                        rangeDeterminationMax,
                        backGroundColor,
                        useBackgroundColorForOutOfRangePixel,
                        doInvert,
                        colormap);
            }
        }

        public static void ColorizeDirectory(
                string directory,
                string outputDirectory,
                double? min,
                double? max,
                Color backGroundColor,
                bool useBackgroundColorForOutOfRangePixel = true,
                bool doInvert = false,
                ColormapTypes colormap = ColormapTypes.Jet) {

            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory);
            }

            FileSystemUtils.CleanDirectory(outputDirectory);

            foreach (string file in Directory.EnumerateFiles(directory)) {

                using (Mat image = Cv2.ImRead(file, ImreadModes.Unchanged)) {

                    if (image.Type() == MatType.CV_64FC1) {

                        image.Colorize(
                            $"{outputDirectory}/{Path.GetFileName(file)}",
                            min,
                            max,
                            backGroundColor,
                            useBackgroundColorForOutOfRangePixel,
                            doInvert,
                            colormap);
                    }
                }
            }
        }

        public static void ConvertFileTo8Bit(
                string file,
                string outputFile,
                double min,
                double max) {

            double? _min = min;
            double? _max = max;

            using (Mat image = new Mat(file, ImreadModes.Unchanged)) {

                using (Mat image8Bit = image.ConvertTo8Bit(
                        ref _min, 
                        ref _max)) {

                    Cv2.ImWrite(
                        outputFile,
                        image8Bit);
                }
            }
        }

        public static Mat ConvertTo8Bit(
                this Mat image) {

            image.CheckDoubleImage();

            double? min = null;
            double? max = null;

            using (Mat<double> _image = new Mat<double>(image)) {

                return _image
                    .GetIndexer()
                    .ConvertTo8Bit(
                        image.Height,
                        image.Width,
                        ref min,
                        ref max);
            }
        }

        public static Mat ConvertTo8Bit(
                this Mat image,
                ref double? min,
                ref double? max) {

            image.CheckDoubleImage();

            using (Mat<double> _image = new Mat<double>(image)) {

                return _image
                    .GetIndexer()
                    .ConvertTo8Bit(
                        image.Height, 
                        image.Width, 
                        ref min,
                        ref max);
            }
        }

        public static Mat ConvertTo64Bit(
                this Mat image) {

            if (image.Channels() != 1) {

                throw new ArgumentException(
                    "This method can only be applied to single channel images.");
            }

            Mat convertedImage = new Mat();

            image.ConvertTo(
                convertedImage,
                MatType.CV_64FC1);

            return convertedImage;
        }

        public static void ConvertFileTo64Bit(
                string file,
                string outputFile) {

            using (Mat image = new Mat(file, ImreadModes.Unchanged)) {

                using (Mat imageConverted = image.ConvertTo64Bit()) {

                    Cv2.ImWrite(
                        outputFile,
                        imageConverted);
                }
            }
        }

        public static Mat Invert(
                this Mat image) {

            if (image.Type() != MatType.CV_8UC1) {
                throw new ArgumentException($"Image must be of type '{MatType.CV_8UC1}'.");
            }

            Mat inverted = new Mat();

            image.ConvertTo(
                inverted, 
                MatType.CV_8UC1, 
                -1.0, 
                byte.MaxValue);

            return inverted;
        }

        public static void ExportImage(
                string file,
                int height,
                int width,
                Func<(int, int), Color> pixelColorizationCallback) {

            int r, c;

            using (Mat image = new Mat(
                    height,
                    width,
                    MatType.CV_8UC3,
                    Scalar.Black)) {

                for (r = 0; r < height; r++) {
                    for (c = 0; c < width; c++) {

                        image.Set(
                            r,
                            c,
                            pixelColorizationCallback((r, c))
                                .ToOpenCV());
                    }
                }

                Cv2.ImWrite(file, image);
            }
        }

        public static void ExportImage(
                string file,
                int height,
                int width,
                Color color,
                IEnumerable<(int, int)> pixels) {

            ExportImage(
                file,
                height,
                width,
                new (Color, IEnumerable<(int, int)>)[] { 
                    (color, pixels)
                });
        }

        public static void ExportImage(
                string file,
                int height,
                int width,
                IEnumerable<(Color, IEnumerable<(int, int)>)> coloredPixels) {

            using (Mat image = new Mat(
                    height,
                    width,
                    MatType.CV_8UC3,
                    Scalar.Black)) {

                foreach ((Color, IEnumerable<(int, int)>) pixelSet in coloredPixels) {
                    foreach ((int, int) pixel in pixelSet.Item2) {

                        image.Set(
                            pixel.Item1,
                            pixel.Item2,
                            pixelSet.Item1.ToOpenCV());
                    }
                }

                Cv2.ImWrite(file, image);
            }
        }

        public static Mat PixelScale<T>(
                this Mat image,
                uint factor) where T : struct {

            T value;
            int r, r2, rMax, c, c2, cMax, f = (int)factor;
            Mat scaled = new Mat(
                image.Height * f,
                image.Width * f,
                image.Type());

            for (r = 0; r < image.Height; r++) {
                for (c = 0; c < image.Width; c++) {

                    value = image.Get<T>(r, c);
                    rMax = (r + 1) * f - 1;
                    cMax = (c + 1) * f - 1;

                    for (r2 = r * f; r2 <= rMax; r2++) {
                        for (c2 = c * f; c2 <= cMax; c2++) {

                            scaled.Set(r2, c2, value);
                        }
                    }
                }
            }

            return scaled;
        }

        public static Mat Convert(
                this Mat image, 
                ColorConversionCodes colorConversionCode) {

            Mat converted = new Mat();
            Cv2.CvtColor(image, converted, colorConversionCode);

            return converted;
        }

        public static Mat ToGreyscale(
                this Mat image) {

            if (image.Channels() == 4) {
                return image.Convert(ColorConversionCodes.RGBA2GRAY);
            }

            return image.Convert(ColorConversionCodes.RGB2GRAY);
        }

        public static Mat Flip(
                this Mat image, 
                FlipMode flipMode) {

            Mat flipped = new Mat();
            Cv2.Flip(image, flipped, flipMode);

            return flipped;
        }

        public static Mat Sobel(
                this Mat image, 
                PixelDirection direction, 
                MatType resultType,
                BorderTypes borderBehaviour = BorderTypes.Default,
                int kernelSize = 3,
                double scale = 1,
                double delta = 0) {

            Mat result = new Mat();

            Cv2.Sobel(
                image, 
                result, 
                resultType,
                direction == PixelDirection.X ? 1 : 0,
                direction == PixelDirection.Y ? 1 : 0,
                kernelSize, 
                scale, 
                delta, 
                borderBehaviour);

            return result;
        }

        public static void DrawShape(
                this Mat image,
                IList<Point2f> coordinates,
                Scalar color,
                int thickness = 1) {

            List<Point2f> shape = coordinates.ToList();

            shape.Add(coordinates[0]);

            image.DrawLineString(
                shape, 
                color, 
                thickness);
        }

        public static void DrawLineString(
                this Mat image,
                IList<Point2f> coordinates,
                Scalar color, int thickness = 1) {

            for (int i = 1; i < coordinates.Count; i++) {

                Cv2.Line(
                    image, 
                    (OpenCvSharp.Point)coordinates[i - 1], 
                    (OpenCvSharp.Point)coordinates[i], 
                    color, 
                    thickness);
            }
        }

        public static void DrawCrossHairs(
                this Mat image,
                IEnumerable<Point2f> coordinates,
                Scalar color,
                float length = 10f,
                int thickness = 1) {

            foreach (Point2f coordinate in coordinates) {

                image.DrawCrossHair(
                    coordinate, 
                    color, 
                    length, 
                    thickness);
            }
        }

        public static void DrawCrossHair(
                this Mat image,
                Point2f coordinate,
                Scalar color,
                float length = 10f,
                int thickness = 1) {

            Cv2.Line(image,
                new OpenCvSharp.Point(
                    coordinate.X - length, 
                    coordinate.Y),
                new OpenCvSharp.Point(
                    coordinate.X + length, 
                    coordinate.Y),
                color, 
                thickness);

            Cv2.Line(image,
                new OpenCvSharp.Point(
                    coordinate.X, 
                    coordinate.Y - length),
                new OpenCvSharp.Point(
                    coordinate.X, 
                    coordinate.Y + length),
                color, 
                thickness);
        }

        public static void DrawPose(
                this Mat image,
                Pose modelPoseInCameraFrame,
                IProjection projection,
                float axisLength = 1f,
                int thickness = 1) {

            image.DrawAxis3(
                projection.Project(
                    new Point3f[] {
                        new Point3f(0f, 0f, 0f),
                        new Point3f(axisLength, 0f, 0f),
                        new Point3f(0f, axisLength, 0f),
                        new Point3f(0f, 0f, axisLength)
                    },
                    modelPoseInCameraFrame.Inverted()),
                thickness);
        }

        public static void DrawAxis3(
                this Mat image,
                Point2f[] points,
                int thickness = 1) {

            if (points.Length != 4) {
                throw new ArgumentException("Points must be 'Origin, X, Y, Z'");
            }

            if (LineExists(points[0], points[1])) {
                Cv2.ArrowedLine(
                    image, 
                    (OpenCvSharp.Point)points[0], 
                    (OpenCvSharp.Point)points[1], 
                    Scalar.Red, 
                    thickness);
            }

            if (LineExists(points[0], points[2])) {
                Cv2.ArrowedLine(
                    image, 
                    (OpenCvSharp.Point)points[0], 
                    (OpenCvSharp.Point)points[2], 
                    Scalar.Green, 
                    thickness);
            }

            if (LineExists(points[0], points[3])) {
                Cv2.ArrowedLine(
                    image, 
                    (OpenCvSharp.Point)points[0], 
                    (OpenCvSharp.Point)points[3], 
                    Scalar.Blue, thickness);
            }
        }

        public static void GetMinMax(
                this Mat image,
                out double min,
                out double max) {

            image.CheckDoubleImage();

            using (Mat<double> _image = new Mat<double>(image)) {

                _image
                    .GetIndexer()
                    .GetMinMax(
                        image.Height,
                        image.Width,
                        null,
                        null,
                        out min,
                        out max);
            }
        }

        private static void GetMinMax(
                this MatIndexer<double> image,
                int height,
                int width,
                double? rangeDeterminationMin,
                double? rangeDeterminationMax,
                ref double? min,
                ref double? max,
                out double _min,
                out double _max) {

            _min = double.MaxValue;
            _max = double.MinValue;

            if (!min.HasValue
                    || !max.HasValue) {

                image.GetMinMax(
                    height,
                    width,
                    rangeDeterminationMin,
                    rangeDeterminationMax,
                    out _min,
                    out _max);
            }

            if (min.HasValue) {
                _min = min.Value;
            }
            else {
                min = _min;
            }

            if (max.HasValue) {
                _max = max.Value;
            }
            else {
                max = _max;
            }
        }

        private static void GetMinMax(
                this MatIndexer<double> image,
                int height,
                int width,
                double? rangeDeterminationMin,
                double? rangeDeterminationMax,
                out double min,
                out double max) {

            double value;

            min = double.MaxValue;
            max = double.MinValue;

            for (int r = 0; r < height; r++) {
                for (int c = 0; c < width; c++) {

                    value = image[r, c];

                    if (value < min
                            && (!rangeDeterminationMin.HasValue 
                                || value >= rangeDeterminationMin.Value)) {

                        min = value;
                    }
                    if (value > max
                            && (!rangeDeterminationMax.HasValue
                                || value <= rangeDeterminationMax.Value)) {

                        max = value;
                    }
                }
            }
        }

        private static Mat ConvertTo8Bit(
                this MatIndexer<double> image,
                int height,
                int width,
                ref double? min,
                ref double? max) {

            image.GetMinMax(
                height,
                width,
                null,
                null,
                ref min,
                ref max,
                out double _min,
                out double _max);

            return image.ConvertTo8Bit(
                height,
                width,
                _min,
                _max);
        }

        private static Mat ConvertTo8Bit(
                this MatIndexer<double> image,
                int height,
                int width,
                double min,
                double max) {

            Mat image8Bit = new Mat(
                height,
                width,
                MatType.CV_8UC1);

            using (Mat<byte> _image8Bit = new Mat<byte>(image8Bit)) {

                image.ConvertTo8Bit(
                    _image8Bit.GetIndexer(),
                    height,
                    width,
                    min,
                    max);
            }

            return image8Bit;
        }

        private static void ConvertTo8Bit(
                this MatIndexer<double> image,
                MatIndexer<byte> result,
                int height,
                int width,
                double min,
                double max) {

            double value;

            for (int r = 0; r < height; r++) {
                for (int c = 0; c < width; c++) {

                    value = image[r, c];

                    if (value < min) {
                        result[r, c] = 0;
                    }
                    else if (value > max) {
                        result[r, c] = 255;
                    }
                    else {

                        result[r, c] = (byte)(value * (byte.MaxValue - 1) / (max - min)
                           + 1 - (byte.MaxValue - 1) * min / (max - min));
                    }
                }
            }
        }

        private static Mat Colorize(
                this MatIndexer<double> image,
                int height,
                int width,
                ref double? min,
                ref double? max,
                double? rangeDeterminationMin,
                double? rangeDeterminationMax,
                Color backgroundColor,
                bool useBackgroundColorForOutOfRangePixel,
                bool doInvert,
                ColormapTypes colormap) {

            double value;
            Vec3b _backgroundColor = backgroundColor.ToOpenCV();
            Mat inverted;
            Mat colorized = new Mat();

            image.GetMinMax(
                height,
                width,
                rangeDeterminationMin,
                rangeDeterminationMax,
                ref min,
                ref max,
                out double _min,
                out double _max);

            using (Mat image8Bit = image.ConvertTo8Bit(
                    height,
                    width,
                    _min,
                    _max)) {

                inverted = doInvert ?
                    image8Bit.Invert() :
                    image8Bit;

                Cv2.ApplyColorMap(
                    inverted,
                    colorized,
                    colormap);
            }

            if (doInvert) {
                inverted.Dispose();
            }

            for (int r = 0; r < height; r++) {
                for (int c = 0; c < width; c++) {

                    value = image[r, c];

                    if (useBackgroundColorForOutOfRangePixel 
                            && (value < _min
                                || value > _max)) {

                        colorized.Set(
                            r,
                            c,
                            _backgroundColor);
                    }
                }
            }

            return colorized;
        }

        private static void Colorize(
                this Mat image,
                string outputFile,
                double? min,
                double? max,
                Colors.Color backGroundColor,
                bool useBackgroundColorForOutOfRangePixel = true,
                bool doInvert = false,
                ColormapTypes colormap = ColormapTypes.Jet) {

            double? _min = min;
            double? _max = max;

            using (Mat colorized = image.Colorize(
                    ref _min,
                    ref _max,
                    backGroundColor,
                    useBackgroundColorForOutOfRangePixel,
                    doInvert,
                    colormap)) {

                outputFile = FileSystemUtils.GetFileWithPostfix(
                    outputFile,
                    $"_min{_min:0.000}_max{_max:0.000}");

                Cv2.ImWrite(
                    outputFile,
                    colorized);
            }
        }

        private static Histogram GetHistogram<T>(
                this Mat image,
                double binSize,
                double offset = 0.0) where T : unmanaged {

            using (Mat<T> _image = new Mat<T>(image)) {

                return _image
                    .GetIndexer()
                    .GetHistogram<T>(
                        image.Height,
                        image.Width,
                        binSize,
                        offset);
            }
        }

        private static Histogram GetHistogram<T>(
                this MatIndexer<T> image,
                int height,
                int width,
                double binSize,
                double offset = 0.0) where T : unmanaged {

            Histogram histogram = new Histogram(
                binSize,
                offset);

            for (int r = 0; r < height; r++) {
                for (int c = 0; c < width; c++) {

                    histogram.Add(
                        (double)(object)image[r, c]);
                }
            }

            return histogram;
        }

        private static bool LineExists(
                Point2f p1, 
                Point2f p2) {

            return
                p1.X > 0 && p1.Y > 0 &&
                p2.X > 0 && p2.Y > 0;
        }

        private static DataType GetDataType(
                this Mat image) {

            switch (image.Type().Depth) {
                case MatType.CV_8U:
                case MatType.CV_8S:
                    return DataType.BYTE;
                case MatType.CV_16U:
                    return DataType.UNSIGNED_SHORT;
                case MatType.CV_16S:
                    return DataType.SHORT;
                case MatType.CV_32S:
                    return DataType.INT;
                case MatType.CV_32F:
                    return DataType.FLOAT;
                case MatType.CV_64F:
                case MatType.CV_USRTYPE1:
                default:
                    return DataType.DOUBLE;
            }
        }

        private static void ForEachPixel<T>(
                Size imageSize,
                Mat.Indexer<T> imageData,
                Action<int, int, T> callback) where T : struct {

            for (int row = 0; row < imageSize.Height; row++) {
                for (int column = 0; column < imageSize.Width; column++) {

                    callback(
                        row, 
                        column, 
                        imageData[row, column]);
                }
            }
        }
    }
}