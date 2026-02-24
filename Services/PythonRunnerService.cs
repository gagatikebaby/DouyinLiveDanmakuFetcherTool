using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DouyinLiveReceiver.Services
{
    /// <summary>
    /// Python进程运行服务，支持exe和py两种方式启动
    /// </summary>
    public class PythonRunnerService : IDisposable
    {
        private Process _process;
        private bool _isRunning;

        public event Action<string> OnOutputReceived;
        public event Action<string> OnErrorReceived;
        public event Action OnProcessExited;

        public bool IsRunning => _isRunning;

        public RunMode Mode { get; set; } = RunMode.Exe;

        public string PythonPath { get; set; } = "python";

        public string ScriptDirectory { get; set; }

        public string ExePath { get; set; }

        public bool Start(string liveId, string forwardHost = "127.0.0.1", int forwardPort = 9999)
        {
            if (_isRunning) return false;

            try
            {
                ProcessStartInfo startInfo;

                if (Mode == RunMode.Exe)
                {
                    if (!File.Exists(ExePath))
                    {
                        OnErrorReceived?.Invoke($"Exe文件不存在: {ExePath}");
                        return false;
                    }

                    var arguments = $"{liveId} --host {forwardHost} --port {forwardPort}";

                    startInfo = new ProcessStartInfo
                    {
                        FileName = ExePath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    };
                }
                else
                {
                    var scriptPath = Path.Combine(ScriptDirectory, "main.py");
                    if (!File.Exists(scriptPath))
                    {
                        OnErrorReceived?.Invoke($"脚本文件不存在: {scriptPath}");
                        return false;
                    }

                    var arguments = $"\"{scriptPath}\" {liveId} --host {forwardHost} --port {forwardPort}";

                    startInfo = new ProcessStartInfo
                    {
                        FileName = PythonPath,
                        Arguments = arguments,
                        WorkingDirectory = ScriptDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    };
                }

                _process = new Process { StartInfo = startInfo };
                _process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        OnOutputReceived?.Invoke(e.Data);
                };
                _process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        OnErrorReceived?.Invoke(e.Data);
                };
                _process.Exited += (s, e) =>
                {
                    _isRunning = false;
                    OnProcessExited?.Invoke();
                };
                _process.EnableRaisingEvents = true;

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                _isRunning = true;

                OnOutputReceived?.Invoke($"【√】进程已启动，直播间ID: {liveId}");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"启动进程失败: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            if (!_isRunning || _process == null) return;

            try
            {
                _process.Kill();
                _process.WaitForExit(3000);
                _process.Dispose();
                _process = null;
                _isRunning = false;
                OnOutputReceived?.Invoke("【√】进程已停止");
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"停止进程失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }

    /// <summary>
    /// 运行模式枚举
    /// </summary>
    public enum RunMode
    {
        Exe,
        Python
    }
}
