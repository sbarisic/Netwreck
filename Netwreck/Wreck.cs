using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Netwrecking {
	public struct NetPacket {
		public IPEndPoint Sender;
		public byte[] RawData;
	}

	public class NetWreck {
		public static IPEndPoint CreateEndPoint(string IP, int Port) {
			return new IPEndPoint(IPAddress.Parse(IP), Port);
		}

		UdpClient UDP;
		int Port;

		public NetWreck(int Port) {
			UDP = new UdpClient(Port);
			this.Port = Port;
		}

		public void SendRaw(byte[] Data, IPEndPoint EndPoint) {
			if (Data.Length > 512)
				throw new Exception("Data packet too large");

			UDP.Send(Data, Data.Length, EndPoint);
		}

		public void SendRaw(byte[] Data, string IP, int Port) {
			SendRaw(Data, CreateEndPoint(IP, Port));
		}

		public NetPacket ReceiveRaw() {
			NetPacket Packet = new NetPacket();
			Packet.Sender = new IPEndPoint(IPAddress.Any, Port);
			Packet.RawData = UDP.Receive(ref Packet.Sender);
			return Packet;
		}
	}
}
