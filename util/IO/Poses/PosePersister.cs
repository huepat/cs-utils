using HuePat.Util.IO.Poses.Format;
using HuePat.Util.Math;
using HuePat.Util.Math.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuePat.Util.IO.Poses {
    public class PosePersister: IDisposable
    {
        private const char DEFAULT_DELIMITER = ' ';

        private StreamWriter writer;
        private readonly IPoseFormatter formatter;
        private readonly char delimiter;

        public PosePersister(char delimiter = DEFAULT_DELIMITER) : 
            this(new DefaultFormatter(), delimiter)
        {
        }

        public PosePersister(
            IPoseFormatter formatter,
            char delimiter = DEFAULT_DELIMITER)
        {
            this.formatter = formatter;
            this.delimiter = delimiter;
        }

        public void Dispose()
        {
            writer?.Dispose();
        }

        public SortedDictionary<long, Pose> Read(string file)
        {
            CheckFile(file);
            string[] values;
            SortedDictionary<long, Pose> poses = new SortedDictionary<long, Pose>();
            formatter.DeformatNewFile();
            foreach (string line in File.ReadLines(file))
            {
                values = line.Split(delimiter);
                if (!formatter.AllowedValueCounts.Contains(values.Length))
                {
                    continue;
                }
                poses.Add(
                    formatter.DeformatTimestamp(values), 
                    formatter.DeformatPose(values));
                formatter.DeformatNewLine();
            }
            return poses;
        }

        public Pose ReadAsMatrix(string file)
        {
            CheckFile(file);
            List<float> values = ReadMatrixValues(file);
            return new Pose(
                values[0], values[1], values[2], values[3],
                values[4], values[5], values[6], values[7],
                values[8], values[9], values[10], values[11]);
        }

        public void OpenFileConnection(string file, bool append=false)
        {
            writer = new StreamWriter(file, append);
            writer.WriteLine(formatter.FormatFileStart());
        }
        
        public void Write(long timestamp, Pose pose)
        {
            writer.WriteLine(formatter.Format(timestamp, pose));
            writer.Flush();
        }

        public void WriteAsMatrix(string file, Pose pose)
        {
            File.WriteAllText(file, 
                $"{pose.Rxx} {pose.Rxy} {pose.Rxz} {pose.X}{Environment.NewLine}" +
                $"{pose.Ryx} {pose.Ryy} {pose.Ryz} {pose.Y}{Environment.NewLine}" +
                $"{pose.Rzx} {pose.Rzy} {pose.Rzz} {pose.Z}{Environment.NewLine}" +
                $"0, 0, 0, 1{Environment.NewLine}");
        }

        public void CloseFileConnection()
        {
            writer.WriteLine(formatter.FormatFileEnd());
            writer.Flush();
            writer.Close();
        }

        private void CheckFile(string file)
        {
            if (!File.Exists(file))
            {
                throw new ArgumentException($"File {file} doesn't exist.");
            }
        }

        private List<float> ReadMatrixValues(string file)
        {
            List<float> values = new List<float>();
            File.ReadLines(file).
                SelectMany(line => line.Split(delimiter)).
                Select(value => float.Parse(value)).
                ToList().
                ForEach(value => values.Add(value));
            CheckMatrixValues(file, values);
            return values;
        }

        private void CheckMatrixValues(string file, IList<float> values)
        {
            if (values.Count != 16)
            {
                throw new ArgumentException(
                    $"{file} should contain 16 values, but contains {values.Count}.");
            }
            if (!((double)values[12]).ApproximateEquals(0) ||
                !((double)values[13]).ApproximateEquals(0) ||
                !((double)values[14]).ApproximateEquals(0) ||
                !((double)values[15]).ApproximateEquals(1))
            {
                throw new ArgumentException(
                    $"Last line of pose matrix in {file} should be '0 0 0 1'.");
            }
        }
    }
}