using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ChessWPF.Models;

namespace ChessWPF.Network
{
    public class LANClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _running;

        public event Action<Move> OnMoveReceived;

        public void Connect(string host, int port)
        {
            _running = true;
            try
            {
                _client = new TcpClient();
                _client.Connect(host, port);
                _stream = _client.GetStream();

                _receiveThread = new Thread(ReceiveLoop);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client connect error: " + ex.Message);
            }
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

        public void Disconnect()
        {
            _running = false;
            _stream?.Close();
            _client?.Close();
        }

        private static Move ParseMove(string data)
        {
            var p = data.Split(',');
            if (p.Length < 8) return null;
            try
            {
                return new Move(
                    int.Parse(p[0]), int.Parse(p[1]),
                    int.Parse(p[2]), int.Parse(p[3]),
                    bool.Parse(p[4]),
                    (PieceType)int.Parse(p[5]),
                    bool.Parse(p[6]),
                    bool.Parse(p[7]));
            }
            catch { return null; }
        }
    }
}
