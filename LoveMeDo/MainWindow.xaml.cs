using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using EasyModbus;

namespace LoveMeDo
{
    public partial class MainWindow : Window
    {
        ModbusClient client;
        Thread conn_check;
        string ip_addr;
        int port;

        public MainWindow()
        {
            InitializeComponent();
            Console.SetOut(new OutRedirect(boxConsoleOutput));
            buttonModbusExecute.IsEnabled = false;
            conn_check = new Thread(ConnectionCheck)
            {
                IsBackground = true
            };
        }

        private void ConnectionCheck()
        {
            while (client.Connected) { }
            Dispatcher.Invoke(() => {
                Console.WriteLine("Connection dropped");
                buttonModbusStart.Click += OnButtonConnectClicked;
                buttonModbusStart.Click -= OnButtonDisconnectClicked;
                buttonModbusStart.Content = "Connect";
                boxModbusIP.IsEnabled = true;
                boxModbusPort.IsEnabled = true;
                buttonModbusExecute.IsEnabled = false;
            } );
            
        }

        public void OnButtonConnectClicked(object sender, RoutedEventArgs e)
        {
            ip_addr = boxModbusIP.Text;
            port = int.Parse(boxModbusPort.Text);
            client = new ModbusClient(ip_addr, port);

            try
            {
                Console.WriteLine("Connecting to " + ip_addr + " ...");
                client.Connect();
            }
            catch (EasyModbus.Exceptions.ConnectionException)
            {
                Console.WriteLine("Connection to " + ip_addr + " failed");
                labelLab2CStatus.Content = "Status: Cannot connect";
            }
            
            if (client.Connected)
            {
                labelLab2CStatus.Content = "Status: Connected to " + ip_addr;
                Console.WriteLine("Connected to " + ip_addr + ":" + port.ToString());
                buttonModbusStart.Click -= OnButtonConnectClicked;
                buttonModbusStart.Click += OnButtonDisconnectClicked;
                buttonModbusStart.Content = "Disconnect";
                boxModbusIP.IsEnabled = false;
                boxModbusPort.IsEnabled = false;
                buttonModbusExecute.IsEnabled = true;
                conn_check.Start();
            }
        }

        public void OnButtonDisconnectClicked(object sender, RoutedEventArgs e)
        {
            if (client.Connected)
            {
                client.Disconnect();
                labelLab2CStatus.Content = "Status: Not connected";
                Console.WriteLine("Disconnected from " + ip_addr);
                buttonModbusStart.Click += OnButtonConnectClicked;
                buttonModbusStart.Click -= OnButtonDisconnectClicked;
                buttonModbusStart.Content = "Connect";
                boxModbusIP.IsEnabled = true;
                boxModbusPort.IsEnabled = true;
                buttonModbusExecute.IsEnabled = false;
            }
        }

        public void OnExecuteButtonClicked(object sender, RoutedEventArgs e)
        {
            if (client.Connected)
            {
                switch (boxModbusOp.Text)
                {
                    case "Read discrete inputs":
                        break;
                    case "Read coils":
                        int offset = int.Parse(boxRegOffset.Text);
                        int quantity = int.Parse(boxRegNumber.Text);
                        bool[] output;
                        try
                        {
                            output = client.ReadCoils(offset, quantity);
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong");
                            output = new bool[] { false };
                        }
                        foreach (bool b in output)
                        {
                            if (b) Console.Write("1 ");
                            else Console.Write("0 ");
                        }
                        Console.Write('\n');
                        
                        break;
                    case "Read holding registers":
                        break;
                    case "Read input registers":
                        break;
                    case "Write single coil":
                        break;
                    case "Write single register":
                        break;
                    case "Write multiple coil":
                        break;
                    case "Write multiple registers":
                        break;
                    case "R/W multiple registers":
                        break;
                }
            }
        }

    }

    public class OutRedirect : TextWriter
    {
        private TextBox box;

        public OutRedirect(TextBox b)
        {
            box = b;
        }

        public override void Write(char value)
        {
            box.Text += value;
        }

        public override void Write(char[] value)
        {
            foreach (char c in value)
            {
                box.Text += c;
            }
        }

        public override void Write(string value)
        {
            box.Text += value;
        }

        public override void WriteLine(char value)
        {
            box.Text += value + "\n";
        }

        public override void WriteLine(char[] value)
        {
            foreach (char c in value)
            {
                box.Text += c;
            }
            box.Text += '\n';
        }

        public override void WriteLine(string value)
        {
            box.Text += value + "\n";
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }

}
