using HuePat.Util.Math.Geometry.Raytracing;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry {
    public interface IGeometryCollection<T> : IGeometry where T : IGeometry {

        T GetNearest(
            Vector3d position, 
            Predicate<T> filter,
            double? distanceThreshold);

        new List<MultiGeometryIntersection<T>> Intersect(Ray ray);

        List<MultiGeometryIntersection<T>> Intersect(
            Ray ray,
            double? distanceTheshold);

        List<T> Intersect(AABox box);
    }
}