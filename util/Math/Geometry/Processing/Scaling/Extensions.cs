using HuePat.Util.Processing.Transformations;

namespace HuePat.Util.Math.Geometry.Processing.Scaling {
    public static class Extensions {
        public static void Scale(
                this IShape shape,
                double scaleFactor,
                bool useParallel = false) {

            shape.GetPoints().Transform(
                new Transformation<Point>() {
                    UseParallel = useParallel,
                    Function = point => {

                        point.Position = scaleFactor * point.Position;
                    }
                });
        }
    }
}
