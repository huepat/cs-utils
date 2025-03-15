using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.Projection;
using HuePat.Util.Object;
using HuePat.Util.Object.Properties;
using OpenCvSharp;

namespace HuePat.Util.Photogrammetry {
    public class InnerOrientation : IProperty {
        public int Height { get; private set; }
        public int Width { get; private set; }
        public double FocalLengthX { get; private set; }
        public double FocalLengthY { get; private set; }
        public double PrincipalPointX { get; set; }
        public double PrincipalPointY { get; set; }
        public double K1 { get; set; }
        public double K2 { get; set; }
        public double K3 { get; set; }
        public double P1 { get; set; }
        public double P2 { get; set; }

        public double FocalLength {
            get {
                return (FocalLengthX + FocalLengthY) / 2.0;
            }
        }

        public InnerOrientation(
                int height,
                int width,
                double focalLengthX,
                double focalLengthY) {

            Height = height;
            Width = width;
            FocalLengthX = focalLengthX;
            FocalLengthY = focalLengthY;
            PrincipalPointX = Height / 2.0;
            PrincipalPointY = Width / 2.0;
            K1 = 0.0;
            K2 = 0.0;
            K3 = 0.0;
            P1 = 0.0;
            P2 = 0.0;
        }

        public InnerOrientation(
                int height,
                int width,
                double focalLength) : 
                    this(
                        height,
                        width,
                        focalLength,
                        focalLength) {
        }

        public InnerOrientation(
                PerspectiveProjection projection) :
                    this(
                        projection.ImageSize.Height,
                        projection.ImageSize.Width,
                        projection.FocalLengthX,
                        projection.FocalLengthY) {

            PrincipalPointX = projection.PrincipalPointX;
            PrincipalPointY = projection.PrincipalPointY;
            K1 = projection.Distortion.Get<double>(0);
            K2 = projection.Distortion.Get<double>(1);
            P1 = projection.Distortion.Get<double>(2);
            P2 = projection.Distortion.Get<double>(3);
            K3 = projection.Distortion.Get<double>(4);
        }

        public IProperty Clone() {

            return new InnerOrientation(
                    Height,
                    Width,
                    FocalLengthX,
                    FocalLengthY) {
                PrincipalPointX = PrincipalPointX,
                PrincipalPointY = PrincipalPointY,
                K1 = K1,
                K2 = K2,
                K3 = K3,
                P1 = P1,
                P2 = P2
            };
        }

        public Mat GetCameraMatrixOpenCV() {

            Mat cameraMatrix = new Mat(
                new Size(3, 3),
                MatType.CV_64FC1);

            cameraMatrix.Set(0, 0, FocalLengthX);
            cameraMatrix.Set(1, 1, FocalLengthY);
            cameraMatrix.Set(0, 2, PrincipalPointX);
            cameraMatrix.Set(1, 2, PrincipalPointY);
            cameraMatrix.Set(2, 2, 1.0);

            return cameraMatrix;
        }

        public Mat GetDistortionCoefficientsOpenCV() {

            Mat distortionCoefficients = new Mat(
                new Size(5, 1),
                MatType.CV_64FC1);

            distortionCoefficients.Set(0, 0, K1);
            distortionCoefficients.Set(1, 0, K2);
            distortionCoefficients.Set(2, 0, P1);
            distortionCoefficients.Set(3, 0, P2);
            distortionCoefficients.Set(4, 0, K3);

            return distortionCoefficients;
        }
    }
}