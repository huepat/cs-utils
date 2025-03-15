using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.Projection;
using OpenCvSharp;

namespace HuePat.Util.Image.Marker.Checkerboard {
    public class CheckerboardTracker {
        //private static readonly PnpParams PNP_CONFIG = new PnpParams(SolvePnPFlags.Iterative);

        //private readonly CheckerboardDetector detector;
        //private readonly PerspectiveProjection cameraProjection;
        //private readonly Point3f[] checkerboardPoints_CheckerboardFrame;

        //public CheckerboardTracker(
        //        CheckerboardDetector detector,
        //        PerspectiveProjection cameraProjection) {
        //    this.detector = detector;
        //    this.cameraProjection = cameraProjection;
        //    checkerboardPoints_CheckerboardFrame = detector.ConstructCheckerboard();
        //}

        //public Pose GetCheckerboardPoseInCameraFrame(Mat frame) {
        //    Point2f[] checkerboardPoints_ImageFrame = detector.DetectCheckerboardPoints(frame);
        //    if (checkerboardPoints_ImageFrame.Length == 0) {
        //        return null;
        //    }
        //    return cameraProjection.GetModelPoseInCameraFrame(
        //        checkerboardPoints_CheckerboardFrame,
        //        checkerboardPoints_ImageFrame,
        //        PNP_CONFIG);
        //}
    }
}