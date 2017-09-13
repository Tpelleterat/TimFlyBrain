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
        public event EventHandler<string> OnElevationChange;
        public event EventHandler<string> OnPitchChange;
        public event EventHandler<string> OnRollChange;
        public event EventHandler OnAskStatus;
        public event EventHandler OnInitialization;

        private const string ELEVATION_COMMAND = "ELEVATION";
        private const string PITCH_COMMAND = "PITCH";
        private const string ROLL_COMMAND = "ROLL";
        private const string STATUS_COMMAND = "STATUS";
        private const string INITIALIZATION_COMMAND = "INITIALIZATION";

        public bool IsConnected
        {
            get; set;
        }

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
            Debug.WriteLine(data);

            ReadCommands(data);
        }

        private void ReadCommands(string messageData)
        {
            List<string> commands = messageData.Split(';')?.ToList();

            Parallel.ForEach(commands, (command) =>
            {
                string data = command.Split('|')?.ToList().Last();

                if (command.Contains(ELEVATION_COMMAND))
                {
                    OnElevationChange?.Invoke(this, data);
                }
                else if (command.Contains(PITCH_COMMAND))
                {
                    OnPitchChange?.Invoke(this, data);
                }
                else if (command.Contains(ROLL_COMMAND))
                {
                    OnRollChange?.Invoke(this, data);
                }
                else if (command.Contains(STATUS_COMMAND))
                {
                    OnAskStatus?.Invoke(this, new EventArgs());
                }
                else if (command.Contains(INITIALIZATION_COMMAND))
                {
                    OnInitialization?.Invoke(this, new EventArgs());
                }
            });
        }

        #endregion
    }
}
