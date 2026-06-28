using System.Windows; 
using System.Windows.Input;
 

namespace Ultron
{
    
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool ValidateInput(bool validate_ip = true, bool validate_user = false, bool validate_pwd = false)
        {

            if (validate_ip)
            {
                if (string.IsNullOrEmpty(IpTextBox.Text.Trim()))
                {
                    StatusTextBlock.Text = "Enter an IP address";
                    return false;
                }
            }

            if (validate_user)
            {
                if (string.IsNullOrEmpty(UsernameTextBox.Text.Trim()))
                {
                    StatusTextBlock.Text = "Enter a username";
                    return false;
                }
            }

            if (validate_pwd)
            {
                if (string.IsNullOrEmpty(PasswordBox.Password))
                {
                    StatusTextBlock.Text = "Enter a pwd";
                    return false;
                }
            }

            return true;
                 
        }

        private async void RdpButton_Click(object sender, RoutedEventArgs e)
        {
            
            var ip = IpTextBox.Text.Trim();
            var user = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (!ValidateInput(validate_ip: true, validate_user: true, validate_pwd: true))
            {
                return;
            };


            StatusTextBlock.Text = $"Attempting RDP to {ip}...";
            try
            {
                var rdpService = new RdpClientService(ip);
                await Task.Run(() => rdpService.Connect(user, password));
            }
            catch { } // bad habits ?
            StatusTextBlock.Text = $"RDP connection to {ip} attempted.";

        }

        private async void SshButton_Click(object sender, RoutedEventArgs e)
        {
            var ip = IpTextBox.Text.Trim();
            var user = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (!ValidateInput(validate_ip : true, validate_user: true, validate_pwd: true))
            {
                return;
            }

            StatusTextBlock.Text = $"Attempting SSH to {ip}...";
            try
            {
                var sshService = new SshClientService(ip, user, password);
                await Task.Run(sshService.Connect);
            }
            catch { }
            StatusTextBlock.Text = $"SSH connection to {ip} attempted.";

        }

        private async void TelnetButton_Click(object sender, RoutedEventArgs e)
        {
            var ip = IpTextBox.Text.Trim();
            
            if (!ValidateInput())
            {
                return;
            }

            StatusTextBlock.Text = $"Attempting Telnet to {ip}...";
            try
            {
                var telnetService = new TelnetClientService(ip);
                await Task.Run(telnetService.Connect);
            }
            catch { }
            StatusTextBlock.Text = $"Telnet connection to {ip} attempted.";
        }

        private async void UltronButton_Click(object sender, RoutedEventArgs e)
        {
            var numRequests = 100;

            var ip = IpTextBox.Text.Trim();
            var user = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (!ValidateInput(validate_ip: true, validate_user: true, validate_pwd: true))
            {
                return;
            }

            StatusTextBlock.Text = $"Ultron Mode Activated!";

            // Fire 100 requests to the target at the same time
            for (var i = 0; i < numRequests; i++)
            {
                if ((i & 1) == 0)
                {
                    // lets fire Telnet
                    try
                    {
                        var telnetService = new TelnetClientService(ip);
                        Task.Run(telnetService.Connect); // This is Ultron Mode, no awaiting
                    }
                    catch { }

                }
                else
                {
                    // lets fire SSH
                    try
                    {
                        var sshService = new SshClientService(ip, user, password);
                        Task.Run(sshService.Connect); // This is Ultron Mode, no awaiting
                    }
                    catch { }
                }
            }
 
            StatusTextBlock.Text = $"Raised {numRequests} requests to {ip}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CardBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
             
            try
            {
                this.DragMove();
            }
            catch { }
        }
    }
}