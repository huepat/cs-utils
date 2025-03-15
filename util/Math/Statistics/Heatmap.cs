using HuePat.Util.Image;
using HuePat.Util.Visualization.Diagrams;
using OpenCvSharp;
using Plotly.NET;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuePat.Util.Math.Statistics {
    public class Heatmap {
        private const double LATERAL_HISTOGRAMM_HEIGHT_RATIO = 0.3;

        private (double, double) origin;
        private (double, double) binSize;
        private Dictionary<(int, int), double> bins;

        public Heatmap(
                (double, double) binSize) :
                    this(
                        binSize,
                        (0.0, 0.0)) {
        }

        public Heatmap(
                (double, double) binSize,
                (double, double) origin) {

            this.binSize = binSize;
            this.origin = origin;

            bins = new Dictionary<(int, int), double>();
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
                bins.Add(bin, 0.0);
            }

            bins[bin] += weight;
        }

        public void Add(
                Heatmap heatmap) {

            if (!heatmap.binSize.Item1.ApproximateEquals(binSize.Item1)
                    || !heatmap.binSize.Item2.ApproximateEquals(binSize.Item2)
                    || !heatmap.origin.Item1.ApproximateEquals(origin.Item1)
                    || !heatmap.origin.Item2.ApproximateEquals(origin.Item2)) {

                throw new ApplicationException(
                    "Only Heatmaps of same origin and binsize can be added.");
            }

            bins.BucketAdd(heatmap.bins);
        }

        public void GetData(
                out double[][] data,
                out double[] xValues,
                out double[] yValues) {

            ((int, int), (int, int)) bounds = bins.Keys.MinMax();

            data = Enumerable
                .Range(bounds.Item1.Item1, bounds.Item2.Item1 - bounds.Item1.Item1)
                .Select(xBin => Enumerable
                    .Range(bounds.Item1.Item2, bounds.Item2.Item2 - bounds.Item1.Item2)
                    .Select(yBin => (xBin, yBin))
                    .Select(bin => bins.ContainsKey(bin) ?
                        bins[bin] :
                        0.0)
                    .ToArray())
                .ToArray();

            xValues = Enumerable
                .Range(bounds.Item1.Item1, bounds.Item2.Item1 - bounds.Item1.Item1)
                .Select(bin => (bin * binSize.Item1) + origin.Item1)
                .ToArray();

            yValues = Enumerable
                .Range(bounds.Item1.Item2, bounds.Item2.Item2 - bounds.Item1.Item2)
                .Select(bin => (bin * binSize.Item2) + origin.Item2)
                .ToArray();
        }

        public void Export(
                string file,
                int numberOfDigits,
                string header = "x; y; z") {

            Export(
                file,
                header,
                numberOfDigits,
                numberOfDigits,
                numberOfDigits);
        }

        public void Export(
                string file,
                string header = "x; y; z",
                int? xNumberOfDigits = null,
                int? yNumberOfDigits = null,
                int? zNumberOfDigits = null) {

            using (StreamWriter writer = new StreamWriter(file)) {

                writer.WriteLine(header);

                foreach ((int, int) bin in bins.Keys) {

                    double x = (bin.Item1 * binSize.Item1) + origin.Item1;
                    double y = (bin.Item2 * binSize.Item2) + origin.Item2;

                    writer.WriteLine(
                        $"{x.Format(xNumberOfDigits)}; " +
                        $"{y.Format(yNumberOfDigits)}; " +
                        $"{bins[bin].Format(zNumberOfDigits)}");
                }
            }
        }

        public Mat CreateImage(
                ColormapTypes colormap,
                Colors.Color backgroundColor,
                bool invertColormap = false) {

            double minValue = bins.Values.Min();
            ((int, int), (int, int)) bounds = bins.Keys.MinMax();

            using (Mat image = new Mat(
                    bounds.Item2.Item2 - bounds.Item1.Item2,
                    bounds.Item2.Item1 - bounds.Item1.Item1,
                    MatType.CV_64FC1,
                    new Scalar(minValue - 1.0))) {

                using (Mat<double> _image = new Mat<double>(image)) {

                    MatIndexer<double> imageData = _image.GetIndexer();

                    foreach ((int, int) bin in bins.Keys) {

                        imageData[
                            bin.Item2 - bounds.Item1.Item2,
                            bin.Item1 - bounds.Item1.Item1] = bins[bin];
                    }
                }

                return image.Colorize(
                    min : minValue,
                    max : null,
                    backGroundColor : backgroundColor,
                    doInvert : invertColormap,
                    colormap : colormap);
            }
        }

        public GenericChart.GenericChart CreateScatterChart() {

            List<double> xValues = new List<double>();
            List<double> yValues = new List<double>();
            List<double> colorScaleValues = new List<double>();

            foreach ((int, int) bin in bins.Keys) {

                xValues.Add((bin.Item1 * binSize.Item1) + origin.Item1);
                yValues.Add((bin.Item2 * binSize.Item2) + origin.Item2);

                colorScaleValues.Add(bins[bin]);
            }

            return PlotlyUtils.CreateScatterChart(
                xValues,
                yValues,
                colorScaleValues);
        }

        public GenericChart.GenericChart CreateHeatmapChart(
                string xAxisTitle,
                string yAxisTitle,
                string colorScaleTitle,
                StyleParam.Colorscale colorScale,
                int? height = null,
                int? width = null) {

            CreateCharts(
                false,
                xAxisTitle,
                yAxisTitle,
                colorScaleTitle,
                colorScale,
                out GenericChart.GenericChart chart,
                out _,
                out _,
                height,
                width);

            return chart;
        }

        public void CreateCharts(
                string xAxisTitle,
                string yAxisTitle,
                string colorScaleTitle,
                StyleParam.Colorscale colorScale,
                out GenericChart.GenericChart heatmapChart,
                out GenericChart.GenericChart xHistogramChart,
                out GenericChart.GenericChart yHistogramChart,
                int? height = null,
                int? width = null) {

            CreateCharts(
                true,
                xAxisTitle,
                yAxisTitle,
                colorScaleTitle,
                colorScale,
                out heatmapChart,
                out xHistogramChart,
                out yHistogramChart,
                height,
                width);
        }

        public void CreateCharts(
                bool withLateralHistograms,
                string xAxisTitle,
                string yAxisTitle,
                string colorScaleTitle,
                StyleParam.Colorscale colorScale,
                out GenericChart.GenericChart heatmapChart,
                out GenericChart.GenericChart xHistogramChart,
                out GenericChart.GenericChart yHistogramChart,
                int? height,
                int? width) {

            GetData(
                out double[][] data,
                out double[] xValues,
                out double[] yValues);

            heatmapChart = PlotlyUtils.CreateHeatmap(
                data,
                xValues,
                yValues,
                xAxisTitle,
                yAxisTitle,
                colorScaleTitle,
                height,
                width,
                colorScale : colorScale,
                colorScaleY : withLateralHistograms ?
                    -0.35 :
                    null,
                colorScaleOrientation : withLateralHistograms ?
                    StyleParam.Orientation.Horizontal :
                    null,
                colorScaleTitleSide : withLateralHistograms ?
                    StyleParam.Side.Bottom :
                    null);

            if (!withLateralHistograms) {

                xHistogramChart = null;
                yHistogramChart = null;

                return;
            }

            Dictionary<string, object> layoutProperties = new Dictionary<string, object>(
                    GenericChart
                        .getLayout(heatmapChart)
                        .GetProperties(true));

            int _width = (int)layoutProperties["width"];
            int _height = (int)layoutProperties["height"];

            double[] _xValues = new double[xValues.Length];
            double[] xHistogramValues = new double[xValues.Length];
            double[] yHistogramValues = new double[yValues.Length];

            for (int x = 0; x < xValues.Length; x++) {

                _xValues[x] = xValues[xValues.Length - x - 1];

                for (int y = 0; y < yValues.Length; y++) {

                    xHistogramValues[x] += data[x][y];
                    yHistogramValues[y] += data[x][y];
                }
            }

            xHistogramChart = PlotlyUtils.CreateBarChart(
                _xValues,
                xHistogramValues,
                invisibleXAxis: true,
                yAxisTitle : colorScaleTitle,
                height : null,
                width : _width);

            yHistogramChart = PlotlyUtils.CreateBarChart(
                yValues,
                yHistogramValues,
                invisibleXAxis: true,
                yAxisTitle : colorScaleTitle,
                height : null,
                width : _height);
        }
    }
}
