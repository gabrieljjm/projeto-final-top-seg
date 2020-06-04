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
		private bool autenticated = false;

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
					case ProtocolSICmdType.USER_OPTION_3:
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
			tbChat.Text = "";
            if (!autenticated)
            {
				string username = tbJogador.Text;
				string pwd = tbPassword.Text;
				
				string encryptedusername = EncryptText(username);
				string encryptedpwd = EncryptText(pwd);

				string bytestring = encryptedusername + " " + encryptedpwd;
				byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, bytestring);
				networkStream.Write(packet, 0, packet.Length);

				networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
				string option = DecryptText(protocolSI.GetStringFromData());
				switch (option)
				{
					case "success":
						autenticated = true;
						tbJogador.Enabled = false;
						tbPassword.Enabled = false;
						tbSala.Enabled = true;
						btAutenticar.Text = "Jogar";
						tbChat.AppendText(string.Format("Autenticação bem sucedida!{0}{0}", Environment.NewLine));
						break;
					case "already":
						tbJogador.Text = "";
						tbPassword.Text = "";
						tbChat.AppendText(string.Format("A conta que está a tentar usar está autenticada neste momento noutro PC.{0}{0}", Environment.NewLine));
						break;
					case "wrong":
						tbChat.AppendText(string.Format("Autenticação falhada.{0}As credenciais fornecidas estão erradas.{0}{0}", Environment.NewLine));
						break;
				}
            }
            else
            {
				string room = tbSala.Text;

				string encryptedroom = EncryptText(room);
				byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, encryptedroom);
				networkStream.Write(packet, 0, packet.Length);

				networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
				string option = DecryptText(protocolSI.GetStringFromData());
				switch (option)
				{
					case "empty":
						tbChat.AppendText(string.Format("Criou a sala {0}!{1}Partilhe o nome da sala para alguém se juntar!{1}{1}", room, Environment.NewLine));
						break;
					case "join":
						tbChat.AppendText(string.Format("Juntou-se à sala {0}!{1}{1}", room, Environment.NewLine));
						break;
					case "full":
						tbChat.AppendText(string.Format("A sala {0} está cheia.{1}Tente outra sala ou espere que esta fique vazia!{1}{1}", room, Environment.NewLine));
						break;
				}
			}
        }

		public string DecryptText(string txt)
		{
			//VARIÁVEL PARA GUARDAR O TEXTO CIFRADO EM BYTES
			byte[] txtCifrado = Convert.FromBase64String(txt);
			//RESERVAR ESPAÇO NA MEMÓRIA PARA COLOCAR O TEXTO E CIFRÁ-LO
			MemoryStream ms = new MemoryStream(txtCifrado);
			//INICIALIZAR O SISTEMA DE CIFRAGEM (READ)
			CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
			//VARIÁVEL PARA GUARDAR O TEXTO DECIFRADO
			byte[] txtDecifrado = new byte[ms.Length];
			//VARIÁVEL PARA TER O NÚMERO DE BYTES DECIFRADOS
			//DECIFRAR OS DADOS
			int bytesLidos = cs.Read(txtDecifrado, 0, txtDecifrado.Length);
			cs.Close();
			//CONVERTER PARA TEXTO
			string textoDecifrado = Encoding.UTF8.GetString(txtDecifrado, 0, bytesLidos);
			//DEVOVLER TEXTO DECIFRADO
			return textoDecifrado;
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