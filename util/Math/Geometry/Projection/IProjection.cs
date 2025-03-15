using OpenCvSharp;

namespace HuePat.Util.Math.Geometry.Projection {
    public interface IProjection {
        Size ImageSize{ get; }

        void FromFile(string file);

        void ToFile(string file);

        Point2f[] Project(
            Point3f[] modelPoints_modelFrame,
            Pose cameraPose_modelFrame);

        Pose GetModelPoseInCameraFrame(
            Point3f[] modelPoints,
            Point2f[] imagePoints,
            PnpParams pnpParams);
    }
}