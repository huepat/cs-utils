using OpenTK.Mathematics;

namespace HuePat.Util.Math.Statistics {
    public class VectorStatistics {
        private readonly DoubleStatistics[] statistics;

        public Vector3d Mean {
            get {
                return new Vector3d(
                    statistics[0].Mean,
                    statistics[1].Mean,
                    statistics[2].Mean);
            }
        }

        public Vector3d Variance {
            get {
                return new Vector3d(
                    statistics[0].Variance,
                    statistics[1].Variance,
                    statistics[2].Variance);
            }
        }

        public Vector3d StandardDeviation {
            get {
                return new Vector3d(
                    statistics[0].StandardDeviation,
                    statistics[1].StandardDeviation,
                    statistics[2].StandardDeviation);
            }
        }

        public VectorStatistics() {
            statistics = new DoubleStatistics[] {
                new DoubleStatistics(),
                new DoubleStatistics(),
                new DoubleStatistics()
            };
        }

        public void Update(Vector3d value) {
            statistics[0].Update(value[0]);
            statistics[1].Update(value[1]);
            statistics[2].Update(value[2]);
        }
    }
}