using HuePat.Util.Object.Properties;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HuePat.Util.Object {
    public static class Extensions {
        public static bool HasProperty(
                this IObject @object,
                string key) {
            if (@object.Properties == null) {
                return false;
            }
            return @object
                .Properties
                .ContainsKey(key);
        }

        public static IProperty GetProperty(
                this IObject @object, 
                string key) {
            return @object.Properties[key];
        }

        public static void SetProperty(
                this IObject @object, 
                string key, 
                IProperty property) {
            if (@object.Properties == null) {
                @object.Properties = new Dictionary<string, IProperty>();
            }
            if (@object.Properties.ContainsKey(key)) {
                @object.Properties[key] = property;
            }
            else {
                @object.Properties.Add(key, property);
            }
        }

        public static void SetProperties(
                this IObject @object,
                Dictionary<string, IProperty> properties) {
            foreach (string key in properties.Keys) {
                @object.SetProperty(
                    key,
                    properties[key]);
            }
        }

        public static void SetProperties(
                this IEnumerable<IObject> objects,
                Dictionary<string, IProperty> properties,
                bool useParallel = false) {
            if (useParallel) {
                objects.SetProperties_Parallel(properties);
            }
            else {
                objects.SetProperties_Sequential(properties);
            }
        }

        public static void SetProperties(
                this IList<IObject> objects,
                Dictionary<string, IProperty> properties,
                bool useParallel = false) {
            if (useParallel) {
                objects.SetProperties_Parallel(properties);
            }
            else {
                objects.SetProperties_Sequential(properties);
            }
        }

        public static void SetProperties(
                this IReadOnlyList<IObject> objects,
                Dictionary<string, IProperty> properties,
                bool useParallel = false) {
            if (useParallel) {
                objects.SetProperties_Parallel(properties);
            }
            else {
                objects.SetProperties_Sequential(properties);
            }
        }

        public static void CopyPropertiesFrom(
                this IObject @object,
                IObject otherObject) {

            if (otherObject.Properties == null) {
                return;
            }

            if (@object.Properties == null) {
                @object.Properties = new Dictionary<string, IProperty>();
            }

            @object.Properties.AddOrOverwrite(
                otherObject.Properties);
        }

        private static void SetProperties_Sequential(
                this IEnumerable<IObject> objects,
                Dictionary<string, IProperty> properties) {
            foreach (IObject @object in objects) {
                @object.SetProperties(properties);
            }
        }

        private static void SetProperties_Parallel(
                this IEnumerable<IObject> objects,
                Dictionary<string, IProperty> properties) {
            Parallel.ForEach(
                objects,
                (@object, loopState) => {
                    @object.SetProperties(properties);
                });
        }

        private static void SetProperties_Parallel(
                this IList<IObject> objects,
                Dictionary<string, IProperty> properties) {
            Parallel.ForEach(
                Partitioner.Create(0, objects.Count),
                (partition, loopState) => {
                    for (int i = partition.Item1; i < partition.Item2; i++) {
                        objects[i].SetProperties(properties);
                    }
                });
        }

        private static void SetProperties_Parallel(
                this IReadOnlyList<IObject> objects,
                Dictionary<string, IProperty> properties) {
            Parallel.ForEach(
                Partitioner.Create(0, objects.Count),
                (partition, loopState) => {
                    for (int i = partition.Item1; i < partition.Item2; i++) {
                        objects[i].SetProperties(properties);
                    }
                });
        }
    }
}