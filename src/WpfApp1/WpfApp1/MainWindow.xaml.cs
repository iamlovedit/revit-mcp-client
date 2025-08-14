using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
namespace WpfApp1
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        async Task InstallNode()
        {
            var tempPath = Path.GetTempPath();
            var msiPath = Path.Combine(tempPath, "node.msi");
            var buffer = Properties.Resources.NodeInstaller;
            File.WriteAllBytes(msiPath, buffer);
            try
            {
                AppendLine("正在安装 Node.js");
                var psi = new ProcessStartInfo("msiexec", $"/i \"{msiPath}\" /quiet")
                {
                    Verb = "runas", // 提权
                    UseShellExecute = true,
                };
                using var process = Process.Start(psi);
                await process.WaitForExitAsync();
                AppendLine("Node安装完成！");
            }
            finally
            {
                File.Delete(msiPath);
            }
        }


        async Task ExecuteShellCommand(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                Verb = "runas",
                UseShellExecute = true,
            };
            using var process = Process.Start(psi);
            await process.WaitForExitAsync();
        }

        static bool IsCommandInstalled(string command)
        {
            try
            {
                var psi = new ProcessStartInfo(command, "--version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var proc = Process.Start(psi);
                string output = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit();
                return output.StartsWith("v");
            }
            catch
            {
                return false;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsCommandInstalled("node"))
            {
                await InstallNode();
            }
            AppendLine("正在配置npm镜像");
            await ExecuteShellCommand("npm", "config set registry https://registry.npmmirror.com");
            AppendLine("配置npm镜像完成");
            await Task.Delay(1000);
            AppendLine("正在安装Claude code");
            await ExecuteShellCommand("npm", "install -g @anthropic-ai/claude-code");
            AppendLine("Claude code安装完成");

            await Task.Delay(1000);
            AppendLine("正在安装Claude code router");
            await ExecuteShellCommand("npm", "install -g @musistudio/claude-code-router");
            AppendLine("Claude code router安装完成");

            AppendLine("创建文件夹和配置");

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configDir = Path.Combine(home, ".claude-code-router");
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            var fileName = "configuration.json";
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            File.Copy(Path.Combine(dir, fileName), Path.Combine(configDir, "config.json"));
            Process.Start(configDir);
            AppendLine("配置完成");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var url = ((Hyperlink)sender).NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(url));
        }

        private void AppendLine(string text)
        {
            Dispatcher.Invoke(() =>
            {
                OutputTextBox.AppendText(text + Environment.NewLine);
                OutputTextBox.ScrollToEnd();
            });
        }
    }

}