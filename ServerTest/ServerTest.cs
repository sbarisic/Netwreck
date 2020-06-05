using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Netwrecking;

namespace ServerTest {
	class Program {
		static void Main(string[] args) {
			Console.Title = "Server Test";
			NetWreck NW = new NetWreck(42000);

			IPEndPoint Sender = null;

			while (true) {
				byte[] Data = NW.ReceiveRaw(out Sender);

				Console.WriteLine(Encoding.UTF8.GetString(Data));

				//Console.WriteLine("Received packet!");
				//NW.FreePacket(Packet);
			}
		}
	}
}
