using HuePat.Util.Colors;
using HuePat.Util.Object;
using HuePat.Util.Object.Properties;
using OpenTK.Mathematics;
using System.Linq;

namespace HuePat.Util.IO.PLY.Reading {
    abstract class BaseParser {
        protected readonly Header header;

        public BaseParser(
                Header header) {

            this.header = header;
        }

        protected string[] SplitValues(
                string line) {

            return line.Split(' ').Where(v => v != "").ToArray();
        }

        protected byte ParseByte(
                string[] values, 
                int index) {

            return byte.Parse(values[index]);
        }

        protected int ParseInteger(
                string[] values, 
                int index) {

            return int.Parse(values[index]);
        }

        protected float ParseFloat(
                string[] values, 
                int index) {

            return float.Parse(values[index]);
        }

        protected double ParseDouble(
                string[] values, 
                int index) {

            return double.Parse(values[index]);
        }

        protected Vector3d ParseVector3d(
                string[] values, 
                int[] indices) {

            return new Vector3d(
                double.Parse(values[indices[0]]),
                double.Parse(values[indices[1]]),
                double.Parse(values[indices[2]]));
        }

        protected Color ParseColor(
                string[] values, 
                int[] indices) {

            if (indices.Length == 4) {
                return new Color(
                    byte.Parse(values[indices[0]]),
                    byte.Parse(values[indices[1]]),
                    byte.Parse(values[indices[2]]),
                    byte.Parse(values[indices[3]]));
            }

            return new Color(
                byte.Parse(values[indices[0]]),
                byte.Parse(values[indices[1]]),
                byte.Parse(values[indices[2]]));
        }

        protected void ParseProperties(
                IObject @object, 
                string[] values,
                HeaderSection headerSection) {

            ParseByteProperties(@object, values, headerSection);
            ParseIntegerProperties(@object, values, headerSection);
            ParseFloatProperties(@object, values, headerSection);
            ParseDoubleProperties(@object, values, headerSection);
            ParseVector3dProperties(@object, values, headerSection);
            ParseColorProperties(@object, values, headerSection);
        }

        protected void ParseByteProperties(
                IObject @object,
                string[] values,
                HeaderSection headerSection) {

            int index;

            foreach (string propertyName in headerSection.BytePropertyIndices.Keys) {

                index = headerSection.BytePropertyIndices[propertyName];

                if (!headerSection.IndicesNotInFormat.Contains(index)) {
                    @object.SetByteProperty(
                        propertyName,
                        ParseByte(values, index));
                }
            }
        }

        protected void ParseIntegerProperties(
                IObject @object,
                string[] values,
                HeaderSection headerSection) {

            int index;

            foreach (string propertyName in headerSection.IntegerPropertyIndices.Keys) {

                index = headerSection.IntegerPropertyIndices[propertyName];

                if (!headerSection.IndicesNotInFormat.Contains(index)) {
                    @object.SetIntegerProperty(
                        propertyName,
                        ParseInteger(values, index));
                }
            }
        }

        protected void ParseFloatProperties(
                IObject @object,
                string[] values,
                HeaderSection headerSection) {

            int index;

            foreach (string propertyName in headerSection.FloatPropertyIndices.Keys) {

                index = headerSection.FloatPropertyIndices[propertyName];

                if (!headerSection.IndicesNotInFormat.Contains(index)) {
                    @object.SetFloatProperty(
                        propertyName,
                        ParseFloat(values, index));
                }
            }
        }

        protected void ParseDoubleProperties(
                IObject @object,
                string[] values,
                HeaderSection headerSection) {

            int index;

            foreach (string propertyName in headerSection.DoublePropertyIndices.Keys) {

                index = headerSection.DoublePropertyIndices[propertyName];

                if (!headerSection.IndicesNotInFormat.Contains(index)) {
                    @object.SetDoubleProperty(
                        propertyName,
                        ParseDouble(values, index));
                }
            }
        }

        protected void ParseVector3dProperties(
                IObject @object,
                string[] values,
                HeaderSection headerSection) {

            int[] indices;

            foreach (string propertyName in headerSection.Vector3dPropertyIndices.Keys) {

                indices = headerSection.Vector3dPropertyIndices[propertyName];

                if (!indices.Any(index => headerSection.IndicesNotInFormat.Contains(index))) {
                    @object.SetVector3Property(
                        propertyName,
                        ParseVector3d(values, indices));
                }
            }
        }

        protected void ParseColorProperties(
                IObject @object, 
                string[] values,
                HeaderSection headerSection) {

            int[] indices;

            foreach (string propertyName in headerSection.ColorPropertyIndices.Keys) {

                indices = headerSection.ColorPropertyIndices[propertyName];

                if (!indices.Any(index => headerSection.IndicesNotInFormat.Contains(index))) {
                    @object.SetColorProperty(
                        propertyName,
                        ParseColor(values, indices));
                }
            }
        }
    }
}