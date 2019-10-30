using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Netwrecking;

namespace ServerTest {
	class Program {
		static void Main(string[] args) {
			Console.Title = "Server Test";
			NetWreck NW = new NetWreck(42000);

			while (true) {
				NetPacket Packet = NW.ReceiveRaw();
				Console.WriteLine("Received packet!");
				NW.FreePacket(Packet);
			}
		}
	}
}
