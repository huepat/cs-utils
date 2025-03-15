using System;
using System.IO;
using System.Linq;
using OpenCvSharp;

namespace HuePat.Util.Math.Geometry.Projection {
    public class RadialDistortionPerspectiveProjection : IProjection {
        private const string IMAGE_WIDTH_IDENTIFIER = "width";
        private const string IMAGE_HEIGHT_IDENTIFIER = "height";
        private const string PRINCIPAL_POINT_X_IDENTIFIER = "principal_point_x";
        private const string PRINCIPAL_POINT_Y_IDENTIFIER = "principal_point_y";
        private const string FOCAL_LENGTH_IDENTIFIER = "focal_length";
        private const string ASPECT_RATIO_IDENTIFIER = "aspect";
        private const string SKEW_IDENTIFIER = "skew";
        private const string RADIAL_DISTORTION_IDENTIFIER = "radial_distortion";

        public Size ImageSize { get; private set; }
        public Point2f PrincipalPoint { get; private set; }
        public float FocalLength { get; private set; }
        public float AspectRatio { get; private set; }
        public float Skew { get; private set; }
        public float RadialDistortion { get; private set; }

        public RadialDistortionPerspectiveProjection(string file) {
            FromFile(file);
        }

        public RadialDistortionPerspectiveProjection(
                Size size,
                Point2f principalPoint,
                float focalLength,
                float aspectRatio,
                float skew,
                float radialDistortion) {
            ImageSize = size;
            PrincipalPoint = principalPoint;
            FocalLength = focalLength;
            AspectRatio = aspectRatio;
            Skew = skew;
            RadialDistortion = radialDistortion;
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
                PrincipalPoint = new Point2f(
                    fileStorage[PRINCIPAL_POINT_X_IDENTIFIER].ReadFloat(),
                    fileStorage[PRINCIPAL_POINT_Y_IDENTIFIER].ReadFloat());
                FocalLength = fileStorage[FOCAL_LENGTH_IDENTIFIER].ReadFloat();
                AspectRatio = fileStorage[ASPECT_RATIO_IDENTIFIER].ReadFloat();
                Skew = fileStorage[SKEW_IDENTIFIER].ReadFloat();
                RadialDistortion = fileStorage[RADIAL_DISTORTION_IDENTIFIER].ReadFloat();
            }
        }

        public void ToFile(string file) {
            using (FileStorage fileStorage = new FileStorage(file, FileStorage.Modes.Write)) {
                fileStorage.Write(IMAGE_WIDTH_IDENTIFIER, ImageSize.Width);
                fileStorage.Write(IMAGE_HEIGHT_IDENTIFIER, ImageSize.Height);
                fileStorage.Write(PRINCIPAL_POINT_X_IDENTIFIER, PrincipalPoint.X);
                fileStorage.Write(PRINCIPAL_POINT_Y_IDENTIFIER, PrincipalPoint.Y);
                fileStorage.Write(FOCAL_LENGTH_IDENTIFIER, FocalLength);
                fileStorage.Write(ASPECT_RATIO_IDENTIFIER, AspectRatio);
                fileStorage.Write(SKEW_IDENTIFIER, Skew);
                fileStorage.Write(RADIAL_DISTORTION_IDENTIFIER, RadialDistortion);
            }
        }

        public Pose GetModelPoseInCameraFrame(Point3f[] modelPoints, Point2f[] imagePoints, PnpParams pnpParams) {
            throw new NotImplementedException();
        }

        public Point2f[] Project(Point3f[] modelPoints_modelFrame, Pose cameraPose_modelFrame) {
            if (modelPoints_modelFrame.Length == 0) {
                return new Point2f[0];
            }
            Pose modelPose_cameraFrame = cameraPose_modelFrame.Inverted();
            Point2f[] modelPoints_imageFrame = modelPoints_modelFrame
                .Select(p => modelPose_cameraFrame * p)
                .Select(p => new Point2f(p.X / p.Z, p.Y / p.Z))
                .Select(Distort)
                .Select(p => new Point2f(
                    FocalLength * (p.X + Skew * p.Y) + PrincipalPoint.X,
                    FocalLength * AspectRatio * p.Y + PrincipalPoint.Y))
                .ToArray();
            FilterPointsOutsideView(modelPoints_imageFrame, modelPoints_modelFrame, modelPose_cameraFrame);
            return modelPoints_imageFrame;
        }

        private Point2f Distort(Point2f undistorted) {
            float r_u_sq = undistorted.X.Squared() + undistorted.Y.Squared();
            float denom = 2 * RadialDistortion * r_u_sq.Sqrt();
            float r_d_sq;
            if (denom < float.Epsilon && denom > -float.Epsilon) {
                r_d_sq = 1 / (1 - 4 * RadialDistortion * r_u_sq);
            }
            else {
                r_d_sq = ((1 - (1 - 4 * RadialDistortion * r_u_sq).Sqrt()) / denom).Squared();
            }
            return new Point2f(
                undistorted.X * (1 + RadialDistortion * r_d_sq),
                undistorted.Y * (1 + RadialDistortion * r_d_sq));
        }

        private void FilterPointsOutsideView(
                Point2f[] modelPoints_imageFrame,
                Point3f[] modelPoints_modelFrame,
                Pose modelPose_cameraFrame) {
            for (int i = 0; i < modelPoints_modelFrame.Length; i++) {
                if (modelPoints_imageFrame[i].X < 0 || modelPoints_imageFrame[i].X > ImageSize.Width ||
                    modelPoints_imageFrame[i].Y < 0 || modelPoints_imageFrame[i].Y > ImageSize.Height ||
                    !IsInsideView(modelPoints_modelFrame[i], modelPose_cameraFrame)
                    ) {
                    modelPoints_imageFrame[i] = new Point2f(-1f, -1f);
                }
            }
        }

        private bool IsInsideView(
                Point3f modelPoint_modelFrame,
                Pose modelPose_cameraFrame) {
            Point3f modelPoint_cameraFrame = modelPose_cameraFrame * modelPoint_modelFrame;
            return System.Math.Atan2(modelPoint_cameraFrame.X, modelPoint_cameraFrame.Z).Abs()
                        < System.Math.Atan2(ImageSize.Width, FocalLength).Abs() &&
                   System.Math.Atan2(modelPoint_cameraFrame.Y, modelPoint_cameraFrame.Z).Abs()
                        < System.Math.Atan2(ImageSize.Height, FocalLength).Abs();
        }
    }        
}
