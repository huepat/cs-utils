using OpenCvSharp;
using System;

namespace HuePat.Util.Math.Geometry.Model {
    public class SurfaceRectangle {
        public Point3f Center { get; private set; }
        public int WidthDimension { get; private set; }
        public float Width { get; private set; }
        public int HeightDimension { get; private set; }
        public float Height { get; private set; }

        public SurfaceRectangle(
                Point3f center,
                int widthDimension, 
                float width,
                int heightDimension, 
                float height) {

            CheckDimension(widthDimension);
            CheckDimension(heightDimension);

            if (widthDimension == heightDimension) {
                throw new ArgumentException("'widthDimension' can't be the same as 'heightDimension'");
            }

            if (width <= 0 || height <= 0) {
                throw new ArgumentException("The rectangle must have a positive extent.");
            }

            Center = center;
            WidthDimension = widthDimension;
            HeightDimension = heightDimension;
            Width = width;
            Height = height;
        }

        public Point3f GetSurfacePoint(
                float x, 
                float y) {

            if (x < 0 || x > Width || y < 0 || y > Height) {
                throw new ArgumentException("Point must be inside surface rectangle");
            }

            Point3f corner = Clone(Center);

            AddOffset(ref corner, WidthDimension, -Width / 2 + x);
            AddOffset(ref corner, HeightDimension, -Height / 2 + y);

            return corner;
        }

        private void CheckDimension(
                int dimension) {

            if (dimension != 1 
                    && dimension != 2 
                    && dimension != 3) {

                throw new ArgumentException("Dimension must be 1, 2 or 3.");
            }
        }

        private Point3f Clone(
                Point3f point) {

            return new Point3f(
                point.X, 
                point.Y, 
                point.Z);
        }

        private void AddOffset(
                ref Point3f point, 
                int dimension, 
                float offset) {

            switch (dimension) {
                case 1:
                    point.X += offset;
                    break;
                case 2:
                    point.Y += offset;
                    break;
                case 3:
                    point.Z += offset;
                    break;
            }
        }
    }
}