using HuePat.Util.Colors;
using HuePat.Util.Math.Geometry;
using HuePat.Util.Math.Geometry.SpatialIndices;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HuePat.Util.IO.CSV.Reading {
    public class CsvReader {
        private static readonly object LOCK = new object();

        private char delimiter;
        public ISpatialIndex<Point> SpatialIndex { private get; set; }
        public PointFormatIndexMapping PointFormatIndexMapping { private get; set; }

        public CsvReader(char delimiter) {
            this.delimiter = delimiter;
            SpatialIndex = new BruteForceIndex<Point>();
            PointFormatIndexMapping = new PointFormatIndexMapping();
        }

        public PointCloud ReadPointCloud(
                string file,
                bool useParallel = false) {
            PointFormatIndexMapping.Check();
            return new PointCloud(
                useParallel ? 
                    ReadPointsParallel(file) :
                    ReadPoints(file),
                SpatialIndex);
        }

        private List<Point> ReadPoints(string file) {
            List<Point> points = new List<Point>();
            foreach (string line in File.ReadAllLines(file)) {
                AddPoint(line, points);
            }
            return points;
        }

        private List<Point> ReadPointsParallel(string file) {
            string[] lines = File.ReadAllLines(file);
            List<Point> points = new List<Point>();
            Parallel.ForEach(
                Partitioner.Create(0, lines.Length),
                () => new List<Point>(),
                (partition, loopState, localPoints) => {
                    for (int i = partition.Item1; i < partition.Item2; i++) {
                        AddPoint(
                            lines[i],
                            localPoints);
                    }
                    return localPoints;
                },
                localPoints => {
                    lock (LOCK) {
                        points.AddRange(localPoints);
                    }
                });
            return points;
        }

        private void AddPoint(
                string line,
                List<Point> points) {
            try {
                string[] values = line.Split(delimiter).Where(value => value != "").ToArray();
                Point point = new Point(
                    double.Parse(values[PointFormatIndexMapping.CoordinateIndices.Item1]),
                    double.Parse(values[PointFormatIndexMapping.CoordinateIndices.Item2]),
                    double.Parse(values[PointFormatIndexMapping.CoordinateIndices.Item3]));
                foreach (int index in PointFormatIndexMapping.DoublePropertyIndices.Keys) {
                    point.SetDoubleProperty(
                        PointFormatIndexMapping.DoublePropertyIndices[index],
                        double.Parse(values[index]));
                }
                foreach ((int, int, int) indices in PointFormatIndexMapping.Vector3PropertyIndices.Keys) {
                    point.SetVector3Property(
                        PointFormatIndexMapping.Vector3PropertyIndices[indices],
                        new Vector3d(
                            double.Parse(values[indices.Item1]),
                            double.Parse(values[indices.Item2]),
                            double.Parse(values[indices.Item3])));
                }
                foreach ((int, int, int) indices in PointFormatIndexMapping.Color3PropertyIndices.Keys) {
                    point.SetColorProperty(
                        PointFormatIndexMapping.Color3PropertyIndices[indices],
                        new Color(
                            byte.Parse(values[indices.Item1]),
                            byte.Parse(values[indices.Item2]),
                            byte.Parse(values[indices.Item3])));
                }
                foreach ((int, int, int, int) indices in PointFormatIndexMapping.Color4PropertyIndices.Keys) {
                    point.SetColorProperty(
                        PointFormatIndexMapping.Color4PropertyIndices[indices],
                        new Color(
                            byte.Parse(values[indices.Item1]),
                            byte.Parse(values[indices.Item2]),
                            byte.Parse(values[indices.Item3]),
                            byte.Parse(values[indices.Item4])));
                }
                points.Add(point);
            }
            catch (System.Exception) {
                return;
            }
        }
    }
}