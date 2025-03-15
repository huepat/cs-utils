using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry.Model {
    public static class ModelCreator {
        public static Point3f[] CreateWireFrameModel(
                Point3f[] vertices,
                int[] fromIndices,
                int[] toIndices,
                float resolution = 0.001f) {

            if (fromIndices.Length != toIndices.Length) {
                throw new ArgumentException(
                    "'fromIndices' and 'toIndices' must have same length.");
            }

            CheckWireFrameIndices(vertices, fromIndices);
            CheckWireFrameIndices(vertices, toIndices);

            List<Point3f> model = new List<Point3f>();

            for (int i = 0; i < fromIndices.Length; i++) {

                model.AddRange(
                    CreateLine(
                        vertices[fromIndices[i]],
                        vertices[toIndices[i]],
                        resolution));
            }

            return model.ToArray();
        }

        public static Point3f[] CreatePlaneModel(
                float distance,
                float width,
                float height,
                float resolution) {

            return CreateSurfaceModel(
                new SurfaceRectangle[] {
                    new SurfaceRectangle(
                        new Point3f(
                            0f, 
                            0f, 
                            distance),
                        1, 
                        width, 
                        2, 
                        height)
                },
                resolution);
        }

        public static Point3f[] CreateRoomModel(
                float length,
                float width,
                float height,
                float resolution) {

            return CreateSurfaceModel(
                new SurfaceRectangle[] {

                    new SurfaceRectangle(
                        new Point3f(
                            0f, 
                            0f, 
                            0f),
                        1, 
                        width, 
                        2, 
                        height),

                    new SurfaceRectangle(
                        new Point3f(
                            -width / 2, 
                            0f, 
                            length / 2),
                        3, 
                        length, 
                        2, 
                        height),

                    new SurfaceRectangle(
                        new Point3f(
                            width / 2, 
                            0f, 
                            length / 2),
                        3, 
                        length, 
                        2, 
                        height),

                    new SurfaceRectangle(
                        new Point3f(
                            0f, 
                            -height / 2, 
                            length / 2),
                        3, 
                        length, 
                        1, 
                        width),

                    new SurfaceRectangle(
                        new Point3f(
                            0f, 
                            height / 2, 
                            length / 2),
                        3, 
                        length, 
                        1, 
                        width),

                    new SurfaceRectangle(
                        new Point3f(
                            0f, 
                            0f, 
                            length),
                        1, 
                        width, 
                        2, 
                        height)
                },
                resolution);
        }

        public static Point3f[] CreateCornerModel(
                float extend,
                float resolution) {

            return CreateSurfaceModel(
                new SurfaceRectangle[] {

                    new SurfaceRectangle(
                        new Point3f(
                            0f, 
                            0f, 
                            extend / 2),
                        1, 
                        extend, 
                        2, 
                        extend),

                    new SurfaceRectangle(
                        new Point3f(
                            0f, 
                            extend / 2, 
                            0f),
                        1, 
                        extend, 
                        3, 
                        extend),

                    new SurfaceRectangle(
                        new Point3f(
                            -extend / 2, 
                            0f, 
                            0f),
                        2, 
                        extend, 
                        3, 
                        extend)
                },
                resolution);
        }

        public static Point3f[] CreateSurfaceModel(
                SurfaceRectangle[] surfaceRectangles,
                float resolution) {

            List<Point3f> model = new List<Point3f>();

            foreach (SurfaceRectangle rectangle in surfaceRectangles) {

                model.AddRange(
                    CreateRectangle(
                        rectangle, 
                        resolution));
            }

            return model.ToArray();
        }

        public static Point3f[] CreateSphereModel(
                Point3f center,
                float radius,
                float angularResolution) {

            List<Point3f> model = new List<Point3f>();

            for (float longitude = 0f; longitude < 360f; longitude += angularResolution) {
                for (float latitude = 0f; latitude < 180f; latitude += angularResolution) {

                    model.Add(new Point3f(
                        center.X + radius * longitude.DegreeToRadian().Cos() * latitude.DegreeToRadian().Sin(),
                        center.Z + radius * longitude.DegreeToRadian().Sin() * latitude.DegreeToRadian().Sin(),
                        center.Z + radius * latitude.DegreeToRadian().Cos()));
                }
            }

            return model.ToArray();
        }

        private static void CheckWireFrameIndices(
                Point3f[] vertices, 
                int[] indices) {

            foreach (int i in indices) {

                if (i > vertices.Length - 1) {

                    throw new ArgumentException(
                        $"Index {i} doesn't point to model vertex.");
                }
            }
        }

        private static Point3f[] CreateRectangle(
                SurfaceRectangle rectangle, 
                float resolution) {

            List<Point3f> model = new List<Point3f>();

            for (float y = 0f; y <= rectangle.Height; y += resolution) {
                for (float x = 0f; x <= rectangle.Width; x += resolution) {

                    model.Add(rectangle.GetSurfacePoint(x, y));
                }
            }

            return model.ToArray();
        }

        private static Point3f[] CreateLine(
                Point3f from, 
                Point3f to, 
                float resolution) {

            List<Point3f> points = new List<Point3f>();

            float dist = (
                (to.X - from.X).Squared() +
                (to.Y - from.Y).Squared() +
                (to.Z - from.Z).Squared()).Sqrt();

            Point3f direction = new Point3f(
                (to.X - from.X) / dist,
                (to.Y - from.Y) / dist,
                (to.Z - from.Z) / dist);

            for (float interval = 0f; interval < dist; interval += resolution) {

                points.Add(new Point3f(
                    from.X + interval * direction.X,
                    from.Y + interval * direction.Y,
                    from.Z + interval * direction.Z));
            }

            return points.ToArray();
        }
    }
}