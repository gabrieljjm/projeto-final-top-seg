using EI.SI;
using System;
using System.Collections.Generic;
using System.IO;
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
		private static List<string> tcpClientsNames = new List<string>();
		private static string path = $@"log{DateTime.Now.Ticks}.txt";

		private const int PORT = 10000;
		static void Main(string[] args)
		{
			//FILE LOGS
			
			// CRIAR UM CONJUNTO IP+PORTO DO CLIEENT
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, PORT);
			// CRIAR UM TCP LISTENER
			TcpListener tcplistener = new TcpListener(endpoint);
			tcplistener.Start();
			string msg = "Server started!";
			File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
			Console.WriteLine(msg);
			while (true)
			{
				TcpClient tcpClient = tcplistener.AcceptTcpClient();
				tcpClientsList.Add(tcpClient);
				NetworkStream networkStream = tcpClient.GetStream();
				ProtocolSI protocolSI = new ProtocolSI();
				int bytesRead = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
				string name = protocolSI.GetStringFromData();
				tcpClientsNames.Add(name);
				Thread thread = new Thread(ClientListener);
				thread.Start(tcpClient);
			}
		}
		public static void ClientListener(object obj)
		{
			TcpClient tcpClient = (TcpClient)obj;
			NetworkStream networkStream = tcpClient.GetStream();
			string msg = (tcpClientsNames[tcpClientsList.IndexOf(tcpClient)] + " connected");
			File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
			Console.WriteLine(msg);
			ProtocolSI protocolSI = new ProtocolSI();

			while (protocolSI.GetCmdType() != ProtocolSICmdType.EOT)
			{
				int bytesRead = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

				if (protocolSI.GetCmdType() == ProtocolSICmdType.DATA)
				{
					msg = (tcpClientsNames[tcpClientsList.IndexOf(tcpClient)] + ": " + protocolSI.GetStringFromData());
					File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
					BroadcastMessage(msg, tcpClient);
					Console.WriteLine(msg);
				}

				if (protocolSI.GetCmdType() == ProtocolSICmdType.EOT)
				{
					msg = (tcpClientsNames[tcpClientsList.IndexOf(tcpClient)] + " disconnected");
					File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
					BroadcastMessage(msg, tcpClient);
					Console.WriteLine(msg);

					networkStream.Close();
					tcpClient.Close();
					tcpClientsNames.RemoveAt(tcpClientsList.IndexOf(tcpClient));
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