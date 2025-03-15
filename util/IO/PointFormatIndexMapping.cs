using System;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.IO {
    public class PointFormatIndexMapping {
        public (int, int, int) CoordinateIndices { get; set; }
        public Dictionary<int, string> DoublePropertyIndices { get; private set; }
        public Dictionary<(int, int, int), string> Vector3PropertyIndices { get; private set; }
        public Dictionary<(int, int, int), string> Color3PropertyIndices { get; private set; }
        public Dictionary<(int, int, int, int), string> Color4PropertyIndices { get; private set; }

        public PointFormatIndexMapping() {
            CoordinateIndices = (0, 1, 2);
            DoublePropertyIndices = new Dictionary<int, string>();
            Vector3PropertyIndices = new Dictionary<(int, int, int), string>();
            Color3PropertyIndices = new Dictionary<(int, int, int), string>();
            Color4PropertyIndices = new Dictionary<(int, int, int, int), string>();
        }

        public PointFormatIndexMapping AddDoubleProperty(
                int index, 
                string label) {
            DoublePropertyIndices.Add(index, label);
            return this;
        }

        public PointFormatIndexMapping AddVector3dProperty(
                (int, int, int) index, 
                string label) {
            Vector3PropertyIndices.Add(index, label);
            return this;
        }

        public PointFormatIndexMapping AddColor3Property(
                (int, int, int) index, 
                string label) {
            Color3PropertyIndices.Add(index, label);
            return this;
        }

        public PointFormatIndexMapping AddColor4Property(
                (int, int, int, int) index, 
                string label) {
            Color4PropertyIndices.Add(index, label);
            return this;
        }

        public void Check() {
            HashSet<int> indices = new HashSet<int>();
            foreach (int index in new int[] {
                        CoordinateIndices.Item1,
                        CoordinateIndices.Item2,
                        CoordinateIndices.Item3
                    }
                    .Concat(DoublePropertyIndices.Keys)
                    .Concat(Unroll(Vector3PropertyIndices.Keys))
                    .Concat(Unroll(Color3PropertyIndices.Keys))
                    .Concat(Color4PropertyIndices.Keys
                        .SelectMany(value => new int[] {
                            value.Item1,
                            value.Item2,
                            value.Item3,
                            value.Item4
                        }))) {
                if (indices.Contains(index)) {
                    throw new ArgumentException(
                        $"PointFormatIndexMapping contains duplicate index {index}.");
                }
                indices.Add(index);
            }
        }

        private IEnumerable<int> Unroll(IEnumerable<(int, int, int)> values) {
            return values.SelectMany(value => new int[] { 
                value.Item1,
                value.Item2,
                value.Item3
            });
        }
    }
}
