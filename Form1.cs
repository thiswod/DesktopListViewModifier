using System.Runtime.InteropServices;

namespace DesktopListViewModifier
{
    public partial class Form1 : Form
    {
        // Windows API 常量
        private const int LVM_FIRST = 0x1000;
        private const int LVM_SETVIEW = LVM_FIRST + 142;
        private const int LV_VIEW_DETAILS = 0x0001;
        private const int LV_VIEW_LARGEICON = 0x0000;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const uint SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_IDLIST = 0x0000; // 使用PIDL格式

        // Windows API 函数
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
            string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, int wParam, string lParam, uint fuFlags, uint uTimeout, out uint lpdwResult);
        
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取桌面ListView句柄
                IntPtr hDesktopListView = GetDesktopListViewHandle();

                if (hDesktopListView != IntPtr.Zero)
                {
                    // 先保存当前窗口状态
                    IntPtr hDesktop = GetDesktopWindow();
                    
                    // 分两步设置详细信息视图：首先设置视图，然后强制刷新
                    SendMessage(hDesktopListView, LVM_SETVIEW, LV_VIEW_DETAILS, 0);
                    
                    // 强制ListView更新其内部状态
                    SendMessage(hDesktopListView, 0x000B, 0, 0); // WM_SETREDRAW = FALSE
                    SendMessage(hDesktopListView, 0x000B, 1, 0); // WM_SETREDRAW = TRUE
                    
                    // 立即刷新ListView
                    RedrawWindow(hDesktopListView, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0002 | 0x0400);
                    
                    // 添加短暂延迟，确保系统有时间处理视图更改
                    System.Threading.Thread.Sleep(50);
                    
                    // 使用更强大的刷新方法
                    ForceDesktopRefresh();
                    
                    MessageBox.Show("已成功修改桌面视图模式为详细信息视图。\n需要手动刷新一下桌面。",
                        "操作成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("找不到桌面ListView控件！", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 强制刷新桌面，特别针对详细信息视图模式
        /// </summary>
        private void ForceDesktopRefresh()
        {
            try
            {
                // 获取关键窗口句柄
                IntPtr hProgman = FindWindow("Progman", "Program Manager");
                IntPtr hDesktopListView = GetDesktopListViewHandle();
                
                // 1. 刷新资源管理器相关进程
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                
                // 2. 向所有顶级窗口广播设置更改消息
                SendMessageTimeout((IntPtr)(-1), WM_SETTINGCHANGE, 0, "Shell", 0x0002, 500, out _);
                
                // 3. 强制重绘ListView和桌面
                if (hDesktopListView != IntPtr.Zero)
                {
                    // 使用多种重绘方式
                    RedrawWindow(hDesktopListView, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0400);
                    SendMessage(hDesktopListView, 0x000F, 0, 0); // WM_PAINT
                }
                
                // 4. 刷新Program Manager窗口
                if (hProgman != IntPtr.Zero)
                {
                    RedrawWindow(hProgman, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0002 | 0x0400);
                }
                
                // 5. 刷新桌面窗口
                IntPtr hDesktop = GetDesktopWindow();
                if (hDesktop != IntPtr.Zero)
                {
                    RedrawWindow(hDesktop, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0400);
                }
                
                // 6. 最后一步：强制刷新整个屏幕
                System.Windows.Forms.SendKeys.SendWait("{F5}");
            }
            catch { /* 忽略刷新过程中的错误 */ }
        }
        
        /// <summary>
        /// 刷新桌面以更新视图显示
        /// </summary>
        private void RefreshDesktop()
        {
            try
            {
                // 针对ListView控件的特殊刷新处理
                IntPtr hDesktopListView = GetDesktopListViewHandle();
                if (hDesktopListView != IntPtr.Zero)
                {
                    // 先发送LVN_ITEMCHANGED通知
                    IntPtr lParam = IntPtr.Zero;
                    SendMessage(hDesktopListView, 0x1000 + 2, 0, 0); // WM_NOTIFY + LVN_ITEMCHANGED
                    
                    // 强制重绘ListView控件
                    RedrawWindow(hDesktopListView, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0002 | 0x0400);
                }
                
                // 方法1: 发送WM_SETTINGCHANGE消息到所有顶级窗口
                SendMessageTimeout((IntPtr)(-1), WM_SETTINGCHANGE, 0, "Environment", 0x0002, 500, out _);
                
                // 方法2: 使用SHChangeNotify通知外壳更新
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                
                // 方法3: 强制重绘桌面
                IntPtr hDesktop = GetDesktopWindow();
                if (hDesktop != IntPtr.Zero)
                {
                    // 使用更全面的重绘标志
                    RedrawWindow(hDesktop, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0400);
                }
                
                // 添加延迟后再次刷新，确保详细信息视图完全更新
                System.Threading.Thread.Sleep(100);
                
                // 二次刷新以确保详细信息视图的列名正确显示
                if (hDesktopListView != IntPtr.Zero)
                {
                    RedrawWindow(hDesktopListView, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0002 | 0x0400);
                }
                
                // 刷新资源管理器窗口
                IntPtr hProgman = FindWindow("Progman", "Program Manager");
                if (hProgman != IntPtr.Zero)
                {
                    SendMessage(hProgman, 0x000F, 0, 0); // WM_PAINT
                }
            }
            catch { /* 忽略刷新过程中的错误 */ }
        }
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        
        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取桌面ListView句柄
                IntPtr hDesktopListView = GetDesktopListViewHandle();

                if (hDesktopListView != IntPtr.Zero)
                {
                    // 恢复默认大图标视图模式
                    SendMessage(hDesktopListView, LVM_SETVIEW, LV_VIEW_LARGEICON, 0);
                    
                    // 强制ListView更新其内部状态
                    SendMessage(hDesktopListView, 0x000B, 0, 0); // WM_SETREDRAW = FALSE
                    SendMessage(hDesktopListView, 0x000B, 1, 0); // WM_SETREDRAW = TRUE
                    
                    // 添加短暂延迟
                    System.Threading.Thread.Sleep(50);
                    
                    // 使用强制刷新方法
                    ForceDesktopRefresh();
                    
                    MessageBox.Show("已成功恢复桌面图标为默认大图标视图\n桌面已自动刷新。",
                        "操作成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("找不到桌面ListView控件！", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private IntPtr GetDesktopListViewHandle()
        {
            // 查找桌面窗口
            IntPtr hProgman = FindWindow("Progman", "Program Manager");
            if (hProgman == IntPtr.Zero) return IntPtr.Zero;

            // 在Windows 10/11中，桌面ListView位于SHELLDLL_DefView窗口中
            IntPtr hShellDefView = FindWindowEx(hProgman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (hShellDefView == IntPtr.Zero)
            {
                // 尝试备用方法：查找WorkerW窗口
                IntPtr hWorkerW = IntPtr.Zero;
                while ((hWorkerW = FindWindowEx(IntPtr.Zero, hWorkerW, "WorkerW", null)) != IntPtr.Zero)
                {
                    hShellDefView = FindWindowEx(hWorkerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (hShellDefView != IntPtr.Zero) break;
                }
            }

            if (hShellDefView == IntPtr.Zero) return IntPtr.Zero;

            // 获取ListView控件
            return FindWindowEx(hShellDefView, IntPtr.Zero, "SysListView32", "FolderView");
        }
    }
}
