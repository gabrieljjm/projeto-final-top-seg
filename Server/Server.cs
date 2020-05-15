using EI.SI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
	class Server
	{
		private static List<TcpClient> tcpClientsList = new List<TcpClient>();

		private const int PORT = 10000;
		static void Main(string[] args)
		{
			// CRIAR UM CONJUNTO IP+PORTO DO CLIEENT
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, PORT);
			// CRIAR UM TCP LISTENER
			TcpListener tcplistener = new TcpListener(endpoint);
			tcplistener.Start();
			Console.WriteLine("Server started");
			while (true)
			{
				TcpClient tcpClient = tcplistener.AcceptTcpClient();
				tcpClientsList.Add(tcpClient);
				Thread thread = new Thread(ClientListener);
				thread.Start(tcpClient);
			}
		}
		public static void ClientListener(object obj)
		{
			TcpClient tcpClient = (TcpClient)obj;
			NetworkStream networkStream = tcpClient.GetStream();
			Console.WriteLine("Client connected");
			ProtocolSI protocolSI = new ProtocolSI();

			while (protocolSI.GetCmdType() != ProtocolSICmdType.EOT)
			{
				int bytesRead = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

				if (protocolSI.GetCmdType() == ProtocolSICmdType.DATA)
				{
					String message = (tcpClient.ToString() + ": " + protocolSI.GetStringFromData());
					BroadcastMessage(message, tcpClient);
					Console.WriteLine(message);
				}

				if (protocolSI.GetCmdType() == ProtocolSICmdType.EOT)
				{
					String message = (tcpClient.ToString() + " disconnected");
					BroadcastMessage(message, tcpClient);
					Console.WriteLine(message);

					networkStream.Close();
					tcpClient.Close();
					tcpClientsList.Remove(tcpClient);
				}
			}
		}
		public static void BroadcastMessage(string msg, TcpClient excludeClient)
		{
			foreach (TcpClient client in tcpClientsList)
			{
				NetworkStream networkStream = client.GetStream();
				ProtocolSI protocolSI = new ProtocolSI();
				byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, msg);
				networkStream.Write(packet, 0, packet.Length);
			}
		}
	}
}