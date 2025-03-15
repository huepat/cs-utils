using System;
using System.Collections.Generic;
using System.IO;

namespace HuePat.Util.Math.Statistics {
    public class StatisticsHeatmap {
        private (double, double) origin;
        private (double, double) binSize;
        private Dictionary<(int, int), DoubleStatistics> bins;

        public StatisticsHeatmap(
                (double, double) binSize) :
                    this(
                        binSize,
                        (0.0, 0.0)) {
        }

        public StatisticsHeatmap(
                (double, double) binSize,
                (double, double) origin) {

            this.binSize = binSize;
            this.origin = origin;

            bins = new Dictionary<(int, int), DoubleStatistics>();
        }

        public StatisticsHeatmap(
                (double, double) binSize,
                (double, double) origin,
                string counterCSVFile,
                string meanCSVFile,
                string standardDeviationCSVFile) :
                    this(
                        binSize,
                        origin) {

            using (StreamReader
                    counterReader = new StreamReader(counterCSVFile),
                    meanReader = new StreamReader(meanCSVFile),
                    standardVariationReader = new StreamReader(standardDeviationCSVFile)) {

                counterReader.ReadLine();
                meanReader.ReadLine();
                standardVariationReader.ReadLine();

                while (counterReader.Peek() >= 0) {

                    string[] values = counterReader
                        .ReadLine()
                        .Split("; ");

                    (int, int) bin = (
                        (int)((double.Parse(values[0]) - origin.Item1) / binSize.Item1),
                        (int)((double.Parse(values[1]) - origin.Item2) / binSize.Item2));

                    bins[bin] = new DoubleStatistics(
                        (int)double.Parse(values[2]),
                        double
                            .Parse(
                                meanReader
                                    .ReadLine()
                                    .Split("; ")[2]),
                        double
                            .Parse(
                                standardVariationReader
                                    .ReadLine()
                                    .Split("; ")[2])
                            .Squared());
                }
            }
        }

        public void Add(
                (double, double) value) {

            Add(value, 1.0);
        }

        public void Add(
                (double, double) value,
                double weight) {

            (int, int) bin = (
                (int)((value.Item1 - origin.Item1) / binSize.Item1),
                (int)((value.Item2 - origin.Item2) / binSize.Item2)
            );

            if (!bins.ContainsKey(bin)) {

                bins.Add(
                    bin, 
                    new DoubleStatistics());
            }

            bins[bin].Update(weight);
        }

        public void Add(
                StatisticsHeatmap heatmap) {

            if (!heatmap.binSize.Item1.ApproximateEquals(binSize.Item1)
                    || !heatmap.binSize.Item2.ApproximateEquals(binSize.Item2)
                    || !heatmap.origin.Item1.ApproximateEquals(origin.Item1)
                    || !heatmap.origin.Item2.ApproximateEquals(origin.Item2)) {

                throw new ApplicationException(
                    "Only Heatmaps of same origin and binsize can be added.");
            }

            bins.BucketAdd(heatmap.bins);
        }

        public Heatmap GetCountHeatmap() {

            return GetHeatmap(
                bin => bin.Counter);
        }

        public Heatmap GetMeanHeatmap() {

            return GetHeatmap(
                bin => bin.Mean);
        }

        public Heatmap GetStandardDeviationHeatmap() {

            return GetHeatmap(
                bin => bin.StandardDeviation);
        }

        private Heatmap GetHeatmap(
                Func<DoubleStatistics, double> valueExtractionCallback) {

            Heatmap heatmap = new Heatmap(
                binSize,
                origin);

            foreach ((int, int) bin in bins.Keys) {

                heatmap.Add(
                    (
                        (bin.Item1 * binSize.Item1) + origin.Item1,
                        (bin.Item2 * binSize.Item2) + origin.Item2
                    ),
                    valueExtractionCallback(bins[bin]));
            }

            return heatmap;
        }
    }
}
