using HuePat.Util.Math.Geometry;

namespace HuePat.Util.IO.Poses.Format {
    public class JsonFormatter : IPoseFormatter {
        private bool firstLine;

        public int[] AllowedValueCounts => throw new System.NotImplementedException();

        public void DeformatNewFile() {
            throw new System.NotImplementedException();
        }

        public void DeformatNewLine() {
            throw new System.NotImplementedException();
        }

        public Pose DeformatPose(string[] values) {
            throw new System.NotImplementedException();
        }

        public long DeformatTimestamp(string[] values) {
            throw new System.NotImplementedException();
        }

        public string FormatFileStart() {
            firstLine = true;
            return "{ \"trail\": [";
        }

        public string Format(long timestamp, Pose pose) {
            string line = "[]";
            if (pose != null) {
                line = $"[{pose.Rxx}, {pose.Ryx}, {pose.Rzx}, {pose.X}, " +
                    $"{pose.Rxy}, {pose.Ryy}, {pose.Rzy}, {pose.Y}, " +
                    $"{pose.Rxz}, {pose.Ryz}, {pose.Rzz}, {pose.Z}]";
            }
            if (firstLine) {
                firstLine = false;
            }
            else {
                line = $", {line}";
            }
            return line;        
        }

        public string FormatFileEnd() {
            return "]}";
        }
    }
}
