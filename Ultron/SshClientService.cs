using Renci.SshNet;

namespace Ultron
{
    public class SshClientService : IDisposable
    {
        private readonly SshClient _client;

        public SshClientService(string host, string username, string password, int port = 22, TimeSpan? timeout = null)
        {
            // good habits first
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("host is required", nameof(host));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("username is required", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("password is required", nameof(username));
            }

            var connectionInfo = new ConnectionInfo(host, port, username, new PasswordAuthenticationMethod(username, password));

            _client = new SshClient(connectionInfo);

            if (timeout.HasValue)
            {
                _client.ConnectionInfo.Timeout = timeout.Value;
            }
        }

        public void Connect()
        {
            if (!_client.IsConnected)
            {
                _client.Connect();
            }
        }

        public void Disconnect()
        {
            if (_client.IsConnected)
            {
                _client.Disconnect();
            }
        }

        public SshResult ExecuteCommand(string command)
        {
            ArgumentNullException.ThrowIfNull(command);
            Connect();
            var cmd = _client.CreateCommand(command);
            var asyncResult = cmd.BeginExecute();

            // Let's wait till we receive something. There might be a better way to do this. But for now this is good enough.
            while (!asyncResult.IsCompleted)
            {
                Thread.Sleep(10);
            }
            cmd.EndExecute(asyncResult);

            var result = new SshResult
            {
                ExitStatus = cmd.ExitStatus,
                Output = cmd.Result,
                Error = cmd.Error
            };

            return result;
        }

        public void Dispose()
        {
            try
            {
                Disconnect();
            }
            catch { }
            _client.Dispose();
        }
    }

    public record SshResult
    {
        public int? ExitStatus { get; init; }
        public string? Output { get; init; }
        public string? Error { get; init; }
    }
}
