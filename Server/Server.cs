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
		private static List<TcpClient> ListTcpClient = new List<TcpClient>();
		private static List<AesCryptoServiceProvider> ListAes = new List<AesCryptoServiceProvider>();
		private static List<string> ListPlayerName = new List<string>();
		private static List<string> ListRoom = new List<string>();
		private static bool loginqueue = false;
		private static bool roomqueue = false;

		private const int SALTSIZE = 8;
		private const int NUMBER_OF_ITERATIONS = 50000;

		//private static string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\logs-jogo-do-galo\\" + $@"log{DateTime.Now.Ticks}.txt";

		private const int PORT = 10000;
		static void Main(string[] args)
		{
			// INSERIR USER NA BASE DE DADOS
			//InsertUser();

			// GERAR UM CLIENTE FALSO
			//FakeClient();

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
				ListTcpClient.Add(tcpClient);
				int position = ListTcpClient.IndexOf(tcpClient);

				AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
				ListAes.Add(aes);

				ListPlayerName.Add("");
				ListRoom.Add("");

				NetworkStream networkStream = tcpClient.GetStream();
				ProtocolSI protocolSI = new ProtocolSI();
				networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
				string publickey = protocolSI.GetStringFromData();

				RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
				rsa.FromXmlString(publickey);

				byte[] packet;

				byte[] symmetrickeyencrypted = rsa.Encrypt(ListAes[position].Key, false);
				packet = protocolSI.Make(ProtocolSICmdType.SECRET_KEY, symmetrickeyencrypted);
				networkStream.Write(packet, 0, packet.Length);
				
				byte[] ivencrypted = rsa.Encrypt(ListAes[position].IV, false);
				packet = protocolSI.Make(ProtocolSICmdType.IV, ivencrypted);
				networkStream.Write(packet, 0, packet.Length);

				Thread thread = new Thread(() => ClientListener(position));
				thread.Start();
			}
		}

		public static void ClientListener(int pos)
		{
			TcpClient tcpClient = ListTcpClient[pos];
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
                        if (true)
                        {
							string combo = protocolSI.GetStringFromData();
							string[] arraycombo = combo.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
							//string room = Decrypt(arraycombo[0], pos);
							string username = DecryptText(arraycombo[0], pos);
							string pwd = DecryptText(arraycombo[1], pos);

							string codword;
							if (VerifyLogin(username, pwd))
							{
								//while (loginqueue) { }
								//loginqueue = true;
								if (!ListPlayerName.Contains(username))
								{
									// user autenticado
									ListPlayerName[pos] = username;
									//loginqueue = false;
									codword = EncryptText("success", pos);
									byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, codword);
									networkStream.Write(packet, 0, packet.Length);
								}
								else
								{
									// user já foi logado noutro client
									//loginqueue = false;
									codword = EncryptText("already", pos);
									byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, codword);
									networkStream.Write(packet, 0, packet.Length);
								}
							}
							else
							{
								// login errado
								codword = EncryptText("wrong", pos);
								byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, codword);
								networkStream.Write(packet, 0, packet.Length);
							}
						}
						break;

					case ProtocolSICmdType.USER_OPTION_2:
                        if (true)
                        {
							string room = DecryptText(protocolSI.GetStringFromData(), pos);
							//while (roomqueue) { }
							//roomqueue = true;
							int count = 0;
							foreach (var rm in ListRoom)
							{
								if (rm.Equals(room))
								{

									count++;
								}
							}
							if (ListRoom[pos].Equals(room))
							{
								count--;
							}
							string codword;
							if (count == 0)
							{
								// user cria uma sala
								ListRoom[pos] = room;
								//roomqueue = false;
								codword = EncryptText("empty", pos);
								byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, codword);
								networkStream.Write(packet, 0, packet.Length);
							}
							else
							{
								if (count == 1)
								{
									codword = EncryptText("join", pos);
									byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, codword);
									networkStream.Write(packet, 0, packet.Length);
									// user junta-se a uma sala
									if (!ListRoom[pos].Equals(""))
									{
										msg = ListPlayerName[pos] + " abandonou a sala!";
										BroadcastMessageRoom(msg, ListRoom[pos]);
									}
									ListRoom[pos] = room;
									//roomqueue = false;
									msg = ListPlayerName[pos] + " juntou-se à sala!";
									msg = string.Format("**{0} juntou-se à sala**", ListPlayerName[pos]);

									BroadcastMessageRoom(msg, ListRoom[pos]);
								}
								else
								{
									codword = EncryptText("full", pos);
									byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, codword);
									networkStream.Write(packet, 0, packet.Length);
									// a sala está cheia
									if (!ListRoom[pos].Equals(""))
                                    {
										msg = string.Format("**{0} abandonou a sala**", ListPlayerName[pos]);
										BroadcastMessageRoom(msg, ListRoom[pos]);
									}
									ListRoom[pos] = "";
									//roomqueue = false;
								}
							}
						}
						break;
					case ProtocolSICmdType.USER_OPTION_3:
                        if (true)
                        {
							string text = DecryptText(protocolSI.GetStringFromData(), pos);
							msg = string.Format("{0}: {1}", ListPlayerName[pos], text);
							BroadcastMessageRoom(msg, ListRoom[pos]);
						}
						break;
					case ProtocolSICmdType.EOT:
                        if (true)
                        {
							msg = (ListPlayerName[ListTcpClient.IndexOf(tcpClient)] + " disconnected");
							//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
							//BroadcastMessage(codword, room);
							Console.WriteLine(msg);
							networkStream.Close();
							tcpClient.Close();
							ListPlayerName.RemoveAt(ListTcpClient.IndexOf(tcpClient));
							ListTcpClient.Remove(tcpClient);
                        }
						break;

				}
			}
		}

		public static void BroadcastMessageRoom(string msg ,string room)
		{
            for (int i = 0; i < ListRoom.Count(); i++)
            {
                if (ListRoom[i].Equals(room))
                {
					NetworkStream networkStream = ListTcpClient[i].GetStream();
					ProtocolSI protocolSI = new ProtocolSI();
					byte[] packet = protocolSI.Make(ProtocolSICmdType.USER_OPTION_3, EncryptText(msg, i));
					networkStream.Write(packet, 0, packet.Length);
				}
            }
		}

		/// <summary>
		/// Desencripta uma string
		/// </summary>
		/// <param name="Texto para desencriptar"></param>
		/// <param name="Posição da AesCryptoServiceProvider"></param>
		/// <returns>String com texto desencriptado</returns>
		public static string DecryptText(string txt, int pos)
		{
			AesCryptoServiceProvider aes = ListAes[pos];
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
		/// <param name="Posição da AesCryptoServiceProvider"></param>
		/// <returns>String com texto encriptado</returns>
		private static string EncryptText(string text, int pos)
		{
			AesCryptoServiceProvider aes = ListAes[pos];
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

		/// <summary>
		/// Verifica o login
		/// </summary>
		/// <param name="Nome de Utilizador desencriptado"></param>
		/// <param name="Palavra-passe desencriptada"></param>
		/// <returns>True ou False</returns>
		public static bool VerifyLogin(string username, string password)
		{
			try
			{
				// Configurar ligação à Base de Dados
				SqlConnection conn = new SqlConnection();
				conn.ConnectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename='C:\Users\gaabr\Documents\Git\projeto-final-top-seg\Server\ServerDB.mdf';Integrated Security=True");
				//conn.ConnectionString = Properties.Settings.Default.connectionString;

				// Abrir ligação à Base de Dados
				conn.Open();

				// Declaração do comando SQL
				String sql = "SELECT * FROM Users WHERE Username = @username";
				SqlCommand cmd = new SqlCommand();
				cmd.CommandText = sql;

				// Declaração dos parâmetros do comando SQL
				SqlParameter param = new SqlParameter("@username", username);

				// Introduzir valor ao parâmentro registado no comando SQL
				cmd.Parameters.Add(param);

				// Associar ligação à Base de Dados ao comando a ser executado
				cmd.Connection = conn;

				// Executar comando SQL
				SqlDataReader reader = cmd.ExecuteReader();

				if (!reader.HasRows)
				{
					throw new Exception("Erro no acesso ao utilizador!");
				}

				// Ler resultado da pesquisa
				reader.Read();

				// Obter Hash (password + salt)
				byte[] saltedPasswordHashStored = (byte[])reader["SaltedPasswordHash"];

				// Obter salt
				byte[] saltStored = (byte[])reader["Salt"];

				conn.Close();

				byte[] hash = GenerateSaltedHash(password, saltStored);

				return saltedPasswordHashStored.SequenceEqual(hash);
			}
			catch
			{
				Console.WriteLine("Um erro ocorreu");
				return false;
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void FakeClient()
        {
			TcpClient tcpClient = new TcpClient();
			ListTcpClient.Add(tcpClient);
			AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
			ListAes.Add(aes);
			ListPlayerName.Add("AAAA");
			ListRoom.Add("B");
		}

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

			string username = "qwe";//Alterar para inserir um username diferente (alterar sempre pois o username é UNIQUE)
			string password = "123";//Alterar para inserir uma password diferente
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