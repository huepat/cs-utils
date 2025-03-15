namespace HuePat.Util.Math.Geometry {
    public interface IFiniteGeometry: IGeometry {
        Mesh Mesh { get; }
        AABox BBox { get; }
        void UpdateBBox();
    }
}