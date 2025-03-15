using HuePat.Util.Object.Properties;
using HuePat.Util.Processing.Transformations;
using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry.Processing.Rotating {
    public static class Extensions {
        public static void Rotate(
                this IShape shape,
                Rotation rotation) {

            Vector3d anchor = rotation.Anchor.HasValue ?
                rotation.Anchor.Value :
                shape.GetCentroid(rotation.UseParallel);

            shape
                .GetPoints()
                .Transform(
                    new Transformation<Point>() { 
                        UseParallel = rotation.UseParallel,
                        Function = point => {

                            point.Rotate(
                                rotation.Matrix,
                                anchor);

                            if (rotation.RotateNormals) {

                                point.SetVector3Property(
                                    rotation.NormalVectorPropertyKey,
                                    point
                                        .GetVector3Property(
                                            rotation.NormalVectorPropertyKey)
                                        .RotateDirection(rotation.Matrix));
                            }
                        }
                    });

            if (rotation.UpdateBBox) {
                shape.UpdateBBox();
            }
        }
    }
}