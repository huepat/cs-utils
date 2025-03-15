namespace HuePat.Util.Math.Geometry.Raytracing {
    public class MultiGeometryIntersection<T> : Intersection where T : IGeometry {
        public T IntersectingGeometry { get; private set; }

        public MultiGeometryIntersection(
                Intersection intersection,
                T intersectingGeometry)
                    : base(
                        intersection.Distance,
                        intersection.Position) {

            IntersectingGeometry = intersectingGeometry;
        }
    }
}
