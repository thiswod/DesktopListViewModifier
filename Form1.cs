using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace DesktopListViewModifier
{
    public partial class Form1 : Form
    {
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

        // Windows API 函数
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
            string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        
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
            // 程序启动时检查注册表中的桌面视图设置
            CheckAndApplySavedView();
            
            // 尝试应用已保存的文字颜色设置
            ApplySavedTextColor();
        }
        /// <summary>
        /// 将桌面视图设置保存到注册表的多个关键位置以确保持久化
        /// </summary>
        /// <summary>
        /// 将桌面视图设置保存到注册表的多个关键位置以确保持久化
        /// </summary>
        /// <param name="useDetailsView">是否使用详细信息视图</param>
        /// <param name="restartExplorer">是否重启Explorer进程（默认为true）</param>
        private void SaveDesktopViewToRegistry(bool useDetailsView, bool restartExplorer = true)
        {
            try
            {
                // 1. 保存到Bags路径 - 控制视图类型的主要位置
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BagsDesktopPath, true))
                {
                    if (key != null)
                    {
                        // Mode值: 0=大图标, 4194304=详细信息
                        key.SetValue(BagsDesktopMode, useDetailsView ? 4194304 : 0, RegistryValueKind.DWord);
                        
                        // Vid值 - 视图标识符: 使用正确的GUID值
                        if (useDetailsView)
                        {
                            // 详细信息视图的特定GUID
                            key.SetValue(BagsDesktopVid, "{0057D0E0-3573-11CF-AE69-08002B2E1262}", RegistryValueKind.String);
                        }
                        else
                        {
                            // 大图标视图的特定GUID
                            key.SetValue(BagsDesktopVid, "{089000C0-ABF5-11CD-A9BA-00AA004A5691}", RegistryValueKind.String);
                        }
                        
                        // FFlags值 - 详细的视图控制标志
                        if (useDetailsView)
                        {
                            // 详细信息视图的完整标志值
                            key.SetValue(BagsDesktopFFlags, 0x43000001, RegistryValueKind.DWord);
                            
                            // 添加额外的关键值以确保详细信息视图正确显示
                            key.SetValue("LogicalViewMode", 3, RegistryValueKind.DWord);
                        }
                        else
                        {
                            // 大图标视图的完整标志值
                            key.SetValue(BagsDesktopFFlags, 0x40000001, RegistryValueKind.DWord);
                            key.SetValue("LogicalViewMode", 1, RegistryValueKind.DWord);
                        }
                    }
                }
                
                // 2. 保存到Streams路径
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ExplorerStreamsDesktop, true))
                {
                    if (key != null)
                    {
                        // 创建或更新二进制设置
                        byte[] settings = new byte[28];
                        // 设置必要的字节值 - 这是一个简化的Streams格式
                        settings[0] = 0x01; // 版本
                        settings[11] = useDetailsView ? (byte)0x30 : (byte)0x00; // 视图类型
                        settings[12] = 0x00;
                        settings[13] = 0x00;
                        settings[14] = 0x00;
                        settings[15] = 0x00;
                        
                        key.SetValue(ExplorerStreamsSettings, settings, RegistryValueKind.Binary);
                    }
                }
                
                // 3. 设置桌面视图缓存 - 清除现有的缓存
                ClearDesktopViewCache();
                
                // 4. 仅在需要时重启Explorer进程以确保设置生效
                // 避免递归调用导致无限循环
                if (restartExplorer)
                {
                    RestartExplorerAndApplyView(useDetailsView);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存注册表设置失败: {ex.Message}");
                MessageBox.Show($"保存设置时出错: {ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        /// <summary>
        /// 清除桌面视图缓存
        /// </summary>
        private void ClearDesktopViewCache()
        {
            try
            {
                // 尝试删除可能干扰的缓存项
                string[] cacheKeys = new string[]
                {
                    "Software\\Microsoft\\Windows\\Shell\\Bags\\AllFolders\\Shell",
                    "Software\\Microsoft\\Windows\\Shell\\Bags\\AllFolders\\ComDlg" 
                };
                
                foreach (string cacheKey in cacheKeys)
                {
                    try
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(cacheKey, false);
                    }
                    catch { /* 忽略不存在的键 */ }
                }
            }
            catch { /* 忽略缓存清理中的错误 */ }
        }
        
        /// <summary>
        /// 重启Explorer并在其启动后应用视图设置
        /// </summary>
        // 用于防止递归调用的静态标志
        private static bool _isRestartingExplorer = false;
        
        private async void RestartExplorerAndApplyView(bool useDetailsView)
        {
            // 使用静态标志防止递归调用
            if (_isRestartingExplorer) return;
            _isRestartingExplorer = true;
            
            try
            {
                // 关闭Explorer进程
                Process[] explorerProcesses = Process.GetProcessesByName("explorer");
                foreach (Process explorer in explorerProcesses)
                {
                    explorer.Kill();
                }
                
                // 等待一段时间确保进程已关闭
                System.Threading.Thread.Sleep(1000);
                
                // 重新启动Explorer进程
                Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\explorer.exe");
                
                // Explorer启动后异步应用视图设置
                await System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        // 等待Explorer完全启动
                        await System.Threading.Tasks.Task.Delay(5000); // 延长等待时间确保完全启动
                        
                        int retryCount = 0;
                        const int maxRetries = 1; // 减少重试次数以避免问题
                        bool viewApplied = false;
                        
                        // 多次尝试应用视图设置
                        while (retryCount <= maxRetries && !viewApplied)
                        {
                            IntPtr hDesktopListView = GetDesktopListViewHandle();
                            if (hDesktopListView != IntPtr.Zero)
                            {
                                // 应用视图设置
                                SendMessage(hDesktopListView, LVM_SETVIEW, useDetailsView ? LV_VIEW_DETAILS : LV_VIEW_LARGEICON, 0);
                                
                                // 对于详细信息视图，使用更强的确保策略
                                if (useDetailsView)
                                {
                                    // 额外添加一次设置以确保生效
                                    await System.Threading.Tasks.Task.Delay(300);
                                    SendMessage(hDesktopListView, LVM_SETVIEW, LV_VIEW_DETAILS, 0);
                                }
                                
                                // 强制刷新
                                ForceDesktopRefresh();
                                
                                // 给系统时间处理视图更改
                                await System.Threading.Tasks.Task.Delay(1000);
                                
                                // 标记为已应用
                                viewApplied = true;
                            }
                            else
                            {
                                // 未找到ListView句柄，增加重试计数并等待
                                retryCount++;
                                await System.Threading.Tasks.Task.Delay(2000);
                            }
                        }
                        
                        // 显示成功消息
                        if (viewApplied)
                        {
                            // 使用BeginInvoke确保在UI线程上显示消息框
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                string viewType = useDetailsView ? "详细信息" : "大图标";
                                MessageBox.Show($"桌面视图已成功设置为{viewType}模式并保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"应用视图设置失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重启Explorer失败: {ex.Message}");
                MessageBox.Show("重启Explorer进程失败，请手动重启explorer.exe以应用设置。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                // 确保重置标志
                _isRestartingExplorer = false;
            }
        }
        
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
                    Console.WriteLine($"当前文字颜色: {currentColor:X}");
                    
                    // 设置新颜色
                    int result = SendMessage(hDesktopListView, LVM_SETTEXTCOLOR, IntPtr.Zero, (IntPtr)color);
                    
                    if (result != 0)
                    {                        
                        // 保存颜色设置到注册表
                        SaveTextColorToRegistry(color);
                        
                        // 强制刷新以应用更改
                        ForceDesktopRefresh();
                        
                        Console.WriteLine($"文字颜色已设置为: {color:X}");
                        return true;
                    }
                    else
                    {                        
                        Console.WriteLine("设置文字颜色失败");
                    }
                }                
                else                
                {                    
                    Console.WriteLine("未找到桌面ListView句柄");                
                }
            }
            catch (Exception ex)            
            {                
                Console.WriteLine($"设置文字颜色时出错: {ex.Message}");            
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
                Console.WriteLine($"保存文字颜色设置失败: {ex.Message}");            
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
                Console.WriteLine($"应用文字颜色设置失败: {ex.Message}");            
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
                Console.WriteLine($"重启Explorer失败: {ex.Message}");
                MessageBox.Show("重启Explorer进程失败，请手动重启explorer.exe以应用设置。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        /// <summary>
        /// 检查并应用已保存的桌面视图设置
        /// </summary>
        private void CheckAndApplySavedView()
        {
            try
            {
                // 从Bags路径检查设置
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(BagsDesktopPath, false))
                {
                    if (key != null)
                    {
                        object modeValue = key.GetValue(BagsDesktopMode);
                        object logicalViewValue = key.GetValue("LogicalViewMode");
                        object vidValue = key.GetValue(BagsDesktopVid);
                        
                        // 多种方式检测是否为详细信息视图
                        bool useDetailsView = false;
                        
                        // 检查Mode值（4194304表示详细信息视图）
                        if (modeValue != null)
                        {
                            int mode = Convert.ToInt32(modeValue);
                            useDetailsView = (mode == 4194304);
                        }
                        
                        // 检查LogicalViewMode值（3表示详细信息视图）
                        if (logicalViewValue != null)
                        {
                            int logicalView = Convert.ToInt32(logicalViewValue);
                            if (logicalView == 3)
                                useDetailsView = true;
                        }
                        
                        // 检查Vid值（详细信息视图的GUID）
                        if (vidValue != null)
                        {
                            string vid = vidValue.ToString();
                            if (vid.Contains("0057D0E0-3573-11CF-AE69-08002B2E1262"))
                                useDetailsView = true;
                        }
                        
                        // 创建一个定时器，在Explorer完全加载后应用视图设置
                        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                        timer.Interval = 3000; // 增加到3秒确保Explorer完全加载
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            
                            // 自动应用保存的视图设置
                            IntPtr hDesktopListView = GetDesktopListViewHandle();
                            if (hDesktopListView != IntPtr.Zero)
                            {
                                // 应用视图设置
                                SendMessage(hDesktopListView, LVM_SETVIEW, useDetailsView ? LV_VIEW_DETAILS : LV_VIEW_LARGEICON, 0);
                                
                                // 如果是详细信息视图，再额外设置一次以确保生效
                                if (useDetailsView)
                                {
                                    // 延迟一小段时间后再次应用
                                    System.Threading.Thread.Sleep(500);
                                    SendMessage(hDesktopListView, LVM_SETVIEW, LV_VIEW_DETAILS, 0);
                                }
                                
                                ForceDesktopRefresh();
                            }
                        };
                        timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用已保存设置失败: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 首先显示提示，说明将要发生什么
                DialogResult result = MessageBox.Show(
                    "即将修改桌面视图为详细信息模式并重启资源管理器。\n此操作将保存设置并确保重启后保持设置。", 
                    "确认操作", 
                    MessageBoxButtons.OKCancel, 
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.OK)
                {
                    // 保存设置到注册表并重启Explorer以确保持久化
                    // 使用默认参数（true）重启Explorer
                    SaveDesktopViewToRegistry(true);
                    
                    // 由于Explorer会重启，我们不在这里显示成功消息，而是在重启后显示
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
                // 首先显示提示，说明将要发生什么                
                DialogResult result = MessageBox.Show(
                    "即将恢复桌面视图为大图标模式并重启资源管理器。\n此操作将保存设置并确保重启后保持设置。", 
                    "确认操作", 
                    MessageBoxButtons.OKCancel, 
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.OK)                
                {                    
                    // 保存设置到注册表并重启Explorer以确保持久化                    
                    // 使用默认参数（true）重启Explorer                    
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
                    MessageBox.Show("桌面文字颜色已成功设置为白色！\n适用于黑色背景的桌面。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);                
                }                
                else                
                {                    
                    MessageBox.Show("设置桌面文字颜色失败，可能需要管理员权限。\n请尝试以管理员身份运行程序。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);                
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
                    MessageBox.Show("设置桌面文字颜色失败，可能需要管理员权限。\n请尝试以管理员身份运行程序。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);                
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
