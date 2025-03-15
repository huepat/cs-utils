using HuePat.Util.Colors;
using HuePat.Util.Math;
using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace HuePat.Util.Image.Binary {
    public static class BinaryImageUtils {
        public static List<(int, int)> GetKernelPixels(
                this bool[,] kernel,
                bool doCenter) {
            int r, rCenter, c, cCenter;
            if (doCenter) {
                rCenter = (kernel.GetLength(0) - 1) / 2;
                cCenter = (kernel.GetLength(1) - 1) / 2;
            }
            else {
                rCenter = cCenter = 0;
            }
            List<(int, int)> pixels = new List<(int, int)>();
            for (r = 0; r < kernel.GetLength(0); r++) {
                for (c = 0; c < kernel.GetLength(1); c++) {
                    if (kernel[r, c]) {
                        pixels.Add((
                            r - rCenter, 
                            c - cCenter));
                    }
                }
            }
            return pixels;
        }

        public static bool[,] GetCircleKernel(
                int radius,
                bool overestimate) {
            int r, c;
            double center = overestimate ? radius : radius - 1;
            double threshold = ((double)radius).Squared();
            bool[,] kernel = new bool[
                2 * radius + (overestimate ? 1 : -1), 
                2 * radius + (overestimate ? 1 : -1)];
            for (r = 0; r < kernel.GetLength(0); r++) {
                for (c = 0; c < kernel.GetLength(1); c++) {
                    if ((center - r).Squared() + (center - c).Squared() <= threshold) {
                        kernel[r, c] = true;
                    }
                }
            }
            return kernel;
        }

        public static bool[,] GetRingKernel(
                int radius,
                bool overestimate,
                bool includeDiagonals) {
            bool found;
            int dr, r, r2, dc, c, c2;
            bool[,] circlekernel = GetCircleKernel(radius, overestimate);
            bool[,] ringKernel = new bool[
                circlekernel.GetLength(0),
                circlekernel.GetLength(1)];
            List<(int, int)> ringPixels = new List<(int, int)>();
            for (r = 0; r < circlekernel.GetLength(0); r++) {
                for (c = 0; c < circlekernel.GetLength(1); c++) {
                    if (!circlekernel[r, c]) {
                        continue;
                    }
                    found = false;
                    for (dr = -1; dr <= 1; dr++) {
                        for (dc = -1; dc <= 1; dc++) {
                            if (!includeDiagonals && dr.Abs() == dc.Abs()) {
                                continue;
                            }
                            r2 = r + dr;
                            c2 = c + dc;
                            if (r2 < 0 || c2 < 0
                                    || r2 >= circlekernel.GetLength(0)
                                    || c2 >= circlekernel.GetLength(1)
                                    || !circlekernel[r2, c2]) {
                                found = true;
                                ringPixels.Add((r, c));
                                break;
                            }
                        }
                        if (found) {
                            break;
                        }
                    }
                }
            }
            foreach ((int, int) pixel in ringPixels) {
                ringKernel[
                    pixel.Item1,
                    pixel.Item2] = true;
            }
            return ringKernel;
        }

        public static bool[,] Binarize<T>(
                this Mat image,
                Func<T, bool> pixelCallback) where T : struct {
            int r, c;
            bool[,] binarized = new bool[
                image.Height,
                image.Width];
            for (r = 0; r < image.Height; r++) {
                for (c = 0; c < image.Width; c++) {
                    binarized[r, c] = pixelCallback(
                        image.Get<T>(r, c));
                }
            }
            return binarized;
        }

        public static Mat ToMat(
                this bool[,] binaryImage) {
            int r, c;
            Mat image = new Mat(
                binaryImage.GetLength(0),
                binaryImage.GetLength(1),
                MatType.CV_8UC3);
            for (r = 0; r < image.Height; r++) {
                for (c = 0; c < image.Width; c++) {
                    image.Set(
                        r,
                        c,
                        binaryImage[r, c] ?
                            Color.White.ToOpenCV() :
                            Color.Black.ToOpenCV());
                }
            }
            return image;
        }

        public static void Export(
                this bool[,] binaryImage,
                string file) {
            using (Mat image = binaryImage.ToMat()) {
                Cv2.ImWrite(file, image);
            }
        }

        public static bool[,] Copy(
                this bool[,] image) {
            int r, c;
            bool[,] copy = new bool[
                image.GetLength(0),
                image.GetLength(1)];
            for (r = 0; r < image.GetLength(0); r++) {
                for (c = 0; c < image.GetLength(1); c++) {
                    copy[r, c] = image[r, c];
                }
            }
            return copy;
        }

        public static bool[,] Close(
                this bool[,] image,
                bool[,] kernel) {
            return image
                .Dilate(kernel)
                .Erode(kernel);
        }

        public static bool[,] Open(
                this bool[,] image,
                bool[,] kernel) {
            return image
                .Erode(kernel)
                .Dilate(kernel);
        }

        public static bool[,] Erode(
                this bool[,] image,
                bool[,] kernel) {
            if (kernel.GetLength(0) % 2 != 1
                    || kernel.GetLength(1) % 2 != 1) {
                throw new ArgumentException("Kernel Size must be odd.");
            }
            bool erode;
            int r, dr, c, dc;
            int s_r = (kernel.GetLength(0) - 1) / 2;
            int s_c = (kernel.GetLength(1) - 1) / 2;
            bool[,] result = new bool[
                image.GetLength(0),
                image.GetLength(1)];
            for (r = s_r; r < image.GetLength(0) - s_r; r++) {
                for (c = s_c; c < image.GetLength(1) - s_c; c++) {
                    if (!image[r, c]) {
                        continue;
                    }
                    erode = false;
                    for (dr = -s_r; dr <= s_r; dr++) {
                        for (dc = -s_c; dc <= s_c; dc++) {
                            if (kernel[dr + s_r, dc + s_r] 
                                    && !image[r + dr, c + dc]) {
                                erode = true;
                                break;
                            }
                        }
                        if (erode) {
                            break;
                        }
                    }
                    if (!erode) {
                        result[r, c] = true;
                    }
                }
            }
            return result;
        }

        public static bool[,] Dilate(
                this bool[,] image,
                bool[,] kernel) {
            if (kernel.GetLength(0) % 2 != 1
                    || kernel.GetLength(1) % 2 != 1) {
                throw new ArgumentException("Kernel Size must be odd.");
            }
            int r, r2, dr, c, c2, dc;
            int s_r = (kernel.GetLength(0) - 1) / 2;
            int s_c = (kernel.GetLength(1) - 1) / 2;
            bool[,] result = new bool[
                image.GetLength(0),
                image.GetLength(1)];
            for (r = s_r; r < image.GetLength(0) - s_r; r++) {
                for (c = s_c; c < image.GetLength(1) - s_c; c++) {
                    if (!image[r, c]) {
                        continue;
                    }
                    for (dr = -s_r; dr <= s_r; dr++) {
                        for (dc = -s_c; dc <= s_c; dc++) {
                            r2 = r + dr;
                            c2 = c + dc;
                            if (kernel[dr + s_r, dc + s_r]) {
                                result[r2, c2] = true;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static bool[,] Substract(
                this bool[,] image,
                bool[,] otherImage) {
            if (image.GetLength(0) != otherImage.GetLength(0)
                    || image.GetLength(1) != otherImage.GetLength(1)) {
                throw new ArgumentException("Images must be of same dimension.");
            }
            int r, c;
            bool[,] result = new bool[
                image.GetLength(0),
                image.GetLength(1)];
            for (r = 0; r < image.GetLength(0); r++) {
                for (c = 0; c < image.GetLength(1); c++) {
                    if (image[r, c] && !otherImage[r, c]) {
                        result[r, c] = true;
                    }
                }
            }
            return result;
        }
    }
}