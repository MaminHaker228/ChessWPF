using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ChessWPF.Models;

namespace ChessWPF.Network
{
    public class LANServer
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenThread;
        private bool _running;

        public event Action<Move> OnMoveReceived;
        public event Action OnClientConnected;

        public const int Port = 5555;

        public void Start()
        {
            _running = true;
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();

            _listenThread = new Thread(() =>
            {
                try
                {
                    _client = _listener.AcceptTcpClient();
                    _stream = _client.GetStream();
                    OnClientConnected?.Invoke();
                    ReceiveLoop();
                }
                catch (Exception ex)
                {
                    if (_running) Console.WriteLine("Server error: " + ex.Message);
                }
            });
            _listenThread.IsBackground = true;
            _listenThread.Start();
        }

        private void ReceiveLoop()
        {
            var buf = new byte[256];
            while (_running)
            {
                try
                {
                    int n = _stream.Read(buf, 0, buf.Length);
                    if (n <= 0) break;
                    string data = Encoding.UTF8.GetString(buf, 0, n).Trim();
                    var move = ParseMove(data);
                    if (move != null) OnMoveReceived?.Invoke(move);
                }
                catch { break; }
            }
        }

        public void SendMove(string data)
        {
            if (_stream == null) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(data + "\n");
                _stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex) { Console.WriteLine("Send error: " + ex.Message); }
        }

        public void Stop()
        {
            _running = false;
            _stream?.Close();
            _client?.Close();
            _listener?.Stop();
        }

        private static Move ParseMove(string data)
        {
            var p = data.Split(',');
            if (p.Length < 8) return null;
            try
            {
                var m = new Move(
                    int.Parse(p[0]), int.Parse(p[1]),
                    int.Parse(p[2]), int.Parse(p[3]),
                    bool.Parse(p[4]),
                    (PieceType)int.Parse(p[5]),
                    bool.Parse(p[6]),
                    bool.Parse(p[7]));
                return m;
            }
            catch { return null; }
        }
    }
}
