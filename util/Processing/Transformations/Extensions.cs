using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HuePat.Util.Processing.Transformations {
    public static class Extensions {
        public static void Transform<T>(
                this IEnumerable<T> objects,
                Transformation<T> transformation) where T : class {

            if (transformation.UseParallel) {
                objects.Transform_Parallel(transformation);
            }
            else {
                objects.Transform_Sequential(transformation);
            }
        }

        public static void Transform<T>(
                this IList<T> objects,
                Transformation<T> transformation) where T : class {

            if (transformation.UseParallel) {
                objects.Transform_Parallel(transformation);
            }
            else {
                objects.Transform_Sequential(transformation);
            }
        }

        public static void Transform<T>(
                this IReadOnlyList<T> objects,
                Transformation<T> transformation) where T : class {

            if (transformation.UseParallel) {
                objects.Transform_Parallel(transformation);
            }
            else {
                objects.Transform_Sequential(transformation);
            }
        }

        private static void Transform_Sequential<T>(
                this IEnumerable<T> objects,
                Transformation<T> transformation) where T : class {

            foreach (T @object in objects) {
                transformation.Function(@object);
            }
        }

        private static void Transform_Sequential<T>(
                this IList<T> objects,
                Transformation<T> transformation) where T : class {

            for (int i = 0; i < objects.Count; i++) {
                transformation.Function(objects[i]);
            }
        }

        private static void Transform_Sequential<T>(
                this IReadOnlyList<T> objects,
                Transformation<T> transformation) where T : class {

            for (int i = 0; i < objects.Count; i++) {
                transformation.Function(objects[i]);
            }
        }

        private static void Transform_Parallel<T>(
                this IEnumerable<T> objects,
                Transformation<T> transformation) where T : class {

            Parallel.ForEach(
                objects,
                (@object, loopState) => {
                    transformation.Function(@object);
                });
        }

        private static void Transform_Parallel<T>(
                this IList<T> objects,
                Transformation<T> transformation) where T : class {

            Parallel.ForEach(
                Partitioner.Create(0, objects.Count),
                (partition, loopState) => {
                    for (int i = partition.Item1; i < partition.Item2; i++) {
                        transformation.Function(objects[i]);
                    }
                });
        }

        private static void Transform_Parallel<T>(
                this IReadOnlyList<T> objects,
                Transformation<T> transformation) where T : class {

            Parallel.ForEach(
                Partitioner.Create(0, objects.Count),
                (partition, loopState) => {
                    for (int i = partition.Item1; i < partition.Item2; i++) {
                        transformation.Function(objects[i]);
                    }
                });
        }
    }
}