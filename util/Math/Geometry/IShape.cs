using OpenTK.Mathematics;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry {
    public enum ShapeType {
        POINT_CLOUD,
        MESH
    }

    public interface IShape : IFiniteGeometry {
        ShapeType Type { get; }

        IShape Clone();
        IReadOnlyList<double> GetSizeWeights();
        IReadOnlyList<Point> GetPoints();
        IReadOnlyList<Vector3d> GetNormals();
        IReadOnlyList<IFiniteGeometry> GetGeometries();
    }
}