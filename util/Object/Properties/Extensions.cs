using HuePat.Util.Colors;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.Object.Properties {
    public static class Extensions {
        public static Dictionary<string, IProperty> Clone(
                this Dictionary<string, IProperty> properties) {
            if (properties == null) {
                return null;
            }
            return new Dictionary<string, IProperty>(properties);
        }

        public static PropertyDescriptor GetDescriptor(this IObject @object) {
            return new PropertyDescriptor()
                .AddDoubleProperties(@object.GetDoubleProperties())
                .AddVector3Properties(@object.GetVector3dProperties())
                .AddColorProperties(@object.GetColorProperties());
        }

        public static bool HasBoolProperty(this IObject @object, string name) {
            return @object.HasProperty(name) 
                && @object.Properties[name] is BoolProperty;
        }

        public static void SetBoolProperty(
                this IObject @object,
                string key,
                bool value) {
            @object.SetProperty(
                key,
                new BoolProperty() { 
                    Value = value
                });
        }

        public static bool GetBoolProperty(this IObject @object, string name) {
            return (@object.Properties[name] as BoolProperty).Value;
        }

        public static string[] GetBoolProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is BoolProperty)
                .ToArray();
        }

        public static void SetByteProperty(
                this IObject @object,
                string key,
                byte value) {
            @object.SetProperty(
                key,
                new ByteProperty() {
                    Value = value
                });
        }

        public static byte GetByteProperty(this IObject @object, string name) {
            return (@object.Properties[name] as ByteProperty).Value;
        }

        public static string[] GetByteProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is ByteProperty)
                .ToArray();
        }

        public static bool HasIntegerProperty(this IObject @object, string name) {
            return @object.HasProperty(name)
                && @object.Properties[name] is IntegerProperty;
        }

        public static void SetIntegerProperty(
                this IObject @object,
                string key,
                int value) {
            @object.SetProperty(
                key,
                new IntegerProperty() {
                    Value = value
                });
        }

        public static int GetIntegerProperty(this IObject @object, string name) {
            return (@object.Properties[name] as IntegerProperty).Value;
        }

        public static string[] GetIntegerProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is IntegerProperty)
                .ToArray();
        }

        public static bool HasLongProperty(this IObject @object, string name) {
            return @object.HasProperty(name)
                && @object.Properties[name] is LongProperty;
        }

        public static void SetLongProperty(
                this IObject @object,
                string key,
                long value) {
            @object.SetProperty(
                key,
                new LongProperty() {
                    Value = value
                });
        }

        public static long GetLongProperty(this IObject @object, string name) {
            return (@object.Properties[name] as LongProperty).Value;
        }

        public static string[] GetLongProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is LongProperty)
                .ToArray();
        }

        public static bool HasFloatProperty(this IObject @object, string name) {
            return @object.HasProperty(name)
                && @object.Properties[name] is FloatProperty;
        }

        public static void SetFloatProperty(
                this IObject @object,
                string key,
                float value) {
            @object.SetProperty(
                key,
                new FloatProperty() {
                    Value = value
                });
        }

        public static float GetFloatProperty(this IObject @object, string name) {
            return (@object.Properties[name] as FloatProperty).Value;
        }

        public static string[] GetFloatProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is FloatProperty)
                .ToArray();
        }

        public static bool HasDoubleProperty(this IObject @object, string name) {
            return @object.HasProperty(name)
                && @object.Properties[name] is DoubleProperty;
        }

        public static void SetDoubleProperty(
                this IObject @object,
                string key,
                double value) {
            @object.SetProperty(
                key,
                new DoubleProperty() {
                    Value = value
                });
        }

        public static double GetDoubleProperty(this IObject @object, string name) {
            return (@object.Properties[name] as DoubleProperty).Value;
        }

        public static string[] GetDoubleProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is DoubleProperty)
                .ToArray();
        }

        public static bool HasStringProperty(this IObject @object, string name) {
            return @object.HasProperty(name)
                && @object.Properties[name] is StringProperty;
        }

        public static void SetStringProperty(
                this IObject @object,
                string key,
                string value) {
            @object.SetProperty(
                key,
                new StringProperty() {
                    Value = value
                });
        }

        public static string GetStringProperty(this IObject @object, string name) {
            return (@object.Properties[name] as StringProperty).Value;
        }

        public static string[] GetStringProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is StringProperty)
                .ToArray();
        }

        public static bool HasVector3Property(this IObject @object, string name) {
            return @object.HasProperty(name)
                && @object.Properties[name] is Vector3Property;
        }

        public static void SetVector3Property(
                this IObject @object,
                string key,
                Vector3d value) {
            @object.SetProperty(
                key,
                new Vector3Property() {
                    Value = value
                });
        }

        public static Vector3d GetVector3Property(this IObject @object, string name) {
            return (@object.Properties[name] as Vector3Property).Value;
        }

        public static string[] GetVector3dProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is Vector3Property)
                .ToArray();
        }

        public static bool HasColorProperty(this IObject @object, string name) {
            return @object.HasProperty(name)
                && @object.Properties[name] is ColorProperty;
        }

        public static void SetColorProperty(
                this IObject @object,
                string key,
                Color value) {
            @object.SetProperty(
                key,
                new ColorProperty() {
                    Value = value
                });
        }

        public static Color GetColorProperty(this IObject @object, string name) {
            return (@object.Properties[name] as ColorProperty).Value;
        }

        public static string[] GetColorProperties(this IObject @object) {
            return @object
                .Properties
                .Keys
                .Where(key => @object.Properties[key] is ColorProperty)
                .ToArray();
        }
    }
}