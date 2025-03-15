using HuePat.Util.Colors;

namespace HuePat.Util.Math.Statistics {
    public class ColorStatistics {
        private DoubleStatistics[] statistics;

        public Color Mean {
            get {
                return new Color(
                    statistics[0].Mean,
                    statistics[1].Mean,
                    statistics[2].Mean,
                    statistics[3].Mean);
            }
        }

        public Color Variance {
            get {
                return new Color(
                    statistics[0].Variance,
                    statistics[1].Variance,
                    statistics[2].Variance,
                    statistics[3].Variance);
            }
        }

        public Color StandardDeviation {
            get {
                return new Color(
                    statistics[0].StandardDeviation,
                    statistics[1].StandardDeviation,
                    statistics[2].StandardDeviation,
                    statistics[3].StandardDeviation);
            }
        }

        public ColorStatistics() {
            statistics = new DoubleStatistics[] {
                new DoubleStatistics(),
                new DoubleStatistics(),
                new DoubleStatistics(),
                new DoubleStatistics()
            };
        }

        public void Update(Color value) {
            statistics[0].Update(value.R);
            statistics[1].Update(value.G);
            statistics[2].Update(value.B);
            statistics[3].Update(value.A);
        }
    }
}