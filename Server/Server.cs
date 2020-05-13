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
		private const int PORT = 10000;
		static void Main(string[] args)
		{
			// CRIAR UM CONJUNTO IP+PORTO DO CLIEENT
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, PORT);
			// CRIAR UM TCP LISTENER
			TcpListener listener = new TcpListener(endpoint);
			listener.Start();
			Console.WriteLine("Servidor Pronto para receber mensagens.");
			int clientCounter = 0;

			while (true)
			{
				TcpClient client = listener.AcceptTcpClient();
				clientCounter++;
				Console.WriteLine("Clientes {0} connectados!", clientCounter);
				//Irá dar erro pois ainda não criámos a class (ver abaixo)
				ClientHandler clientHandler = new ClientHandler(client, clientCounter);
				clientHandler.Handle();
			}
		}
	}

	//CRIAÇÃO DO CLIENTE NO SERVIDOR
	class ClientHandler
	{
		private TcpClient client;
		private int clientID;

		//PARA CRIAR O CLIENTE E DAR-LHE UM NÚMERO
		public ClientHandler(TcpClient client, int clientID)
		{
			this.client = client;
			this.clientID = clientID;
		}

		//Sistema Threads para monitorizar o input do utilizador, executar tarefas em background e para tratar de vários inputs em simultâneo
		public void Handle()
		{
			Thread thread = new Thread(threadHandler);
			thread.Start();
		}

		//ESTABELECER COMUNICAÇÃO E OBTER MENSAGENS DO CLIENTE
		private void threadHandler()
		{
			NetworkStream networkStream = this.client.GetStream();
			ProtocolSI protocolSI = new ProtocolSI();
			while (protocolSI.GetCmdType() != ProtocolSICmdType.EOT)
			{
				// LER DOS DADOS DO CLIENTE		
				int bytesRead = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
				// CRIAR RESPOSTA PARA O CLIENTE
				byte[] ack;
				switch (protocolSI.GetCmdType())
				{
					case ProtocolSICmdType.DATA:
						String texto = "Cliente " + clientID + ": " + protocolSI.GetStringFromData();
						Console.WriteLine(texto);
						// CRIAR RESPOSTA PARA O CLIENTE COM PROTOCOLSI
						ack = protocolSI.Make(ProtocolSICmdType.ACK);
						// ENVIAR RESPOSTA PARA O CLIENTE
						networkStream.Write(ack, 0, ack.Length);
						//byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, texto);
						//networkStream.Write(packet, 0, packet.Length);
						break;
					case ProtocolSICmdType.EOT:
						Console.WriteLine("Fim do Thread do Cliente {0}", clientID);
						// CRIAR RESPOSTA PARA O CLIENTE COM PROTOCOLSI
						ack = protocolSI.Make(ProtocolSICmdType.ACK);
						// ENVIAR RESPOSTA PARA O CLIENTE
						networkStream.Write(ack, 0, ack.Length);
						break;
				}
			}
			// FECHA  A LIGAÇÕES
			networkStream.Close();
			client.Close();
		}
	}
}
