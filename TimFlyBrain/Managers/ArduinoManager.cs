using Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

//Arduino Nano Device Name : USB-SERIAL CH340
//Arduino Uno Device Name : Arduino

namespace Managers
{
    /// <summary>
    /// Gestion de la communication avec le client
    /// </summary>
    public class ArduinoManager
    {
        const string ELEVATION_MESSAGE = "E{0};";
        const string PICK_MESSAGE = "P{0}{1};";
        const string ROLL_MESSAGE = "R{0}{1};";
        const string NEGATIF_CHARACTER = "N";

        const string CALIBRATION_FRONTLEFT_MESSAGE = "FL{0};";
        const string CALIBRATION_FRONTRIGHT_MESSAGE = "FR{0};";
        const string CALIBRATION_BACKLEFT_MESSAGE = "BL{0};";
        const string CALIBRATION_BACKRIGHT_MESSAGE = "BR{0};";

        const string INIT_OK_MESSAGE = "INIT_OK";

        public event EventHandler OnArduinoConnected;
        public event EventHandler OnArduinoDisconnected;
        public event EventHandler OnReceiveInitialzationOk;
        private readonly SerialCommunicationService _serialCommunicationService;
        private string _messageBuilder = string.Empty;
        private int _pichIndice;
        private int _rollIndice;
        private int _elevationIndice;
        private bool _newElevationValue;
        private bool _movementLoopOn;

        /// <summary>
        /// Initialise la nouvelle instance
        /// </summary>
        public ArduinoManager()
        {
            _serialCommunicationService = new SerialCommunicationService();
            _serialCommunicationService.OnSerialDisconnected += OnSerialCommunicationServiceDisconnected;
            _serialCommunicationService.OnSerialConnected += OnSerialCommunicationServiceConnected;
            _serialCommunicationService.OnMessageReceived += OnSerialCommunicationServiceMessageReceived;
        }

        /// <summary>
        /// Permet d'initialiser le manager en démarrant le serveur socket
        /// </summary>
        public void StartConnectionLoop()
        {
            Task.Run(async () =>
            {
                bool connected = false;

                while (!connected)
                {
                    DeviceInformation arduinoDevice = await _serialCommunicationService.GetDevice("FT232R USB UART");

                    if (arduinoDevice != null)
                    {
                        connected = await _serialCommunicationService.Connect(arduinoDevice.Id);
                    }

                    if (!connected)
                        await Task.Delay(1000);
                }
            });
        }

        /// <summary>
        /// Permet de gérer les données reçus pour les lier les une au autres pour faire un message
        /// </summary>
        private void ReceiveSerialData(string data)
        {
            bool messageEnd = false;
            string totalMessage = string.Empty;
            string beginMessage = string.Empty;

            if (data.Contains(":"))
            {
                int beginCaracterIndex = data.IndexOf(':');
                beginMessage = data.Substring(beginCaracterIndex + 1);
            }

            if (!data.Contains(":") && !data.Contains(";"))
            {
                _messageBuilder += data;
            }

            //La fin du message est dans le message
            if (data.Contains(";"))
            {
                int beginCaracterIndex = data.IndexOf(';');
                totalMessage = _messageBuilder + data.Replace(data.Substring(beginCaracterIndex), "");
                _messageBuilder = "";
                messageEnd = true;
            }

            if (!string.IsNullOrEmpty(beginMessage))
            {
                _messageBuilder = beginMessage;
            }

            //Si le message contient le caractère de début et de fin, le message est complet
            if (messageEnd)
            {
                ManageSerialMessage(totalMessage);
            }
        }

        private void ManageSerialMessage(string message)
        {
            if (message.Contains(INIT_OK_MESSAGE))
            {
                Initialisation();
            }
        }

        /// <summary>
        /// Permet d'envoyer le message serie "i" pour donner l'ordre à la carte de s'initialiser
        /// </summary>
        public void SendInisialisation()
        {
            _serialCommunicationService.SendMessage("i");
        }

        public void Initialisation()
        {
            OnReceiveInitialzationOk?.Invoke(this, new EventArgs());

            Task.Run(async () => await StartMovementLoop());
        }

        /// <summary>
        /// Permet d'envoyer un message libre
        /// </summary>
        /// <param name="message">Message a envoyer</param>
        public void ChangeElevation(int value)
        {
            int compensedValue = value * 5;

            if (_elevationIndice != compensedValue)
            {
                _elevationIndice = compensedValue;
                _newElevationValue = true;
            }
        }

        /// <summary>
        /// Permet de modifier le tangage
        /// </summary>
        /// <param name="value">valeur</param>
        public void ChangePich(int value)
        {
            int compensedValue = value * 5;

            _pichIndice = compensedValue;
        }

        /// <summary>
        /// Permet de modifier le roulis
        /// </summary>
        /// <param name="value">valeur</param>
        public void ChangeRoll(int value)
        {
            int compensedValue = value * 5;

            _rollIndice = compensedValue;
        }

        /// <summary>
        /// Permet de lancer la boucle d'envoie des mouvements
        /// </summary>
        public async Task StartMovementLoop()
        {
            _movementLoopOn = true;

            await Task.Run(async () =>
            {
                while (_movementLoopOn)
                {
                    string message = string.Empty;

                    string pichStringMessage = ConvertNumericToMessageString(Convert.ToUInt32(Math.Abs(_pichIndice)));
                    if (_pichIndice < 0)
                    {
                        message = string.Format(PICK_MESSAGE, NEGATIF_CHARACTER, pichStringMessage);
                    }
                    else
                    {
                        message = string.Format(PICK_MESSAGE, "", pichStringMessage);
                    }

                    string rollStringMessage = ConvertNumericToMessageString(Convert.ToUInt32(Math.Abs(_rollIndice)));
                    if (_rollIndice < 0)
                    {
                        message += string.Format(ROLL_MESSAGE, NEGATIF_CHARACTER, rollStringMessage);
                    }
                    else
                    {
                        message += string.Format(ROLL_MESSAGE, "", rollStringMessage);
                    }

                    if (_newElevationValue)
                    {
                        _newElevationValue = false;
                        message += string.Format(ELEVATION_MESSAGE, ConvertNumericToMessageString(Convert.ToUInt32(_elevationIndice)));
                    }

                    _serialCommunicationService.SendMessage(message);

                    await Task.Delay(500);
                }
            });
        }

        /// <summary>
        /// Convertir un valeur numérique en un valeur string de 4 caractères obligatoirement (remplacement du vide par des zéros)
        /// </summary>
        /// <param name="value">Valeur numérique</param>
        /// <returns>Valeur convertie</returns>
        public string ConvertNumericToMessageString(uint value)
        {
            string valueMessage = value.ToString();

            while (valueMessage.Count() < 4)
            {
                valueMessage = "0" + valueMessage;
            }

            return valueMessage;
        }

        /// <summary>
        /// Permet d'envoyer un message serie pour l'arrêt du drone
        /// </summary>
        public void StopDrone()
        {
            //_serialCommunicationService.SendMessage("S");

            ChangePich(0);
            ChangeRoll(0);

            if (_elevationIndice < 10)
            {
                ChangeElevation(0);
            }else
            {
                //TODO faire boucle pour réduire progressivement
            }
        }

        /// <summary>
        /// Permet d'envoyer la valeur de calibrage du moteur avant gauche. Seulement durant la phase d'initialisation
        /// </summary>
        public void SendCalibrateValueFrontLeft(int value)
        {
            string stringValue = ConvertNumericToMessageString(Convert.ToUInt32(value));

            _serialCommunicationService.SendMessage(string.Format(CALIBRATION_FRONTLEFT_MESSAGE, stringValue));
        }

        /// <summary>
        /// Permet d'envoyer la valeur de calibrage du moteur avant droite. Seulement durant la phase d'initialisation
        /// </summary>
        public void SendCalibrateValueFrontRight(int value)
        {
            string stringValue = ConvertNumericToMessageString(Convert.ToUInt32(value));

            _serialCommunicationService.SendMessage(string.Format(CALIBRATION_FRONTRIGHT_MESSAGE, stringValue));
        }

        /// <summary>
        /// Permet d'envoyer la valeur de calibrage du moteur arrière gauche. Seulement durant la phase d'initialisation
        /// </summary>
        public void SendCalibrateValueBackLeft(int value)
        {
            string stringValue = ConvertNumericToMessageString(Convert.ToUInt32(value));

            _serialCommunicationService.SendMessage(string.Format(CALIBRATION_BACKLEFT_MESSAGE, stringValue));
        }

        /// <summary>
        /// Permet d'envoyer la valeur de calibrage du moteur arrière droite. Seulement durant la phase d'initialisation
        /// </summary>
        public void SendCalibrateValueBackRight(int value)
        {
            string stringValue = ConvertNumericToMessageString(Convert.ToUInt32(value));

            _serialCommunicationService.SendMessage(string.Format(CALIBRATION_BACKRIGHT_MESSAGE, stringValue));
        }

        #region Handlers

        /// <summary>
        /// Se déclanche quand la communication serie est déconnecté
        /// </summary>
        private void OnSerialCommunicationServiceDisconnected(object sender, EventArgs e)
        {
            OnArduinoDisconnected?.Invoke(this, new EventArgs());
            StartConnectionLoop();
        }

        private void OnSerialCommunicationServiceConnected(object sender, EventArgs e)
        {
            OnArduinoConnected?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Se déclanche quand un message est reçu
        /// </summary>
        private void OnSerialCommunicationServiceMessageReceived(object sender, string data)
        {
            ReceiveSerialData(data);
        }

        #endregion
    }
}
