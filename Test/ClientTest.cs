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
			Console.WriteLine("Starting");

			NetWreck NW = new NetWreck(42001);
			NetWreckClient Srv = null;

			NW.OnClientConnected += (Cli) => {
				Console.WriteLine("Connected");
				Srv = Cli;
			};

			NW.OnClientDisconnected += (Cli) => {
				Console.WriteLine("Disconnected");
			};

			NW.StartUpdateLoop();
			NW.ConnectToServer(NetWreck.CreateEndPoint("127.0.0.1", 42000));

			int Len = 0;

			while (true) {
				Thread.Sleep(0);

				if (Srv == null)
					continue;

				if (NW.LastSendTime(Srv) > 100) {
					if (Len++ > 8)
						Len = 1;

					NetPacket Packet = NW.AllocPacket();
					Packet.Type = PacketType.Default;
					Packet.Payload = Encoding.UTF8.GetBytes(new string('-', Len));

					NW.SendPacket(Packet, Srv);
					NW.FreePacket(Packet);
				}
			}
		}
	}
}
