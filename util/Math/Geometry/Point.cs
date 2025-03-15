using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry {
    public class Point : IFiniteGeometry {
        public static Point operator +(
                Point left, 
                Point right) {

            return left + right.position;
        }

        public static Point operator +(
                Point left, 
                Vector3d right) {

            Point result = left.Clone();

            result.position = left.position + right;

            return result;
        }

        public static Point operator -(
                Point left, 
                Point right) {

            return left - right.position;
        }

        public static Point operator -(
                Point left, 
                Vector3d right) {

            Point result = left.Clone();

            result.position = left.position - right;

            return result;
        }

        public static Point operator *(
                Point point, 
                double factor) {

            Point result = point.Clone();

            result.position = point.position * factor;

            return result;
        }

        public static Point operator *(
                double factor, 
                Point point) {

            return point * factor;
        }

        public static Point operator /(
                Point point, 
                double denominator) {

            return point * (1.0 / denominator);
        }

        public static double DistanceBetween(
                Point point1, 
                Point point2) {

            return point1.DistanceTo(point2);
        }

        private Vector3d position;

        public Vector3d Position {
            get { return position; }
            set { position = value; }
        }

        public double X {
            get { return position.X; }
            set { position.X = value; }
        }

        public double Y {
            get { return position.Y; }
            set { position.Y = value; }
        }

        public double Z {
            get { return position.Z; }
            set { position.Z = value; }
        }

        public Dictionary<string, IProperty> Properties { get; set; }

        public AABox BBox {
            get {
                return new AABox(Position, Position);
            }
        }

        public Mesh Mesh {
            get {
                return new Mesh(
                    new Point[] { this },
                    new Face[0]);
            }
        }

        public Point(
                double x, 
                double y, 
                double z) :
                    this(new Vector3d(x, y, z)) {
        }

        public Point(
                Vector3d position) {

            this.position = position;
        }

        public Point SetColor(
                Color color) {

            (this as IObject).SetColor(color);

            return this;
        }

        public Point Clone() {

            return new Point(
                    new Vector3d(Position)) {

                Properties = Properties.Clone()
            };
        }

        public void UpdateBBox() {
            // nothing to do
        }

        public double DistanceTo(
                Point point) {

            return DistanceTo(point.position);
        }

        public double DistanceTo(
                Vector3d position) {

            return (position - Position).Length;
        }

        public Vector3d DirectionTo(
                Point point) {

            return (point.position - Position).Normalized();
        }

        public bool Intersects(
                AABox box) {

            return
                X >= box.Min.X && X <= box.Max.X &&
                Y >= box.Min.Y && Y <= box.Max.Y &&
                Z >= box.Min.Z && Z <= box.Max.Z;
        }

        public List<Intersection> Intersect(
                Ray ray) {

            Vector3d lambda = new Vector3d(
                (position.X - ray.Origin.X) / ray.Direction.X,
                (position.Y - ray.Origin.Y) / ray.Direction.Y,
                (position.Z - ray.Origin.Z) / ray.Direction.Z);

            if((lambda.X - lambda.Y).Abs() > ray.Delta 
                    || (lambda.X - lambda.Z).Abs() > ray.Delta 
                    || (lambda.Z - lambda.Z).Abs() > ray.Delta) {

                return new List<Intersection>();
            }

            return new List<Intersection> {
                new Intersection(
                    DistanceTo(ray.Origin),
                    position)
            };
        }

        public void Rotate(
                Matrix3d rotation, 
                Vector3d anchorPoint) {

            Position = Position.RotateCoordinate(
                rotation,
                anchorPoint);
        }
    }
}