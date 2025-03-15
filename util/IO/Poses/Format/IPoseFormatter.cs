using HuePat.Util.Math.Geometry;

namespace HuePat.Util.IO.Poses.Format {
    public interface IPoseFormatter
    {
        int[] AllowedValueCounts { get; }

        void DeformatNewFile();

        void DeformatNewLine();

        long DeformatTimestamp(string[] values);

        Pose DeformatPose(string[] values);

        string FormatFileStart();

        string Format(long timestamp, Pose pose);

        string FormatFileEnd();
    }
}