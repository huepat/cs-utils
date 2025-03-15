using OpenCvSharp;

namespace HuePat.Util.Math.Geometry.Projection {
    public class PnpParams
    {
        public SolvePnPFlags Algorithm { get; set; }

        public bool UseRansac { get; set; }

        public int IterationsCount { get; set; }

        public float ReprojectionError { get; set; }

        public double Confidence { get; set; }

        public PnpParams(
            SolvePnPFlags algorithm = SolvePnPFlags.EPNP,
            bool useRansac = false,
            int iterationsCount = 100,
            float reprojectionError = 8f,
            double confidence = 0.99)
        {
            Algorithm = algorithm;
            UseRansac = useRansac;
            IterationsCount = iterationsCount;
            ReprojectionError = reprojectionError;
            Confidence = confidence;
        }
    }
}