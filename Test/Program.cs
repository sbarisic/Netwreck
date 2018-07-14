using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Netwrecking;

namespace Test {
	class Program {
		static bool Receiver = false;

		static void Main(string[] Args) {
			/*foreach (var Arg in Args)
				if (Arg == "--receiver")
					Receiver = true;

			NetWreck NW = new NetWreck(Receiver ? 42000 : 42001);

			if (Receiver)
				DoReceive(NW);
			else
				DoSend(NW);*/

			NetWreck NW = new NetWreck(42000);
			DoReceive(NW);
		}

		static void DoReceive(NetWreck Net) {
			while (true) {
				NetPacket Packet = Net.ReceiveRaw();
				byte[] Rotation = Packet.RawData.Skip(36).Take(12).Reverse().ToArray();

				float Z = BitConverter.ToSingle(Rotation, 0) + 180;
				float Y = BitConverter.ToSingle(Rotation, 4) + 180;
				float X = BitConverter.ToSingle(Rotation, 8) + 180;

				//Console.WriteLine("X = {0}; Y = {1}; Z = {2}", X, Y, Z);
				Console.WriteLine(X);

				/*Console.WriteLine("Received `{0}´", Encoding.UTF8.GetString(Packet.RawData));
				Net.SendRaw(Encoding.UTF8.GetBytes("Data received!"), Packet.Sender);*/
			}
		}

		static void DoSend(NetWreck Net) {
			while (true) {
				Console.Write("> ");
				Net.SendRaw(Encoding.UTF8.GetBytes(Console.ReadLine()), "127.0.0.1", 42000);
				Console.WriteLine(Encoding.UTF8.GetString(Net.ReceiveRaw().RawData));
			}
		}
	}
}
