using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuePat.Util.IO.TXT {
    public static class TXTReader {

        public static double[,] ReadMatrix2D(
                string filePath,
                string delimiter = " ") {

            double[][] values;
            double[,] matrix;

            values = File
                .ReadAllLines(filePath)
                .Select(line => line
                    .Split(delimiter)
                    .Where(value => value.Length > 0)
                    .Select(value => double.Parse(value))
                    .ToArray())
                .ToArray();

            if (values
                    .Select(lineValues => lineValues.Length)
                    .Distinct()
                    .Count() > 1) {

                throw new ArgumentException("Data is not a matrix.");
            }

            matrix = new double[
                values.Length,
                values[0].Length];

            for (int r = 0; r < values.Length; r++) {
                for (int c = 0; c < values.Length; c++) {

                    matrix[r, c] = values[r][c];
                }
            }

            return matrix;
        }
    }
}
