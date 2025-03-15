using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry.Raytracing {
    public class Intersection {
        public double Distance { get; private set; }
        public Vector3d Position { get; private set; }

        public Intersection(
                double distance,
                Vector3d position) {
            Distance = distance;
            Position = position;
        }
    }
}