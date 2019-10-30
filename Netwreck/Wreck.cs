using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;

namespace Netwrecking {
	public class NetPacket {
		public IPEndPoint Sender;
		public byte[] RawData;

		internal NetPacket() {
		}
	}


	struct WreckMessage {
		public enum MessageType : int {
			None = 0,
			BeginMessage,
			MessageData,
			EndMessage
		}

		public int PacketNumber;
		public MessageType Type;

		public WreckMessage(int PacketNumber, MessageType Type) {
			this.PacketNumber = PacketNumber;
			this.Type = Type;
		}
	}

	public unsafe class NetWreck {
		const int MaxDataSize = 1024;
		const bool ARTIFICAL_DELAY = false;

		public static IPEndPoint CreateEndPoint(string IP, int Port) {
			return new IPEndPoint(IPAddress.Parse(IP), Port);
		}

		ConcurrentQueue<NetPacket> PacketPool;
		UdpClient UDP;
		int Port;

		public NetWreck(int Port, int PacketPoolSize = 128) {
			PacketPool = new ConcurrentQueue<NetPacket>();
			for (int i = 0; i < PacketPoolSize; i++)
				PacketPool.Enqueue(new NetPacket());

			UDP = new UdpClient(Port);
			this.Port = Port;
		}

		public void FreePacket(NetPacket Packet) {
			if (PacketPool.Contains(Packet))
				throw new Exception("Can not free packet");

			PacketPool.Enqueue(Packet);
		}

		public NetPacket AllocPacket() {
			for (int i = 0; i < 1000; i++) {
				if (PacketPool.TryDequeue(out NetPacket P))
					return P;
				Thread.Sleep(0);
			}

			throw new Exception("Could not allocate packet");
		}

		// Raw methods

		public void SendRaw(byte[] Data, IPEndPoint EndPoint) {
			if (Data.Length > MaxDataSize)
				throw new Exception("Data packet too large");

			if (ARTIFICAL_DELAY)
				WreckUtils.RandomDelay();

			UDP.Send(Data, Data.Length, EndPoint);
		}

		public void SendRaw<T>(T Msg, IPEndPoint EndPoint) where T : unmanaged {
			byte[] MsgBuffer = new byte[sizeof(T)];
			T* MsgPtr = &Msg;

			for (int i = 0; i < MsgBuffer.Length; i++)
				MsgBuffer[i] = ((byte*)MsgPtr)[i];

			SendRaw(MsgBuffer, EndPoint);
		}

		public void SendRaw(byte[] Data, string IP, int Port) {
			SendRaw(Data, CreateEndPoint(IP, Port));
		}

		public NetPacket ReceiveRaw() {
			NetPacket Packet = AllocPacket();
			Packet.Sender = new IPEndPoint(IPAddress.Any, Port);
			Packet.RawData = UDP.Receive(ref Packet.Sender);
			return Packet;
		}

		// Methods that do automatic fragmentation

		public void Send(byte[] Data, IPEndPoint EndPoint) {
			byte[][] Packets = WreckUtils.BufferSplit(Data, MaxDataSize);

			foreach (var Packet in Packets) {
				SendRaw(Packet, EndPoint);
			}
		}
	}
}
