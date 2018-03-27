using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Net.Sockets;
using Sharp7;
using NModbus;


namespace LoveMeDo
{
    public partial class MainWindow : Window
    {
        ModbusFactory mbus_factory;
        TcpClient mbus_client;
        IModbusMaster mbus_master;
        S7Client s7client;
        Socket sock;
        Thread conn_check;
        string ip_addr;
        int port;
        bool manual_dc;

        public MainWindow()
        {
            InitializeComponent();
            Console.SetOut(new OutRedirect(boxConsoleOutput));
            buttonModbusExecute.IsEnabled = false;
            buttonS7Run.IsEnabled = false;
            buttonS7Stop.IsEnabled = false;
            conn_check = new Thread(MbusConnectionCheck)
            {
                IsBackground = true
            };
            boxModbusOp.SelectionChanged += new SelectionChangedEventHandler(OnMbusSelChanged);
            mbus_factory = new ModbusFactory();
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        // Modbus section handlers
        public void OnMbusSelChanged(object sender, EventArgs e)
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

        private void MbusConnectionCheck()
        {
            while (mbus_client.Connected) { Thread.Sleep(500); }
            if (!manual_dc)
            {
                Dispatcher.Invoke(() => {
                    Console.WriteLine("Connection dropped");
                    buttonModbusStart.Click += OnButtonMbusConnectClicked;
                    buttonModbusStart.Click -= OnButtonMbusDisconnectClicked;
                    buttonModbusStart.Content = "Connect";
                    boxModbusIP.IsEnabled = true;
                    boxModbusPort.IsEnabled = true;
                    buttonModbusExecute.IsEnabled = false;
                });
            }
        }

        public void OnButtonMbusConnectClicked(object sender, RoutedEventArgs e)
        {
            ip_addr = boxModbusIP.Text;
            port = int.Parse(boxModbusPort.Text);
            mbus_client = new TcpClient();
            mbus_master = mbus_factory.CreateMaster(mbus_client);
            
            try
            {
                Console.WriteLine("Соединяю с " + ip_addr + " ...");
                mbus_client.Connect(ip_addr, port);
            }
            catch (SocketException)
            {
                Console.WriteLine("Соединение с " + ip_addr + " прервано");
                labelLab2CStatus.Content = "Состояние: Ошибка соединения";
            }
            
            if (mbus_client.Connected)
            {
                labelLab2CStatus.Content = "Состояние: Соединен с " + ip_addr;
                Console.WriteLine("Соединен с " + ip_addr + ":" + port.ToString());
                buttonModbusStart.Click -= OnButtonMbusConnectClicked;
                buttonModbusStart.Click += OnButtonMbusDisconnectClicked;
                buttonModbusStart.Content = "Отсоединить";
                boxModbusIP.IsEnabled = false;
                boxModbusPort.IsEnabled = false;
                buttonModbusExecute.IsEnabled = true;
                manual_dc = false;
                conn_check.Start();
            }
        }

        public void OnButtonMbusDisconnectClicked(object sender, RoutedEventArgs e)
        {
            if (mbus_client.Connected)
            {
                manual_dc = true;
                mbus_client.Close();
                labelLab2CStatus.Content = "Состояние: Нет соединения";
                Console.WriteLine("Отключен от " + ip_addr);
                buttonModbusStart.Click += OnButtonMbusConnectClicked;
                buttonModbusStart.Click -= OnButtonMbusDisconnectClicked;
                buttonModbusStart.Content = "Соединить";
                boxModbusIP.IsEnabled = true;
                boxModbusPort.IsEnabled = true;
                buttonModbusExecute.IsEnabled = false;
            }
        }

        public void OnButtonMbusExecuteClicked(object sender, RoutedEventArgs e)
        {
            if (mbus_client.Connected)
            {
                ushort offset = ushort.Parse(boxRegOffset.Text);
                ushort quantity = ushort.Parse(boxRegNumber.Text);
                byte slave = 0x01;
                ushort val = ushort.Parse(boxModbusWriteValue.Text);
                switch (boxModbusOp.Text)
                {
                    case "Read discrete inputs":
                        bool[] discrete_output;
                        try
                        {
                            discrete_output = mbus_master.ReadInputs(slave, offset, quantity);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                            discrete_output = new bool[0];
                        }
                        catch (InvalidModbusRequestException)
                        {
                            Console.WriteLine("Что-то пошло не так...");
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
                            coil_output = mbus_master.ReadCoils(slave, offset, quantity);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                            coil_output = new bool[0];
                        }
                        catch (InvalidModbusRequestException)
                        {
                            Console.WriteLine("Что-то пошло не так...");
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
                        ushort[] holding_output;

                        try
                        {
                            holding_output = mbus_master.ReadHoldingRegisters(slave, offset, quantity);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                            holding_output = new ushort[0];
                        }
                        catch (InvalidModbusRequestException)
                        {
                            Console.WriteLine("Что-то пошло не так...");
                            holding_output = new ushort[0];
                        }
                        foreach (int i in holding_output)
                        {
                            Console.Write(i.ToString() + " ");
                        }
                        Console.WriteLine("");
                        break;
                    case "Read input registers":
                        ushort[] inreg_output;

                        try
                        {
                            inreg_output = mbus_master.ReadInputRegisters(slave, offset, quantity);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                            inreg_output = new ushort[0];
                        }
                        catch (InvalidModbusRequestException)
                        {
                            Console.WriteLine("Что-то пошло не так...");
                            inreg_output = new ushort[0];
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
                            mbus_master.WriteSingleCoil(slave, offset, coil);
                            Console.WriteLine("ЗАП OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                        }
                        catch (InvalidModbusRequestException)
                        {
                            Console.WriteLine("Что-то пошло не так...");
                        }
                        break;
                    case "Write single register":
                        try
                        {
                            mbus_master.WriteSingleRegister(slave, offset, val);
                            Console.WriteLine("ЗАП OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                        }
                        catch (InvalidModbusRequestException)
                        {
                            Console.WriteLine("Что-то пошло не так...");
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
                            mbus_master.WriteMultipleCoils(slave, offset, coil_input);
                            Console.WriteLine("ЗАП OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                        }
                        catch (InvalidModbusRequestException)
                        {
                            Console.WriteLine("Что-то пошло не так...");
                        }
                        break;
                    case "Write multiple registers":
                        ushort[] reg_out = new ushort[quantity];
                        for (int i = 0; i < quantity; i++)
                        {
                            reg_out[0] = val;
                        }
                        try
                        {
                            mbus_master.WriteMultipleRegisters(slave, offset, reg_out);
                            Console.WriteLine("ЗАП OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                        }
                        catch (InvalidModbusRequestException)
                        {
                            Console.WriteLine("Что-то пошло не так...");
                        }
                        break;
                    case "R/W multiple registers":
                        ushort[] reg_write = new ushort[quantity];
                        ushort[] reg_read;
                        ushort offset_write = ushort.Parse(boxRegOffsetWr.Text);
                        for (int i = 0; i < quantity; i++)
                        {
                            reg_write[i] = val;
                        }
                        try
                        {
                            reg_read = mbus_master.ReadWriteMultipleRegisters(slave, offset, quantity, offset_write, reg_write);
                            Console.WriteLine("ЧТЕН OK");
                            foreach (int i in reg_read)
                            {
                                Console.Write(i + " ");
                            }
                            Console.WriteLine("\nЗАП OK");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine("Ошибка соединения. Соединение сброшено");
                            mbus_client.Close();
                        }
                        catch (InvalidModbusRequestException ex)
                        {
                            Console.WriteLine("Что-то пошло не так...");
                        }
                        break;
                }
            }
        }

        // Raw data send section
        public void OnButtonSendRawClicked(object sender, RoutedEventArgs e)
        {
            string ip_addr = boxModbusSendIP.Text;
            int port = int.Parse(boxModbusSendPort.Text);
            string string_buffer = boxModbusSendData.Text;
            byte[] send_buffer = GetBytes(string_buffer);
            byte[] receive_buffer = new byte[1024];

            if (!sock.Connected)
            {
                try
                {
                    sock.Connect(ip_addr, port);
                }
                catch
                {
                    Console.WriteLine("Не могу соединить с " + ip_addr);
                    return;
                }
            }
            try
            {
                sock.Send(send_buffer);
            }
            catch
            {
                Console.WriteLine("Не могу отправить данные на " + ip_addr);
                sock.Disconnect(false);
                return;
            }
            try
            {
                var len = sock.Receive(receive_buffer);
                Array.Resize<byte>(ref receive_buffer, len);
                Console.WriteLine("RX: " + BitConverter.ToString(receive_buffer));
            }
            catch
            {
                Console.WriteLine("Не могу получить ответ");
                sock.Disconnect(false);
                return;
            }
            sock.Shutdown(SocketShutdown.Both);

        }
        
        // S7 section handlers
        public void OnButtonS7ConnectClicked(object sender, RoutedEventArgs e)
        {
            s7client = new S7Client();
            ip_addr = boxS7Ip.Text;
            int rack = int.Parse(boxS7Rack.Text);
            int slot = int.Parse(boxS7Slot.Text);
            
            try
            {
                s7client.ConnectTo(ip_addr, rack, slot);
                Console.WriteLine("Соединен с " + ip_addr);
                buttonS7Connect.Click -= OnButtonS7ConnectClicked;
                buttonS7Connect.Click += OnButtonS7DisconnectClicked;
                buttonS7Connect.Content = "Отсоединить";
                int status = 0;
                s7client.PlcGetStatus(ref status);
                if (status == S7Consts.S7CpuStatusRun)
                {
                    buttonS7Run.IsEnabled = false;
                    buttonS7Stop.IsEnabled = true;
                }
                
            }
            catch
            {
                Console.WriteLine("Не могу соединить с" + ip_addr);
            }
        }

        public void OnButtonS7DisconnectClicked(object sender, RoutedEventArgs e)
        {
            if (s7client.Connected)
            {
                s7client.Disconnect();
                Console.WriteLine("Отсоединен от " + ip_addr);
                buttonS7Connect.Click += OnButtonS7ConnectClicked;
                buttonS7Connect.Click -= OnButtonS7DisconnectClicked;
                buttonS7Connect.Content = "Соединить";
            }
        }


        public void OnButtonS7RunClicked(object sender, RoutedEventArgs e)
        {
            int status = 0;
            if (s7client.Connected)
            {
                s7client.PlcGetStatus(ref status);
                if (status == S7Consts.S7CpuStatusRun)
                {
                    s7client.PlcHotStart();
                    buttonS7Run.IsEnabled = false;
                    buttonS7Stop.IsEnabled = true;
                }
            }
        }

        public void OnButtonS7StopClicked(object sender, RoutedEventArgs e)
        {
            int status = 0;
            if (s7client.Connected)
            {
                s7client.PlcGetStatus(ref status);
                if (status == S7Consts.S7CpuStatusStop)
                {
                    s7client.PlcStop();
                    buttonS7Run.IsEnabled = true;
                    buttonS7Stop.IsEnabled = false;
                }
            }
        }

        // Split whitespace separated string into byte array
        public static byte[] GetBytes(string value)
        {
            return value.Split(' ').Select(s => byte.Parse(s, System.Globalization.NumberStyles.HexNumber)).ToArray();
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
