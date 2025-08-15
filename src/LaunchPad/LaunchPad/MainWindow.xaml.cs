using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
namespace LaunchPad
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        async Task InstallNode()
        {
            if (await IsCommandInstalled("node"))
            {
                AppendLine("Node.js已安装，跳过");
                return;
            }
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

        async Task InstallGit()
        {
            if (await IsCommandInstalled("git"))
            {
                AppendLine("Git已安装，跳过");
                return;
            }
            var tempPath = Path.GetTempPath();
            var gitPath = Path.Combine(tempPath, "git-installer.exe");
            var buffer = Properties.Resources.GitInstaller;
            File.WriteAllBytes(gitPath, buffer);

            try
            {
                AppendLine("正在安装Git");
                await ExecuteShellCommand(gitPath,
                    @"/VERYSILENT /NORESTART /SP- /SUPPRESSMSGBOXES /DIR=C:\Program Files\Git");
                AppendLine("Git安装完成！");
            }
            finally
            {
                File.Delete(gitPath);
            }
        }

        static async Task ExecuteShellCommand(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                Verb = "runas",
                UseShellExecute = true,
            };
            using var process = Process.Start(psi);
            await process.WaitForExitAsync();
        }

        static async Task<bool> IsCommandInstalled(string command)
        {
            try
            {
                var psi = new ProcessStartInfo(command, "--version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                await proc?.WaitForExitAsync();
                return proc?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ApiKeyTextBox.Text))
            {
                MessageBox.Show("请先填写api key");
                return;
            }

            await InstallNode();

            await InstallGit();

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
            var configFile = Path.Combine(dir, fileName);
            var configText = File.ReadAllText(configFile);
            configText = configText.Replace("${APIKEY}", ApiKeyTextBox.Text);
            var targetConfigFile = Path.Combine(configDir, "config.json");
            File.WriteAllText(targetConfigFile, configText);
            AppendLine("配置完成");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var url = ((Hyperlink)sender).NavigateUri.AbsoluteUri;
            try
            {
                Process.Start(new ProcessStartInfo(url));
            }
            catch (Exception)
            {
                Clipboard.SetText(url);
                MessageBox.Show("已复制链接，请到浏览器打开！");
            }

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