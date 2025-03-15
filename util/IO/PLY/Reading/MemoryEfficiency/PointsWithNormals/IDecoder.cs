using HuePat.Util.Math.Geometry;
using System;
using System.Collections.Generic;

namespace HuePat.Util.IO.PLY.Reading {
    interface IDecoder {
        Action<Point> PointProcessor { get; set; }
        Action<Face> FaceProcessor { get; set; }

        List<Point> ReadPoints(
            string file, 
            Header header);

        void ReadMesh(
            string file, 
            bool swithNormals,
            Header header, 
            out List<Point> vertices,
            out List<Face> faces);
    }
}