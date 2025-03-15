using System;

namespace HuePat.Util.IO.PLY.Reading {
    class HeaderParser {
        private const string VERTEX_COUNT_POSTFIX = "element vertex ";
        private const string FACE_COUNT_POSTFIX = "element face ";
        private const string FORMAT_POSTFIX = "format ";
        private const string FORMAT_BINARY_LITTLE_ENDIAN_POSTFIX = "format binary_little_endian";
        private const string FORMAT_BINARY_BIG_ENDIAN_POSTFIX = "format binary_big_endian";
        private const string FORMAT_ASCII_POSTFIX = "format ascii";

        private enum HeaderSection {
            START, VERTICES, FACES
        }

        private bool vertexListInitialized;
        private PlyEncoding encoding;
        private HeaderSection section = HeaderSection.START;
        private int headerLineCount, positionCount, vertexCount, faceCount;
        private VertexSectionParser vertexSectionParser;
        private FaceSectionParser faceSectionParser;

        public int LineBreakByteSize { private get; set; }

        public bool AreVertexCoordinatesFloat {
            set {
                vertexSectionParser.AreCoordinatesFloat = value;
            }
        }

        public bool IsVertexNormalVectorFloat {
            set {
                vertexSectionParser.IsNormalVectorFloat = value;
            }
        }

        public bool IsFaceNormalVectorFloat {
            set {
                faceSectionParser.IsNormalVectorFloat = value;
            }
        }

        public Format VertexFormat {
            set {
                vertexSectionParser.Format = value;
            }
        }

        public Format FaceFormat {
            set {
                faceSectionParser.Format = value;
            }
        }

        public HeaderParser() {

            vertexListInitialized = false;
            vertexSectionParser = new VertexSectionParser();
            faceSectionParser = new FaceSectionParser();
        }

        public void Initialize() {

            headerLineCount = 0;
            positionCount = 0;
            vertexCount = 0;
            faceCount = 0;

            vertexSectionParser.Initialize();
            faceSectionParser.Initialize();
        }

        public Header ParseHeaderLine(
                string line) {

            headerLineCount++;

            positionCount += line.Length + LineBreakByteSize;

            if (line.StartsWith(FORMAT_POSTFIX)) {

                if (line.StartsWith(FORMAT_ASCII_POSTFIX)) {
                    encoding = PlyEncoding.ASCII;
                }
                else if(line.StartsWith(FORMAT_BINARY_LITTLE_ENDIAN_POSTFIX)) {
                    encoding = PlyEncoding.BINARY_LITTLE_ENDIAN;
                }
                else if (line.StartsWith(FORMAT_BINARY_BIG_ENDIAN_POSTFIX)) {
                    encoding = PlyEncoding.BINARY_BIG_ENDIAN;
                }
            }
            else if (line.StartsWith(VERTEX_COUNT_POSTFIX)) {

                section = HeaderSection.VERTICES;
                vertexCount = ParseCount(line, VERTEX_COUNT_POSTFIX);
            }
            else if (section == HeaderSection.VERTICES 
                    && line.StartsWith("property")) {

                string[] propertyParts = line.Split(' ');

                vertexSectionParser.ParsePropertyIndex(
                    propertyParts[1], 
                    propertyParts[2]);
            }
            else if (line.StartsWith(FACE_COUNT_POSTFIX)) {

                section = HeaderSection.FACES;

                faceCount = ParseCount(
                    line, 
                    FACE_COUNT_POSTFIX);
            }
            else if (section == HeaderSection.FACES 
                    && line.StartsWith("property")) {

                if(line.StartsWith("property list uchar int vertex_indices")
                     || line.StartsWith("property list uchar uint vertex_indices")) {

                    vertexListInitialized = true;
                }
                else if(!vertexListInitialized) {

                    throw new ApplicationException(
                        "Vertex list must be first element of header section.");
                }
                else if (line.StartsWith("property list")) {

                    faceSectionParser.ParseAdditionalListProperty(line);
                }
                else {
                    string[] propertyParts = line.Split(' ');

                    faceSectionParser.ParsePropertyIndex(
                        propertyParts[1],
                        propertyParts[2]);
                }
            }
            else if (line.Equals("end_header")) {

                return CreateHeader();
            }

            return null;
        }

        private int ParseCount(
                string line, 
                string postfix) {

            return int.Parse(
                line.Substring(
                    postfix.Length));
        }

        private Header CreateHeader() {

            return new Header(
                encoding,
                headerLineCount, 
                positionCount,
                vertexSectionParser.Create(vertexCount),
                faceSectionParser.Create(faceCount));
        }
    }
}