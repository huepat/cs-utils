using HuePat.Util.Visualization.Diagrams;
using Plotly.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuePat.Util.Math.Statistics {
    public class Histogram {

        private double origin;
        private double binSize;
        private Dictionary<int, double> bins;

        public Histogram(
                double binSize,
                double origin = 0.0) { 

            this.binSize = binSize;
            this.origin = origin;

            bins = new Dictionary<int, double>();
        }

        public void Add(
                double value) {

            Add(value, 1.0);
        }

        public void Add(
                double value,
                double weight) {

            int bin = (int)((value - origin) / binSize);

            if (!bins.ContainsKey(bin)) {
                bins.Add(bin, 0.0);
            }

            bins[bin] += weight;
        }

        public void Add(
                Histogram histogram) {

            if (!histogram.binSize.ApproximateEquals(binSize)
                    || !histogram.origin.ApproximateEquals(origin)) {

                throw new ApplicationException(
                    "Only Histograms of same origin and binsize can be added.");
            }

            bins.BucketAdd(histogram.bins);
        }

        public void GetData(
                out double[] xValues,
                out double[] yValues) {

            (int, int) bounds = bins.Keys.MinMax();

            xValues = Enumerable
                .Range(bounds.Item1, bounds.Item2 - bounds.Item1)
                .Select(BinToValue)
                .ToArray();

            yValues = Enumerable
                .Range(bounds.Item1, bounds.Item2 - bounds.Item1)
                .Select(bin => bins.ContainsKey(bin) ?
                    bins[bin] :
                    0.0)
                .ToArray();
        }

        public void Export(
                string file,
                string header = "x; y",
                int? xNumberOfDigits = null,
                int? yNumberOfDigits = null) {

            using (StreamWriter writer = new StreamWriter(file)) {

                writer.WriteLine(header);

                foreach (int bin in bins.Keys) {

                    writer.WriteLine(
                        $"{BinToValue(bin).Format(xNumberOfDigits)}; " +
                            $"{bins[bin].Format(yNumberOfDigits)}");
                }
            }
        }

        public GenericChart.GenericChart CreateChart(
                string xAxisTitle,
                int? height = null,
                int? width = null) {

            GetData(
                out double[] xValues,
                out double[] yValues);

            return PlotlyUtils.CreateBarChart(
                xValues,
                yValues,
                xAxisTitle : xAxisTitle,
                height : height,
                width : width);
        }

        private double BinToValue(
                int bin) { 

            return (bin * binSize) + origin; 
        }
    }
}
