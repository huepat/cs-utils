using OpenTK.Mathematics;
using System.Linq;

namespace HuePat.Util.IO.PLY.Reading.MemoryEfficiency.PointsWithNormals {
    abstract class BaseParser {
        protected readonly Header header;

        public BaseParser(
                Header header) {

            this.header = header;
        }

        protected string[] SplitValues(
                string line) {

            return line.Split(' ').Where(v => v != "").ToArray();
        }

        protected Vector3d ParseVector3d(
                string[] values, 
                int[] indices) {

            return new Vector3d(
                double.Parse(values[indices[0]]),
                double.Parse(values[indices[1]]),
                double.Parse(values[indices[2]]));
        }
    }
}