using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Threading;

namespace UDPChat
{
    public partial class Form1 : Form
    {

        bool _done = true; // флаг остановки слушающего потока
        UdpClient _client; // сокет клиента
        IPAddress _groupAddress; // групповой адрес рассылки
        int _localPort; // локальный порт для приема сообщений
        int _remotePort; // удаленный порт для отправки сообщений
        int _ttl; // количество маршрутизаторов

        IPEndPoint _remoteEP;
        UnicodeEncoding _encoding = new UnicodeEncoding();
        string _name; // имя пользователя в разговоре
        string _message; // сообщение для отправки

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                NameValueCollection config = ConfigurationManager.AppSettings;
                _groupAddress = IPAddress.Parse(config["GroupAddress"]);
                _localPort = int.Parse(config["LocalPort"]);
                _remotePort = int.Parse(config["RemotePort"]);
                _ttl = int.Parse(config["TTL"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _name = textBoxName.Text;
            textBoxName.ReadOnly = true;
            try
            {
                _client = new UdpClient(_localPort);
                _client.JoinMulticastGroup(_groupAddress, _ttl);
                _remoteEP = new IPEndPoint(_groupAddress, _remotePort);
                Thread receive = new Thread(new ThreadStart(Listener));
                receive.IsBackground = true;
                receive.Start();
                byte[] data = _encoding.GetBytes(_name + "подключился");
                _client.Send(data, data.Length, _remoteEP);
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Listener()
        {
            _done = false;
            try
            {
                while (!_done)
                {
                    IPEndPoint point = null;
                    byte[] buffer = _client.Receive(ref point);
                    _message = _encoding.GetString(buffer);
                    this.Invoke(new Action(() => { textBxHistory.Text = $"{DateTime.Now.ToShortTimeString()} { _message } \r\n {textBoxMessage.Text }"; }));

                }
            }
            catch(SocketException sex)
            {
                if (sex.ErrorCode != 10004)
                {
                    MessageBox.Show(sex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            textBxHistory.Text = $"{DateTime.Now.ToShortTimeString()}  {textBoxName.Text}    {textBoxMessage.Text}\r\n{textBxHistory.Text}";
            try
            {
                byte[] data = _encoding.GetBytes($"{_name}:{textBoxMessage.Text}");
                _client.Send(data, data.Length, _remoteEP);
                textBoxMessage.Clear();
                textBoxMessage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopListener();
            textBoxName.ReadOnly = false;
        }
        void StopListener()
        {
            byte[] data = _encoding.GetBytes($"{_name} покинул чат");
            _client.Send(data, data.Length, _remoteEP);
            _client.DropMulticastGroup(_groupAddress);
            _client.Close();
            _done = true;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_done == false)
            {
                StopListener();
            }
        }
    }
}
