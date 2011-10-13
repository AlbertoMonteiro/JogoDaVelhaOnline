using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Collections.Generic;

namespace JogoDaVelha
{
    public class Comunicador : IDisposable
    {
        private TcpListener listener;
        private TcpClient client;

        public delegate void MensagemRecebidaDelegate(Mensagem mensagem);
        public event MensagemRecebidaDelegate QuandoReceberMensagem;
        private string hostname;
        private Thread thread;

        public Comunicador()
        {
            listener = new TcpListener(IPAddress.Any, 18000);
            listener.Start();
            thread = new Thread(EscutaClients);
            thread.Start();
        }

        public void IniciaClient(string hostname)
        {
            this.hostname = hostname;
        }

        public void SolicitarJogo(string nomeJogador)
        {
            client = new TcpClient(hostname, 18000);
            NetworkStream stream = client.GetStream();

            BinaryFormatter mensagem = new BinaryFormatter();
            mensagem.Serialize(stream, new Mensagem(nomeJogador));
            stream.Close();
            client.Close();
        }

        private void EscutaClients()
        {
            while (true)
            {
                var conexao = listener.AcceptTcpClient();
                hostname = ((IPEndPoint)conexao.Client.RemoteEndPoint).Address.ToString();
                MemoryStream tempStream = new MemoryStream();

                NetworkStream stream = conexao.GetStream();

                Byte[] bytes = new Byte[256];
                int i;

                // Loop to receive all the data sent by the client.
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    tempStream.Write(bytes, 0, i);

                conexao.Close();// Shutdown and end connection

                BinaryFormatter mensagem = new BinaryFormatter();
                tempStream.Seek(0, SeekOrigin.Begin);
                Mensagem mensagemRecebida = (Mensagem)mensagem.Deserialize(tempStream);
                QuandoReceberMensagem(mensagemRecebida);
            }
        }

        public void Dispose()
        {
            if (listener != null)
                listener.Stop();
            if (client != null)
                client.Close();
            if (thread != null)
                thread.Abort();
        }

        public void AceitarJogo(bool aceito, string nomeDoJogador)
        {
            client = new TcpClient(hostname, 18000);
            NetworkStream stream = client.GetStream();

            var mensagem = new BinaryFormatter();
            mensagem.Serialize(stream, new Mensagem(nomeDoJogador)
            {
                Tipo = aceito ? TipoMensagem.JogoAceito : TipoMensagem.JogoRecusado
            });
            stream.Close();
            client.Close();
        }

        internal void InformarJogada(int x, int y, string nomeDoJogador, bool euVenci)
        {
            client = new TcpClient(hostname, 18000);
            NetworkStream stream = client.GetStream();

            var mensagem = new BinaryFormatter();
            mensagem.Serialize(stream, new Mensagem(nomeDoJogador)
            {
                Tipo = TipoMensagem.JogadaEfetuada,
                EuVenci = euVenci,
                Posicao = new KeyValuePair<int, int>(x, y)
            });
            stream.Close();
            client.Close();
        }
    }
}
