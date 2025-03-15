using HuePat.Util.Object;
using HuePat.Util.Object.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HuePat.Util.Math.Geometry.Processing.Properties {
    public static class Extensions {
        private static readonly object LOCK = new object();
        private static readonly object LOCK2 = new object();

        public static HashSet<T> GetVertexPropertyValues<T>(
                this Mesh mesh,
                Func<Point, T> propertyExtracor,
                bool useParallel = false) {

            if (useParallel) {
                return mesh.Vertices.GetPropertyValues_Parallel(propertyExtracor);
            }

            return mesh.Vertices.GetPropertyValues_Sequential(propertyExtracor);
        }

        public static HashSet<T> GetFacePropertyValues<T>(
                this Mesh mesh,
                Func<Face, T> propertyExtracor,
                bool useParallel = false) {

            if (useParallel) {
                return mesh.GetPropertyValues_Parallel(propertyExtracor);
            }

            return mesh.GetPropertyValues_Sequential(propertyExtracor);
        }

        public static PointCloud PropagatePropertiesToPoints(
                this PointCloud pointCloud,
                bool useParallel = false) {

            if (pointCloud.Properties != null) {

                pointCloud.SetProperties(
                    pointCloud.Properties,
                    useParallel);
            }

            return pointCloud;
        }

        public static Mesh PropagatePropertiesToVertices(
                this Mesh mesh,
                bool useParallel = false) {

            return mesh.PropagateProperties(
                mesh.Vertices,
                useParallel);
        }

        public static Mesh PropagatePropertiesToFaces(
                this Mesh mesh,
                bool useParallel = false) {

            return mesh.PropagateProperties(
                mesh,
                useParallel);
        }

        public static Mesh UnwrapFaceProperties(
                this Mesh mesh,
                PropertyDescriptor propertyDescriptor,
                bool useParallel = false) {

            return useParallel ?
                mesh.UnwrapFaceProperties_Parallel(propertyDescriptor) :
                mesh.UnwrapFaceProperties_Sequential(propertyDescriptor);
        }

        public static Mesh UnwrapProperties(
                this Face face,
                PropertyDescriptor propertyDescriptor) {

            List<Point> vertices = new List<Point>();
            List<Face> faces = new List<Face>();

            face.UnwrapProperties(
                propertyDescriptor,
                vertices,
                faces);

            return new Mesh(vertices, faces);
        }

        private static HashSet<T_value> GetPropertyValues_Sequential<T_value, T_meshElement>(
                this IReadOnlyList<T_meshElement> meshElements,
                Func<T_meshElement, T_value> propertyExtractor) {

            HashSet<T_value> values = new HashSet<T_value>();

            foreach (T_meshElement meshElement in meshElements) {
                values.Add(
                    propertyExtractor(meshElement));
            }

            return values;
        }

        private static HashSet<T_values> GetPropertyValues_Parallel<T_values, T_meshElement>(
                this IReadOnlyList<T_meshElement> meshElements,
                Func<T_meshElement, T_values> propertyExtractor) {

            HashSet<T_values> values = new HashSet<T_values>();

            Parallel.ForEach(
                Partitioner.Create(0, meshElements.Count),
                () => new HashSet<T_values>(),
                (partition, loopState, localValues) => {

                    for (int i = partition.Item1; i < partition.Item2; i++) {
                        localValues.Add(
                            propertyExtractor(meshElements[i]));
                    }

                    return localValues;
                },
                localValues => {
                    lock (LOCK) {
                        values.AddRange(localValues);
                    }
                });

            return values;
        }

        private static Mesh PropagateProperties(
                this Mesh mesh,
                IReadOnlyList<IObject> objects,
                bool useParallel) {

            if (mesh.Properties != null) {

                objects.SetProperties(
                    mesh.Properties,
                    useParallel);
            }
            
            return mesh;
        }

        private static Mesh UnwrapFaceProperties_Sequential(
                this Mesh mesh,
                PropertyDescriptor propertyDescriptor) {

            List<Point> vertices = new List<Point>();
            List<Face> faces = new List<Face>();

            foreach (Face face in mesh) {
                face.UnwrapProperties(
                    propertyDescriptor,
                    vertices,
                    faces);
            }

            return new Mesh(vertices, faces);
        }

        private static Mesh UnwrapFaceProperties_Parallel(
                this Mesh mesh,
                PropertyDescriptor propertyDescriptor) {

            List<Mesh> meshes = new List<Mesh>();

            Parallel.ForEach(
                Partitioner.Create(0, mesh.Count),
                () => (
                    new List<Point>(),
                    new List<Face>()),
                (partition, loopState, localMeshComponents) => {
                    for (int i = partition.Item1; i < partition.Item2; i++) {
                        mesh[i].UnwrapProperties(
                            propertyDescriptor,
                            localMeshComponents.Item1,
                            localMeshComponents.Item2);
                    }
                    return localMeshComponents;
                },
                localMeshComponents => {
                    Mesh localMesh = new Mesh(
                        localMeshComponents.Item1,
                        localMeshComponents.Item2);
                    lock (LOCK2) {
                        meshes.Add(localMesh);
                    }
                });

            return Mesh.From(meshes);
        }

        private static void UnwrapProperties(
                this Face face,
                PropertyDescriptor propertyDescriptor,
                List<Point> vertices,
                List<Face> faces) {

            vertices.Add(
                new Point(face.Vertex1.Position));
            vertices.Add(
                new Point(face.Vertex2.Position));
            vertices.Add(
                new Point(face.Vertex3.Position));

            faces.Add(
                new Face(
                    vertices.Count - 3,
                    vertices.Count - 2,
                    vertices.Count - 1,
                    vertices));

            for (int i = vertices.Count - 1; i >= vertices.Count - 3; i--) {
                foreach (string property in propertyDescriptor.ByteProperties) {
                    vertices[i].SetByteProperty(
                        property,
                        face.GetByteProperty(property));
                }
                foreach (string property in propertyDescriptor.IntegerProperties) {
                    vertices[i].SetIntegerProperty(
                        property,
                        face.GetIntegerProperty(property));
                }
                foreach (string property in propertyDescriptor.FloatProperties) {
                    vertices[i].SetFloatProperty(
                        property,
                        face.GetFloatProperty(property));
                }
                foreach (string property in propertyDescriptor.DoubleProperties) {
                    vertices[i].SetDoubleProperty(
                        property,
                        face.GetDoubleProperty(property));
                }
                foreach (string property in propertyDescriptor.Vector3Properties) {
                    vertices[i].SetVector3Property(
                        property,
                        face.GetVector3Property(property));
                }
                foreach (string property in propertyDescriptor.ColorProperties) {
                    vertices[i].SetColorProperty(
                        property,
                        face.GetColorProperty(property));
                }
            }
        }
    }
}