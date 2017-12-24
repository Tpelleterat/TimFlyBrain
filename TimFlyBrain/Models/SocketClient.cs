using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Models
{
    public class SocketClient
    {
        public event EventHandler OnClientDisconnected;
        public event EventHandler<string> OnMessageReceived;
        private StreamSocket _clientSocket;
        Semaphore _clientDisconnectSemaphore;//La déconnexion peut se faire à la réception d'un message ou à l'envoie. Il ne faut pas quelle se fasse deux fois.
        Semaphore _messageSendSemaphore;
        bool _isClientConnected;
        StreamWriter _writer;

        /// <summary>
        /// Obtenir ou définir(privé) l'identifiant du client
        /// </summary>
        public string Id { get; private set; }

        public SocketClient(string id, StreamSocket clientSocket)
        {
            if (clientSocket == null)
                throw new ArgumentException("Le StreamSocket ne peut pas être null");
            Id = id;

            _clientDisconnectSemaphore = new Semaphore(1, 1);
            _messageSendSemaphore = new Semaphore(1, 1);
            _isClientConnected = true;

            _clientSocket = clientSocket;
            _writer = new StreamWriter(clientSocket.OutputStream.AsStreamForWrite());

            Task taskReceive = Task.Run(() => { WaitForData(clientSocket); });
            taskReceive.ConfigureAwait(false);
        }

        /// <summary>
        /// Boucle de lecture des données reçu. Si l'utilisateur se déconnecte,
        /// une exception est déclanché lors de la lecture et on arrêt la boucle en appelant la méthode ClientDisconnect
        /// </summary>
        private void WaitForData(StreamSocket socket)
        {
            var dr = new StreamReader(socket.InputStream.AsStreamForRead());

            while (_isClientConnected)
            {
                try
                {
                    string message = dr.ReadLine();

                    if (!string.IsNullOrEmpty(message))
                    {
                        OnMessageReceived?.Invoke(this, message);
                    }
                    else
                    {
                        ClientDisconnect();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    ClientDisconnect();
                }
            }
            dr.Dispose();
        }

        /// <summary>
        /// Permet d'envoyer un message socket. Ne fait rien si l'utilisateur n'est pas connecté
        /// </summary>
        /// <param name="message">Ne peut être null</param>
        public async Task SendMessage(string message)
        {
            _messageSendSemaphore.WaitOne();

            if (!_isClientConnected)
                return;

            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Le message ne peut pas être vide ou null");

            try
            {
                await _writer.WriteLineAsync(message);
                await _writer.FlushAsync();
            }
            catch (Exception)
            {
                ClientDisconnect();
            }

            _messageSendSemaphore.Release();
        }

        /// <summary>
        /// Permet de nettoyer les variavbles d'un client déconnecté
        /// </summary>
        private void ClientDisconnect()
        {
            _clientDisconnectSemaphore.WaitOne();

            if (_isClientConnected != false)
            {
                Debug.WriteLine("Client disconnect");

                _isClientConnected = false;
                _writer = null;
                _clientSocket.Dispose();
                _clientSocket = null;

                if (OnClientDisconnected != null)
                    OnClientDisconnected(this, new EventArgs());
            }

            _clientDisconnectSemaphore.Release();
        }



        public override string ToString()
        {
            if(_clientSocket != null)
            {
                return _clientSocket.ToString();
            }

            return "Client déconnecté";
        }
    }
}
