using HuePat.Util.Math;

namespace HuePat.Util.Time {
    public static class Extensions {
        public static string FormatMilliseconds(this int milliseconds) {
            return ((double)milliseconds).FormatMilliseconds();
        }

        public static string FormatMilliseconds(this long milliseconds) {
            return ((double)milliseconds).FormatMilliseconds();
        }

        public static string FormatMilliseconds(this double milliseconds) {
            int days, hours, minutes, seconds;
            days = (int)((double)milliseconds / (24 * 60 * 60 * 1000)).Floor();
            if (days > 0) {
                milliseconds -= days * 24 * 60 * 60 * 1000;
            }
            hours = (int)((double)milliseconds / (60 * 60 * 1000)).Floor();
            if (hours > 0) {
                milliseconds -= hours * 60 * 60 * 1000;
            }
            minutes = (int)((double)milliseconds / (60 * 1000)).Floor();
            if (minutes > 0) {
                milliseconds -= minutes * 60 * 1000;
            }
            seconds = (int)((double)milliseconds / 1000).Floor();
            if (seconds > 0) {
                milliseconds -= seconds * 1000;
            }
            string s = "";
            if (days > 0) {
                s += $"{days}d/";
            }
            if (hours > 0) {
                s += $"{hours}h/";
            }
            if (minutes > 0) {
                s += $"{minutes}m/";
            }
            if (seconds > 0) {
                s += $"{seconds}s/";
            }
            s += $"{milliseconds:0}ms";
            return s;
        }
    }
}
