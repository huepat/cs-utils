using HuePat.Util.Math.Geometry;
using HuePat.Util.Object;
using HuePat.Util.Object.Properties;

namespace HuePat.Util.Photogrammetry
{
    public static class Extensions
    {
        public const string CAMERA_ID_PROPERTY_KEY = "camera";
        public const string IMAGE_FILE_NAME_PROPERTY_KEY = "image";
        public const string INNER_ORIENTATION_PROPERTY_KEY = "inner_orientation";

        public static Pose SetCameraId(
                this Pose pose,
                int cameraId) {

            pose.SetIntegerProperty(
                CAMERA_ID_PROPERTY_KEY,
                cameraId);

            return pose;
        }

        public static int GetCameraId(
                this Pose pose) {

            return pose.GetIntegerProperty(CAMERA_ID_PROPERTY_KEY);
        }

        public static Pose SetImageFileName(
                this Pose pose,
                string imageFileName) {

            pose.SetStringProperty(
                IMAGE_FILE_NAME_PROPERTY_KEY,
                imageFileName);

            return pose;
        }

        public static bool HasImageFileName(
                this Pose pose) {

            return pose.HasStringProperty(IMAGE_FILE_NAME_PROPERTY_KEY);
        }

        public static string GetImageFileName(
                this Pose pose) {

            return pose.GetStringProperty(IMAGE_FILE_NAME_PROPERTY_KEY);
        }

        public static InnerOrientation GetInnerOrientation(
                this Pose pose) {

            return pose.GetProperty(INNER_ORIENTATION_PROPERTY_KEY) as InnerOrientation;
        }

        public static Pose SetInnerOrientation(
                this Pose pose,
                InnerOrientation innerOrientation) {

            pose.SetProperty(
                INNER_ORIENTATION_PROPERTY_KEY,
                innerOrientation);

            return pose;
        }
    }
}