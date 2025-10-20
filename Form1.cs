using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace DesktopListViewModifier
{
    public partial class Form1 : Form
    {
        // 全局定时器，用于定期检查并应用文字颜色设置
        private System.Windows.Forms.Timer _colorMonitorTimer;
        
        // Windows API 常量
        private const int LVM_FIRST = 0x1000;
        private const int LVM_SETVIEW = LVM_FIRST + 142;
        private const int LVM_GETTEXTCOLOR = LVM_FIRST + 35;
        private const int LVM_SETTEXTCOLOR = LVM_FIRST + 36;
        private const int LV_VIEW_DETAILS = 0x0001;
        private const int LV_VIEW_LARGEICON = 0x0000;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const uint SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_IDLIST = 0x0000; // 使用PIDL格式
        
        // 注册表路径 - 用于保存文字颜色设置
        private const string DesktopTextColorPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
        private const string DesktopTextColorValueName = "ListViewTextColor"; // 自定义值名用于保存文字颜色
        
        // 注册表路径
        private const string BagsDesktopPath = "Software\\Microsoft\\Windows\\Shell\\Bags\\1\\Desktop";
        private const string BagsDesktopFFlags = "FFlags";
        private const string BagsDesktopMode = "Mode";
        private const string BagsDesktopVid = "Vid";
        private const string ExplorerStreamsDesktop = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Streams\\Desktop";
        private const string ExplorerStreamsSettings = "Settings";
        private const string ExplorerViewsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\Folder\\NavPane\\FolderTypes";
        private const string DesktopSortPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace";

        // Windows API声明
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessageIntParam(IntPtr hWnd, int Msg, IntPtr wParam, int lParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SendNotifyMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);
        
        public Form1()
        {
            InitializeComponent();
            
            // 初始化颜色监控定时器
            _colorMonitorTimer = new System.Windows.Forms.Timer();
            _colorMonitorTimer.Interval = 3000; // 每3秒检查一次
            _colorMonitorTimer.Tick += ColorMonitorTimer_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 简化版实现，不再检查注册表中的桌面视图设置
            ApplySavedTextColor();
            
            // 启动颜色监控定时器
            _colorMonitorTimer.Start();
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 停止定时器以释放资源
            _colorMonitorTimer?.Stop();
        }
        
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 清理定时器资源
            _colorMonitorTimer?.Dispose();
        }
        /// <summary>
        /// 保存桌面视图设置到注册表
        /// </summary>
        private void SaveDesktopViewToRegistry(bool useDetailsView)
        {
            try
            {
                // 创建或打开BagsDesktop注册表项
                using (RegistryKey bagsDesktopKey = Registry.CurrentUser.CreateSubKey(BagsDesktopPath))
                {
                    if (bagsDesktopKey != null)
                    {
                        // 设置相应的FFlags值
                        if (useDetailsView)
                        {
                            // 详细信息视图的FFlags值
                            bagsDesktopKey.SetValue("FFlags", 1126170625, RegistryValueKind.DWord);
                        }
                        else
                        {
                            // 大图标视图的FFlags值（用户确认有效的值）
                            bagsDesktopKey.SetValue("FFlags", 1075839524, RegistryValueKind.DWord);
                        }
                    }
                }
                
                // 重启Explorer以应用设置
                RestartExplorer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存桌面视图设置失败: {ex.Message}");
                throw;
            }
        }

        // 简化版实现，不再需要复杂的视图缓存清理和异步应用视图方法

        /// <summary>
        /// 设置桌面图标的文字颜色
        /// </summary>
        /// <param name="color">要设置的颜色（RGB格式）</param>
        /// <returns>是否成功设置颜色</returns>
        private bool SetDesktopTextColor(int color)
        {
            try
            {
                IntPtr hDesktopListView = GetDesktopListViewHandle();
                if (hDesktopListView != IntPtr.Zero)
                {
                    // 获取当前颜色
                    int currentColor = SendMessage(hDesktopListView, LVM_GETTEXTCOLOR, IntPtr.Zero, IntPtr.Zero);
                    Debug.WriteLine($"当前文字颜色: {currentColor:X}");

                    // 设置新颜色
                    int result = SendMessage(hDesktopListView, LVM_SETTEXTCOLOR, IntPtr.Zero, (IntPtr)color);

                    if (result != 0)
                    {
                        // 保存颜色设置到注册表
                        SaveTextColorToRegistry(color);

                        Debug.WriteLine($"文字颜色已设置为: {color:X}");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("设置文字颜色失败");
                    }
                }
                else
                 {
                    Debug.WriteLine("未找到桌面ListView句柄");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置文字颜色时出错: {ex.Message}");
            }
            return false;
        }
        /// <summary>
        /// 将文字颜色设置保存到注册表
        /// </summary>
        /// <param name="color">RGB颜色值</param>
        private void SaveTextColorToRegistry(int color)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(DesktopTextColorPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue(DesktopTextColorValueName, color, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存文字颜色设置失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 应用已保存的文字颜色设置
        /// </summary>
        private void ApplySavedTextColor()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopTextColorPath, false))
                {
                    if (key != null)
                    {
                        object colorValue = key.GetValue(DesktopTextColorValueName);
                        if (colorValue != null)
                        {
                            int color = Convert.ToInt32(colorValue);
                            // 创建定时器，在Explorer完全加载后应用颜色设置                            
                            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                            timer.Interval = 4000; // 等待Explorer完全加载                            
                            timer.Tick += (s, e) =>
                            {
                                timer.Stop();
                                // 应用已保存的颜色设置                                
                                SetDesktopTextColor(color);
                            };
                            timer.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"应用文字颜色设置失败: {ex.Message}");
            }
        }
        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, int wParam, string lParam, uint fuFlags, uint uTimeout, out uint lpdwResult);
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        /// <summary>
        /// 刷新桌面
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

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 首先显示提示，说明将要发生什么
                DialogResult result = MessageBox.Show(
                    "即将修改桌面视图为详细信息模式并重启资源管理器。\n重启后请在桌面右键菜单中进行适当设置。", 
                    "确认操作", 
                    MessageBoxButtons.OKCancel, 
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.OK)
                {
                    // 保存设置到注册表并重启Explorer
                    SaveDesktopViewToRegistry(true);
                    
                    // 由于Explorer会重启，我们不在这里显示成功消息
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 重启Windows Explorer进程以应用设置
        /// </summary>
        private void RestartExplorer()
        {
            try
            {
                // 关闭Explorer进程
                Process[] explorerProcesses = Process.GetProcessesByName("explorer");
                foreach (Process explorer in explorerProcesses)
                {
                    explorer.Kill();
                }
                
                // 等待一段时间确保进程已关闭
                System.Threading.Thread.Sleep(500);
                
                // 重新启动Explorer进程
                Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\explorer.exe");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"重启Explorer失败: {ex.Message}");
                MessageBox.Show("重启Explorer进程失败，请手动重启explorer.exe以应用设置。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {            
            try            
            {                
                // 首先显示提示，说明将要发生什么                
                DialogResult result = MessageBox.Show(
                    "即将恢复桌面视图为大图标模式并重启资源管理器。", 
                    "确认操作", 
                    MessageBoxButtons.OKCancel, 
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.OK)                
                {                    
                    // 保存设置到注册表并重启Explorer                    
                    SaveDesktopViewToRegistry(false);
                    
                    // 由于Explorer会重启，我们不在这里显示成功消息                
                }            
            }            
            catch (Exception ex)            
            {                
                MessageBox.Show($"操作失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);            
            }        
        }
        
        /// <summary>
        /// 设置桌面文字为白色（用于黑色背景）
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {            
            try            
            {                
                // 白色的RGB值
                int whiteColor = 0xFFFFFF; // 白色
                
                if (SetDesktopTextColor(whiteColor))                
                {
                    RefreshDesktop();
                    MessageBox.Show("桌面文字颜色已成功设置为白色！\n适用于黑色背景的桌面。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);                
                }                
                else                
                {                    
                    MessageBox.Show("设置桌面文字颜色失败，可能需要管理员权限。\n请尝试以管理员身份运行程序。\n\n如果仍无法工作，请尝试手动刷新桌面（F5）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);                
                }            
            }            
            catch (Exception ex)            
            {                
                MessageBox.Show($"操作失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);            
            }        
        }
        
        /// <summary>
        /// 设置桌面文字为黑色（默认颜色，用于浅色背景）
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {            
            try            
            {                
                // 黑色的RGB值（或者让系统使用默认值）
                int blackColor = 0x000000; // 黑色
                
                if (SetDesktopTextColor(blackColor))                
                {                    
                    MessageBox.Show("桌面文字颜色已成功设置为黑色！\n适用于浅色背景的桌面。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);                
                }                
                else                
                {                    
                    MessageBox.Show("设置桌面文字颜色失败，可能需要管理员权限。\n请尝试以管理员身份运行程序。\n\n如果仍无法工作，请尝试手动刷新桌面（F5）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);                
                }            
            }            
            catch (Exception ex)            
            {                
                MessageBox.Show($"操作失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);            
            }        
        }
        /// <summary>
        /// 获取桌面ListView控件句柄
        /// 在简化版中不再需要具体实现
        /// </summary>
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

        /// <summary>
        /// 颜色监控定时器Tick事件处理程序
        /// 定期检查并应用保存的文字颜色设置
        /// </summary>
        private void ColorMonitorTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 从注册表获取保存的颜色设置
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopTextColorPath, false))
                {
                    if (key != null)
                    {
                        object colorValue = key.GetValue(DesktopTextColorValueName);
                        if (colorValue != null)
                        {
                            int savedColor = Convert.ToInt32(colorValue);
                            
                            // 获取当前ListView的颜色
                            IntPtr hDesktopListView = GetDesktopListViewHandle();
                            if (hDesktopListView != IntPtr.Zero)
                            {
                                int currentColor = SendMessage(hDesktopListView, LVM_GETTEXTCOLOR, IntPtr.Zero, IntPtr.Zero);
                                
                                // 如果当前颜色与保存的颜色不同，则重新应用
                                if (currentColor != savedColor)
                                {
                                    RefreshDesktop();
                                    Debug.WriteLine($"检测到颜色变化，重新应用保存的颜色: {savedColor:X}");
                                    SetDesktopTextColor(savedColor);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"颜色监控定时器出错: {ex.Message}");
            }
        }
        
        // 枚举窗口的委托
        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
        
        // EnumWindows API
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    }
}
