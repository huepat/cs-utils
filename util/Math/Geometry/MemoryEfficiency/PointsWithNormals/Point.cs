using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals {
    public struct Point {
        public Vector3d Position;
        public Vector3d Normal;

        public Point(
                Vector3d position,
                Vector3d normal) {

            Position = position;
            Normal = normal;
        }

        public double DistanceTo(
                Vector3d position) {

            return (Position - position).Length;
        }
    }
}
