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
using System.Buffers;
using System.IO;

namespace Netwrecking {
	public class NetPacket {
		public NetWreckClient Sender;
		public int PacketNum;
		public byte[] Payload;

		internal NetPacket() {
		}
	}

	public unsafe class NetWreckClient {
		public IPEndPoint SenderEndPoint;

		internal NetWreckClient(IPEndPoint SenderEndPoint) {
			this.SenderEndPoint = SenderEndPoint;
		}
	}

	public delegate void OnClientConnectedFunc(NetWreckClient Cli);
	public delegate void OnClientDisconnectedFunc(NetWreckClient Cli);
	public delegate void OnPacketReceivedFunc(NetPacket Packet);

	public unsafe class NetWreck {
		const bool IS_DEBUG = true;
		const int MaxDataSize = 1024;

		public static IPEndPoint CreateEndPoint(string IP, int Port) {
			return new IPEndPoint(IPAddress.Parse(IP), Port);
		}

		ConcurrentQueue<NetPacket> PacketPool;
		ArrayPool<byte> ByteArrayPool;
		List<NetWreckClient> ServerClientList;

		UdpClient UDP;
		int Port;

		bool IsServer;
		NetWreckClient ServerConnectionClient;

		public event OnClientConnectedFunc OnClientConnected;
		public event OnClientDisconnectedFunc OnClientDisconnected;
		public event OnPacketReceivedFunc OnPacketReceived;

		public NetWreck(int Port, bool IsServer = false, int PacketPoolSize = 128) {
			PacketPool = new ConcurrentQueue<NetPacket>();
			for (int i = 0; i < PacketPoolSize; i++)
				PacketPool.Enqueue(new NetPacket());

			UDP = new UdpClient(Port);
			UDP.DontFragment = true;

			this.Port = Port;
			this.IsServer = IsServer;

			ByteArrayPool = ArrayPool<byte>.Create();
			ServerClientList = new List<NetWreckClient>();
		}

		public void ConnectToServer(IPEndPoint Server) {
			ServerConnectionClient = new NetWreckClient(Server);
		}

		void Update() {
			IPEndPoint Sender = IsServer ? null : ServerConnectionClient.SenderEndPoint;
			byte[] Raw = ReceiveRaw(ref Sender);

			NetWreckClient Cli = IsServer ? FindOrCreateClient(Sender) : ServerConnectionClient;



		}

		NetWreckClient FindOrCreateClient(IPEndPoint EndPoint) {
			foreach (var C in ServerClientList) {
				if (C.SenderEndPoint == EndPoint)
					return C;
			}

			NetWreckClient Cli = new NetWreckClient(EndPoint);
			ServerClientList.Add(Cli);
			OnClientConnected?.Invoke(Cli);
			return Cli;
		}

		public void StartUpdateLoop() {
			Thread UpdateThread = new Thread(() => {
				while (true) {
					Update();
				}
			});

			UpdateThread.IsBackground = true;
			UpdateThread.Start();
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

		public void SendRaw(byte[] Data, int Length, IPEndPoint EndPoint) {
			if (Data.Length > MaxDataSize)
				throw new Exception("Data packet too large");

			UDP.Send(Data, Length, EndPoint);
		}

		public void SendRaw<T>(T Msg, IPEndPoint EndPoint) where T : unmanaged {
			byte[] MsgBuffer = new byte[sizeof(T)];
			T* MsgPtr = &Msg;

			for (int i = 0; i < MsgBuffer.Length; i++)
				MsgBuffer[i] = ((byte*)MsgPtr)[i];

			SendRaw(MsgBuffer, MsgBuffer.Length, EndPoint);
		}

		void SendRaw(byte[] Data, string IP, int Port) {
			SendRaw(Data, Data.Length, CreateEndPoint(IP, Port));
		}

		byte[] ReceiveRaw(ref IPEndPoint Sender) {
			if (Sender == null)
				Sender = new IPEndPoint(IPAddress.Any, Port);

			return UDP.Receive(ref Sender);
		}

		/*void SendPacket(NetPacket P, string IP, int Port) {

			SendRaw(P.RawData, CreateEndPoint(IP, Port));
		}

		NetPacket ReceivePacket() {
			NetPacket P = AllocPacket();
			P.RawData = ReceiveRaw(out P.Sender);
			return P;
		}*/

		/*void SendMessage(WreckMessage Msg, IPEndPoint EndPoint) {
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
		}*/
	}
}
