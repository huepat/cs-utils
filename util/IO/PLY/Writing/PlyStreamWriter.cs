using HuePat.Util.Math.Geometry;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;

namespace HuePat.Util.IO.PLY.Writing {
    public class PlyStreamWriter: PlyWriterBase, IDisposable {
        private bool isInitialized;
        private int pointCount;
        private int faceCount;
        private string file;
        private string pointTempFile;
        private string faceTempFile;
        private IEncoder pointTempFileEncoder;
        private IEncoder faceTempFileEncoder;

        public PlyStreamWriter(
                string file) {
            
            isInitialized = true;
            pointCount = faceCount = 0;
            this.file = file;

            if (File.Exists(file)) {
                File.Delete(file);
            }

            pointTempFile = FileSystemUtils.GetTempFile(
                FileSystemUtils.GetFileWithPostfix(file, "_points"));
            faceTempFile = FileSystemUtils.GetTempFile(
                FileSystemUtils.GetFileWithPostfix(file, "_faces"));

            OnPointConfigUpdate();
            OnFaceConfigUpdate();
        }

        public void Dispose() {

            pointTempFileEncoder.Dispose();
            faceTempFileEncoder.Dispose();

            WriteHeader(
                file, 
                pointCount, 
                faceCount);

            using (Stream output = new FileStream(
                    file, 
                    FileMode.Append, 
                    FileAccess.Write, 
                    FileShare.None)) {

                using (Stream points = File.OpenRead(pointTempFile)) {
                    points.CopyTo(output);
                }

                using (Stream faces = File.OpenRead(faceTempFile)) {
                    faces.CopyTo(output);
                }
            }

            File.Delete(pointTempFile);
            File.Delete(faceTempFile);
        }

        public void Write(
                PointCloud pointCloud) {

            Write(pointCloud as IReadOnlyList<Point>);
        }

        public void Write(
                IReadOnlyList<Point> points) {

            for (int i = 0; i < points.Count; i++) {
                Write(points[i]);
            }
        }

        public void Write(
                Vector3d coordinate) {

            Write(new Point(coordinate));
        }

        public void Write(
                Point point) {

            pointCount++;

            pointTempFileEncoder.Encode(point);
        }

        public void Write(
                IFiniteGeometry geometry) {

            Write(geometry.Mesh);
        }

        public void Write(
                Mesh mesh) {

            int offset = pointCount;

            Write(mesh.Vertices);

            for (int i = 0; i < mesh.Count; i++) {

                faceCount++;

                faceTempFileEncoder.Encode(
                    mesh[i], 
                    offset);
            }
        }

        protected override void OnPointConfigUpdate() {

            base.OnPointConfigUpdate();

            if (isInitialized) {
                pointTempFileEncoder?.Dispose();
                pointTempFileEncoder = GetEncoder(pointTempFile);
            }
        }

        protected override void OnFaceConfigUpdate() {

            base.OnFaceConfigUpdate();

            if (isInitialized) {
                faceTempFileEncoder?.Dispose();
                faceTempFileEncoder = GetEncoder(faceTempFile);
            }
        }
    }
}