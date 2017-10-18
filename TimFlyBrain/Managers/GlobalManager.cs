using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimFlyBrain.Models;

namespace TimFlyBrain.Managers
{
    public class GlobalManager
    {
        private BrainStatusEnum status = BrainStatusEnum.Fly;
        private ArduinoManager _arduinoManager;
        private ClientManager _clientManager;
        private bool _isInitialized;

        #region Singleton

        private static GlobalManager instance;

        public static GlobalManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GlobalManager();
                }
                return instance;
            }
        }

        private GlobalManager()
        {

        }

        #endregion

        public async Task Init()
        {
            if (_isInitialized)
                return;

            _arduinoManager = new ArduinoManager();
            _arduinoManager.OnArduinoConnected += OnArduinoConnected;
            _arduinoManager.OnArduinoDisconnected += OnArduinoDisconnected;
            _arduinoManager.OnReceiveInitialzationOk += OnArduinoManagerInitialzationOk;
            _arduinoManager.StartConnectionLoop();

            _clientManager = new ClientManager();
            _clientManager.OnElevationChange += OnClientManagerElevationChange;
            _clientManager.OnRollChange += OnClientManagerRollChange;
            _clientManager.OnPitchChange += OnClientManagerPitchChange;
            _clientManager.OnInitialization += OnClientManagerInitialization;
            _clientManager.OnAskStatus += OnClientManagerAskStatus;

            await _clientManager.Init();

            _isInitialized = true;
        }

        private async Task ChangeStatus(BrainStatusEnum newStatus)
        {
            status = newStatus;
            await SendStatus();
        }

        private async Task SendStatus()
        {
            if (_clientManager.IsConnected)
            {
                await _clientManager.SendStatus(status.ToString());
            }
        }

        #region Handlers

        #region Arduino handlers


        private async void OnArduinoDisconnected(object sender, EventArgs e)
        {
            await ChangeStatus(BrainStatusEnum.ControllerNotConnected);
        }

        private async void OnArduinoConnected(object sender, EventArgs e)
        {
            await ChangeStatus(BrainStatusEnum.Initialisation);
        }

        private async void OnArduinoManagerInitialzationOk(object sender, EventArgs e)
        {
            await ChangeStatus(BrainStatusEnum.Fly);
        }

        #endregion

        #region Client handlers

        private async void OnClientManagerAskStatus(object sender, EventArgs e)
        {
            await SendStatus();
        }

        private void OnClientManagerElevationChange(object sender, string e)
        {
            int value = 0;

            if (int.TryParse(e, out value))
            {
                _arduinoManager.ChangeElevation(value);
            }
        }

        private void OnClientManagerRollChange(object sender, string e)
        {
            int value = 0;

            if (int.TryParse(e, out value))
            {
                _arduinoManager.ChangeRoll(value);
            }
        }

        private void OnClientManagerPitchChange(object sender, string e)
        {
            int value = 0;

            if (int.TryParse(e, out value))
            {
                _arduinoManager.ChangePich(value);
            }
        }

        private void OnClientManagerInitialization(object sender, EventArgs e)
        {
            _arduinoManager.SendInisialisation();
        }

        #endregion

        #endregion

    }
}
