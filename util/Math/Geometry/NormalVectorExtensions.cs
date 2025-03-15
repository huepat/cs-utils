using HuePat.Util.Object;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System.Linq;

namespace HuePat.Util.Math.Geometry {
    public static class NormalVectorExtensions {
        public static string NORMAL_VECTOR_PROPERTY_NAME = "normalVector";

        public static bool HasNormalVector(
                this IObject @object) {

            return @object.HasVector3Property(NORMAL_VECTOR_PROPERTY_NAME);
        }

        public static Vector3d GetNormalVector(
                this IObject @object) {

            return @object.GetVector3Property(NORMAL_VECTOR_PROPERTY_NAME);
        }

        public static void SetNormalVector(
                this IObject @object, 
                Vector3d normalVector) {

            @object.SetVector3Property(
                NORMAL_VECTOR_PROPERTY_NAME, 
                normalVector);
        }

        public static PropertyDescriptor AddNormalVector(
                this PropertyDescriptor descriptor) {

            descriptor.AddVector3Property(NORMAL_VECTOR_PROPERTY_NAME);

            return descriptor;
        }

        public static bool HasNormalVector(
                this PropertyDescriptor descriptor) {

            return descriptor.Vector3Properties.Contains(NORMAL_VECTOR_PROPERTY_NAME);
        }
    }
}
