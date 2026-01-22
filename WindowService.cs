using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace KoReadingABook
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class WindowService
    {
        private readonly int _currentProcessId;

        public WindowService()
        {
            _currentProcessId = Environment.ProcessId;
        }

        public List<WindowInfo> GetTargetWindows()
        {
            var windows = new List<WindowInfo>();
            IntPtr currentForeground = NativeMethods.GetForegroundWindow();
            string currentTitle = GetWindowTitle(currentForeground);

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (IsTargetWindow(hWnd))
                {
                    // Exclude currently active window handle
                    if (hWnd == currentForeground) return true;

                    var title = GetWindowTitle(hWnd);
                    
                    // Exclude invalid titles or same title as current app (switching to same app type)
                    if (string.IsNullOrWhiteSpace(title)) return true;
                    if (title == currentTitle) return true;

                    windows.Add(new WindowInfo { Handle = hWnd, Title = title });
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        private bool IsTargetWindow(IntPtr hWnd)
        {
            // 1. Visible
            if (!NativeMethods.IsWindowVisible(hWnd)) return false;

            // 2. Not Minimized (IsIconic returns true if minimized)
            if (NativeMethods.IsIconic(hWnd)) return false;

            // 3. Check for specific exclusions (Progman, Shell_TrayWnd, Self)
            if (IsExcludedClass(hWnd)) return false;
            
            // 4. Self check (Process ID)
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            if (processId == _currentProcessId) return false;

            // 5. Toolwindow check
            long exStyle = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0) return false;

            // 6. Size check (Exclude tiny/invisible windows)
            if (NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width < 10 || height < 10) return false;
            }

            // 7. Title check
            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.Length == 0) return false;

            // Explicit Self Title Check
            string title = sb.ToString();
            if (title == "Reading a Book") return false;
            
            // Exclude System Apps per user request
            if (title == "設定" || title == "Settings") return false;
            if (title == "タスク マネージャー" || title == "Task Manager") return false;

            return true;
        }

        public string GetWindowTitle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return "None";
            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public IntPtr GetActiveWindowHandle()
        {
            return NativeMethods.GetForegroundWindow();
        }

        private bool IsExcludedClass(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
            string className = sb.ToString();

            return className == "Progman" || className == "Shell_TrayWnd";
        }

        public void ActivateWindow(IntPtr hWnd)
        {
            // 1. Restore if minimized
            if (NativeMethods.IsIconic(hWnd))
            {
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            }

            // 2. Alt Key Trick (Simulate user interaction to bypass ForegroundLock)
            // Press Alt
            NativeMethods.keybd_event(0x12, 0, 0, UIntPtr.Zero);
            
            // 3. Attach Thread Input
            uint currentThreadId = NativeMethods.GetCurrentThreadId();
            uint targetThreadId = NativeMethods.GetWindowThreadProcessId(hWnd, out _);
            bool attached = false;

            if (currentThreadId != targetThreadId)
            {
                attached = NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, true);
            }

            // 4. Force to Top
            NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
            
            NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_NOTOPMOST, 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);

            NativeMethods.SetForegroundWindow(hWnd);
            NativeMethods.BringWindowToTop(hWnd);

            // Cleanup
            if (attached)
            {
                NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, false);
            }
            
            // Release Alt
            NativeMethods.keybd_event(0x12, 0, 0x0002, UIntPtr.Zero);
        }
    }
}
