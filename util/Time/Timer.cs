using System;
using System.Runtime.InteropServices;

namespace HuePat.Util.Time
{
    public class Timer {
        private const string SYSTEM_LIB = "Kernel32.dll";
        [DllImport(SYSTEM_LIB)]
        private static extern int QueryPerformanceCounter(ref long count);
        [DllImport(SYSTEM_LIB)]
        private static extern int QueryPerformanceFrequency(ref long frequency);

        private static Timer instance;

        public static Timer Instance {
            get {
                if (instance == null) {
                    instance = new Timer();
                }
                return instance;
            }
        }

        private readonly long frequency = 0;
        private readonly bool isPerformanceCounterSupported = false;

        public long Timestamp {
            get {
                return TickCount * 1000 / frequency;
            }
        }

        private long TickCount {
            get {
                if (isPerformanceCounterSupported) {
                    long tickCount = 0;
                    QueryPerformanceCounter(ref tickCount);
                    return tickCount;
                }
                else {
                    return Environment.TickCount;
                }
            }
        }

        private Timer() {
            if (QueryPerformanceFrequency(ref frequency) != 0
                    && frequency != 1000) {
                isPerformanceCounterSupported = true;
            }
            else {
                frequency = 1000;
            }
        }
    }
}