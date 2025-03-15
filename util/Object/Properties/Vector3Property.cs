using OpenTK.Mathematics;

namespace HuePat.Util.Object.Properties {
    public class Vector3Property : IProperty {
        public Vector3d Value { get; set; }

        public IProperty Clone() {
            return new Vector3Property() {
                Value = new Vector3d(Value)
            };
        }
    }
}