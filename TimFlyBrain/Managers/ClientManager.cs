using Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace Managers
{
    /// <summary>
    /// Gestion de la communication avec le client
    /// </summary>
    public class ClientManager
    {
        private SocketServerService _socketServerService;
        public event EventHandler OnCalibrationRequest;

        public bool IsConnected { get; set; }

        public ClientManager()
        {
            _socketServerService = new SocketServerService();
        }

        /// <summary>
        /// Permet d'initialiser le manager en démarrant le serveur socket
        /// </summary>
        public async Task Init()
        {
            _socketServerService.OnClientConnected += OnSocketClientConnected;
            _socketServerService.OnClientsDisconnected += OnSocketClientDisconnected;
            _socketServerService.OnMessageReceived += OnSocketMessageReceived;
            _socketServerService.InitServer();

            await _socketServerService.StartServer();
        }

        /// <summary>
        /// Envoie les position de l'acceleromètre au client connecté
        /// </summary>
        public async Task SendAccelerometerPosition(int x, int y, int z)
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append(x);
            messageBuilder.Append("|");
            messageBuilder.Append(y);
            messageBuilder.Append("|");
            messageBuilder.Append(z);

            await _socketServerService.SendMessage(messageBuilder.ToString());
        }

        public async Task SendMessage(string message)
        {
            await _socketServerService.SendMessage(message);
        }

        #region Handlers

        /// <summary>
        /// Se déclanche quand le client mobile se connecte de la communication socket
        /// </summary>
        private void OnSocketClientConnected(object sender, string clientId)
        {
            IsConnected = true;
        }

        /// <summary>
        /// Se déclanche quand le client mobile se déconnecte de la communication socket
        /// </summary>
        private void OnSocketClientDisconnected(object sender, EventArgs e)
        {
            IsConnected = false;
        }

        /// <summary>
        /// Se déclanche quand un message est reçu du client mobile
        /// </summary>
        private void OnSocketMessageReceived(object sender, string data)
        {
            if(data != null && data.ToLower().Equals("calibration"))
            {
                if (OnCalibrationRequest != null)
                    OnCalibrationRequest(this, new EventArgs());
            }
        }

        #endregion
    }
}
