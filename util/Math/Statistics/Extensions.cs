using HuePat.Util.Math.Geometry;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.Math.Statistics {
    public static class Extensions {
        public static void BucketAdd<T>(
                this Dictionary<T, DoubleStatistics> dictionary,
                Dictionary<T, DoubleStatistics> dictionary2) {

            foreach (T key in dictionary2.Keys) {

                if (!dictionary.ContainsKey(key)) {

                    dictionary.Add(
                        key,
                        dictionary2[key]);
                }
                else {

                    dictionary[key].Update(
                        dictionary2[key]);
                }
            }
        }

        public static double AddNoise(
                this double value, 
                double standardDeviation) {

            return value + Random.GetSign() * standardDeviation *
                (-2.0 * (1.0 - Random.GetDouble()).Log()).Sqrt() *
                (2.0 * System.Math.PI * 1.0 - Random.GetDouble()).Sin();
        }

        public static double Mean(
                this Vector3d vector) {

            return new double[] { 
                vector.X, 
                vector.Y, 
                vector.Z 
            }.Mean();
        }

        public static Vector3d Mean(
                this IEnumerable<Vector3d> vectors) {

            int count = 0;
            Vector3d sum = new Vector3d();

            foreach (Vector3d vector in vectors) {
                count++;
                sum += vector;
            }

            return sum / count;
        }

        public static Vector3d WeightedMean(
                this IEnumerable<(Vector3d, double)> valueWeightPairs) {

            double totalWeigth = 0.0;
            Vector3d sum = new Vector3d();

            foreach ((Vector3d, double) valueWeightPair in valueWeightPairs) {

                sum += valueWeightPair.Item1 * valueWeightPair.Item2;
                totalWeigth += valueWeightPair.Item2;
            }

            return sum / totalWeigth;
        }

        public static double Mean(
                this IEnumerable<double> values) {

            return values.Average();
        }

        public static double WeightedMean(
                this IEnumerable<(double, double)> valueWeightPairs) {

            double sum = 0.0;
            double totalWeigth = 0.0;

            foreach ((double, double) valueWeightPair in valueWeightPairs) {

                sum += valueWeightPair.Item1 * valueWeightPair.Item2;
                totalWeigth += valueWeightPair.Item2;
            }

            return sum / totalWeigth;
        }

        public static Vector3d Median(
                this IEnumerable<Vector3d> vectors) {

            return new Vector3d(
                vectors.Select(vector => vector.X).Median(),
                vectors.Select(vector => vector.Y).Median(),
                vectors.Select(vector => vector.Z).Median());
        }

        public static Vector3d WeightedMedian(
                this IEnumerable<(Vector3d, double)> valueWeightPairs) {

            return new Vector3d(
                valueWeightPairs
                    .Select(valueWeightPair => (
                        valueWeightPair.Item1.X,
                        valueWeightPair.Item2
                    ))
                    .WeightedMedian(),
                valueWeightPairs
                    .Select(valueWeightPair => (
                        valueWeightPair.Item1.Y,
                        valueWeightPair.Item2
                    ))
                    .WeightedMedian(),
                valueWeightPairs
                    .Select(valueWeightPair => (
                        valueWeightPair.Item1.Z,
                        valueWeightPair.Item2
                    ))
                    .WeightedMedian());
        }

        public static int Median(
                this IEnumerable<int> values) {

            return (int)values
                .Select(value => (double)value)
                .Median()
                .Round();
        }

        public static double Median(
                this IEnumerable<double> values) {

            List<double> sorted = values
                .OrderBy(value => value)
                .ToList();

            if (sorted.Count % 2 == 0) {

                return (sorted[sorted.Count / 2] 
                        + sorted[sorted.Count / 2 - 1]
                    ) / 2.0;
            }

            return sorted[sorted.Count / 2];
        }

        public static double WeightedMedian(
                this IEnumerable<(double, double)> valueWeightPairs) {

            double weightSum = 0.0;
            double totalweightSum;

            List<(double, double)> sorted = valueWeightPairs
                .OrderBy(valueWeightPair => valueWeightPair.Item1)
                .ToList();

            totalweightSum = sorted.Sum(valueWeightPair => valueWeightPair.Item2);

            foreach ((double, double) valueWeightPair in sorted) {

                weightSum += valueWeightPair.Item2;

                if (weightSum / totalweightSum >= 0.5) {
                    return valueWeightPair.Item1;
                }
            }

            throw new ApplicationException();
        }

        public static double RMSE(
                this IEnumerable<double> errorValues) {

            return (
                errorValues
                    .Select(errorValue => errorValue.Squared())
                    .Sum() 
                / errorValues.Count()
            ).Sqrt();
        }

        public static double StandardDeviation(
                this IEnumerable<double> values) {

            double mean = values.Average();

            return (
                values
                    .Select(value => (value - mean).Squared())
                    .Sum()
                / (values.Count() - 1)
            ).Sqrt();
        }

        public static DoubleStatistics GetPropertyStatistics(
                this PointCloud pointCloud,
                string propertyIdentifier) {

            return pointCloud.GetPropertyStatistics(
                new string[] { propertyIdentifier })[0];
        }

        public static DoubleStatistics[] GetPropertyStatistics(
                this PointCloud pointCloud,
                IList<string> propertyIdentifiers) {

            DoubleStatistics[] statistics = propertyIdentifiers
                .Select(propertyIdentifier => new DoubleStatistics())
                .ToArray();

            foreach (Point point in pointCloud) {

                for (int i = 0; i < propertyIdentifiers.Count; i++) {

                    statistics[i].Update(
                        (double)point.GetFloatProperty(propertyIdentifiers[i]));
                }
            }

            return statistics;
        }
    }
}