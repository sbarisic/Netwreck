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
using System.IO;

namespace Netwrecking {
	public class NetPacket {
		public IPEndPoint Sender;
		public byte[] RawData;

		internal NetPacket() {
		}
	}


	struct WreckMessage : NetworkSerializable {
		public enum MessageType : int {
			None = 0,
			BeginMessage,
			BeginMessageACK,
			MessageData,
			EndMessage
		}

		public int MessageNumber;
		public int PacketNumber;
		public MessageType Type;
		public byte[] Data;

		public WreckMessage(int MessageNumber, int PacketNumber, MessageType Type) {
			this.MessageNumber = MessageNumber;
			this.PacketNumber = PacketNumber;
			this.Type = Type;
			this.Data = null;
		}

		public void Serialize(BinaryWriter Writer) {
			Writer.Write(MessageNumber);
			Writer.Write(PacketNumber);
			Writer.Write((int)Type);

			if (Data == null)
				Writer.Write(0);

			Writer.Write(Data.Length);
			Writer.Write(Data);
		}

		public void Deserialize(BinaryReader Reader) {
			MessageNumber = Reader.ReadInt32();
			PacketNumber = Reader.ReadInt32();
			Type = (MessageType)Reader.ReadInt32();

			int Len = Reader.ReadInt32();

			if (Len == 0)
				Data = null;
			else
				Data = Reader.ReadBytes(Len);
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

		public byte[] ReceiveRaw(out IPEndPoint Sender) {
			Sender = new IPEndPoint(IPAddress.Any, Port);
			return UDP.Receive(ref Sender);
		}

		void SendMessage(WreckMessage Msg, IPEndPoint EndPoint) {
			SendRaw(WreckUtils.Serialize(Msg), EndPoint);
		}

		WreckMessage ReceiveMessage() {
			byte[] Data = ReceiveRaw(out IPEndPoint EndPoint);
			return WreckUtils.Deserialize<WreckMessage>(Data);
		}

		// -------------------------------------------------------------------------------------------------------------------

		public void Send(byte[] Data, IPEndPoint EndPoint) {
			byte[][] Packets = WreckUtils.BufferSplit(Data, MaxDataSize);

			SendMessage(new WreckMessage(0, 0, WreckMessage.MessageType.BeginMessage), EndPoint);
			WreckMessage Msg = ReceiveMessage();


			foreach (var Packet in Packets) {
				SendRaw(Packet, EndPoint);
			}
		}

		public byte[] Receive() {
			return null;
		}
	}
}
