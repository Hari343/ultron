using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

namespace Ultron
{
  
    public class TelnetClientService : IDisposable
    {

        // Telnet command codes
        private const byte IAC = 255;
        private const byte DONT = 254;
        private const byte DO = 253;
        private const byte WONT = 252;
        private const byte WILL = 251;

        private readonly string _host;
        private readonly int _port;
        private readonly Encoding _encoding;
        private readonly int _receiveTimeoutMs;

        private TcpClient? _tcp;
        private NetworkStream? _stream;
        private readonly Lock _streamLock = new();

        public TelnetClientService(string host, int port = 23, Encoding? encoding = null, int receiveTimeoutMs = 5000)
        {
            // good habits first
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("host is required", nameof(host));
            }

            _host = host;
            _port = port;
            _encoding = encoding ?? Encoding.ASCII;
            _receiveTimeoutMs = receiveTimeoutMs;
        }

        public void Connect()
        {
            if (_tcp != null && _tcp.Connected)
            {
                return;
            }

            _tcp = new TcpClient
            {
                ReceiveTimeout = _receiveTimeoutMs,
                SendTimeout = _receiveTimeoutMs
            };

            _tcp.Connect(_host, _port);
            _stream = _tcp.GetStream();
        }

        public void Disconnect()
        {
            lock (_streamLock)
            {
                try
                {
                    _stream?.Close();
                }
                catch { }

                try
                {
                    _tcp?.Close();
                }
                catch { }

                _stream = null;
                _tcp = null;
            }
        }

        // We don't need this functionality for the purpose of this project, but it would be useful to have in the future if we want to support more complex interactions with telnet servers.
        public TelnetResult ExecuteCommand(string command, string? waitFor = null, int? timeoutMs = null)
        {
            ArgumentNullException.ThrowIfNull(command);

            // If accessed before calling connect()
            if (_stream == null)
            {
                Connect();
            }

            var effectiveTimeout = timeoutMs ?? _receiveTimeoutMs;

            lock (_streamLock)
            {
                // write command + CRLF
                var write = _encoding.GetBytes(command + "\r\n");
                _stream!.Write(write, 0, write.Length);
                _stream.Flush();

                // read response
                var response = ReadUntil(waitFor, effectiveTimeout);
                return new TelnetResult { Response = response, Success = true };
            }
        }

        // Fancy telnet protocol implementation with basic IAC support
        private string ReadUntil(string? waitFor, int timeoutMs)
        {
            if (_stream == null) return string.Empty;
            var sw = Stopwatch.StartNew();
            var ms = timeoutMs;
            var buffer = new byte[1024];
            var memory = new MemoryStream();

            while (sw.ElapsedMilliseconds < ms)
            {
                if (!_stream.DataAvailable)
                {
                    System.Threading.Thread.Sleep(20);
                    continue;
                }

                var read = 0;
                try
                {
                    read = _stream.Read(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    break;
                }

                if (read <= 0)
                {
                    break;
                }

                 
                for (int i = 0; i < read; i++)
                {
                    var b = buffer[i];
                    if (b == IAC)
                    {
                        
                        if (i + 1 < read)
                        {
                            var cmd = buffer[++i];
                            if (cmd == IAC)
                            {
                                memory.WriteByte(IAC); // escaped 255
                                continue;
                            }

                            if (cmd == DO || cmd == DONT || cmd == WILL || cmd == WONT)
                            {
                                if (i + 1 < read)
                                {
                                    var opt = buffer[++i];
                                    // reply: if remote asks DO -> send WONT, if remote WILL -> send DONT
                                    var reply = (byte[]) [];

                                    if (cmd == DO)
                                        reply = [IAC, WONT, opt];
                                    else if (cmd == WILL)
                                        reply = [IAC, DONT, opt];
                                    else
                                        continue;

                                    try
                                    {
                                        _stream.Write(reply, 0, reply.Length);
                                    }
                                    catch { }

                                }
                                continue;
                            }
                        }
                        // if incomplete, ignore
                        continue;
                    }

                    memory.WriteByte(b);
                }

                var str = _encoding.GetString(memory.ToArray());
                if (!string.IsNullOrEmpty(waitFor))
                {
                    if (str.Contains(waitFor))
                        return str;
                }
            }

            return _encoding.GetString(memory.ToArray());
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    public record TelnetResult
    {
        public string? Response { get; init; }
        public bool? Success { get; init; }
    }
}
