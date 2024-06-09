using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using WindowsInput;
using WindowsInput.Native;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;




namespace DesktopRecording
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private System.Threading.Timer _timer;
        private string appPath, videoAppPath, videoSavePath, videoConfigPath, videoProcessName = "bdcam";
        public MainWindow()
        {
            InitializeComponent();
            appPath = AppDomain.CurrentDomain.BaseDirectory;
            videoAppPath = $"{appPath}Bandicam\\BandicamPortable.exe"; // 指定要打开的应用程序的路径
            videoConfigPath = $"{appPath}Bandicam\\Data\\Bandicam\\Bandicam.reg";// 指定要打开的应用程序配置的路径
            StartMonitoring();//定义不间断监控录频软件
            
            if (IsStartupSet("DesktopRecording"))
            {
                开机启动.Content = "取消开机启动";
            }
            else
            {
                开机启动.Content = "设置开机启动";
            }
            //Loaded += MainWindow_Loaded;  这里是最小化，先不开，测试好了之后再开
        }
        //Bandicam\\BandicamPortable.exe
        //Bandicam\\Data\\Bandicam\\Bandicam.reg

        /// <summary>
        /// 启动录屏软件//改为开启开机启动或取消开机启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (开机启动.Content == "设置开机启动")
            {
                SetStartup("DesktopRecording", $"{appPath}DesktopRecording.exe");
            }
            else
            {
                RemoveStartup("DesktopRecording");
            }

            if (IsStartupSet("DesktopRecording"))
            {
                开机启动.Content = "取消开机启动";
            }
            else
            {
                开机启动.Content = "设置开机启动";
            }

            // SavePathChange(videoAppPath, _videoConfigPath, "视频存放文件夹");   20240608路径修改成功后会被覆盖，应该是配置找的不对，先不用
            //改成自动启动了
            // Process process = new Process();
            // process.StartInfo = new ProcessStartInfo(videoAppPath);
            // process.Start();

        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// 停止录屏软件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SimulateShiftF12();

            //StartRecording();  //试验了一下，不行
            TerminareProcess(videoProcessName);
        }

        /// <summary>
        /// 删除指定日期的录频软件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int minutesEarlier;
            string inConfigSavePath = SavePathSerch(videoAppPath, videoConfigPath);
            string pattern = "\"([^\"]*)\"=\"([^\"]*)\"";
            string videoSavePath;
            Match match = Regex.Match(inConfigSavePath, pattern);

            if (match.Success && match.Groups.Count > 2)
            {
                videoSavePath = match.Groups[2].Value.Replace("\\\\", "\\");
                if (int.TryParse(textboxTime.Text, out minutesEarlier))
                {
                    minutesEarlier = -Math.Abs(minutesEarlier);
                    DeleteMP4File(videoSavePath, minutesEarlier);
                }
                else
                {
                   System.Windows.MessageBox.Show("请输入整数");
                }

            }
            else
            {
                System.Windows.MessageBox.Show("未找到存储路径");
            }


        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (监控自启动.Content == "监控自启动开")
            {
                StartMonitoring();
            }
            else
            {
                _timer.Dispose();
                监控自启动.Content = "监控自启动开";
            }
        }


        /// <summary>
        /// 停止录屏软件进程
        /// </summary>
        /// <param name="processName"></param>
        private void TerminareProcess(string processName)
        {
            //先停止录制再结束应用程序


            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    System.Windows.MessageBox.Show("程序未运行！");
                    return;
                }
                foreach (var process in processes)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"发生错误：{ex.Message}");
                throw;
            }
        }



        private void StartMonitoring(int interval = 1000)
        {
            _timer = new  System.Threading.Timer(CheckProcess, null, 0, interval);
            监控自启动.Content = "监控自启动关";
        }

        private void CheckProcess(object state)
        {
            var processes = Process.GetProcessesByName(videoProcessName);
            bool isRunning = processes.Any();

            // 使用 Dispatcher 在 UI 线程上更新状态
            Dispatcher.Invoke(() =>
            {
                label.Content = isRunning ? $"录频软件{videoProcessName} 运行中" : $"录频软件{videoProcessName} 未运行";
            });
            if (!isRunning)
            {
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo(videoAppPath);
                process.Start();
            }


        }

        #region 模拟按下Shift+F12快捷键


        private void SimulateShiftF12()
        {
            try
            {
                var simulator = new InputSimulator();

                simulator.Keyboard.KeyDown(VirtualKeyCode.SHIFT);
                Thread.Sleep(20);
                simulator.Keyboard.KeyDown(VirtualKeyCode.F12);
                Thread.Sleep(20);
                simulator.Keyboard.KeyUp(VirtualKeyCode.F12);
                Thread.Sleep(20);
                simulator.Keyboard.KeyUp(VirtualKeyCode.SHIFT);



            }
            catch (Exception EX)
            {
                MessageBox.Show(EX.Message);
                throw;
            }
        }

        #endregion

       

        /// <summary>
        /// 修改视频保存路径
        /// </summary>
        /// <param name="videoAppPath"></param>
        /// <param name="videoConfigPath"></param>
        /// <param name="newVideoSavePath"></param>
        private void SavePathChange(string videoAppPath, string videoConfigPath, string newVideoSavePath)
        {
            string regFilePath = videoConfigPath; //  .reg 文件路径
            string searchText = "sOutputFolder";
            string replaceText = $"\"sOutputFolder\"=\"{videoAppPath}{newVideoSavePath}";

            try
            {
                // 创建一个临时文件用于存储修改后的内容
                string tempFilePath = System.IO.Path.GetTempFileName();

                using (var reader = new StreamReader(regFilePath))
                using (var writer = new StreamWriter(tempFilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains(searchText))
                        {
                            line = replaceText;
                        }
                        writer.WriteLine(line);
                    }
                }

                // 替换原文件
                File.Delete(regFilePath);
                File.Move(tempFilePath, regFilePath);

                //MessageBox.Show("路径已成功修改。");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"发生错误: {ex.Message}");
            }
        }


        private string SavePathSerch(string videoAppPath, string videoConfigPath)
        {
            string regFilePath = videoConfigPath; //  .reg 文件路径
            string searchText = "sOutputFolder";

            using (var reader = new StreamReader(regFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(searchText))
                    {
                        return line;
                    }

                }
            }
            return "未查找到保存路径";

        }

        private void DeleteMP4File(string directoryPath, int minutesEarlier = -5)
        {
            DateTime currentTime = DateTime.Now;
            DateTime cutoffTime = currentTime.AddMinutes(minutesEarlier); // 当前时间5分钟前的时间

            try
            {
                string[] files = Directory.GetFiles(directoryPath);
                foreach (string file in files)
                {
                    DateTime creationTime = File.GetCreationTime(file);
                    if (creationTime < cutoffTime)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }
        }
        /// <summary>
        /// 最小化窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 最小化并隐藏窗口
            WindowState = WindowState.Minimized;
            Hide();
        }

       

        /// <summary>
        /// 设置开机启动
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="executablePath"></param>
        private static void SetStartup(string appName, string executablePath)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.SetValue(appName, executablePath);
            }
        }

        /// <summary>
        /// 取消开机启动
        /// </summary>
        /// <param name="appName"></param>
        private static void RemoveStartup(string appName)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.DeleteValue(appName, false);
            }
        }

        /// <summary>
        /// 检查是否开机启动
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        private static bool IsStartupSet(string appName)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
            {
                if (key != null)
                {
                    object value = key.GetValue(appName);
                    return value != null;
                }
                return false;
            }
        }

        /// <summary>
        /// 关闭时释放资源
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            SimulateShiftF12();
            TerminareProcess(videoProcessName);
            _timer.Dispose();
            base.OnClosed(e);
        }
    }



}
