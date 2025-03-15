using System;
using System.Collections.Generic;

namespace HuePat.Util.Math.Statistics {
    public class StatisticsHistogram {

        private double origin;
        private double binSize;
        private Dictionary<int, DoubleStatistics> bins;

        public StatisticsHistogram(
                double binSize,
                double origin = 0.0) { 

            this.binSize = binSize;
            this.origin = origin;

            bins = new Dictionary<int, DoubleStatistics>();
        }

        public void Add(
                double value,
                double weight) {

            int bin = (int)((value - origin) / binSize);

            if (!bins.ContainsKey(bin)) {

                bins.Add(
                    bin, 
                    new DoubleStatistics());
            }

            bins[bin].Update(weight);
        }

        public Histogram GetMeanHistogram() {

            return GetHistogram(
                bin => bin.Mean);
        }

        public Histogram GetStandardDeviationHistogram() {

            return GetHistogram(
                bin => bin.StandardDeviation);
        }

        private Histogram GetHistogram(
                Func<DoubleStatistics, double> valueExtractionCallback) {

            Histogram histogram = new Histogram(
                binSize,
                origin);

            foreach (int bin in bins.Keys) {

                histogram.Add(
                    (bin * binSize) + origin,
                    valueExtractionCallback(bins[bin]));
            }

            return histogram;
        }
    }
}
