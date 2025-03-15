using HuePat.Util.Object.Properties;
using HuePat.Util.Processing.Transformations;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry.Processing.PoseTransformation {
    public static class Extensions {

        public static void Transform(
                this IShape shape,
                Pose transform,
                bool useParallel = false) {

            shape.GetPoints().Transform(
                new Transformation<Point>() {
                    UseParallel = useParallel,
                    Function = point => {

                        point.Position = transform * point.Position;
                    }
                });
        }

        public static void Transform(
                this List<Pose> trajectory,
                Pose transform) {

            for (int i = 0; i < trajectory.Count; i++) {

                Dictionary<string, IProperty> properties = trajectory[i].Properties;

                trajectory[i] = transform * trajectory[i];

                trajectory[i].Properties = properties;
            }
        }
    }
}
