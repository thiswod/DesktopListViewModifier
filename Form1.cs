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

        // 仅保留必要的API声明，移除不再需要的常量
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {            
            // 简化版实现，不再检查注册表中的桌面视图设置
            
            // 尝试应用已保存的文字颜色设置
            ApplySavedTextColor();
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
                Console.WriteLine($"保存桌面视图设置失败: {ex.Message}");
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
                // 简化版：直接保存到注册表并提示重启
                SaveTextColorToRegistry(color);
                
                // 显示提示消息
                MessageBox.Show(
                    "文字颜色设置已保存到注册表。\n请重启资源管理器以应用更改。",
                    "提示", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                    
                return true;
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
                Console.WriteLine($"重启Explorer失败: {ex.Message}");
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
        /// <summary>
        /// 获取桌面ListView控件句柄
        /// 在简化版中不再需要具体实现
        /// </summary>
        private IntPtr GetDesktopListViewHandle()
        {
            return IntPtr.Zero;
        }
    }
}
