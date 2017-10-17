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
        #region Attributes

        private SocketServerService _socketServerService;
        public event EventHandler<string> OnElevationChange;
        public event EventHandler<string> OnPitchChange;
        public event EventHandler<string> OnRollChange;
        public event EventHandler OnAskStatus;
        public event EventHandler OnInitialization;
        public event EventHandler OnDisconnect;
        public event EventHandler OnConnect;

        private const string DATA_SEPARATOR_COMMAND = "|";
        private const string ELEVATION_COMMAND = "ELEVATION";
        private const string PITCH_COMMAND = "PITCH";
        private const string ROLL_COMMAND = "ROLL";
        private const string ASKSTATUS_COMMAND = "ASKSTATUS";
        private const string STATUS_COMMAND = "STATUS";
        private const string INITIALIZATION_COMMAND = "INITIALIZATION";

        #endregion

        #region Properties

        public bool IsConnected
        {
            get; set;
        }

        #endregion

        public ClientManager()
        {
            _socketServerService = new SocketServerService();
        }

        #region Methods

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

        public async Task SendStatus(string status)
        {
            await _socketServerService.SendMessage(string.Concat(STATUS_COMMAND, DATA_SEPARATOR_COMMAND, status));
        }

        public async Task SendMessage(string message)
        {
            await _socketServerService.SendMessage(message);
        }

        #endregion

        #region Handlers

        /// <summary>
        /// Se déclanche quand le client mobile se connecte de la communication socket
        /// </summary>
        private void OnSocketClientConnected(object sender, string clientId)
        {
            IsConnected = true;
            OnConnect?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Se déclanche quand le client mobile se déconnecte de la communication socket
        /// </summary>
        private void OnSocketClientDisconnected(object sender, EventArgs e)
        {
            IsConnected = false;
            OnDisconnect?.Invoke(this, new EventArgs());
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
                else if (command.Contains(ASKSTATUS_COMMAND))
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
