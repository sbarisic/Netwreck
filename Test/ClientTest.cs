using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Netwrecking;
using System.Net;

namespace ClientTest {
	class Program {
		static void Main(string[] Args) {
			Console.Title = "Client Test";
			Thread.Sleep(1000);

			NetWreck NW = new NetWreck(42001);
			IPEndPoint ServerEndPoint = NetWreck.CreateEndPoint("127.0.0.1", 42000);

			//byte[] LargeData = new byte[2049];

			int Len = 0;

			while (true) {
				/*Console.Write("Input: ");
				string In = Console.ReadLine();*/

				if (Len > 8)
					Len = 0;
				Len++;

				Thread.Sleep(100);
				NW.SendRaw(Encoding.UTF8.GetBytes(new string('-', Len)), ServerEndPoint);
			}
		}
	}
}
