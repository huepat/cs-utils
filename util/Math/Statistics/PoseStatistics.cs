using HuePat.Util.Math.Geometry;

namespace HuePat.Util.Math.Statistics {
    public class PoseStatistics {
        private readonly VectorStatistics position;
        private readonly AngleVectorStatistics orientation;

        public Pose Mean {
            get {
                return new Pose() {
                    Position = position.Mean,
                    EulerAngles = orientation.Mean
                };
            }
        }

        public Pose Variance {
            get {
                return new Pose() {
                    Position = position.Variance,
                    EulerAngles = orientation.Variance
                };
            }
        }

        public Pose StandardDeviation {
            get {
                return new Pose() {
                    Position = position.StandardDeviation,
                    EulerAngles = orientation.StandardDeviation
                };
            }
        }

        public PoseStatistics() {
            position = new VectorStatistics();
            orientation = new AngleVectorStatistics();
        }

        public void Update(Pose pose) {
            position.Update(pose.Position);
            orientation.Update(pose.EulerAngles);
        }
    }
}