using HuePat.Util.Image;
using HuePat.Util.Object;
using HuePat.Util.Object.Properties;
using OpenCvSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HuePat.Util.Colors {
    public static class ColorExtensions {
        public static string COLOR_PROPERTY_NAME = "color";

        public static bool HasColor(
                this IObject @object) {

            return @object.HasColorProperty(COLOR_PROPERTY_NAME);
        }

        public static Color GetColor(
                this IObject @object) {

            return @object.GetColorProperty(COLOR_PROPERTY_NAME);
        }

        public static void SetColor(
                this IObject @object, 
                Color color) {

            @object.SetColorProperty(
                COLOR_PROPERTY_NAME, 
                color);
        }

        public static PropertyDescriptor AddColor(
                this PropertyDescriptor descriptor) {

            descriptor.AddColorProperty(COLOR_PROPERTY_NAME);

            return descriptor;
        }

        public static bool HasColor(
                this PropertyDescriptor descriptor) {

            return descriptor.ColorProperties.Contains(COLOR_PROPERTY_NAME);
        }

        public static void Write(
                this BinaryWriter writer,
                Color color,
                bool withAlphaChannel) {

            writer.Write(color.R);
            writer.Write(color.G);
            writer.Write(color.B);

            if (withAlphaChannel) {
                writer.Write(color.A);
            }
        }

        public static Color Colorize(
                this double value,
                double? min,
                double? max,
                bool doInvert = false,
                bool useBackgroundColorForOutOfRangePixel = true,
                ColormapTypes colormap = ColormapTypes.Jet) {

            return new double[] { 
                value 
            }.Colorize(
                ref min,
                ref max,
                backGroundColor : Color.Black,
                doInvert : doInvert,
                useBackgroundColorForOutOfRangePixel : useBackgroundColorForOutOfRangePixel,
                colormap : colormap)[0];
        }

        public static Color[] Colorize(
                this IList<double> values,
                double? min,
                double? max,
                Color backGroundColor,
                bool useBackgroundColorForOutOfRangePixel = true,
                bool doInvert = false,
                ColormapTypes colormap = ColormapTypes.Jet) {

            return Colorize(
                values,
                ref min,
                ref max,
                backGroundColor,
                useBackgroundColorForOutOfRangePixel,
                doInvert,
                colormap);
        }

        public static Color[] Colorize(
                this IList<double> values,
                ref double? min,
                ref double? max,
                Color backGroundColor,
                bool useBackgroundColorForOutOfRangePixel = true,
                bool doInvert = false,
                ColormapTypes colormap = ColormapTypes.Jet) {

            Color[] colors = new Color[values.Count];

            using (Mat valuesToColor = new Mat(
                    values.Count, 
                    1, 
                    MatType.CV_64FC1,
                    values.ToArray())) {

                using (Mat colorized = valuesToColor.Colorize(
                        ref min,
                        ref max,
                        backGroundColor,
                        useBackgroundColorForOutOfRangePixel,
                        doInvert,
                        colormap)) {

                    using (Mat<Vec3b> _colorized = new Mat<Vec3b>(colorized)) {

                        MatIndexer<Vec3b> colorizedData = _colorized.GetIndexer();

                        for (int i = 0; i < values.Count; i++) {

                            colors[i] = colorizedData[i].ToColor();
                        }
                    }
                }
            }

            return colors;
        }
    }
}