using System;

namespace TetoteCoreFixes {
    public static class Native {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public static bool IsTouchEnabled() {
            const int maxtouchesIndex = 95;
            int maxTouches = GetSystemMetrics(maxtouchesIndex);

            return maxTouches > 0;
        }

        public static bool IsDiskFull(Exception ex) {
            const int HR_ERROR_HANDLE_DISK_FULL = unchecked((int)0x80070027);
            const int HR_ERROR_DISK_FULL = unchecked((int)0x80070070);

            return ex.HResult == HR_ERROR_HANDLE_DISK_FULL
                   || ex.HResult == HR_ERROR_DISK_FULL;
        }
    }
}