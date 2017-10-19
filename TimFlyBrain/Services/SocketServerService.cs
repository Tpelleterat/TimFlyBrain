using Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Service
{
    public class SocketServerService
    {
        public event EventHandler<string> OnClientConnected;
        public event EventHandler OnClientsDisconnected;
        public event EventHandler<string> OnMessageReceived;
        private StreamSocketListener _socketListener;
        Semaphore messageSendSemaphore;
        Semaphore clientDisconnectSemaphore;
        List<SocketClient> _clients;
        bool _someClientConnected;

        /// <summary>
        /// Initialise la nouvelle instance
        /// </summary>
        public SocketServerService()
        {
            messageSendSemaphore = new Semaphore(1, 1);
            clientDisconnectSemaphore = new Semaphore(1, 1);
            _clients = new List<SocketClient>();
        }

        /// <summary>
        /// Permet d'initialiser le serveur
        /// </summary>
        public void InitServer()
        {
            _socketListener = new StreamSocketListener();
            _socketListener.ConnectionReceived += OnSocketListenerClientConnected;
        }

        /// <summary>
        /// Permet de démarrer le serveur
        /// </summary>
        public async Task StartServer()
        {
            await _socketListener.BindServiceNameAsync("1337");
        }

        /// <summary>
        /// Se déclanche quand un nouvelle utilisateur se connecte à la communication socket
        /// </summary>
        private void OnSocketListenerClientConnected(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
#warning indiquer un id au nouveau client
            SocketClient client = new SocketClient("", args.Socket);
            client.OnClientDisconnected += OnClientDisconnected;
            client.OnMessageReceived += OnClientMessageReceived;
            _clients.Add(client);

            _someClientConnected = true;

            OnClientConnected?.Invoke(this, client.Id);
        }

        /// <summary>
        /// Permet d'envoyer un message socket. Ne fait rien si l'utilisateur n'est pas connecté
        /// </summary>
        /// <param name="message">Ne peut être null</param>
        public async Task SendMessage(string message)
        {
            if (!_someClientConnected)
                return;

            messageSendSemaphore.WaitOne();
            clientDisconnectSemaphore.WaitOne();

            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Le message ne peut pas être vide ou null");

            foreach (SocketClient client in _clients)
            {
                await client.SendMessage(message);
            }

            clientDisconnectSemaphore.Release();
            messageSendSemaphore.Release();
        }

        /// <summary>
        /// Se déclanche quand un client se déconnecte du serveur socket
        /// </summary>
        private void OnClientDisconnected(object sender, EventArgs e)
        {
            clientDisconnectSemaphore.WaitOne();

            SocketClient disconnectedClient = sender as SocketClient;

            if(disconnectedClient != null)
            {
                disconnectedClient.OnClientDisconnected -= OnClientDisconnected;
                disconnectedClient.OnMessageReceived -= OnClientMessageReceived;

                _clients.Remove(disconnectedClient);
            }

            if (_clients.Count == 0 && OnClientsDisconnected != null)
            {
                _someClientConnected = false;
                OnClientsDisconnected(this, new EventArgs());
            }

            clientDisconnectSemaphore.Release();
        }

        /// <summary>
        /// Se déclanche quand un client envoie un message
        /// </summary>
        private void OnClientMessageReceived(object sender, string message)
        {
            OnMessageReceived?.Invoke(this, message);
        }
    }
}
