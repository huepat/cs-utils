using HuePat.Util.Math.Geometry.Processing.Voxelization;
using System.Collections.Generic;
using System.Diagnostics;

namespace HuePat.Util.Math.Geometry.Processing.Subsampling {
    public static class Extensions {
        public static PointCloud Subsample(
                this PointCloud pointCloud,
                SubsamplingConfig config) {

            List<Point> subsampledPoints = new List<Point>();

            pointCloud.Voxelize(
                config.Resolution,
                null,
                null,
                gridSize => { },
                (voxel, points) => {

                    if (config.OutputVoxelPointCount) {
                        Trace.WriteLine(points.Count);
                    }

                    if (points.Count >= config.MinPointCount) {

                        Point point = config.GeometryAggregationCallback(points);
                        config.PropertyAggregationCallback(point, points);
                        subsampledPoints.Add(point);
                    }
                },
                config.UseParallel,
                false);

            return new PointCloud(
                subsampledPoints,
                config.ResultSpatialIndex);
        }
    }
}