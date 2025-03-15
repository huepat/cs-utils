using System.Collections.Generic;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry.Raytracing {
    public class Ray: IGeometry {
        public double Delta { get; private set; }
        public Vector3d Origin { get; private set; }
        public Vector3d Direction { get; private set; }
        public Vector3d InverseDirection { get; private set; }
        public Dictionary<string, IProperty> Properties { get; set; }

        public Ray(
                Vector3d origin, 
                Vector3d direction,
                double delta = double.Epsilon) {

            Origin = origin;
            Direction = direction.Normalized();

            InverseDirection = new Vector3d(
                Direction.X == 0.0 ? double.MaxValue : 1.0 / Direction.X,
                Direction.Y == 0.0 ? double.MaxValue : 1.0 / Direction.Y,
                Direction.Z == 0.0 ? double.MaxValue : 1.0 / Direction.Z);

            Delta = delta;
        }

        public Vector3d At(
                double distance) {

            if (distance < 0.0) {
                return Origin;
            }

            return Origin + distance * Direction;
        }

        public double DistanceTo(
                Vector3d position) {

            return Vector3d.Cross(
                Direction, 
                position - Origin
            ).Length;
        }

        public bool Intersects(
                AABox box) {

            return box.Intersects(this);
        }

        public List<Intersection> Intersect(
                Ray ray) {

            List<Intersection> result = new List<Intersection>();

            double dx = ray.Origin.X - Origin.X;
            double dy = ray.Origin.Y - Origin.Y;
            double det = ray.Direction.X * Direction.Y - ray.Direction.Y * Direction.X;
            double u = (dy * ray.Direction.X - dx * ray.Direction.Y) / det;
            double v = (dy * Direction.X - dx * Direction.Y) / det;

            if (u < 0 || v < 0) {
                return result;
            }

            Vector3d intersection = At(v);

            result.Add(
                new Intersection(
                    (intersection - Origin).Length, 
                    intersection));

            return result;
        }

        public Intersection WeakIntersect(
                Ray ray) {

            Vector3d c = Vector3d.Cross(
                Direction,
                ray.Direction);

            double denom = c.Length.Squared();

            Vector3d t = ray.Origin - Origin;

            double t1 = new Matrix3d(
                    t.X, ray.Direction.X, c.X,
                    t.Y, ray.Direction.Y, c.Y,
                    t.Z, ray.Direction.Z, c.Z)
                .Determinant / denom;

            double t2 = new Matrix3d(
                    t.X, Direction.X, c.X,
                    t.Y, Direction.Y, c.Y,
                    t.Z, Direction.Z, c.Z)
                .Determinant / denom;

            if (t1 > 0) {
                t1 = 0;
            }

            if (t2 > 0) {
                t2 = 0;
            }

            return new Intersection(
                denom,
                0.5 * (Origin + t1 * Direction 
                    + ray.Origin + t2 * ray.Direction));
        }

        public void Rotate(
                Matrix3d rotation, 
                Vector3d anchorPoint) {

            Origin = Origin.RotateCoordinate(
                rotation,
                anchorPoint);

            Direction = Direction.RotateDirection(rotation);
        }
    }
}