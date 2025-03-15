using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuePat.Util.Image.Marker.Checkerboard {
    public static class CheckerboardUtils {
        public static Point3f[] GetCheckerboardPoints3D(
                Size checkerboardSize,
                float squareSize) {

            List<Point3f> corners = new List<Point3f>();

            for (int r = 0; r < checkerboardSize.Height; r++) {
                for (int c = 0; c < checkerboardSize.Width; c++) {

                    corners.Add(
                        new Point3f(
                            c * squareSize,
                            r * squareSize,
                            0f));
                }
            }

            return corners.ToArray();
        }

        public static Point2f[] GetCornerPointsFromCheckerboardPoints(
                Size checkerboardSize,
                Point2f[] checkerboardPoints) {

            return new Point2f[] {
                checkerboardPoints[checkerboardSize.Width - 1],
                checkerboardPoints[0],
                checkerboardPoints[(checkerboardSize.Height - 1) * checkerboardSize.Width],
                checkerboardPoints[checkerboardSize.Height * checkerboardSize.Width - 1]
            };
        }

        public static Point2f GetCenterFromCheckerboardPoints(
                Size checkerboardSize,
                Point2f[] checkerboardPoints) {

            Point2f[] checkerboardCorners = GetCornerPointsFromCheckerboardPoints(
                checkerboardSize,
                checkerboardPoints);

            Point2f b = checkerboardCorners[3] - checkerboardCorners[2];
            Point2f d1 = checkerboardCorners[0] - checkerboardCorners[2];
            Point2f d2 = checkerboardCorners[1] - checkerboardCorners[3];

            return checkerboardCorners[2] + d1 
                * ((b.X * d2.Y - b.Y * d2.X) 
                / (d1.X * d2.Y - d1.Y * d2.X));
        }

        public static Point2f[] DetectCheckerboardPoints(
                this Mat image,
                Size checkerboardSize,
                bool doSubPixelRefinement = false,
                bool doDisambiguation = false) {

            Point2f[] checkerboardPoints = new Point2f[0];

            if (!Cv2.FindChessboardCorners(
                        image,
                        checkerboardSize,
                        out checkerboardPoints)
                    || checkerboardPoints.Length != checkerboardSize.Width * checkerboardSize.Height) {

                return new Point2f[] { };
            }

            // does not work in any case!
            if (doDisambiguation
                    && (checkerboardPoints[checkerboardPoints.Length - 1].X < checkerboardPoints[0].X
                        || checkerboardPoints[checkerboardPoints.Length - 1].Y < checkerboardPoints[0].Y)) {

                Array.Reverse(checkerboardPoints);
            }

            if (!doSubPixelRefinement) {

                return checkerboardPoints;
            }

            using (Mat greyscale = image.ToGreyscale()) {

                checkerboardPoints = Cv2.CornerSubPix(
                    greyscale,
                    checkerboardPoints,
                    new Size(11, 11),
                    new Size(-1, -1),
                    new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.1));
            }

            return checkerboardPoints;
        }

        public static void DrawCheckerboardPoints(
                this Mat image,
                Size checkerboardSize,
                Point2f[] checkerboardPoints) {

            Cv2.DrawChessboardCorners(
                image,
                checkerboardSize,
                checkerboardPoints,
                true);
        }

        public static Mat CreateMaskFromCheckerboardPoints(
                Size imageSize,
                Size checkerboardSize,
                Point2f[] checkerboardPoints) {

            Point2f[] checkerboardCorners = GetCornerPointsFromCheckerboardPoints(
                checkerboardSize,
                checkerboardPoints);

            return ImageUtils.CreateMaskFromPolygon(
                imageSize,
                checkerboardCorners);
        }
    }
}
