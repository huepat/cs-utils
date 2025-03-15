namespace HuePat.Util.Math.Statistics {
    public class DoubleStatistics {
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

        public DoubleStatistics() :
                    this(
                        0,
                        0.0,
                        0.0,
                        0.0,
                        0.0) {
        }

        public DoubleStatistics(
                long counter,
                double mean,
                double variance) :
                    this(
                        counter,
                        mean, 
                        variance,
                        0.0,
                        0.0) {
        }

        public DoubleStatistics(
                long counter,
                double mean,
                double variance,
                double min,
                double max) {

            Min = min;
            Max = max;
            Mean = mean;
            Variance = variance;
            Counter = counter;
        }

        public void Update(
                double value) {

            if (value < Min) {
                Min = value;
            }

            if (value > Max) {
                Max = value;
            }

            double oldMean = Mean;

            Mean = oldMean + (value - oldMean) / (Counter + 1);

            if (Counter > 0) {
                Variance = (1.0 - 1.0 / Counter) * Variance + (Counter + 1) * (Mean - oldMean).Squared();
            }

            Counter++;
        }

        public void Update(
                DoubleStatistics statistics) {

            if (statistics.Min < Min) {
                Min = statistics.Min;
            }

            if (statistics.Max > Max) {
                Max = statistics.Max;
            }

            double oldMean = Mean;

            Mean = (Counter * oldMean + statistics.Counter * statistics.Mean)
                / (Counter + statistics.Counter);

            Variance = ((Counter - 1) * Variance 
                    + (statistics.Counter - 1) * statistics.Variance 
                    + Counter * (oldMean - Mean).Squared()
                    + statistics.Counter * (statistics.Mean - Mean).Squared())
                / (Counter + statistics.Counter - 1);

            Counter += statistics.Counter;
        }
    }
}