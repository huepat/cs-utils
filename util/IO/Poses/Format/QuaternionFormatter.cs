using HuePat.Util.Math.Geometry;
using OpenTK.Mathematics;

namespace HuePat.Util.IO.Poses.Format {
    public class QuaterionFormatter : IPoseFormatter
    {
        public int[] AllowedValueCounts { get { return new int[] { 8 }; } }

        public Pose DeformatPose(string[] values)
        {
            return new Pose() {
                Position = new Vector3d(
                    double.Parse(values[1]),
                    double.Parse(values[2]),
                    double.Parse(values[3])),
                Quaternion = new Quaterniond(
                    double.Parse(values[4]),
                    double.Parse(values[5]),
                    double.Parse(values[6]),
                    double.Parse(values[7]))
            };
        }

        public long DeformatTimestamp(string[] values)
        {
            return long.Parse(values[0]);
        }

        public string Format(long timestamp, Pose pose)
        {
            Quaterniond quaternion = pose.Quaternion;
            return 
                $"{timestamp} {pose.X} {pose.Y} {pose.Z} " +
                $"{quaternion.X} {quaternion.Y} {quaternion.Z} {quaternion.W}";
        }

        public void DeformatNewFile()
        {
            // nothing to do
        }

        public void DeformatNewLine()
        {
            // nothing to do
        }

        public string FormatFileStart() {
            return "";
        }

        public string FormatFileEnd() {
            return "";
        }
    }
}