using EI.SI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjetoFinalTopSeg
{
    public partial class FormClient1 : Form
    {
		private RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
		private AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
		private const int PORT = 10000;
		private NetworkStream networkStream;
		private ProtocolSI protocolSI;
		private TcpClient client;
		private Thread thread;

		public FormClient1()
        {
			InitializeComponent();
			
			// CRIA CHAVE PUBLICA
			string publickey = rsa.ToXmlString(false);
			string bothkeys = rsa.ToXmlString(true);

			// CRIAR UM CONJUNTO IP+PORTA DO SERVIDOR
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, PORT);
			// CRIAR O CLIENTE TCP
			client = new TcpClient();
			// EFETUAR A LIGAÇÃO AO SERVIDOR
			client.Connect(endpoint);
			// OBTER A LIGAÇÃO DO SERVIDOR
			networkStream = client.GetStream();
			protocolSI = new ProtocolSI();
			byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, publickey);
			networkStream.Write(packet, 0, packet.Length);

			networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

			byte[] privatekeyencrypted = protocolSI.GetData();
			byte[] privatekey = rsa.Decrypt(privatekeyencrypted, false);

			aes.Key = privatekey;

			thread = new Thread(ThreadHandler);
			thread.Start();
		}

		private void ThreadHandler()
		{
			while (true)
			{
				networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                switch (protocolSI.GetCmdType())
                {
					case ProtocolSICmdType.EOF:
						break;
					case ProtocolSICmdType.DATA:
						tbChat.Invoke((Action)delegate
						{
							tbChat.AppendText(protocolSI.GetStringFromData() + Environment.NewLine);
						});
						break;
					case ProtocolSICmdType.USER_OPTION_1:

						break;
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

        private void FormClient1_FormClosing(object sender, FormClosingEventArgs e)
		{
			thread.Abort();
			//EOT - End Of Transmission
			byte[] eot = protocolSI.Make(ProtocolSICmdType.EOT);
			networkStream.Write(eot, 0, eot.Length);
			//Fechar todas as ligações
			networkStream.Close();
			client.Close();
		}

        private void btAutenticar_Click(object sender, EventArgs e)
        {
			byte[] opt1 = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1);
			networkStream.Write(opt1, 0, opt1.Length);
			//Enviar mensagem de cliente para servidor
			string msg = tbJogador.Text;
			tbJogador.Clear();
			// ProtocolSICmdTyp. - interpreta o tipo de mensagem/pacote recebido
			// protocolSI.Make() - cria uma mensagem/pacote de um tipo específico
			byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, msg);
			// ENVIAR A MENSAGEM PELA LIGAÇÃO
			networkStream.Write(packet, 0, packet.Length);
		}
    }
}