using HuePat.Util.Math.Geometry.MemoryEfficiency.PointsWithNormals;
using System.Collections.Generic;

namespace HuePat.Util.IO.PLY.Reading.MemoryEfficiency.PointsWithNormals {
    interface IDecoder {

        List<Point> ReadPoints(
            string file, 
            Header header);
    }
}