using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.Object.Properties {
    public class PropertyDescriptor {
        private readonly List<string> boolProperties;
        private readonly List<string> byteProperties;
        private readonly List<string> integerProperties;
        private readonly List<string> floatProperties;
        private readonly List<string> doubleProperties;
        private readonly List<string> stringProperties;
        private readonly List<string> vector3Properties;
        private readonly List<string> colorProperties;

        public IReadOnlyList<string> BoolProperties {
            get {
                return boolProperties;
            }
        }

        public IReadOnlyList<string> ByteProperties {
            get {
                return byteProperties;
            }
        }

        public IReadOnlyList<string> IntegerProperties {
            get {
                return integerProperties;
            }
        }

        public IReadOnlyList<string> FloatProperties {
            get {
                return floatProperties.ToArray();
            }
        }

        public IReadOnlyList<string> DoubleProperties {
            get {
                return doubleProperties.ToArray();
            }
        }

        public IReadOnlyList<string> StringProperties {
            get {
                return stringProperties.ToArray();
            }
        }

        public IReadOnlyList<string> Vector3Properties {
            get {
                return vector3Properties.ToArray();
            }
        }

        public IReadOnlyList<string> ColorProperties {
            get {
                return colorProperties.ToArray();
            }
        }

        public PropertyDescriptor (PropertyDescriptor descriptor): this() {
            AddByteProperties(descriptor.ByteProperties);
            AddIntegerProperties(descriptor.IntegerProperties);
            AddFloatProperties(descriptor.FloatProperties);
            AddDoubleProperties(descriptor.DoubleProperties);
            AddVector3Properties(descriptor.Vector3Properties);
            AddColorProperties(descriptor.ColorProperties);
        }

        public PropertyDescriptor () {
            byteProperties = new List<string>();
            integerProperties = new List<string>();
            floatProperties = new List<string>();
            doubleProperties = new List<string>();
            vector3Properties = new List<string>();
            colorProperties = new List<string>();
        }

        public PropertyDescriptor AddBoolProperty(string property) {
            boolProperties.Add(property);
            return this;
        }

        public PropertyDescriptor AddBoolProperties(IEnumerable<string> properties) {
            boolProperties.AddRange(properties);
            return this;
        }

        public PropertyDescriptor AddByteProperty(string property) {
            byteProperties.Add(property);
            return this;
        }

        public PropertyDescriptor AddByteProperties(IEnumerable<string> properties) {
            byteProperties.AddRange(properties);
            return this;
        }

        public PropertyDescriptor AddIntegerProperty(string property) {
            integerProperties.Add(property);
            return this;
        }

        public PropertyDescriptor AddIntegerProperties(IEnumerable<string> properties) {
            integerProperties.AddRange(properties);
            return this;
        }

        public PropertyDescriptor AddFloatProperty(string property) {
            floatProperties.Add(property);
            return this;
        }

        public PropertyDescriptor AddFloatProperties(IEnumerable<string> properties) {
            floatProperties.AddRange(properties);
            return this;
        }

        public PropertyDescriptor AddDoubleProperty(string property) {
            doubleProperties.Add(property);
            return this;
        }

        public PropertyDescriptor AddDoubleProperties(IEnumerable<string> properties) {
            doubleProperties.AddRange(properties);
            return this;
        }

        public PropertyDescriptor AddStringProperty(string property) {
            stringProperties.Add(property);
            return this;
        }

        public PropertyDescriptor AddStringProperties(IEnumerable<string> properties) {
            stringProperties.AddRange(properties);
            return this;
        }

        public PropertyDescriptor AddVector3Property(string property) {
            vector3Properties.Add(property);
            return this;
        }

        public PropertyDescriptor AddVector3Properties(IEnumerable<string> properties) {
            vector3Properties.AddRange(properties);
            return this;
        }

        public PropertyDescriptor AddColorProperty(string property) {
            colorProperties.Add(property);
            return this;
        }

        public PropertyDescriptor AddColorProperties(IEnumerable<string> properties) {
            colorProperties.AddRange(properties);
            return this;
        }

        public bool IsCompatibleWith(PropertyDescriptor descriptor) {
            return
                descriptor.BoolProperties.All(property => boolProperties.Contains(property)) &&
                descriptor.ByteProperties.All(property => byteProperties.Contains(property)) &&
                descriptor.IntegerProperties.All(property => integerProperties.Contains(property)) &&
                descriptor.FloatProperties.All(property => floatProperties.Contains(property)) &&
                descriptor.DoubleProperties.All(property => doubleProperties.Contains(property)) &&
                descriptor.StringProperties.All(property => stringProperties.Contains(property)) &&
                descriptor.Vector3Properties.All(property => vector3Properties.Contains(property)) &&
                descriptor.ColorProperties.All(property => colorProperties.Contains(property));
        }
    }
}