using HuePat.Util.Math.Geometry;
using System;

namespace HuePat.Util.IO.PLY.Writing {
    public interface IEncoder : IDisposable {
        void Encode(Point point);
        void Encode(Face face, int offset);
    }
}