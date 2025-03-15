using HuePat.Util.Colors;
using HuePat.Util.Object.Properties;

namespace HuePat.Util.IO {
    public class ColoredPointFormat: PointFormat {
        public ColoredPointFormat() : base() {
            PropertyDescriptor = new PropertyDescriptor().AddColor();
        }
    }
}