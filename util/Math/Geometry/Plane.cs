using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry {
    public class Plane: IGeometry {
        public Vector3d Origin { get; set; }
        public Vector3d Normal { get; set; }
        public Dictionary<string, IProperty> Properties { get; set; }

        public Plane(
                Vector3d position1, 
                Vector3d position2, 
                Vector3d position3) {

            Triangle triangle = new Triangle(
                position1, 
                position2, 
                position3);

            Origin = triangle.Centroid;
            Normal = triangle.Normal;
        }

        public Plane(
                Vector3d origin, 
                Vector3d normal) {

            Origin = origin;
            Normal = normal;
        }

        public double DistanceTo(
                Vector3d position) {

            return (
                Normal.X * (position.X - Origin.X) +
                Normal.Y * (position.Y - Origin.Y) +
                Normal.Z * (position.Z - Origin.Z)
            ).Abs();
        }

        public bool Intersects(
                AABox box) {

            return DistanceTo(box.Center)
                .Abs() <=
                    box.Size.X * Normal.X.Abs() +
                    box.Size.Y * Normal.Y.Abs() +
                    box.Size.Z * Normal.Z.Abs();
        }

        public List<Intersection> Intersect(
                Ray ray) {

            double num = Vector3d.Dot(
                Origin - ray.Origin, 
                Normal);

            double denom = Vector3d.Dot(
                Normal, 
                ray.Direction);

            if (denom.Abs() < ray.Delta) {
                // not entirely correct, ray could lie within plane
                return new List<Intersection>();
            }

            double distance = num / denom;

            if (distance < 0 
                    && distance.Abs() > ray.Delta) {

                return new List<Intersection>();
            }

            return new List<Intersection>() {
                new Intersection(
                    distance, 
                    ray.At(distance))
            };
        }

        public void Rotate(
                Matrix3d rotation,
                Vector3d anchorPoint) {

            Origin = Origin.RotateCoordinate(
                rotation,
                anchorPoint);

            Normal = Normal.RotateDirection(rotation);
        }

        public Pose GetPose() {

            Vector3d zAxis = Normal;

            Vector3d yAxis = Vector3d.Cross(
                new Vector3d(1, 0, 0), 
                zAxis
            ).Normalized();

            Vector3d xAxis = Vector3d.Cross(yAxis, zAxis).Normalized();

            return new Pose() {
                Position = Origin,
                OrientationMatrix = new Matrix3d(
                    xAxis.X, yAxis.X, zAxis.X,
                    xAxis.Y, yAxis.Y, zAxis.Y,
                    xAxis.Z, yAxis.Z, zAxis.Z)
            };
        }

        public Mesh GetMesh(
                double size) {

            return GetMesh(
                new Vector2d(size));
        }

        public Mesh GetMesh(
                Vector2d size) {

            Vector3d v1 = Random.GetVector3d();

            v1 -= Vector3d.Dot(v1, Normal) * Normal;

            v1.Normalize();

            Vector3d v2 = Vector3d.Cross(Normal, v1);

            Point[] vertices = new Point[] {
                new Point(Origin - size[0] * v1 - size[1] * v2),
                new Point(Origin - size[0] * v1 + size[1] * v2),
                new Point(Origin + size[0] * v1 - size[1] * v2),
                new Point(Origin + size[0] * v1 + size[1] * v2)
            };
            return new Mesh(
            vertices,
                new Face[] {
                        new Face(0, 1, 2, vertices),
                        new Face(1, 2, 3, vertices)
                }
            );
        }
    }
}