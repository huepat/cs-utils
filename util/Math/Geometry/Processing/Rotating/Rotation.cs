using OpenTK.Mathematics;

namespace HuePat.Util.Math.Geometry.Processing.Rotating {
    public class Rotation {
        public bool UpdateBBox { get; set; }
        public bool UseParallel { get; set; }
        public bool RotateNormals { get; set; }
        public string NormalVectorPropertyKey { get; set; }
        public Vector3d? Anchor { get; set; }
        public Matrix3d Matrix { get; private set; }

        public Rotation(
                Matrix3d matrix) {

            Matrix = matrix;
            NormalVectorPropertyKey = NormalVectorExtensions.NORMAL_VECTOR_PROPERTY_NAME;
        }

        public Rotation(
                double angle,
                Vector3d axis) :
                    this(axis.GetRotationAround(angle))  {
        }
    }
}