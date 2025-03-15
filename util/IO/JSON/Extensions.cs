using HuePat.Util.Math.Geometry;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.IO.JSON {
    public static class Extensions {

        public static bool ReadBool(
                this JToken jsonData,
                string identifier) {

            return (bool)jsonData.ReadProperty(identifier);
        }

        public static int ReadInteger(
                this JToken jsonData,
                string identifier) {

            return (int)jsonData.ReadProperty(identifier);
        }

        public static double ReadDouble(
                this JToken jsonData,
                string identifier) {

            return (double)jsonData.ReadProperty(identifier);
        }

        public static Vector3d ReadVector3d(
                this JToken jsonData,
                string identifier) {

            JArray array = jsonData.ReadProperty(identifier) as JArray;

            return new Vector3d(
                (double)array[0],
                (double)array[1],
                (double)array[2]);
        }

        public static Matrix3d ReadMatrix3d(
                this JToken jsonData,
                string identifier) {

            JArray array = jsonData.ReadProperty(identifier) as JArray;

            return new Matrix3d(
                (double)(array[0] as JArray)[0],
                    (double)(array[0] as JArray)[1],
                    (double)(array[0] as JArray)[2],
                (double)(array[1] as JArray)[0],
                    (double)(array[1] as JArray)[1],
                    (double)(array[1] as JArray)[2],
                (double)(array[2] as JArray)[0],
                    (double)(array[2] as JArray)[1],
                    (double)(array[2] as JArray)[2]);
        }

        public static Pose ReadPose(
                this JToken jsonData,
                string identifier) {

            return (jsonData.ReadProperty(identifier) as JArray)
                .ParsePose();
        }

        public static List<Pose> ReadPoseArray(
                this JToken jsonData,
                string identifier) {

            List<Pose> poses = new List<Pose>();

            JArray array = jsonData.ReadProperty(identifier) as JArray;

            for (int i = 0; i < array.Count; i++) {

                poses.Add(
                    (array[i] as JArray).ParsePose());
            }

            return poses;
        }

        public static JToken ReadProperty(
                this JToken jsonData,
                string identifier) {

            return (jsonData as JObject)
                .Children()
                .Select(property => property as JProperty)
                .Where(property => property.Name == identifier)
                .First()
                .Value;
        }

        private static Pose ParsePose(
                this JArray array) {

            return new Pose(
                (double)(array[0] as JArray)[0],
                    (double)(array[0] as JArray)[1],
                    (double)(array[0] as JArray)[2],
                    (double)(array[0] as JArray)[3],
                (double)(array[1] as JArray)[0],
                    (double)(array[1] as JArray)[1],
                    (double)(array[1] as JArray)[2],
                    (double)(array[1] as JArray)[3],
                (double)(array[2] as JArray)[0],
                    (double)(array[2] as JArray)[1],
                    (double)(array[2] as JArray)[2],
                    (double)(array[2] as JArray)[3]);
        }
    }
}
