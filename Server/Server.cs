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
		private static List<TcpClient> tcpClientList = new List<TcpClient>();
		private static List<string> tcpNameList = new List<string>();
		private static List<string> gameList = new List<string>();
		//private static string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\logs-jogo-do-galo\\" + $@"log{DateTime.Now.Ticks}.txt";

		private const int PORT = 10000;
		static void Main(string[] args)
		{
			// CRIAR UM CONJUNTO IP+PORTA DO CLIENTE
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, PORT);
			// CRIAR UM TCP LISTENER
			TcpListener tcpListener = new TcpListener(endpoint);
			tcpListener.Start();
			string msg = "Server started!";
			//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
			Console.WriteLine(msg);
			while (true)
			{
				TcpClient tcpClient = tcpListener.AcceptTcpClient();
				tcpClientList.Add(tcpClient);
				NetworkStream networkStream = tcpClient.GetStream();
				ProtocolSI protocolSI = new ProtocolSI();
				int bytesRead = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
				string name = protocolSI.GetStringFromData();
				tcpNameList.Add(name);
				Thread thread = new Thread(ClientListener);
				thread.Start(tcpClient);
			}
		}

		public static void ClientListener(object obj)
		{
			TcpClient tcpClient = (TcpClient)obj;
			NetworkStream networkStream = tcpClient.GetStream();
			string msg = (tcpNameList[tcpClientList.IndexOf(tcpClient)] + " connected");
			//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
			Console.WriteLine(msg);
			ProtocolSI protocolSI = new ProtocolSI();

			while (protocolSI.GetCmdType() != ProtocolSICmdType.EOT)
			{
				int bytesRead = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                switch (protocolSI.GetCmdType())
                {
					case ProtocolSICmdType.USER_OPTION_1:
						msg = protocolSI.ToString();
						//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
						BroadcastMessage(msg, tcpClient);
						Console.WriteLine(msg);
						break;
					case ProtocolSICmdType.DATA:
						Console.WriteLine("aaaaaaaa");
						msg = (tcpNameList[tcpClientList.IndexOf(tcpClient)] + ": " + protocolSI.GetStringFromData());
						//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
						BroadcastMessage(msg, tcpClient);
						Console.WriteLine(msg);
						break;
					case ProtocolSICmdType.EOT:
						msg = (tcpNameList[tcpClientList.IndexOf(tcpClient)] + " disconnected");
						//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
						BroadcastMessage(msg, tcpClient);
						Console.WriteLine(msg);
						networkStream.Close();
						tcpClient.Close();
						tcpNameList.RemoveAt(tcpClientList.IndexOf(tcpClient));
						tcpClientList.Remove(tcpClient);
						break;

				}

				//if (protocolSI.GetCmdType() == ProtocolSICmdType.USER_OPTION_1)
				//{
				//	Console.WriteLine("aaaaaaaa");
				//	msg = (tcpNameList[tcpClientList.IndexOf(tcpClient)] + ": " + protocolSI.());
				//	//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
				//	BroadcastMessage(msg, tcpClient);
				//	Console.WriteLine(msg);
				//}

				//if (protocolSI.GetCmdType() == ProtocolSICmdType.EOT)
				//{
				//	msg = (tcpNameList[tcpClientList.IndexOf(tcpClient)] + " disconnected");
				//	//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
				//	BroadcastMessage(msg, tcpClient);
				//	Console.WriteLine(msg);

				//	networkStream.Close();
				//	tcpClient.Close();
				//	tcpNameList.RemoveAt(tcpClientList.IndexOf(tcpClient));
				//	tcpClientList.Remove(tcpClient);
				//}
			}
		}

		public static void BroadcastMessage(string msg, TcpClient excludeClient)
		{
			foreach (TcpClient client in tcpClientList)
			{
				NetworkStream networkStream = client.GetStream();
				ProtocolSI protocolSI = new ProtocolSI();
				byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, msg);
				networkStream.Write(packet, 0, packet.Length);
			}
		}
	}
}