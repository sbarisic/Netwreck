using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Netwrecking;
using System.Net;

namespace ClientTest {
	class Program {
		static void Main(string[] Args) {
			Console.Title = "Client Test";
			NetWreck NW = new NetWreck(42001);

			IPEndPoint ServerEndPoint = NetWreck.CreateEndPoint("127.0.0.1", 42000);
			byte[] LargeData = new byte[2049];

			

			while (true) {
				Console.Write("Input: ");
				string In = Console.ReadLine();

				NW.SendRaw(Encoding.UTF8.GetBytes(In), ServerEndPoint);
			}
		}
	}
}
