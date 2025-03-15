using System;

namespace HuePat.Util.IO.PLY {
    public enum PlyEncoding {
        BINARY_LITTLE_ENDIAN,
        BINARY_BIG_ENDIAN,
        ASCII
    }

    public static class Extensions {
        public static string GetString(
                this PlyEncoding encoding) {

            switch (encoding) {
                case PlyEncoding.BINARY_LITTLE_ENDIAN:
                    return "binary_little_endian";
                case PlyEncoding.BINARY_BIG_ENDIAN:
                    return "binary_big_endian";
                case PlyEncoding.ASCII:
                    return "ascii";
            }

            throw new ArgumentException();
        }
    }
}