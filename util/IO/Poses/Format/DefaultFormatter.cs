using HuePat.Util.Math.Geometry;

namespace HuePat.Util.IO.Poses.Format {
    public class DefaultFormatter : IPoseFormatter
    {
        private static readonly int[] ALLOWED_VALUE_COUNTS = new int[] { 13, 1 };

        public int[] AllowedValueCounts
        {
            get
            {
                return ALLOWED_VALUE_COUNTS;
            }
        }

        public void DeformatNewFile()
        {
            // nothing to do
        }

        public void DeformatNewLine()
        {
            // nothing to do
        }

        public long DeformatTimestamp(string[] values)
        {
            return long.Parse(values[0]);
        }

        public Pose DeformatPose(string[] values)
        {
            if (values.Length == 1)
            {
                return null;
            }
            return new Pose(
                float.Parse(values[4]),
                float.Parse(values[7]),
                float.Parse(values[10]),
                float.Parse(values[1]),
                float.Parse(values[5]),
                float.Parse(values[8]),
                float.Parse(values[11]),
                float.Parse(values[2]),
                float.Parse(values[6]),
                float.Parse(values[9]),
                float.Parse(values[12]),
                float.Parse(values[3]));
        }

        public string Format(long timestamp, Pose pose)
        {
            if (pose == null)
            {
                timestamp.ToString();
            }
            return 
                $"{timestamp} {pose.X} {pose.Y} {pose.Z} " +
                $"{pose.Rxx} {pose.Ryx} {pose.Rzx} " +
                $"{pose.Rxy} {pose.Ryy} {pose.Rzy} " +
                $"{pose.Rxz} {pose.Ryz} {pose.Rzz}";
        }

        public string FormatFileStart() {
            return "";
        }

        public string FormatFileEnd() {
            return "";
        }
    }
}