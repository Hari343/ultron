using System;
using System.Diagnostics;
using System.Text;

namespace Ultron
{
    
    public class RdpClientService : IDisposable
    {
        private readonly string _host;
        private Process? _mstscProcess;
        private bool _credentialAdded;

        public RdpClientService(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("host is required", nameof(host));
            }

            _host = host;
        }

        public Process? Connect(string username, string password)
        {
            if (username == null)
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            AddCredential(username, password);

            var args = new StringBuilder();
            args.Append($"/v:{_host}");

            var psi = new ProcessStartInfo
            {
                FileName = "mstsc",
                Arguments = args.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _mstscProcess = Process.Start(psi);
            return _mstscProcess;
        }

       
        public void Disconnect()
        {
            try
            {
                if (_mstscProcess != null && !_mstscProcess.HasExited)
                {
                    try
                    {
                        _mstscProcess.Kill(entireProcessTree: true);
                    }
                    catch { }

                    try
                    {
                        _mstscProcess.Dispose();
                    }
                    catch { }

                    _mstscProcess = null;
                }
            }
            finally
            {
                if (_credentialAdded)
                {
                    RemoveCredential();
                    _credentialAdded = false;
                }
            }
        }

        private void AddCredential(string username, string password)
        {
            // cmdkey /generic:TERMSRV/host /user:username /pass:password
            var args = $"/generic:TERMSRV/{_host} /user:{EscapeArg(username)} /pass:{EscapeArg(password)}";
            var r = RunProcess("cmdkey", args, waitForExit: true);
            _credentialAdded = r.ExitCode == 0;
        }

        private void RemoveCredential()
        {
            // cmdkey /delete:TERMSRV/host
            var args = $"/delete:TERMSRV/{_host}";
            RunProcess("cmdkey", args, waitForExit: true);
        }

        private static (int ExitCode, string StdOut, string StdErr) RunProcess(string fileName, string arguments, bool waitForExit)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null)
            {
                return (-1, string.Empty, "failed to start process");
            }

            if (waitForExit)
            {
                p.WaitForExit();
                var outp = p.StandardOutput.ReadToEnd();
                var err = p.StandardError.ReadToEnd();
                return (p.ExitCode, outp, err);
            }

            return (0, string.Empty, string.Empty);
        }

        private static string EscapeArg(string arg)
        {
            if (string.IsNullOrEmpty(arg))
            {
                return "";
            }
            // Surround with double-quotes if contains spaces
            return arg.Contains(' ') ? '"' + arg.Replace("\"", "\\\"") + '"' : arg;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
