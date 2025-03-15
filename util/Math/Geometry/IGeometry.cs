using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry {
    public interface IGeometry: IObject {
        double DistanceTo(Vector3d position);
        bool Intersects(AABox box);
        List<Intersection> Intersect(Ray ray);
    }
}