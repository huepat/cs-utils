using HuePat.Util.Math;
using HuePat.Util.Math.Geometry;
using OpenTK.Mathematics;

namespace HuePat.Util.IO.Poses.Format {
    public class EulerAngleDegreeFormatter : IPoseFormatter
    {
        public int[] AllowedValueCounts { get { return new int[] { 7 }; } }

        public Pose DeformatPose(string[] values)
        {
            Pose pose = Pose.Identity;
            pose.Position = new Vector3d(
                double.Parse(values[1]),
                double.Parse(values[2]),
                double.Parse(values[3])
            );
            pose.EulerAngles = new Vector3d(
                double.Parse(values[4]).DegreeToRadian(),
                double.Parse(values[5]).DegreeToRadian(),
                double.Parse(values[6]).DegreeToRadian());
            return pose;
        }

        public long DeformatTimestamp(string[] values)
        {
            return long.Parse(values[0]);
        }

        public string Format(long timestamp, Pose pose)
        {
            Vector3d eulerAngles = pose.EulerAngles.RadianToDegree();
            return 
                $"{timestamp} {pose.X} {pose.Y} {pose.Z} " +
                $"{eulerAngles[0]} {eulerAngles[1]} {eulerAngles[2]}";
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