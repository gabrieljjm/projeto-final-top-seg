using EI.SI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
		private AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
		private const int PORT = 10000;
		private NetworkStream networkStream;
		private ProtocolSI protocolSI;
		private TcpClient client;
		private Thread thread;

		public FormClient1()
        {
			InitializeComponent();

			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
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
			byte[] packet = protocolSI.Make(ProtocolSICmdType.PUBLIC_KEY, publickey);
			networkStream.Write(packet, 0, packet.Length);

			networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

			byte[] symmetrickeyencrypted = protocolSI.GetData();
			byte[] symmetrickey = rsa.Decrypt(symmetrickeyencrypted, false);

			networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

			byte[] ivencrypted = protocolSI.GetData();
			byte[] iv = rsa.Decrypt(ivencrypted, false);

			aes.Key = symmetrickey;
			aes.IV = iv;

			//thread = new Thread(ServerListener);
			//thread.Start();
		}

		private void ServerListener()
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
			string encryptedroom = EncryptText(tbServidor.Text);
			string encryptedusername = EncryptText(tbJogador.Text);
			string encryptedpwd = EncryptText(tbPassword.Text);

			string bytestring = encryptedroom + " " + encryptedusername + " " + encryptedpwd;
			byte[] packetroom = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, bytestring);
			networkStream.Write(packetroom, 0, packetroom.Length);
			//byte[] packetusername = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, encryptedusername);
			//networkStream.Write(packetusername, 0, packetusername.Length);
			//byte[] packetpwd = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, encryptedpwd);
			//networkStream.Write(packetpwd, 0, packetpwd.Length);
        }

		private string EncryptText(string text)
        {
			//VARIÁVEL PARA GUARDAR O TEXTO DECIFRADO EM BYTES
			byte[] txtDecifrado = Encoding.UTF8.GetBytes(text);
			//VARIÁVEL PARA GUARDAR O TEXTO CIFRADO EM BYTES
			byte[] txtCifrado;
			//RESERVARA ESPAÇO NA MEMÓRIA PARA COLOCAR O TEXTO E CIFRÁ-LO
			MemoryStream ms = new MemoryStream();
			//INICIALIZAR O SISTEMA DE CIFRAGEM (WRITE)
			CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write); ;
			//CIFRAR OS DADOS
			cs.Write(txtDecifrado, 0, txtDecifrado.Length);
			cs.Close();
			//GUARDAR OS DADOS CIFRADO QUE ESTÃO NA MEMÓRIA
			txtCifrado = ms.ToArray();
			//CONVERTER OS BYTES PARA BASE64 (TEXTO)
			string txtCifradoB64 = Convert.ToBase64String(txtCifrado);
			//DEVOLVER OS BYTES CRIADOS EM BASE64
			return txtCifradoB64;
		}
	}
}