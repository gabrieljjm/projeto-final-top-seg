using EI.SI;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
	class Server
	{
		private static List<TcpClient> tcpClientList = new List<TcpClient>();
		private static List<AesCryptoServiceProvider> aesList = new List<AesCryptoServiceProvider>();
		private static List<string> playerNameList = new List<string>();
		private static List<string> roomList = new List<string>();

		private const int SALTSIZE = 8;
		private const int NUMBER_OF_ITERATIONS = 50000;

		//private static string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\logs-jogo-do-galo\\" + $@"log{DateTime.Now.Ticks}.txt";

		private const int PORT = 10000;
		static void Main(string[] args)
		{
			// INSERIR USER NA BASE DE DADOS
			//InsertUser();

			// CRIAR UM CONJUNTO IP+PORTA DO CLIENTE
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, PORT);

			// CRIAR UM TCP LISTENER
			TcpListener tcpListener = new TcpListener(endpoint);
			tcpListener.Start();
			string msg = "Server Started!";
			//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
			Console.WriteLine(msg);
			while (true)
			{
				TcpClient tcpClient = tcpListener.AcceptTcpClient();
				tcpClientList.Add(tcpClient);
				int position = tcpClientList.IndexOf(tcpClient);

				AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
				aesList.Add(aes);

				playerNameList.Add("");
				roomList.Add("");

				NetworkStream networkStream = tcpClient.GetStream();
				ProtocolSI protocolSI = new ProtocolSI();
				networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
				string publickey = protocolSI.GetStringFromData();

				RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
				rsa.FromXmlString(publickey);

				byte[] packet;

				byte[] symmetrickeyencrypted = rsa.Encrypt(aesList[position].Key, false);
				packet = protocolSI.Make(ProtocolSICmdType.SECRET_KEY, symmetrickeyencrypted);
				networkStream.Write(packet, 0, packet.Length);
				
				byte[] ivencrypted = rsa.Encrypt(aesList[position].IV, false);
				packet = protocolSI.Make(ProtocolSICmdType.IV, ivencrypted);
				networkStream.Write(packet, 0, packet.Length);

				Thread thread = new Thread(() => ClientListener(position));
				thread.Start();
			}
		}

		public static void ClientListener(int pos)
		{
			TcpClient tcpClient = tcpClientList[pos];
			NetworkStream networkStream = tcpClient.GetStream();
			string msg = ("Client nr" + (pos + 1) + " connected");
			//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
			Console.WriteLine(msg);
			Console.WriteLine(pos);
			ProtocolSI protocolSI = new ProtocolSI();

			while (protocolSI.GetCmdType() != ProtocolSICmdType.EOT)
			{
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                switch (protocolSI.GetCmdType())
                {
					case ProtocolSICmdType.USER_OPTION_1:
						string combo = protocolSI.GetStringFromData();
						string[] arraycombo = combo.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

						//byte[] encryptedroom = Encoding.UTF8.GetBytes(arraycombo[0]);
						//byte[] encryptedusername = Encoding.UTF8.GetBytes(arraycombo[1]);
						//byte[] encryptedpwd = Encoding.UTF8.GetBytes(arraycombo[2]);

						/*
						string encryptedroom = protocolSI.GetStringFromData();
						networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
						string encryptedusername = protocolSI.GetStringFromData();
						networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
						string encryptedpwd = protocolSI.GetStringFromData();*/

						string room = Decrypt(arraycombo[0], pos);
                        string username = Decrypt(arraycombo[1], pos);
                        string pwd = Decrypt(arraycombo[2], pos);


                        break;

					case ProtocolSICmdType.EOT:
						msg = (playerNameList[tcpClientList.IndexOf(tcpClient)] + " disconnected");
						//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
						BroadcastMessage(msg);
						Console.WriteLine(msg);
						networkStream.Close();
						tcpClient.Close();
						playerNameList.RemoveAt(tcpClientList.IndexOf(tcpClient));
						tcpClientList.Remove(tcpClient);
						break;

				}
			}
		}

		public static void BroadcastMessage(string msg)
		{
			foreach (TcpClient client in tcpClientList)
			{
				NetworkStream networkStream = client.GetStream();
				ProtocolSI protocolSI = new ProtocolSI();
				byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, msg);
				networkStream.Write(packet, 0, packet.Length);
			}
		}

		public static string Decrypt(string txt, int pos)
		{
			AesCryptoServiceProvider aes = aesList[pos];
			//VARIÁVEL PARA GUARDAR O TEXTO CIFRADO EM BYTES
			byte[] txtCifrado = Convert.FromBase64String(txt);
			//RESERVAR ESPAÇO NA MEMÓRIA PARA COLOCAR O TEXTO E CIFRÁ-LO
			MemoryStream ms = new MemoryStream(txtCifrado);
			//INICIALIZAR O SISTEMA DE CIFRAGEM (READ)
			CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
			//VARIÁVEL PARA GUARDAR O TEXTO DECIFRADO
			byte[] txtDecifrado = new byte[ms.Length];
			//VARIÁVEL PARA TER O NÚMERO DE BYTES DECIFRADOS
			int bytesLidos = 0;
			//DECIFRAR OS DADOS
			bytesLidos = cs.Read(txtDecifrado, 0, txtDecifrado.Length);
			cs.Close();
			//CONVERTER PARA TEXTO
			string textoDecifrado = Encoding.UTF8.GetString(txtDecifrado, 0, bytesLidos);
			//DEVOVLER TEXTO DECIFRADO
			return textoDecifrado;
		}


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


		/// <summary>
		/// Insert user in database just for testing
		/// </summary>
		public static void InsertUser()
		{
			string path = Directory.GetCurrentDirectory();
			DirectoryInfo parentDir = Directory.GetParent(path);
			Console.WriteLine(path);
			// Alterar o finalpath = ""; para o caminho caso o programa não busque o caminho corretamente
			//string finalpath = path + "\\ServerDB.mdf";
			string finalpath = @"C:\Users\gaabr\Documents\Git\projeto-final-top-seg\Server\ServerDB.mdf";

			string username = "Francisco";//Alterar para inserir um username diferente (alterar sempre pois o username é UNIQUE)
			string password = "123abc456";//Alterar para inserir uma password diferente
			byte[] salt = GenerateSalt(SALTSIZE);
			byte[] saltedPasswordHash = GenerateSaltedHash(password, salt);

			SqlConnection conn;
			try
			{

				// Configurar ligação à Base de Dados
				conn = new SqlConnection();
				conn.ConnectionString = string.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename='{0}';Integrated Security=True", finalpath);
				//conn.ConnectionString = Properties.Settings.Default.connectionString;

				// Abrir ligação à Base de Dados
				conn.Open();

				// Declaração dos parâmetros do comando SQL
				SqlParameter paramUsername = new SqlParameter("@username", username);
				SqlParameter paramPassHash = new SqlParameter("@saltedPasswordHash", saltedPasswordHash);
				SqlParameter paramSalt = new SqlParameter("@salt", salt);

				// Declaração do comando SQL
				String sql = "INSERT INTO Users (Username, SaltedPasswordHash, Salt) VALUES (@username,@saltedPasswordHash,@salt)";

				// Prepara comando SQL para ser executado na Base de Dados
				SqlCommand cmd = new SqlCommand(sql, conn);

				// Introduzir valores aos parâmentros registados no comando SQL
				cmd.Parameters.Add(paramUsername);
				cmd.Parameters.Add(paramPassHash);
				cmd.Parameters.Add(paramSalt);

				// Executar comando SQL
				int lines = cmd.ExecuteNonQuery();

				// Fechar ligação
				conn.Close();
				if (lines == 0)
				{
					// Se forem devolvidas 0 linhas alteradas então o não foi executado com sucesso
					throw new Exception("Erro na inserção do utilizador");
				}
				Console.WriteLine("Registado com sucesso!");
			}
			catch (Exception e)
			{
				throw new Exception("Erro na inserção do utilizador" + e.Message);
			}
		}

		private static byte[] GenerateSalt(int size)
		{
			//Generate a cryptographic random number.
			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
			byte[] buff = new byte[size];
			rng.GetBytes(buff);
			return buff;
		}

		private static byte[] GenerateSaltedHash(string plainText, byte[] salt)
		{
			Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(plainText, salt, NUMBER_OF_ITERATIONS);
			return rfc2898.GetBytes(32);
		}
	}
}