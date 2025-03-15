using System.Diagnostics;

namespace HuePat.Util.Math.Statistics {
    public class FloatStatistics {
        public long Count { get; private set; }
        public float Min { get; private set; }
        public float Max { get; private set; }
        public float Mean { get; private set; }
        public float Variance { get; private set; }

        public float StandardDeviation {
            get {
                return Variance.Sqrt();
            }
        }

        public FloatStatistics() {
            Min = float.MaxValue;
            Max = float.MinValue;
            Mean = 0f;
            Variance = 0f;
            Count = 0;
        }

        public void Update(float value) {
            if (value < Min) {
                Min = value;
            }
            if (value > Max) {
                Max = value;
            }
            float oldMean = Mean;
            Mean = oldMean + (value - oldMean) / (Count + 1);
            if (Count > 0) {
                Variance = (1f - 1f / Count) * Variance + (Count + 1) * (Mean - oldMean).Squared();
            }
            Count++;
        }
    }
}