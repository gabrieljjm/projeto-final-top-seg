using EI.SI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client2
{
    public partial class FormClient2 : Form
    {
		private const int PORT = 10000;
		NetworkStream networkStream;
		ProtocolSI protocolSI;
		TcpClient client;
		Thread thread;

		public FormClient2()
		{
			InitializeComponent();/*
			// CRIAR UM CONJUNTO IP+PORTA DO SERVIDOR
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, PORT);
			// CRIAR O CLIENTE TCP
			client = new TcpClient();
			// EFETUAR A LIGAÇÃO AO SERVIDOR
			client.Connect(endpoint);
			// OBTER A LIGAÇÃO DO SERVIDOR
			networkStream = client.GetStream();
			protocolSI = new ProtocolSI();

			string msg = "Username2";
			byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, msg);
			networkStream.Write(packet, 0, packet.Length);

			thread = new Thread(threadHandler);
			thread.Start();*/
		}

		private void threadHandler()
		{
			while (true)
			{
				networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
				if (protocolSI.GetCmdType() == ProtocolSICmdType.EOF)
				{
					break;
				}
				if (protocolSI.GetCmdType() == ProtocolSICmdType.DATA)
				{
					tbChat.Invoke((Action)delegate
					{
						tbChat.AppendText(protocolSI.GetStringFromData() + Environment.NewLine);
					});
				}
			}
		}

		private void btEnviar_Click(object sender, EventArgs e)
		{
			//Enviar mensagem de cliente para servidor
			string msg = tbMensagem.Text;
			tbMensagem.Clear();
			// ProtocolSICmdTyp. - interpreta o tipo de mensagem/pacote recebido
			// protocolSI.Make() - cria uma mensagem/pacote de um tipo específico
			byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, msg);
			// ENVIAR A MENSAGEM PELA LIGAÇÃO
			networkStream.Write(packet, 0, packet.Length);
		}

		private void FormClient2_FormClosing(object sender, FormClosingEventArgs e)
		{
			thread.Abort();
			//EOT - End Of Transmission
			byte[] eot = protocolSI.Make(ProtocolSICmdType.EOT);
			networkStream.Write(eot, 0, eot.Length);
			//Fechar todas as ligações
			networkStream.Close();
			client.Close();
		}
    }
}