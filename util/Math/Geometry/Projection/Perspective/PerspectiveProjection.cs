using HuePat.Util.Photogrammetry;
using OpenCvSharp;
using OpenTK.Mathematics;
using System;
using System.IO;

namespace HuePat.Util.Math.Geometry.Projection {
    public class PerspectiveProjection : IProjection, IDisposable {
        private const string IMAGE_WIDTH_IDENTIFIER = "width";
        private const string IMAGE_HEIGHT_IDENTIFIER = "height";
        private const string CAMERA_MATRIX_IDENTIFIER = "cameraMatrix";
        private const string DISTORTION_IDENTIFIER = "distortion";

        private static Mat ToMat(double[,] values) {
            Mat mat = new Mat(
                values.GetLength(0),
                values.GetLength(1),
                MatType.CV_64FC1);
            for (int i = 0; i < values.GetLength(0); i++) {
                for (int j = 0; j < values.GetLength(1); j++) {
                    mat.Set(i, j, values[i, j]);
                }
            }
            return mat;
        }

        private static Mat ToMat(double[] values) {
            Mat mat = new Mat(values.Length, 1, MatType.CV_64FC1);
            for (int i = 0; i < values.Length; i++) {
                mat.Set(i, values[i]);
            }
            return mat;
        }

        private static Mat ToMat(Point3f[] points) {
            return new Mat(points.Length, 1, MatType.CV_32FC3, points);
        }

        private static Mat ToMat(Point2f[] points) {
            return new Mat(points.Length, 1, MatType.CV_32FC2, points);
        }

        public Size ImageSize { get; private set; }

        public Mat CameraMatrix { get; private set; }

        public Mat Distortion { get; private set; }

        public Vector2d FocalLength {
            get {
                return new Vector2d(FocalLengthX, FocalLengthY);
            }
        }

        public double FocalLengthX {
            get {
                return CameraMatrix.Get<double>(0, 0);
            }
        }

        public double FocalLengthY {
            get {
                return CameraMatrix.Get<double>(1, 1);
            }
        }

        public Vector2d PrincipalPoint {
            get {
                return new Vector2d(PrincipalPointX, PrincipalPointY);
            }
        }

        public double PrincipalPointX {
            get {
                return CameraMatrix.Get<double>(0, 2);
            }
        }

        public double PrincipalPointY {
            get {
                return CameraMatrix.Get<double>(1, 2);
            }
        }

        public double Skew {
            get {
                return CameraMatrix.Get<double>(0, 1);
            }
        }

        public double[,] CameraMatrixAsArray {
            get {
                return CameraMatrix.ToDoubleArray2D();
            }
        }

        public double[] DistortionAsArray {
            get {
                return Distortion.ToDoubleArray();
            }
        }

        public double[] RadialDistortion {
            get {
                return new double[] {
                    Distortion.Get<double>(0),
                    Distortion.Get<double>(1),
                    Distortion.Get<double>(4)
                };
            }
        }

        public double[] TangentialDistortion {
            get {
                return new double[] {
                    Distortion.Get<double>(2),
                    Distortion.Get<double>(3)
                };
            }
        }

        public PerspectiveProjection()
        {
        }

        public PerspectiveProjection(string file) {
            FromFile(file);
        }

        public PerspectiveProjection(
                Size imageSize,
                double[,] cameraMatrix,
                double[] distortion):
            this(imageSize, ToMat(cameraMatrix), ToMat(distortion))
        {
        }

        public PerspectiveProjection(
                Size imageSize,
                Mat cameraMatrix,
                Mat distortion) {
            ImageSize = imageSize;
            CameraMatrix = cameraMatrix;
            Distortion = distortion;
        }

        public PerspectiveProjection(
                InnerOrientation innerOrientation) :
                    this(
                        new Size(
                            innerOrientation.Width,
                            innerOrientation.Height),
                        innerOrientation.GetCameraMatrixOpenCV(),
                        innerOrientation.GetDistortionCoefficientsOpenCV()) {
        }

        public void Dispose() {
            CameraMatrix?.Dispose();
            Distortion?.Dispose();
        }

        public Vector2d GetFocalLength() {
            return new Vector2d(
                CameraMatrix.Get<double>(0, 0),
                CameraMatrix.Get<double>(1, 1));
        }

        public Vector2d GetFocalLength(System.Drawing.Size imageSize) {
            return GetFocalLength(imageSize.Width, imageSize.Height);
        }

        public Vector2d GetFocalLength(int imageWidth, int imageHeight) {
            return new Vector2d(
                CameraMatrix.Get<double>(0, 0) * ImageSize.Width / imageWidth,
                CameraMatrix.Get<double>(1, 1) * ImageSize.Height / imageHeight);
        }

        public Vector2d GetPrincipalPoint() {
            return new Vector2d(
                CameraMatrix.Get<double>(0, 2),
                CameraMatrix.Get<double>(1, 2));
        }

        public Vector2d GetPrincipalPoint(System.Drawing.Size imageSize) {
            return GetPrincipalPoint(imageSize.Width, imageSize.Height);
        }

        public Vector2d GetPrincipalPoint(int imageWidth, int imageHeight) {
            return new Vector2d(
                CameraMatrix.Get<double>(0, 2) * imageWidth / ImageSize.Width,
                CameraMatrix.Get<double>(1, 2) * imageHeight / ImageSize.Height);
        }

        public void FromFile(string file) {
            if (!File.Exists(file)) {   
                throw new ArgumentException(
                    $"File '{file}' doesn't exist.");
            }
            using (FileStorage fileStorage = new FileStorage(file, FileStorage.Modes.Read)) {
                ImageSize = new Size(
                    fileStorage[IMAGE_WIDTH_IDENTIFIER].ReadFloat(),
                    fileStorage[IMAGE_HEIGHT_IDENTIFIER].ReadFloat());
                CameraMatrix = fileStorage[CAMERA_MATRIX_IDENTIFIER].ReadMat();
                Distortion = fileStorage[DISTORTION_IDENTIFIER].ReadMat();
            }
        }

        public void ToFile(string file) {
            using (FileStorage fileStorage = new FileStorage(file, FileStorage.Modes.Write)) {
                fileStorage.Write(IMAGE_WIDTH_IDENTIFIER, ImageSize.Width);
                fileStorage.Write(IMAGE_HEIGHT_IDENTIFIER, ImageSize.Height);
                fileStorage.Write(CAMERA_MATRIX_IDENTIFIER, CameraMatrix);
                fileStorage.Write(DISTORTION_IDENTIFIER, Distortion);
            }
        }

        public Point2f[] Project(Point3f[] modelPoints_modelFrame, Pose cameraPose_modelFrame) {
            if (modelPoints_modelFrame.Length == 0) {
                return new Point2f[0];
            }
            Point2f[] modelPoints_imageFrame = new Point2f[] { };
            double[,] jacobian = new double[,] { };
            Pose modelPose_cameraFrame = cameraPose_modelFrame.Inverted();
            Cv2.ProjectPoints(
                modelPoints_modelFrame,
                modelPose_cameraFrame.RodriguesElements.ToArray(),
                modelPose_cameraFrame.Position.ToArray(),
                CameraMatrixAsArray, DistortionAsArray,
                out modelPoints_imageFrame, out jacobian);
            FilterPointsOutsideView(
                modelPoints_imageFrame,
                modelPoints_modelFrame,
                modelPose_cameraFrame);
            return modelPoints_imageFrame;
        }

        public Pose GetModelPoseInCameraFrame(Point3f[] modelPoints, Point2f[] imagePoints, PnpParams pnpParams) {
            if (modelPoints.Length != imagePoints.Length ||
                modelPoints.Length == 0) {
                return null;
            }
            if (pnpParams.UseRansac) {
                return SolvePnpRansac(modelPoints, imagePoints, pnpParams);
            }
            return SolvePnp(modelPoints, imagePoints, pnpParams.Algorithm);
        }

        private void FilterPointsOutsideView(
                Point2f[] modelPoints_imageFrame,
                Point3f[] modelPoints_modelFrame,
                Pose modelPose_cameraFrame) {
            for (int i = 0; i < modelPoints_modelFrame.Length; i++) {
                if (modelPoints_imageFrame[i].X < 0 || modelPoints_imageFrame[i].X > ImageSize.Width ||
                    modelPoints_imageFrame[i].Y < 0 || modelPoints_imageFrame[i].Y > ImageSize.Height ||
                    !IsInsideView(modelPoints_modelFrame[i], modelPose_cameraFrame)) {
                    modelPoints_imageFrame[i] = new Point2f(-1f, -1f);
                }
            }
        }

        private bool IsInsideView(
                Point3f modelPoint_modelFrame,
                Pose modelPose_cameraFrame) {
            Point3f modelPoint_cameraFrame = modelPose_cameraFrame * modelPoint_modelFrame;
            return System.Math.Atan2(modelPoint_cameraFrame.X, modelPoint_cameraFrame.Z).Abs()
                        < System.Math.Atan2(ImageSize.Width, 2 * FocalLengthX).Abs() &&
                   System.Math.Atan2(modelPoint_cameraFrame.Y, modelPoint_cameraFrame.Z).Abs()
                        < System.Math.Atan2(ImageSize.Height, 2 * FocalLengthY).Abs();
        }

        private Pose SolvePnp(
                Point3f[] modelPoints,
                Point2f[] imagePoints,
                SolvePnPFlags pnpAlgorithm) {
            Mat position = new Mat();
            Mat rodriguesElements = new Mat();
            Cv2.SolvePnP(
                ToMat(modelPoints),
                ToMat(imagePoints),
                CameraMatrix, Distortion,
                rodriguesElements, position,
                false,
                pnpAlgorithm);
            return CreatePose(position, rodriguesElements);
        }

        private Pose SolvePnpRansac(
                Point3f[] modelPoints,
                Point2f[] imagePoints,
                PnpParams pnpParams) {
            Mat position = new Mat();
            Mat rodriguesElements = new Mat();
            Cv2.SolvePnPRansac(
                ToMat(modelPoints), 
                ToMat(imagePoints),
                CameraMatrix, Distortion,
                rodriguesElements, position,
                false,
                pnpParams.IterationsCount, 
                pnpParams.ReprojectionError, 
                pnpParams.Confidence,
                null,
                pnpParams.Algorithm);
            return CreatePose(position, rodriguesElements);
        }

        private Pose CreatePose(Mat position, Mat rodriguesElements) {
            double[] positionArray = position.ToDoubleArray();
            double[] rodriguesElementsArray = rodriguesElements.ToDoubleArray();
            return new Pose() {
                Position = new Vector3d(
                    positionArray[0],
                    positionArray[1],
                    positionArray[2]),
                RodriguesElements = new Vector3d(
                    rodriguesElementsArray[0],
                    rodriguesElementsArray[1],
                    rodriguesElementsArray[2])
            };
        }
    }
}