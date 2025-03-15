using System;

namespace HuePat.Util.Math.Statistics {
    public class AngleStatistics {
        public long Counter { get; private set; }
        public double Min { get; private set; }
        public double Max { get; private set; }
        public double Mean { get; private set; }
        public double Variance { get; private set; }

        public double StandardDeviation {
            get {
                return Variance.Sqrt();
            }
        }

        public AngleStatistics() {

            Min = double.MaxValue;
            Max = double.MinValue;
            Mean = 0f;
            Variance = 0f;
            Counter = 0;
        }

        public void Update(
                double value) {

            Update(
                value,
                Angles.Substract);
        }

        public void UpdateDegree(
                double value) {

            Update(
                value,
                Angles.SubstractDegree);
        }

        private void Update(
                double value,
                Func<double, double, double> substractionCallback) {

            if (value < Min) {
                Min = value;
            }

            if (value > Max) {
                Max = value;
            }

            double oldMean = Mean;

            Mean = oldMean + substractionCallback(value, oldMean) / (Counter + 1);

            if (Counter > 0) {

                Variance = (1 - 1 / Counter) * Variance + (Counter + 1) 
                    * substractionCallback(Mean, oldMean).Squared();
            }

            Counter++;
        }
    }
}