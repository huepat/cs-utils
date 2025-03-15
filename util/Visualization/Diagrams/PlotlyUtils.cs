using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using System.Collections.Generic;

namespace HuePat.Util.Visualization.Diagrams {
    public static class PlotlyUtils {

        public static GenericChart.GenericChart CreateScatterChart(
                List<double> xValues,
                List<double> yValues,
                List<double> colorScaleValues) {

            return Chart2D.Chart.Scatter<double, double, string>(
                X: xValues,
                Y: yValues,
                Mode: StyleParam.Mode.Markers,
                Marker: Marker.init(
                    ShowScale: true),
                ShowLegend: true,
                MarkerColorScale: StyleParam.Colorscale.Jet,
                MarkerColor: Color.fromColorScaleValues(colorScaleValues)
            );
        }

        public static GenericChart.GenericChart CreateBarChart(
                double[] xValues,
                double[] yValues,
                bool invisibleXAxis = false,
                bool invisibleYAxis = false,
                string xAxisTitle = "",
                string yAxisTitle = "",
                StyleParam.Side xAxisSide = null,
                StyleParam.Side yAxisSide = null,
                int? height = null,
                int? width = null) {

            Trace trace = new Trace("bar");

            trace.SetValue("x", xValues);
            trace.SetValue("y", yValues);

            trace.SetValue(
                "marker",
                Marker.init(
                    Color: Color.fromString("gray"),
                    Outline: Line.init(
                        Color: Color.fromString("gray"))));

            Layout layout = CreateLayout(height, width);

            layout.SetValue("bargap", 0.0);

            layout.SetValue(
                "xaxis",
                CreateAxis(
                    xAxisTitle,
                    xAxisSide));

            layout.SetValue(
                "yaxis",
                CreateAxis(
                    yAxisTitle,
                    yAxisSide));

            GenericChart.Figure figure = GenericChart.Figure.create(
                ListModule.OfSeq(new[] { trace }),
                layout);

            GenericChart.GenericChart chart = GenericChart.fromFigure(figure);

            if (invisibleXAxis) {

                chart.SetInvisibleAxis(true);
            }

            if (invisibleYAxis) {

                chart.SetInvisibleAxis(false);
            }

            return chart;
        }

        public static GenericChart.GenericChart CreateHeatmap(
                IEnumerable<double[]> data,
                IEnumerable<double> xValues,
                IEnumerable<double> yValues,
                string xAxisTitle = "",
                string yAxisTitle = "",
                string colorScaleTitle = "",
                int? height = null,
                int? width = null,
                double? colorScaleY = null, 
                double? colorScaleX = null,
                StyleParam.Colorscale colorScale = null,
                StyleParam.Orientation colorScaleOrientation = null,
                StyleParam.Side colorScaleTitleSide = null) {

            //Title title = Title.init(colorScaleTitle);

            //if (colorScaleTitleSide != null) {

            //    title.SetValue("side", colorScaleTitleSide);
            //}

            //ColorBar colorBar = ColorBar.init<double, double>(
            //    Title: title
            //);

            //if (colorScaleOrientation != null) {

            //    colorBar.SetValue("orientation", colorScaleOrientation);
            //}

            //if (colorScaleX.HasValue) {

            //    colorBar.SetValue("x", colorScaleX);
            //}

            //if (colorScaleY.HasValue) {

            //    colorBar.SetValue("y", colorScaleY);
            //}

            //Layout layout = CreateLayout(height, width);

            GenericChart.GenericChart chart = Chart2D.Chart.Heatmap
                        <double[], double, double, double, double>(
                    data,
                    X: new FSharpOption<IEnumerable<double>>(xValues),
                    Y: new FSharpOption<IEnumerable<double>>(yValues),
                    ColorScale: colorScale//,
                    //ColorBar : colorBar
                    )
                //.WithLayout(layout)
                ;

            //chart.WithXAxisStyle(
            //    Title.init(xAxisTitle));

            //chart.WithYAxisStyle(
            //    Title.init(yAxisTitle));

            return chart;
        }

        private static LinearAxis CreateAxis(
                string title,
                StyleParam.Side side) {

            LinearAxis axis = new LinearAxis();

            axis.SetValue("title", title);

            if (side != null) {

                axis.SetValue("side", side);
            }

            return axis;
        }

        private static void SetInvisibleAxis(
                this GenericChart.GenericChart chart,
                bool isX) {

            LinearAxis invisibleAxis = new LinearAxis();

            invisibleAxis.SetValue("visible", false);

            if (isX) {
                chart.WithXAxis(invisibleAxis);
            }
            else {
                chart.WithYAxis(invisibleAxis);
            }
        }

        private static Layout CreateLayout(
                int? height,
                int? width) {

            Layout layout = new Layout();

            if (height.HasValue) {
                layout.SetValue("height", height.Value);
            }

            if (width.HasValue) {
                layout.SetValue("width", width.Value);
            }

            return layout;
        }
    }
}
