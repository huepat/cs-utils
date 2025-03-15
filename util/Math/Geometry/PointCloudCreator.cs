using HuePat.Util.Math.Geometry.SpatialIndices;
using System.Collections.Generic;

namespace HuePat.Util.Math.Geometry {
    public class PointCloudCreator {
        private readonly ISpatialIndex<Point> spatialIndex;
        private readonly List<Point> points = new List<Point>();

        public PointCloudCreator(): 
                this(new BruteForceIndex<Point>()) {
        }

        public PointCloudCreator(
                ISpatialIndex<Point> spatialIndex) {

            this.spatialIndex = spatialIndex;
        }

        public void Add(
                Point point) {

            points.Add(point);
        }

        public void Add(
                IEnumerable<Point> points) {

            this.points.AddRange(points);
        }

        public PointCloud Create() {

            return new PointCloud(
                points, 
                spatialIndex);
        }
    }
}