using HuePat.Util.Math;
using OpenTK.Mathematics;
using System.Linq;

namespace HuePat.Util.Colors {
    public class Color {
        public const byte COLOR_COMPARISON_DELTA = 10;
        public static readonly Color Black = new Color(0, 0, 0);
        public static readonly Color White = new Color(255, 255, 255);
        public static readonly Color Red = new Color(255, 0, 0);
        public static readonly Color Green = new Color(0, 255, 0);
        public static readonly Color Blue = new Color(0, 0, 255);
        public static readonly Color Yellow = new Color(255, 255, 0);
        public static readonly Color Magenta = new Color(255, 0, 255);
        public static readonly Color Cyan = new Color(0, 255, 255);
        public static readonly Color LightGray = new Color(211, 211, 211);
        public static readonly Color Gray = new Color(169, 169, 169);
        public static readonly Color DarkGray = new Color(105, 105, 105);
        public static readonly Color Orange = new Color(255, 140, 0);
        public static readonly Color LightRed = new Color(250, 128, 114);
        public static readonly Color DarkRed = new Color(139, 0, 0);
        public static readonly Color LightBlue = new Color(100, 149, 237);
        public static readonly Color DarkBlue = new Color(0, 0, 128);
        public static readonly Color LightGreen = new Color(152, 251, 152);
        public static readonly Color ForestGreen = new Color(34, 139, 34);
        public static readonly Color DarkGreen = new Color(0, 90, 0);
        public static readonly Color Olive = new Color(128, 128, 0);
        public static readonly Color DarkOlive = new Color(85, 107, 47);
        public static readonly Color DarkCyan = new Color(0, 139, 139);
        public static readonly Color Pink = new Color(255, 165, 210);
        public static readonly Color Purple = new Color(128, 0, 128);
        public static readonly Color Violet = new Color(138, 43, 226);
        public static readonly Color Indigo = new Color(75, 0, 130);
        public static readonly Color LightBrown = new Color(205, 133, 63);
        public static readonly Color Brown = new Color(139, 69, 19);
        public static readonly Color Beige = new Color(255, 222, 173);

        public static Color operator +(Color left, Color right) {
            return new Color(
                Add(left.R, right.R),
                Add(left.G, right.G),
                Add(left.B, right.B));
        }

        public static Color operator *(Color color, float factor) {
            return new Color(
                Multiply(color.R, factor),
                Multiply(color.G, factor),
                Multiply(color.B, factor));
        }

        public static bool ApproximateEquals(
                Color color1, 
                Color color2,
                byte delta = COLOR_COMPARISON_DELTA) {
            return (color1.R - color2.R).Abs() < COLOR_COMPARISON_DELTA
                && (color1.G - color2.G).Abs() < COLOR_COMPARISON_DELTA
                && (color1.B - color2.B).Abs() < COLOR_COMPARISON_DELTA;
        }

        private static byte Add(byte left, byte right) {
            return Clamp(left + right);
        }

        private static byte Multiply(byte value, float factor) {
            return Clamp((int)(value * factor).Round());
        }

        private static byte Clamp(int value) {
            if (value <= 0) {
                return 0;
            }
            if (value >= 255) {
                return 255;
            }
            return (byte)value;
        }

        public byte R { get; private set; }
        public byte G { get; private set; }
        public byte B { get; private set; }
        public byte A { get; private set; }

        public Vector3d HSV {
            get {
                double min = new byte[] { R, G, B }.Min();
                double max = new byte[] { R, G, B }.Max();
                double h, s, v = max;
                double delta = max - min;
                if (max != 0.0)
                    s = delta / max;
                else {
                    return new Vector3d(-1.0, 0.0, -1.0);
                }
                if (R == max) {
                    h = (G - B) / delta;
                }
                else if (G == max) {
                    h = 2 + (B - R) / delta;
                }
                else {
                    h = 4 + (R - G) / delta;
                }
                h *= 60.0;
                if (h < 0.0)
                    h += 360.0;
                if (double.IsNaN(h)) {
                    h = 0.0;
                }
                return new Vector3d(h, s, v);
            }
            set {
                double i, f, p, q, t;
                double h = value[0];
                double s = value[1];
                double v = value[2];
                if (s == 0.0) {
                    R = G = B = (byte)(v * 2.55);
                    return;
                }
                h /= 60.0;
                i = (int)h.Floor();
                f = h - i;
                p = v * (1 - s);
                q = v * (1 - s * f);
                t = v * (1 - s * (1 - f));
                switch (i) {
                    case 0:
                        R = (byte)v;
                        G = (byte)t;
                        B = (byte)p;
                        break;
                    case 1:
                        R = (byte)q;
                        G = (byte)v;
                        B = (byte)p;
                        break;
                    case 2:
                        R = (byte)p;
                        G = (byte)v;
                        B = (byte)t;
                        break;
                    case 3:
                        R = (byte)p;
                        G = (byte)q;
                        B = (byte)v;
                        break;
                    case 4:
                        R = (byte)t;
                        G = (byte)p;
                        B = (byte)v;
                        break;
                    default:
                        R = (byte)v;
                        G = (byte)p;
                        B = (byte)q;
                        break;
                }
            }
        }

        public Color(): this(0, 0, 0) {}

        public Color(double r, double g, double b): this(r, g, b, 255.0) {
        }

        public Color(double r, double g, double b, double a) :
                this(Clamp((int)r), Clamp((int)g), Clamp((int)b), Clamp((int)a)) {
        }

        public Color(byte r, byte g, byte b): this(r, g, b, 255) {
        }

        public Color(byte r, byte g, byte b, byte a) {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color Clone() {
            return new Color(R, G, B, A);
        }
    }
}