using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry.SpatialIndices {
    public interface ISpatialIndex<T> : IFiniteGeometryCollection<T> where T : IFiniteGeometry {
        ISpatialIndex<T> CopyEmpty();
        void Load(IEnumerable<T> geometries);
    }
}