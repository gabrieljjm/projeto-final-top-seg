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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjetoFinalTopSeg
{
    public partial class FormClient1 : Form
    {
		private const int PORT = 10000;
		NetworkStream networkStream;
		ProtocolSI protocolSI;
		TcpClient client;

		public FormClient1()
        {
			InitializeComponent();
			// CRIAR UM CONJUNTO IP+PORTO DO SERVIDOR
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, PORT);
			// CRIAR O CLIENTE TCP
			client = new TcpClient();
			// EFETUAR A LIGAÇÃO AO SERVIDOR
			client.Connect(endpoint);
			// OBTER A LIGAÇÃO DO SERVIDOR
			networkStream = client.GetStream();
			protocolSI = new ProtocolSI();
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

			while (protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
			{
				//Buffer - propriedade que permite armazenar a mensagem/pacote recebida
				networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
			}
		}

		private void FormClient1_FormClosing(object sender, FormClosingEventArgs e)
		{
			//EOT - End Of Transmission
			byte[] eot = protocolSI.Make(ProtocolSICmdType.EOT);
			networkStream.Write(eot, 0, eot.Length);
			networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
			//Fechar todas as ligações
			networkStream.Close();
			client.Close();
		}
	}
}
