using OpenCvSharp;
using System;

namespace HuePat.Util.Math.Statistics {
    public class FloatImageStatistics {
        private bool hasStarted = false;
        private float nullValue;

        public int ImageCount { get; private set; }
        public Mat PixelStackCount { get; private set; }
        public Mat Mean { get; private set; }
        public Mat Variance { get; private set; }
        public Func<float, bool> PixelFilter { get; set; }

        public float NullValue { 
            set {
                if (hasStarted) {
                    return;
                }
                for (int r = 0; r < Mean.Height; r++) {
                    for (int c = 0; c < Mean.Width; c++) {
                        Mean.Set(r, c, value);
                        Variance.Set(r, c, value);
                    }
                }
                nullValue = value;
            }
        }

        public Mat StandardDeviation {
            get {
                float value;
                Mat standardDeviation = new Mat(Variance.Size(), MatType.CV_32FC1);
                for (int r = 0; r < Variance.Height; r++) {
                    for (int c = 0; c < Variance.Width; c++) {
                        value = Variance.Get<float>(r, c);
                        if (value != nullValue) {
                            value = value.Sqrt();
                        }
                        standardDeviation.Set(r, c, value);
                    }
                }
                return standardDeviation;
            }
        }

        public Mat StackCompleteness {
            get {
                int value;
                Mat stackCompleteness = new Mat(PixelStackCount.Size(), MatType.CV_32FC1, Scalar.Black);
                for (int r = 0; r < PixelStackCount.Height; r++) {
                    for (int c = 0; c < PixelStackCount.Width; c++) {
                        value = PixelStackCount.Get<int>(r, c);
                        if (value == 0) {
                            stackCompleteness.Set(r, c, nullValue);
                        }
                        else {
                            stackCompleteness.Set(r, c, (float)value / ImageCount);
                        }
                    }
                }
                return stackCompleteness;
            }
        }

        public FloatImageStatistics(Size size) {
            PixelStackCount = new Mat(size, MatType.CV_32SC1, Scalar.Black);
            Mean = new Mat(size, MatType.CV_32FC1, Scalar.Black);
            Variance = new Mat(size, MatType.CV_32FC1, Scalar.Black);
            PixelFilter = pixelValue => true;
        }

        public void Update(Mat image) {
            if (image.Size() != Mean.Size() 
                    || image.Type() != MatType.CV_32FC1) {
                throw new ApplicationException();
            }
            hasStarted = true;
            ImageCount++;
            for (int r = 0; r < image.Height; r++) {
                for (int c = 0; c < image.Width; c++) {
                    float value = image.Get<float>(r, c);
                    if (!PixelFilter(value)) {
                        continue;
                    }
                    int pixelCount = PixelStackCount.Get<int>(r, c);
                    if (pixelCount == 0) {
                        Mean.Set(r, c, 0f);
                        Variance.Set(r, c, 0f);
                    }
                    float oldMean = Mean.Get<float>(r, c);
                    Mean.Set(r, c,
                        oldMean + (value - oldMean) / (pixelCount + 1));
                    if (pixelCount > 0) {
                        Variance.Set(r, c,
                            (1f - 1f / pixelCount) * Variance.Get<float>(r, c) + (pixelCount + 1) 
                                * (Mean.Get<float>(r, c) - oldMean).Squared());
                    }
                    PixelStackCount.Set(r, c, pixelCount + 1);
                }
            }
        }

        public Mat GetMask(float stackCompletenessThreshold) {
            Mat mask = new Mat(PixelStackCount.Size(), MatType.CV_8UC3, Scalar.Black);
            for (int r = 0; r < PixelStackCount.Height; r++) {
                for (int c = 0; c < PixelStackCount.Width; c++) {
                    if ((float)PixelStackCount.Get<int>(r, c) / ImageCount >= stackCompletenessThreshold) {
                        mask.Set(r, c, new Vec3b(255, 255, 255));
                    }
                }
            }
            return mask;
        }

        public int GetPixelImageCount(float stackCompletenessThreshold) {
            int pixelStackCountValue;
            int pixelImageCount = 0;
            for (int r = 0; r < PixelStackCount.Height; r++) {
                for (int c = 0; c < PixelStackCount.Width; c++) {
                    pixelStackCountValue = PixelStackCount.Get<int>(r, c);
                    if ((stackCompletenessThreshold == 1f && pixelStackCountValue > 0)
                            || (float)pixelStackCountValue / ImageCount >= stackCompletenessThreshold) {
                        pixelImageCount++;
                    }
                }
            }
            return pixelImageCount;
        }
    }
}