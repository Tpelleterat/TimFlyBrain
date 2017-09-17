using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

/*
<DeviceCapability Name="serialcommunication">
    <Device Id="any">
    <Function Type="name:serialPort" />
    </Device>
</DeviceCapability>

Commande changer com carte aruino Raspberry Pi en Powershell
devcon status usb*
reg add "HKLM\SYSTEM\ControlSet001\Enum\usb\VID_1A86&PID_7523\5&3753427A&0&3\Device Parameters" /v "PortName" /t REG_SZ /d "COM5" /f
    */

namespace Services
{
    public class SerialCommunicationService
    {

        public event EventHandler<string> OnMessageReceived;
        public event EventHandler OnSerialConnected;
        public event EventHandler OnSerialDisconnected;
        private bool _deviceConnected;
        SerialDevice _serialPort;
        DataWriter _writer;

        /// <summary>
        /// Obtenir le device par le nom. Retourne Null si non trouvé
        /// </summary>
        public async Task<DeviceInformation> GetDevice(string name)
        {
            string aqs = SerialDevice.GetDeviceSelector();
            var dis = await DeviceInformation.FindAllAsync(aqs);

            var device = dis.FirstOrDefault(dev => dev.Name.ToLower().Contains(name.ToLower()));

            if (device != null)
            {
                return device;
            }

            return null;
        }

        /// <summary>
        /// Connexion au device en série.
        /// </summary>
        public async Task<bool> Connect(string deviceId)
        {
            _serialPort = await SerialDevice.FromIdAsync(deviceId);

            if (_serialPort != null)
            {
                _serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                _serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                _serialPort.BaudRate = 9600;
                _serialPort.Parity = SerialParity.None;
                _serialPort.StopBits = SerialStopBitCount.One;
                _serialPort.DataBits = 8;
                _serialPort.Handshake = SerialHandshake.None;

                _writer = new DataWriter(_serialPort.OutputStream);

                _deviceConnected = true;

                Task taskReceive = Task.Run(() => { ReadLoop(_serialPort.InputStream); });
                await taskReceive.ConfigureAwait(false);

                OnSerialConnected?.Invoke(this, new EventArgs());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Lance une boucle pour lire les données envoyées par série
        /// </summary>
        /// <param name="stream"></param>
        private async void ReadLoop(IInputStream stream)
        {
            var dataReader = new DataReader(stream);
            byte[] buffer = new byte[8];

            while (_deviceConnected)
            {
                try
                {
                    var bytesRead = await dataReader.LoadAsync((uint)buffer.Length);
                    dataReader.ReadBytes(buffer);
                    var str = Encoding.UTF8.GetString(buffer);

                    Task taskReceive = Task.Run(() =>
                    {
                        if (OnMessageReceived != null)
                            OnMessageReceived(this, str);
                    });
                    await taskReceive.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("(ReadLoop) Exception : " + ex);
                    Disconnect();
                }
            }

            dataReader.Dispose();
        }

        /// <summary>
        /// Envoie un message par la communication série.
        /// Attention : Si la connexion n'est pas ouverte il ne se passe rien
        /// </summary>
        public void SendMessage(string message)
        {
            if (_deviceConnected)
            {
                try
                {
                    Task<UInt32> storeAsyncTask;

                    _writer.WriteString(message);
                    storeAsyncTask = _writer.StoreAsync().AsTask();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("(SendMessage) Exception : " + ex);
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// Permet de néttoyer les variables en cas de déconnection
        /// </summary>
        private void Disconnect()
        {
            _deviceConnected = false;
            _serialPort = null;

            OnSerialDisconnected?.Invoke(this, new EventArgs());
        }
    }
}
