using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry.Raytracing {
    public static class Extensions {
        public static PointCloud Visualize(
                this Ray ray,
                double length,
                double pointDistance) {

            List<Point> points = new List<Point>();

            for (double d = 0.0; d < length; d += pointDistance) {

                points.Add(
                    new Point(ray.Origin + d * ray.Direction));
            }

            return new PointCloud(points);
        }
    }
}