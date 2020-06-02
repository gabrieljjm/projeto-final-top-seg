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
		private static List<string> tcpNameList = new List<string>();
		private static List<string> gameList = new List<string>();
		private const int SALTSIZE = 8;
		private const int NUMBER_OF_ITERATIONS = 50000;

		//private static string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\logs-jogo-do-galo\\" + $@"log{DateTime.Now.Ticks}.txt";

		private const int PORT = 10000;
		static void Main(string[] args)
		{
			//InsertUser();

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
                _ = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                switch (protocolSI.GetCmdType())
                {
					case ProtocolSICmdType.USER_OPTION_1:
                        _ = networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                        if (protocolSI.GetCmdType() == ProtocolSICmdType.DATA)
                        {
							string name = protocolSI.GetStringFromData();
							msg = (tcpNameList[tcpClientList.IndexOf(tcpClient)] + " - changed name to " + name);
							tcpNameList[tcpClientList.IndexOf(tcpClient)] = name;
							//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
							BroadcastMessage(msg);
							Console.WriteLine(msg);
                        }
						break;

					case ProtocolSICmdType.DATA:
						msg = (tcpNameList[tcpClientList.IndexOf(tcpClient)] + ": " + protocolSI.GetStringFromData());
						//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
						BroadcastMessage(msg);
						Console.WriteLine(msg);
						break;

					case ProtocolSICmdType.EOT:
						msg = (tcpNameList[tcpClientList.IndexOf(tcpClient)] + " disconnected");
						//File.AppendAllText(path, msg + Environment.NewLine, Encoding.UTF8);
						BroadcastMessage(msg);
						Console.WriteLine(msg);
						networkStream.Close();
						tcpClient.Close();
						tcpNameList.RemoveAt(tcpClientList.IndexOf(tcpClient));
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

		/// <summary>
		/// Insert user in database just for testing
		/// </summary>
		public static void InsertUser()
		{
			string path = Directory.GetCurrentDirectory();
			DirectoryInfo parentDir = Directory.GetParent(path);
			Console.WriteLine(path);
			//Alterar o finalpath = ""; para o caminho caso o programa não busque o caminho corretamente
			string finalpath = path + "\\ServerDB.mdf";

			string username = "Gabriel";//Alterar para inserir um username diferente (alterar sempre pois o username é UNIQUE)
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
				//MessageBox.Show("Registado com sucesso!");
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