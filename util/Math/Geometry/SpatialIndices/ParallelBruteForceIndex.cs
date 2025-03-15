using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HuePat.Util.Math.Geometry.Raytracing;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry.SpatialIndices {
    public class ParallelBruteForceIndex<T>: ISpatialIndex<T> where T: IFiniteGeometry {
        private List<T> geometries;

        public Dictionary<string, IProperty> Properties { get; set; }

        public AABox BBox {
            get {
                return AABox.FromContainedGeometries(
                    geometries,
                    true);
            }
        }

        public Mesh Mesh => throw new NotImplementedException();

        public void UpdateBBox() {
            // nothing to do
        }

        public ISpatialIndex<T> CopyEmpty() {

            return new ParallelBruteForceIndex<T>() { 
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

            object @lock = new object();
            double minDistance = double.MaxValue;
            T nearest = default(T);

            Parallel.ForEach(
                Partitioner.Create(0, geometries.Count),
                () => (double.MaxValue, default(T)),
                (partition, loopState, localState) => {

                    double distance;

                    for (int i = partition.Item1; i < partition.Item2; i++) {

                        if (filter != null 
                                && !filter(geometries[i])) {
                            continue;
                        }

                        distance = geometries[i].DistanceTo(position);

                        if (distanceThreshold.HasValue
                                && distance > distanceThreshold.Value) {
                            continue;
                        }

                        if (distance < localState.Item1) {
                            localState.Item1 = distance;
                            localState.Item2 = geometries[i];
                        }
                    }

                    return localState;
                },
                localState => {

                    lock (@lock) {

                        if (localState.Item1 < minDistance) {

                            minDistance = localState.Item1;
                            nearest = localState.Item2;
                        }
                    }
                });

            return nearest;
        }

        public bool Intersects(
                AABox box) {

            return geometries
                .AsParallel()
                .Any(geometry => geometry.Intersects(box));
        }

        public List<T> Intersect(
                AABox box) {

            return geometries
                .AsParallel()
                .Where(geometry => geometry.Intersects(box))
                .ToList();
        }

        public List<MultiGeometryIntersection<T>> Intersect(
                Ray ray) {

            return Intersect(ray, null);
        }

        public List<MultiGeometryIntersection<T>> Intersect(
                Ray ray,
                double? distanceTheshold) {

            return geometries
                .AsParallel()
                .Where(geometry => {

                    List<Intersection> intersections = geometry.BBox.Intersect(ray);

                    if (intersections.Count == 0) {
                        return false;
                    }

                    if (distanceTheshold.HasValue) {

                        return intersections.Any(intersection => 
                            intersection.Distance <= distanceTheshold.Value);
                    }

                    return true;
                })
                .SelectMany(geometry => geometry
                    .Intersect(ray)
                    .Select(intersection => new MultiGeometryIntersection<T>(intersection, geometry)))
                .ToList();
        }

        List<Intersection> IGeometry.Intersect(
                Ray ray) {

            return Intersect(ray)
                .Select(intersection => intersection as Intersection)
                .ToList();
        }
    }
}