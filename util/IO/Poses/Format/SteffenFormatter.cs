using HuePat.Util.Math.Geometry;

namespace HuePat.Util.IO.Poses.Format {
    public class SteffenFormatter : IPoseFormatter
    {
        private static readonly int[] ALLOWED_VALUE_COUNTS = new int[] { 16 };
        public long counter = 0;

        public int[] AllowedValueCounts
        {
            get
            {
                return ALLOWED_VALUE_COUNTS;
            }
        }

        public void DeformatNewFile()
        {
            counter = 0;
        }

        public void DeformatNewLine()
        {
            counter++;
        }

        public long DeformatTimestamp(string[] values)
        {
            return counter;
        }

        public Pose DeformatPose(string[] values)
        {
            return new Pose(
                float.Parse(values[0]),
                float.Parse(values[4]),
                float.Parse(values[8]),
                float.Parse(values[12]),
                float.Parse(values[1]),
                float.Parse(values[5]),
                float.Parse(values[9]),
                float.Parse(values[13]),
                float.Parse(values[2]),
                float.Parse(values[6]),
                float.Parse(values[10]),
                float.Parse(values[14]));
        }

        public string Format(long timestamp, Pose pose)
        {
            if (pose == null)
            {
                return " ";
            }
            return
                $"{timestamp} " +
                $"{pose.Rxx} {pose.Ryx} {pose.Rzx} 0 " +
                $"{pose.Rxy} {pose.Ryy} {pose.Rzy} 0 " +
                $"{pose.Rxz} {pose.Ryz} {pose.Rzz} 0 " +
                $"{pose.X} {pose.Y} {pose.Z} 1";
        }

        public string FormatFileStart() {
            return "";
        }

        public string FormatFileEnd() {
            return "";
        }
    }
}