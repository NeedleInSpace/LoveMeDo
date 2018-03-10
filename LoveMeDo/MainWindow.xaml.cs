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
        bool manual_dc;

        public MainWindow()
        {
            InitializeComponent();
            Console.SetOut(new OutRedirect(boxConsoleOutput));
            buttonModbusExecute.IsEnabled = false;
            conn_check = new Thread(ConnectionCheck)
            {
                IsBackground = true
            };
            boxModbusOp.SelectionChanged += new SelectionChangedEventHandler(OnModbusSelChanged);
        }

        public void OnModbusSelChanged(object sender, EventArgs e)
        {
            // Deactivate write controls
            if (boxModbusOp.SelectedIndex >= 0 && boxModbusOp.SelectedIndex <= 3)
            {
                boxModbusWriteValue.IsEnabled = false;
                boxRegOffsetWr.IsEnabled = false;
            }
            else
            {
                boxModbusWriteValue.IsEnabled = true;
                boxRegOffsetWr.IsEnabled = true;
            }
        }

        private void ConnectionCheck()
        {
            while (client.Connected) { Thread.Sleep(1000); }
            if (!manual_dc)
            {
                Dispatcher.Invoke(() => {
                    Console.WriteLine("Connection dropped");
                    buttonModbusStart.Click += OnButtonConnectClicked;
                    buttonModbusStart.Click -= OnButtonDisconnectClicked;
                    buttonModbusStart.Content = "Connect";
                    boxModbusIP.IsEnabled = true;
                    boxModbusPort.IsEnabled = true;
                    buttonModbusExecute.IsEnabled = false;
                });
            }
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
                manual_dc = false;
                conn_check.Start();
            }
        }

        public void OnButtonDisconnectClicked(object sender, RoutedEventArgs e)
        {
            if (client.Connected)
            {
                manual_dc = true;
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
                int offset = int.Parse(boxRegOffset.Text);
                int quantity = int.Parse(boxRegNumber.Text);
                int val = int.Parse(boxModbusWriteValue.Text);
                switch (boxModbusOp.Text)
                {
                    case "Read discrete inputs":
                        bool[] discrete_output;
                        try
                        {
                            discrete_output = client.ReadDiscreteInputs(offset, quantity);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                            discrete_output = new bool[0];
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong, check parameters");
                            discrete_output = new bool[0];
                        }
                        foreach (bool b in discrete_output)
                        {
                            if (b) Console.Write("1 ");
                            else Console.Write("0 ");
                        }
                        Console.Write('\n');                        
                        break;
                    case "Read coils":
                        bool[] coil_output;
                        try
                        {
                            coil_output = client.ReadCoils(offset, quantity);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                            coil_output = new bool[0];
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong, check parameters");
                            coil_output = new bool[0];
                        }
                        foreach (bool b in coil_output)
                        {
                            if (b) Console.Write("1 ");
                            else Console.Write("0 ");
                        }
                        Console.Write('\n');
                        break;
                    case "Read holding registers":
                        int[] holding_output;

                        try
                        {
                            holding_output = client.ReadHoldingRegisters(offset, quantity);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                            holding_output = new int[0];
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong, check parameters");
                            holding_output = new int[0];
                        }
                        foreach (int i in holding_output)
                        {
                            Console.Write(i.ToString() + " ");
                        }
                        Console.WriteLine("");
                        break;
                    case "Read input registers":
                        int[] inreg_output;

                        try
                        {
                            inreg_output = client.ReadInputRegisters(offset, quantity);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                            inreg_output = new int[0];
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong, check parameters");
                            inreg_output = new int[0];
                        }
                        foreach (int i in inreg_output)
                        {
                            Console.Write(i.ToString() + " ");
                        }
                        Console.WriteLine("");
                        break;
                    case "Write single coil":

                        bool coil = val > 0 ? true : false;
                        try
                        {
                            client.WriteSingleCoil(offset, coil);
                            Console.WriteLine("Write OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong, check parameters");
                        }
                        break;
                    case "Write single register":
                        try
                        {
                            client.WriteSingleRegister(offset, val);
                            Console.WriteLine("Write OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong, check parameters");
                        }
                        break;
                    case "Write multiple coil":
                        bool[] coil_input = new bool[quantity];
                        for (int i = 0; i < quantity; i++)
                        {
                            coil_input[i] = val > 0 ? true : false;
                        }
                        try
                        {
                            client.WriteMultipleCoils(offset, coil_input);
                            Console.WriteLine("Write OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong, check parameters");
                        }
                        break;
                    case "Write multiple registers":
                        int[] reg_out = new int[quantity];
                        for (int i = 0; i < quantity; i++)
                        {
                            reg_out[0] = val;
                        }
                        try
                        {
                            client.WriteMultipleRegisters(offset, reg_out);
                            Console.WriteLine("Write OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                        }
                        catch (EasyModbus.Exceptions.ModbusException)
                        {
                            Console.WriteLine("Something went wrong, check parameters");
                        }
                        break;
                    case "R/W multiple registers":
                        int[] reg_write = new int[quantity];
                        int[] reg_read;
                        int offset_write = int.Parse(boxRegOffsetWr.Text);
                        for (int i = 0; i < quantity; i++)
                        {
                            reg_write[i] = val;
                        }
                        try
                        {
                            reg_read = client.ReadWriteMultipleRegisters(offset, quantity, offset_write, reg_write);
                            Console.WriteLine("Read OK");
                            foreach (int i in reg_read)
                            {
                                Console.Write(i + " ");
                            }
                            Console.WriteLine("\nWrite OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("There's something wrong with connection, dropping it");
                            client.Disconnect();
                        }
                        catch (EasyModbus.Exceptions.ModbusException ex)
                        {
                            if (ex is EasyModbus.Exceptions.FunctionCodeNotSupportedException)
                            {
                                Console.WriteLine("Function code not supported");
                            }
                            else
                            {
                                Console.WriteLine("Something went wrong, check parameters");
                            }
                        }
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
