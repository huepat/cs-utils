using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry.Raytracing {
    public class BoundedRay : Ray {
        public double StartBound { get; private set; }
        public double EndBound { get; private set; }

        public BoundedRay(
                Vector3d origin,
                Vector3d direction,
                double startBound,
                double endBound) :
                    base(origin, direction) {

            StartBound = startBound;
            EndBound = endBound;
        }
    }
}