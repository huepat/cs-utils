using HuePat.Util.IO;
using OpenCvSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuePat.Util.Image {
    public static class VideoUtils {
        public static void ExtractFrames(
                string videoFile,
                string outputDirectory,
                string exportFormat = "jpg",
                int? everyXFrames = null,
                bool maximizeSharpness = false) {

            int frameIndex = 0;
            int stackFrameIndex = 0;
            int frameExportIndex = 0;
            int stackExportIndex;
            int _everyXFrames = everyXFrames.HasValue ?
                everyXFrames.Value :
                1;
            Mat[] frames = new Mat[_everyXFrames];

            if (Directory.Exists(outputDirectory)) {
                FileSystemUtils.CleanDirectory(outputDirectory);
            }
            else {
                Directory.CreateDirectory(outputDirectory);
            }

            foreach (Mat frame in ReadPersistentFrames(videoFile)) {

                frames[stackFrameIndex++] = frame;

                if (stackFrameIndex == _everyXFrames) {

                    // ToDo: fix this!
                    // Something got lost in a faulty revert...

                    //stackExportIndex = maximizeSharpness ?
                    //    Enumerable
                    //        .Range(0, _everyXFrames)
                    //        .WhereMax(i => frames[i].GetSharpness())
                    //        .First() :
                    //    0;

                    //Cv2.ImWrite(
                    //    $"{outputDirectory}/{Path.GetFileNameWithoutExtension(videoFile)}" 
                    //        + $"_{frameExportIndex++}({frameIndex + stackExportIndex}).{exportFormat}",
                    //    frames[stackExportIndex]);

                    frameIndex += _everyXFrames;
                    stackFrameIndex = 0;
                    frames.Dispose();
                    frames = new Mat[_everyXFrames];
                }
            }
        }

        public static IEnumerable<Mat> ReadFrames(
                string videoFile) {

            using (VideoCapture videoCapture = new VideoCapture(videoFile)) {
                using (Mat frame = new Mat()) {

                    while (videoCapture.Read(frame)) {
                        yield return frame;
                    }
                }
            }
        }

        public static IEnumerable<Mat> ReadPersistentFrames(
                string videoFile) {

            using (VideoCapture videoCapture = new VideoCapture(videoFile)) {

                while (true) {

                    Mat frame = new Mat();

                    if (!videoCapture.Read(frame)) {
                        break;
                    }

                    yield return frame;
                }
            }
        }
    }
}