using System;
using System.Collections.Generic;
using System.Linq;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry.SpatialIndices {
    public class BruteForceIndex<T> : ISpatialIndex<T> where T : IFiniteGeometry {
        private List<T> geometries;

        public Dictionary<string, IProperty> Properties { get; set; }

        public AABox BBox {
            get {
                return AABox.FromContainedGeometries(geometries);
            }
        }

        public Mesh Mesh { 
            get {
                return Mesh.From(
                    (IEnumerable<IFiniteGeometry>)geometries);
            }
        }

        public void UpdateBBox() {
            // nothing to do
        }

        public ISpatialIndex<T> CopyEmpty() {

            return new BruteForceIndex<T>() { 
                Properties = Properties
            };
        }

        public void Load(
                IEnumerable<T> geometries) {

            this.geometries = new List<T>();
            this.geometries.AddRange(geometries);
        }

        public double DistanceTo(
                Vector3d position) {

            return GetNearest(
                    position,
                    null,
                    null)
                .DistanceTo(position);
        }

        public T GetNearest(
                Vector3d position, 
                Predicate<T> filter,
                double? distanceThreshold) {

            T nearest = default(T);
            double distance;
            double minDistance = double.MaxValue;

            foreach (T geometry in geometries) {

                if (filter != null
                        && !filter(geometry)) {
                    continue;
                }

                distance = geometry.DistanceTo(position);

                if (distanceThreshold.HasValue
                        && distance > distanceThreshold) {
                    continue;
                }

                if (distance < minDistance) {

                    minDistance = distance;
                    nearest = geometry;
                }
            }

            return nearest;
        }

        public bool Intersects(
                AABox box) {

            return geometries.Any(
                geometry => geometry.Intersects(box));
        }

        public List<T> Intersect(
                AABox box) {

            List<T> intersectingGeometries = new List<T>();

            foreach (T geometry in geometries) {

                if (geometry.Intersects(box)) {
                    intersectingGeometries.Add(geometry);
                }
            }

            return intersectingGeometries;
        }

        public List<MultiGeometryIntersection<T>> Intersect(
                Ray ray) {

            return Intersect(ray, null);
        }

        public List<MultiGeometryIntersection<T>> Intersect(
                Ray ray,
                double? distanceTheshold) {

            List<Intersection> boxIntersections;
            List<MultiGeometryIntersection<T>> intersections = new List<MultiGeometryIntersection<T>>();

            foreach (T geometry in geometries) {

                boxIntersections = geometry.BBox.Intersect(ray);

                if (boxIntersections.Count == 0
                        || (distanceTheshold.HasValue
                            && !boxIntersections.Any(intersection => 
                                intersection.Distance <= distanceTheshold.Value))) {

                    continue;
                }

                intersections.AddRange(
                    geometry
                        .Intersect(ray)
                        .Select(intersection => new MultiGeometryIntersection<T>(intersection, geometry)));
            }

            return intersections;
        }

        List<Intersection> IGeometry.Intersect(
                Ray ray) {

            return Intersect(ray)
                .Select(intersection => (Intersection)intersection)
                .ToList();
        }
    }
}