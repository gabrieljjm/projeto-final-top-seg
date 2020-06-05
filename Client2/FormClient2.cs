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

namespace Client2
{
    public partial class FormClient2 : Form
    {
		private AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
		private const int PORT = 10000;
		private NetworkStream networkStream;
		private ProtocolSI protocolSI;
		private TcpClient client;
		private Thread thread;
		private bool autenticated = false;
		private string currentRoom = "";
		private string[] gameState = new string[10] { "", "N", "N", "N", "N", "N", "N", "N", "N", "N" };
		private bool owner = false;

		public FormClient2()
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

			thread = new Thread(ServerListener);
			thread.Start();
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
					case ProtocolSICmdType.USER_OPTION_1:
						if (true)
						{
							string option = DecryptText(protocolSI.GetStringFromData());
							Login(option);
						}
						break;
					case ProtocolSICmdType.USER_OPTION_2:
						if (true)
						{
							string option = DecryptText(protocolSI.GetStringFromData());
							ChangedRoom(option);
						}
						break;
					case ProtocolSICmdType.USER_OPTION_3:
						if (true)
						{
							string msg = DecryptText(protocolSI.GetStringFromData());
							ChatReceived(msg);
						}
						break;
					case ProtocolSICmdType.USER_OPTION_4:
						if (true)
						{
							string msg = DecryptText(protocolSI.GetStringFromData());
							OpponentJoinedRoom(msg);
						}
						break;
					case ProtocolSICmdType.USER_OPTION_5:
						if (true)
						{
							string msg = DecryptText(protocolSI.GetStringFromData());
							OpponentLeftRoom(msg);
						}
						break;
					case ProtocolSICmdType.USER_OPTION_6:
						if (true)
						{
							string msg = DecryptText(protocolSI.GetStringFromData());
							JoinedRoom(msg);
						}
						break;
					case ProtocolSICmdType.USER_OPTION_7:
						if (true)
						{
							string msg = DecryptText(protocolSI.GetStringFromData());
							gameState = msg.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
							UpdateGame();
						}
						break;
					case ProtocolSICmdType.USER_OPTION_8:
						if (true)
						{
							string option = DecryptText(protocolSI.GetStringFromData());
							GameOver(option);
						}
						break;
				}
			}
		}

		private void Login(string option)
		{
			switch (option)
			{
				case "success":
					autenticated = true;
					Invoke((Action)delegate
					{
						tbChat.AppendText(string.Format("**autenticação bem sucedida**"));
						tbSala.Enabled = true;
						btAutenticar.Enabled = true;
						btAutenticar.Text = "Jogar";
					});
					break;
				case "already":
					Invoke((Action)delegate
					{
						tbChat.AppendText(string.Format("**autenticação falhada**{0}**conta em uso**", Environment.NewLine));
						btAutenticar.Enabled = true;
						tbJogador.Enabled = true;
						tbJogador.Text = "";
						tbPassword.Enabled = true;
						tbPassword.Text = "";
					});
					break;
				case "wrong":
					Invoke((Action)delegate
					{
						tbChat.AppendText(string.Format("**autenticação falhada**{0}**as credenciais fornecidas estão erradas**", Environment.NewLine));
						btAutenticar.Enabled = true;
						tbJogador.Enabled = true;
						tbPassword.Enabled = true;
					});
					break;
			}
		}

		private void ChangedRoom(string option)
		{
			switch (option)
			{
				case "empty":
					Invoke((Action)delegate
					{
						tbChat.Text = "";
						tbChat.AppendText(string.Format("**você criou a sala**{0}", Environment.NewLine));
						btAutenticar.Enabled = true;
						tbSala.Enabled = true;
						btEnviar.Enabled = true;
						tbMensagem.Enabled = true;
						lbJogador1.Text = tbJogador.Text;
						lbJogador2.Text = "";
						lbWins1.Text = "0";
						lbWins2.Text = "";
						lbEmpates.Text = "0";
						panel1.Enabled = false;
						button1.Enabled = true;
						button2.Enabled = true;
						button3.Enabled = true;
						button4.Enabled = true;
						button5.Enabled = true;
						button6.Enabled = true;
						button7.Enabled = true;
						button8.Enabled = true;
						button9.Enabled = true;

						button1.Text = "";
						button2.Text = "";
						button3.Text = "";
						button4.Text = "";
						button5.Text = "";
						button6.Text = "";
						button7.Text = "";
						button8.Text = "";
						button9.Text = "";

						lbJogador1.Text = tbJogador.Text;
						lbJogador2.Text = "";
						lbWins1.Text = "0";
						lbWins2.Text = "";
						lbEmpates.Text = "0";
					});
					break;
				case "join":
					Invoke((Action)delegate
					{
						tbChat.Text = "";
						btAutenticar.Enabled = true;
						tbSala.Enabled = true;
						btEnviar.Enabled = true;
						tbMensagem.Enabled = true;
						lbJogador1.Text = tbJogador.Text;
						lbJogador2.Text = "";
						lbWins1.Text = "0";
						lbWins2.Text = "0";
						lbEmpates.Text = "0";
					});
					break;
				case "full":
					Invoke((Action)delegate
					{
						tbChat.Text = "";
						tbChat.AppendText(string.Format("**a sala está cheia**{0}", Environment.NewLine));
						currentRoom = "";
						btAutenticar.Enabled = true;
						tbSala.Enabled = true;
						lbJogador1.Text = tbJogador.Text;
						lbJogador2.Text = "";
						lbWins1.Text = "0";
						lbWins2.Text = "";
						lbEmpates.Text = "0";
					});
					break;
			}
		}

		private void ChatReceived(string msg)
		{
			Invoke((Action)delegate
			{
				tbChat.AppendText(msg + Environment.NewLine);
			});
		}

		private void OpponentJoinedRoom(string msg)
		{
			Invoke((Action)delegate
			{
				tbChat.AppendText(string.Format("{0} entrou na sala{1}", msg, Environment.NewLine));

				lbJogador1.Text = tbJogador.Text;
				lbJogador2.Text = msg;
				lbWins1.Text = "0";
				lbWins2.Text = "0";
				lbEmpates.Text = "0";
			});
			owner = true;
			StartGame();
		}

		private void OpponentLeftRoom(string msg)
		{
			Invoke((Action)delegate
			{
				tbChat.AppendText(string.Format("{0} abandonou a sala{1}", msg, Environment.NewLine));
				panel1.Enabled = false;

				button1.Enabled = true;
				button2.Enabled = true;
				button3.Enabled = true;
				button4.Enabled = true;
				button5.Enabled = true;
				button6.Enabled = true;
				button7.Enabled = true;
				button8.Enabled = true;
				button9.Enabled = true;

				button1.Text = "";
				button2.Text = "";
				button3.Text = "";
				button4.Text = "";
				button5.Text = "";
				button6.Text = "";
				button7.Text = "";
				button8.Text = "";
				button9.Text = "";

				lbJogador1.Text = tbJogador.Text;
				lbJogador2.Text = "";
				lbWins1.Text = "0";
				lbWins2.Text = "";
				lbEmpates.Text = "0";
			});
		}

		private void JoinedRoom(string msg)
		{
			Invoke((Action)delegate
			{
				lbJogador2.Text = msg;
			});
			owner = false;
			StartGame();
		}

		private void StartGame()
		{
			for (int i = 0; i < gameState.Length; i++)
			{
				gameState[i] = "N";
			}
			if (owner)
			{
				gameState[0] = "O";
				Invoke((Action)delegate
				{
					panel1.Enabled = true;
				});
			}
			else
			{
				gameState[0] = "X";
				Invoke((Action)delegate
				{
					panel1.Enabled = false;
				});
			}
			Invoke((Action)delegate
			{
				button1.Enabled = true;
				button2.Enabled = true;
				button3.Enabled = true;
				button4.Enabled = true;
				button5.Enabled = true;
				button6.Enabled = true;
				button7.Enabled = true;
				button8.Enabled = true;
				button9.Enabled = true;

				button1.Text = "";
				button2.Text = "";
				button3.Text = "";
				button4.Text = "";
				button5.Text = "";
				button6.Text = "";
				button7.Text = "";
				button8.Text = "";
				button9.Text = "";
			});

		}

		private void UpdateGame()
		{

			if (owner)
			{
				gameState[0] = "O";
			}
			else
			{
				gameState[0] = "X";
			}

			Invoke((Action)delegate
			{
				if (gameState[1] != "N")
					button1.Text = gameState[1];
				//
				if (gameState[2] != "N")
					button2.Text = gameState[2];
				//
				if (gameState[3] != "N")
					button3.Text = gameState[3];
				//
				if (gameState[4] != "N")
					button4.Text = gameState[4];
				//
				if (gameState[5] != "N")
					button5.Text = gameState[5];
				//
				if (gameState[6] != "N")
					button6.Text = gameState[6];
				//
				if (gameState[7] != "N")
					button7.Text = gameState[7];
				//
				if (gameState[8] != "N")
					button8.Text = gameState[8];
				//
				if (gameState[9] != "N")
					button9.Text = gameState[9];
				//
				if (button1.Text == "")
					button1.Enabled = true;
				else
					button1.Enabled = false;
				//
				if (button2.Text == "")
					button2.Enabled = true;
				else
					button2.Enabled = false;
				//
				if (button3.Text == "")
					button3.Enabled = true;
				else
					button3.Enabled = false;
				//
				if (button4.Text == "")
					button4.Enabled = true;
				else
					button4.Enabled = false;
				//
				if (button5.Text == "")
					button5.Enabled = true;
				else
					button5.Enabled = false;
				//
				if (button6.Text == "")
					button6.Enabled = true;
				else
					button6.Enabled = false;
				//
				if (button7.Text == "")
					button7.Enabled = true;
				else
					button7.Enabled = false;
				//
				if (button8.Text == "")
					button8.Enabled = true;
				else
					button8.Enabled = false;
				//
				if (button9.Text == "")
					button9.Enabled = true;
				else
					button9.Enabled = false;

				panel1.Enabled = true;
			});
		}

		private void GameOver(string option)
		{
			switch (option)
			{
				case "loss":
					Invoke((Action)delegate
					{
						tbChat.AppendText(string.Format("**você perdeu**{0}", Environment.NewLine));
						int losses = Convert.ToInt32(lbWins2.Text);
						losses++;
						lbWins2.Text = Convert.ToString(losses);
						owner = true;
					});
					break;
				case "won":
					Invoke((Action)delegate
					{
						tbChat.AppendText(string.Format("**você ganhou**{0}", Environment.NewLine));
						int wins = Convert.ToInt32(lbWins1.Text);
						wins++;
						lbWins1.Text = Convert.ToString(wins);
						owner = false;
					});
					break;
				case "tie":
					Invoke((Action)delegate
					{
						tbChat.AppendText(string.Format("**empate**{0}", Environment.NewLine));
						int ties = Convert.ToInt32(lbEmpates.Text);
						ties++;
						lbEmpates.Text = Convert.ToString(ties);
					});
					break;
			}
			StartGame();
		}

		private void btEnviar_Click(object sender, EventArgs e)
		{
			if (!tbMensagem.Text.Equals(""))
			{
				//Enviar mensagem de cliente para servidor
				string encryptedtext = EncryptText(tbMensagem.Text.Trim());
				tbMensagem.Clear();
				// ProtocolSICmdTyp. - interpreta o tipo de mensagem/pacote recebido
				// protocolSI.Make() - cria uma mensagem/pacote de um tipo específico
				byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_3, encryptedtext);
				// ENVIAR A MENSAGEM PELA LIGAÇÃO
				networkStream.Write(packet, 0, packet.Length);
			}
		}

		private void btAutenticar_Click(object sender, EventArgs e)
		{
			if (!autenticated)
			{
				if (!tbJogador.Text.Equals("") && !tbPassword.Text.Equals(""))
				{
					if (!tbJogador.Text.Contains(" ") || !tbPassword.Text.Contains(" "))
					{
						tbChat.Text = "";
						btAutenticar.Enabled = false;
						tbJogador.Enabled = false;
						tbPassword.Enabled = false;
						string encryptedusername = EncryptText(tbJogador.Text);
						string encryptedpwd = EncryptText(tbPassword.Text);

						string bytestring = encryptedusername + " " + encryptedpwd;
						byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, bytestring);
						networkStream.Write(packet, 0, packet.Length);
					}
					else
					{
						MessageBox.Show("O nome de jogador e a password não podem ter espaços!");
					}
				}
				else
				{
					MessageBox.Show("Preencha os campos todos!");
				}
			}
			else
			{
				if (!tbSala.Text.Equals(""))
				{
					if (!tbSala.Text.Contains(" "))
					{
						if (!tbSala.Text.Equals(currentRoom))
						{
							tbChat.Text = "";
							btEnviar.Enabled = false;
							tbMensagem.Enabled = false;
							btAutenticar.Enabled = false;
							tbSala.Enabled = false;
							currentRoom = tbSala.Text;

							string encryptedroom = EncryptText(tbSala.Text);
							byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, encryptedroom);
							networkStream.Write(packet, 0, packet.Length);
						}
						else
						{
							MessageBox.Show("Insira uma sala diferente da atual!");
						}
					}
					else
					{
						MessageBox.Show("A sala não pode ter espaços!");
					}
				}
				else
				{
					MessageBox.Show("Insira uma sala!");
				}
			}
		}

		/// <summary>
		/// Desencripta uma string
		/// </summary>
		/// <param name="Texto para desencriptar"></param>
		/// <returns>String com texto desencriptado</returns>
		private string DecryptText(string txt)
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

		/// <summary>
		/// Encripta uma string
		/// </summary>
		/// <param name="Texto para encriptar"></param>
		/// <returns>String com texto encriptado</returns>
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

		private void SendGameState()
		{
			panel1.Enabled = false;
			string gamestring = "";
			foreach (var item in gameState)
			{
				gamestring += item + " ";
			}
			string encryptedtext = EncryptText(gamestring.Trim());
			byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_7, encryptedtext);
			networkStream.Write(packet, 0, packet.Length);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button1.Text = symbol;
			gameState[1] = symbol;
			SendGameState();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button2.Text = symbol;
			gameState[2] = symbol;
			SendGameState();
		}

		private void button3_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button3.Text = symbol;
			gameState[3] = symbol;
			SendGameState();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button4.Text = symbol;
			gameState[4] = symbol;
			SendGameState();
		}

		private void button5_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button5.Text = symbol;
			gameState[5] = symbol;
			SendGameState();
		}

		private void button6_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button6.Text = symbol;
			gameState[6] = symbol;
			SendGameState();
		}

		private void button7_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button7.Text = symbol;
			gameState[7] = symbol;
			SendGameState();
		}

		private void button8_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button8.Text = symbol;
			gameState[8] = symbol;
			SendGameState();
		}

		private void button9_Click(object sender, EventArgs e)
		{
			string symbol = "X";
			if (owner)
			{
				symbol = "O";
			}
			button9.Text = symbol;
			gameState[9] = symbol;
			SendGameState();
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