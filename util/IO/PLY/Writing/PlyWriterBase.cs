using System;

namespace HuePat.Util.IO.PLY.Writing {
    public abstract class PlyWriterBase {
        private PlyEncoding encoding;
        protected Format vertexFormat;
        protected Format faceFormat;

        public bool WriteCoordinatesAsFloat { private get; set; }
        public PlyEncoding Encoding {
            private get {
                return encoding;
            }
            set {
                encoding = value;

                OnPointConfigUpdate();
                OnFaceConfigUpdate();
            }
        }

        public PointFormat PointFormat {
            set {
                vertexFormat = value.Create();
                OnPointConfigUpdate();
            }
        }

        public FaceFormat FaceFormat {
            set {
                faceFormat = value.Create();
                OnFaceConfigUpdate();
            }
        }

        public PlyWriterBase() {

            WriteCoordinatesAsFloat = true;
            Encoding = PlyEncoding.BINARY_LITTLE_ENDIAN;
            PointFormat = new PointFormat();
            FaceFormat = new FaceFormat();
        }

        protected virtual void OnPointConfigUpdate() {
        }

        protected virtual void OnFaceConfigUpdate() {
        }

        protected void WriteHeader(
                string file,
                long vertexCount,
                long faceCount) {

            HeaderWriter.Write(
                file,
                WriteCoordinatesAsFloat,
                Encoding,
                vertexCount,
                faceCount,
                vertexFormat,
                faceFormat);
        }

        protected IEncoder GetEncoder(
                string file) {

            switch (Encoding) {
                case PlyEncoding.BINARY_LITTLE_ENDIAN:
                    return new BinaryEncoder(
                        file,
                        true,
                        WriteCoordinatesAsFloat,
                        vertexFormat,
                        faceFormat);
                case PlyEncoding.BINARY_BIG_ENDIAN:
                    return new BinaryEncoder(
                        file,
                        false,
                        WriteCoordinatesAsFloat,
                        vertexFormat,
                        faceFormat);
                case PlyEncoding.ASCII:
                    return new AsciiEncoder(
                        file,
                        WriteCoordinatesAsFloat,
                        vertexFormat,
                        faceFormat);
            }

            throw new ApplicationException();
        }
    }
}