using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace wechatmsg
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


    public class Win32ServiceImpl
    {
       
        public int GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return Win32NativeUtil.GetWindowLong(hWnd, nIndex);
        }

        public  bool PostMessage(int hWnd, uint Msg, int wParam, int lParam)
        {
            return Win32NativeUtil.PostMessage(new IntPtr(hWnd), Msg, wParam, lParam);
        }

        public int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            return Win32NativeUtil.SetWindowLong(hWnd, nIndex, dwNewLong);
        }

        public bool ShowWindow(IntPtr hWnd, int flags)
        {
            return Win32NativeUtil.ShowWindow(hWnd, flags);
        }

        public void HideInTaskbar(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, Win32Const.GWL_STYLE);
            style |= Win32Const.WS_EX_TOOLWINDOW;
            style &= ~(Win32Const.WS_EX_APPWINDOW);
            bool visible = Win32NativeUtil.IsWindowVisible(hwnd);
            ShowWindow(hwnd, Win32Const.SW_HIDE);
            SetWindowLong(hwnd, Win32Const.GWL_STYLE, style);
            if(visible)
            {
                ShowWindow(hwnd, Win32Const.SW_SHOW);
            }
        }

        public void ChangePosition(IntPtr hwnd, int x, int y)
        {
            Win32NativeUtil.SetWindowPos(hwnd, 0, x, y, 0, 0, (uint)(SetWindowPosFlags.IgnoreResize | SetWindowPosFlags.IgnoreZOrder ));
        }

        public void ResizeWindow(IntPtr hwnd, int w, int h)
        {
            Win32NativeUtil.SetWindowPos(hwnd, 0, 0, 0, w, h, (uint)(SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreZOrder));
        }

        public IntPtr FindWindow(string clazz, string title)
        {
           return Win32NativeUtil.FindWindowW(clazz, title);
        }

        public bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab)
        {
            return Win32NativeUtil.SwitchToThisWindow(hWnd, fAltTab);
        }


        public int SetForegroundWindow(IntPtr hWnd)
        {
            return Win32NativeUtil.SetForegroundWindow(hWnd);
        }

        public string GetClassname(IntPtr hwnd)
        {
            StringBuilder sb = new StringBuilder(256);
            if (Win32NativeUtil.GetClassName(hwnd, sb, sb.Capacity) != 0)
            {
                return sb.ToString();
            }
            return null;
        }

        public string GetTitle(IntPtr hwnd)
        {
            int capacity = Win32NativeUtil.GetWindowTextLength(hwnd) * 2;
            if(capacity == 0)
            {
                capacity = 256;
            }
            StringBuilder stringBuilder = new StringBuilder(capacity);
            Win32NativeUtil.GetWindowText(hwnd, stringBuilder, stringBuilder.Capacity);
            string title= stringBuilder.ToString();
            if(string.IsNullOrEmpty(title))
            {
                title= GetControlText(hwnd.ToInt32());
            }
            return title;
        }

        public string GetControlText(int control)
        {
            //uint WM_GETTEXTLENGTH = 0x000E;
            uint WM_GETTEXT = 0x000D;
            //int len = Win32NativeUtil.SendMessage(control, WM_GETTEXTLENGTH, 0, 0) + 1;
            StringBuilder sb = new StringBuilder(256);
            Win32NativeUtil.SendMessage(control, WM_GETTEXT, 256, sb);
            return sb.ToString();
        }

        public int GetParent(int hwnd)
        {
            return Win32NativeUtil.GetParent(hwnd);
        }

        public void Topmot(IntPtr hwnd)
        {
            Win32NativeUtil.SetWindowPos(hwnd, -1, 0, 0, 0, 0, (uint)(0x0001 | 0x0002 | 0x0040 | 0x4000));
        }

        public void CloseWindow(IntPtr hwnd)
        {
            int WM_SYSCOMMAND = 0x0112;
            int SC_CLOSE = 0xF060;
            Win32NativeUtil.PostMessage(hwnd, (uint)WM_SYSCOMMAND, SC_CLOSE, 0);
        }


        public bool IsWindowVisible(IntPtr hwnd)
        {
           return Win32NativeUtil.IsWindowVisible(hwnd);
        }

        public bool IsModalWindow(IntPtr hwnd)
        {
            // child windows cannot have owners
            Win32NativeUtil.WINDOWINFO info = new Win32NativeUtil.WINDOWINFO();
            Win32NativeUtil.GetWindowInfo(hwnd, ref info);
            long WS_CHILD = 0x40000000L;
            long WS_DISABLED = 0x08000000L;
            if ((info.dwStyle & WS_CHILD) != 0) return false;

            IntPtr hwndOwner = Win32NativeUtil.GetWindow(hwnd, Win32NativeUtil.GetWindow_Cmd.GW_OWNER);
            if (hwndOwner == IntPtr.Zero) return false; // not an owned window

            Win32NativeUtil.GetWindowInfo(hwndOwner, ref info);

            long WS_POPUP = 0x80000000L;
            if ((info.dwStyle & WS_DISABLED & WS_POPUP) != 0)
            {
                return false;
            }
            return true; // an owned window whose owner is disabled
        }

        public bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam)
        {
             return Win32NativeUtil.EnumChildWindows(hwndParent, lpEnumFunc, lParam);
        }


        private List<int> childrenWindows;
        public int FindDescendantHwndByClazz(IntPtr parent, string clazz)
        {
            childrenWindows = new List<int>();
            Win32NativeUtil.EnumChildWindows(parent, listAllChildren, IntPtr.Zero);
            foreach (IntPtr child in childrenWindows)
            {
                string clazzCurrent = this.GetClassname(child);
                if (clazz.Equals(clazzCurrent))
                {
                    return child.ToInt32();
                }
            }
            return 0;
        }

        public int FindDescendantHwndByTitle(IntPtr parent, string title)
        {
            childrenWindows = new List<int>();
            Win32NativeUtil.EnumChildWindows(parent, listAllChildren, IntPtr.Zero);

            StringBuilder sb = new StringBuilder(256);
            foreach (IntPtr child in childrenWindows)
            {
                string title0 = this.GetTitle(child);
                if (title.Equals(title0))
                {
                    return child.ToInt32();
                }
            }
            return 0;
        }

        private bool listAllChildren(IntPtr hWnd, IntPtr lParam)
        {
            childrenWindows.Add(hWnd.ToInt32());
            return true;
        }

        public void SendClick(IntPtr hwnd, int localX, int localY)
        {
            mouseTo(hwnd.ToInt32(), localX, localY, 0);
            //  mouseTo(hwnd, x, y, 1);
            mouseTo(hwnd.ToInt32(), localX, localY, 2);
        }


        /**
        * <summary>真实的鼠标click</summary>
        */
        public void MouseClickGlobal(int screenX, int screenY)
        {
            double screenWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            double screenHeigth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;

            int dx = (int)(screenX * 65535 / screenWidth);
            int dy = (int)(screenY * 65535 / screenHeigth);
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_MOVE | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//移动到需要点击的位置
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_LEFTDOWN | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//点击
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_LEFTUP | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//抬起
        }

        /**
         * <summary>真实的鼠标click</summary>
         */
        public void MouseClick(IntPtr hwnd, int localX, int localY)
        {
            double screenWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            double screenHeigth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            Win32NativeUtil.Win32RECT rect = new Win32NativeUtil.Win32RECT();
            Win32NativeUtil.GetWindowRect(hwnd, out rect);
            double screenX = rect.Left + localX;
            double screenY = rect.Top + localY;
            int dx = (int)(screenX * 65535 / screenWidth);
            int dy = (int)(screenY * 65535 / screenHeigth);
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_MOVE | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//移动到需要点击的位置
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_LEFTDOWN | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//点击
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_LEFTUP | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//抬起
        }

        /**
        * <summary>真实的鼠标click</summary>
        */
        public void MouseClickRight(IntPtr hwnd, int localX, int localY)
        {
            double screenWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            double screenHeigth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            Win32NativeUtil.Win32RECT rect = new Win32NativeUtil.Win32RECT();
            Win32NativeUtil.GetWindowRect(hwnd, out rect);
            double screenX = rect.Left + localX;
            double screenY = rect.Top + localY;
            int dx = (int)(screenX * 65535 / screenWidth);
            int dy = (int)(screenY * 65535 / screenHeigth);
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_MOVE | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//移动到需要点击的位置
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_RIGHTDOWN | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//点击
            Win32NativeUtil.mouse_event(Win32NativeUtil.MOUSEEVENTF_RIGHTUP | Win32NativeUtil.MOUSEEVENTF_ABSOLUTE, dx, dy, 0, 0);//抬起
        }

        public void SendKeyDownAndUp(int hwnd, Key key)
        {
            var keyMsg = KeyInterop.VirtualKeyFromKey(key);
            Win32NativeUtil.SendMessage(hwnd, 0x0100, keyMsg, 0);
            Win32NativeUtil.SendMessage(hwnd, 0x0101, keyMsg, 0);
        }

        private void mouseTo(int hwnd, int x, int y, int status)
        {
            int mouseDownCode = 0x01;
            uint moveCODE = 0x0200;
            int paramW = status != 2 ? mouseDownCode : 0;
            uint code = status == 0 ? (uint)0x201 : (status == 1 ? moveCODE : (uint)0x202); // Left click up code 
            Win32NativeUtil.SendMessage(hwnd, code, paramW, x + (y << 16)); // Mouse button up 
            Console.WriteLine("code:" + code + " down:" + paramW);
        }
    }

    public class Win32NativeUtil
    {
        public enum TernaryRasterOperations : uint
        {
            /// <summary>dest = source</summary>
            // ReSharper disable once InconsistentNaming
            SRCCOPY = 0x00CC0020

            ///// <summary>dest = source OR dest</summary>
            //SRCPAINT = 0x00EE0086,

            ///// <summary>dest = source AND dest</summary>
            //SRCAND = 0x008800C6,

            ///// <summary>dest = source XOR dest</summary>
            //SRCINVERT = 0x00660046,

            ///// <summary>dest = source AND (NOT dest)</summary>
            //SRCERASE = 0x00440328,

            ///// <summary>dest = (NOT source)</summary>
            //NOTSRCCOPY = 0x00330008,

            ///// <summary>dest = (NOT src) AND (NOT dest)</summary>
            //NOTSRCERASE = 0x001100A6,

            ///// <summary>dest = (source AND pattern)</summary>
            //MERGECOPY = 0x00C000CA,

            ///// <summary>dest = (NOT source) OR dest</summary>
            //MERGEPAINT = 0x00BB0226,

            ///// <summary>dest = pattern</summary>
            //PATCOPY = 0x00F00021,

            ///// <summary>dest = DPSnoo</summary>
            //PATPAINT = 0x00FB0A09,

            ///// <summary>dest = pattern XOR dest</summary>
            //PATINVERT = 0x005A0049,

            ///// <summary>dest = (NOT dest)</summary>
            //DSTINVERT = 0x00550009,

            ///// <summary>dest = BLACK</summary>
            //BLACKNESS = 0x00000042,

            ///// <summary>dest = WHITE</summary>
            //WHITENESS = 0x00FF0062
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr memcmp(IntPtr b1, IntPtr b2, int count);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc,
             int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int GetDlgCtrlID(int hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetDlgItemText(int dialog, int nIDDlgItem, [Out] StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int flags);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetParent(int hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "FindWindowW")]
        public static extern System.IntPtr FindWindowW([System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPTStr)] string lpClassName, [System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPTStr)] string lpWindowName);

        [DllImport("user32")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab);


        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        //GetWindowRect返回指定窗口的边框矩形的尺寸。该尺寸以相对于屏幕坐标左上角的屏幕坐标给出
        public static extern int GetWindowRect(IntPtr hwnd, out Win32RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(int hwnd, uint wMsg, int wParam, StringBuilder lParam);


        [DllImport("user32.dll")]
        public static extern int GetDesktopWindow();


        [StructLayout(LayoutKind.Sequential)]
        public struct Win32RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWINFO
        {
            public uint cbSize;
            public Win32RECT rcWindow;
            public Win32RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public WINDOWINFO(Boolean? filler)
                : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
            {
                cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
            }

        }


        public enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

       public static  readonly int MOUSEEVENTF_LEFTDOWN = 0x0002;//模拟鼠标移动
        public static readonly int MOUSEEVENTF_MOVE = 0x0001;//模拟鼠标左键按下
        public static readonly int MOUSEEVENTF_LEFTUP = 0x0004;//模拟鼠标左键抬起
        public static  readonly int MOUSEEVENTF_ABSOLUTE = 0x8000;//鼠标绝对位置
        public static  readonly int MOUSEEVENTF_RIGHTDOWN = 0x0008; //模拟鼠标右键按下 
        public static  readonly int MOUSEEVENTF_RIGHTUP = 0x0010; //模拟鼠标右键抬起 
        public static readonly int MOUSEEVENTF_MIDDLEDOWN = 0x0020; //模拟鼠标中键按下 
        public static readonly int MOUSEEVENTF_MIDDLEUP = 0x0040;// 模拟鼠标中键抬起 
    }

    public enum SetWindowPosFlags : uint
    {
        /// <summary>If the calling thread and the thread that owns the window are attached to different input queues, 
        /// the system posts the request to the thread that owns the window. This prevents the calling thread from 
        /// blocking its execution while other threads process the request.</summary>
        /// <remarks>SWP_ASYNCWINDOWPOS</remarks>
        SynchronousWindowPosition = 0x4000,
        /// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
        /// <remarks>SWP_DEFERERASE</remarks>
        DeferErase = 0x2000,
        /// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
        /// <remarks>SWP_DRAWFRAME</remarks>
        DrawFrame = 0x0020,
        /// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to 
        /// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE 
        /// is sent only when the window's size is being changed.</summary>
        /// <remarks>SWP_FRAMECHANGED</remarks>
        FrameChanged = 0x0020,
        /// <summary>Hides the window.</summary>
        /// <remarks>SWP_HIDEWINDOW</remarks>
        HideWindow = 0x0080,
        /// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the 
        /// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter 
        /// parameter).</summary>
        /// <remarks>SWP_NOACTIVATE</remarks>
        DoNotActivate = 0x0010,
        /// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid 
        /// contents of the client area are saved and copied back into the client area after the window is sized or 
        /// repositioned.</summary>
        /// <remarks>SWP_NOCOPYBITS</remarks>
        DoNotCopyBits = 0x0100,
        /// <summary>Retains the current position (ignores X and Y parameters).</summary>
        /// <remarks>SWP_NOMOVE</remarks>
        IgnoreMove = 0x0002,
        /// <summary>Does not change the owner window's position in the Z order.</summary>
        /// <remarks>SWP_NOOWNERZORDER</remarks>
        DoNotChangeOwnerZOrder = 0x0200,
        /// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to 
        /// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent 
        /// window uncovered as a result of the window being moved. When this flag is set, the application must 
        /// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
        /// <remarks>SWP_NOREDRAW</remarks>
        DoNotRedraw = 0x0008,
        /// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
        /// <remarks>SWP_NOREPOSITION</remarks>
        DoNotReposition = 0x0200,
        /// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
        /// <remarks>SWP_NOSENDCHANGING</remarks>
        DoNotSendChangingEvent = 0x0400,
        /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
        /// <remarks>SWP_NOSIZE</remarks>
        IgnoreResize = 0x0001,
        /// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
        /// <remarks>SWP_NOZORDER</remarks>
        IgnoreZOrder = 0x0004,
        /// <summary>Displays the window.</summary>
        /// <remarks>SWP_SHOWWINDOW</remarks>
        ShowWindow = 0x0040,
    }

    public static class Win32Const
    {
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const int GWL_STYLE = (-16);
        public const int GWL_EXSTYLE = (-20);
        public const int LWA_ALPHA = 0x2;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_THICKFRAME = 0x00040000;
    }
}
