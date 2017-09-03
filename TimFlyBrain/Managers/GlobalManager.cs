using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimFlyBrain.Managers
{
    public class GlobalManager
    {
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
            _arduinoManager.StartConnectionLoop();

            _clientManager = new ClientManager();
            await _clientManager.Init();

            _isInitialized = true;
        }
    }
}
