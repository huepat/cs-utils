using HuePat.Util.Colors;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util {
    public static class Random {
        private static readonly int MAX_DISTINCT_COLOR_COUNT = 1000;
        private static readonly object LOCK = new object();

        [ThreadStatic]
        private static System.Random generator;
        private static System.Random globalGenerator = new System.Random(DateTime.Now.Millisecond);

        public static bool GetBool() {

            return GetGenerator()
                .Next(0, 2) == 0;
        }

        public static bool GetBool(
            double successRate) {

            return GetGenerator()
                .NextDouble() <= successRate;
        }

        public static int GetSign() {

            return GetBool() ? -1 : 1;
        }

        public static byte GetByte() {

            byte[] bytes = new byte[1];

            GetGenerator()
                .NextBytes(bytes);

            return bytes[0];
        }

        public static int GetInteger(
                int min, 
                int max) {

            return GetGenerator()
                .Next(
                    min, 
                    max + 1);
        }

        public static double GetDouble() {

            return GetGenerator()
                .NextDouble();
        }

        public static double GetDouble(
                double min, 
                double max) {

            return min + GetGenerator().NextDouble() * (max - min);
        }

        public static Vector3i GetVector3i(
                Vector3i min,
                Vector3i max) {

            return new Vector3i(
                GetInteger(min.X, max.X),
                GetInteger(min.Y, max.Y),
                GetInteger(min.Z, max.Z));
        }

        public static Vector3d GetVector3d() {

            return new Vector3d(
                GetDouble(),
                GetDouble(),
                GetDouble());
        }

        public static Vector3d GetVector3d(
                Vector3d min, 
                Vector3d max) {

            return new Vector3d(
                GetDouble(min.X, max.X),
                GetDouble(min.Y, max.Y),
                GetDouble(min.Z, max.Z));
        }

        public static Color GetColor(
                bool withAlpha = false) {

            if (withAlpha) {

                byte[] bytes = new byte[3];
                GetGenerator().NextBytes(bytes);

                return new Color(
                    bytes[0], 
                    bytes[1], 
                    bytes[2]);
            }
            else {

                byte[] bytes = new byte[4];
                GetGenerator().NextBytes(bytes);

                return new Color(
                    bytes[0], 
                    bytes[1], 
                    bytes[2], 
                    bytes[3]);
            }
        }

        public static Dictionary<T, Color> GetColors<T>(
                IList<T> labels) {

            return GetColors(
                labels, 
                new Color[0]);
        }

        public static Dictionary<T, Color> GetColors<T>(
                IList<T> labels,
                IEnumerable<Color> colorsNotToUse) {

            List<Color> colors;
            Dictionary<T, Color> result = new Dictionary<T, Color>();

            colors = GetColors(
                labels.Count,
                colorsNotToUse);

            for (int i = 0; i < labels.Count; i++) {
                result.Add(
                    labels[i], 
                    colors[i]);
            }

            return result;
        }

        public static List<Color> GetColors(
                int colorCount, 
                bool withAlpha = false) {

            return GetColors(
                colorCount, 
                new Color[0], 
                withAlpha);
        }

        public static List<Color> GetColors(
                int colorCount,
                IEnumerable<Color> colorsNotToUse,
                bool withAlpha = false) {

            List<Color> result = new List<Color>();

            for (int i = 0; i < colorCount; i++) {

                Color color;

                do {
                    color = GetColor(withAlpha);
                } while (
                    colorsNotToUse.Any(colorNotToUse => 
                        Color.ApproximateEquals(
                                color, 
                                colorNotToUse)) 
                            || (colorCount <= MAX_DISTINCT_COLOR_COUNT 
                                && result.Any(randomColor => 
                                    Color.ApproximateEquals(
                                        color, 
                                        randomColor))));

                result.Add(color);
            }

            return result;
        }

        private static System.Random GetGenerator() {

            if (generator == null) {
                lock (LOCK) {
                    generator = new System.Random(
                        globalGenerator.Next());
                }
            }

            return generator;
        }
    }
}