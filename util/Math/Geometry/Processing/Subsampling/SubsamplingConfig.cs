using HuePat.Util.Math.Geometry.SpatialIndices;
using HuePat.Util.Math.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.Math.Geometry.Processing.Subsampling {
    public class SubsamplingConfig {
        public bool OutputVoxelPointCount { get; set; }
        public bool UseParallel { get; set; }
        public int MinPointCount { get; set; }
        public double Resolution { get; private set; }
        public ISpatialIndex<Point> ResultSpatialIndex { get; set; }
        public Func<List<Point>, Point> GeometryAggregationCallback { get; set; }
        public Action<Point, List<Point>> PropertyAggregationCallback { get; set; }

        public SubsamplingConfig(
                double resolution) {

            Resolution = resolution;
            MinPointCount = 0;
            ResultSpatialIndex = new BruteForceIndex<Point>();
            PropertyAggregationCallback = (newPoint, oldPoints) => { };
            GeometryAggregationCallback = oldPoints => new Point(
                oldPoints
                    .Select(point => point.Position)
                    .Mean());
        }
    }
}