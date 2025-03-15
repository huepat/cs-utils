using HuePat.Util.Math.Geometry.Raytracing;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry {
    public interface IFiniteGeometryCollection<T> : 
            IGeometryCollection<T>, 
            IFiniteGeometry 
                where T : IFiniteGeometry {

        new List<MultiGeometryIntersection<T>> Intersect(Ray ray);
    }
}