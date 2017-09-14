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
        BrainStatusEnum status = BrainStatusEnum.ControllerNotConnected;
        ArduinoManager _arduinoManager;
        ClientManager _clientManager;
        bool _isInitialized;

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

        public async void Init()
        {
            if (_isInitialized)
                return;

            _arduinoManager = new ArduinoManager();
            _arduinoManager.OnArduinoConnected += OnArduinoConnected;
            _arduinoManager.OnArduinoDisconnected += OnArduinoDisconnected;
            _arduinoManager.StartConnectionLoop();

            _clientManager = new ClientManager();
            _clientManager.OnElevationChange += _clientManager_OnElevationChange;
            _clientManager.OnRollChange += _clientManager_OnRollChange;
            _clientManager.OnPitchChange += _clientManager_OnPitchChange;
            _clientManager.OnInitialization += _clientManager_OnInitialization;
            _clientManager.OnAskStatus += _clientManager_OnAskStatus;

            await _clientManager.Init();

            _isInitialized = true;
        }

        private async void _clientManager_OnAskStatus(object sender, EventArgs e)
        {
            await SendStatus();
        }

        private void OnArduinoDisconnected(object sender, EventArgs e)
        {
            status = BrainStatusEnum.ControllerNotConnected;

        }

        private void OnArduinoConnected(object sender, EventArgs e)
        {
            status = BrainStatusEnum.Initialisation;
        }

        private async void ChangeStatus(BrainStatusEnum newStatus)
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

        private void _clientManager_OnElevationChange(object sender, string e)
        {
            int value = 0;

            if(int.TryParse(e,out value))
            {
                _arduinoManager.ChangeElevation(value);
            }
        }

        private void _clientManager_OnRollChange(object sender, string e)
        {
            int value = 0;

            if (int.TryParse(e, out value))
            {
                _arduinoManager.ChangeRoll(value);
            }
        }

        private void _clientManager_OnPitchChange(object sender, string e)
        {
            int value = 0;

            if (int.TryParse(e, out value))
            {
                _arduinoManager.ChangePich(value);
            }
        }

        private void _clientManager_OnInitialization(object sender, EventArgs e)
        {
            _arduinoManager.SendInisialisation();
        }
    }
}
