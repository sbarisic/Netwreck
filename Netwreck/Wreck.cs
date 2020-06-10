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
using System.Diagnostics;

namespace Netwrecking {
	public enum ClientState : byte {
		Disconnected = 0,
		Connecting,
		Connected,
	}

	public enum PacketType : byte {
		Default = 0,
		ConnectionRequest,
		ConnectionAccept,
		ConnectionReject,
		Disconnect,
	}

	public class NetPacket : NetworkSerializable {
		const int PacketHashNum = 0x522A07BF;

		public NetWreckClient Sender;

		public int PacketNum;
		public PacketType Type;
		public byte[] Payload;

		internal bool PacketValid;

		internal NetPacket() {
		}

		public void Serialize(BinaryWriter Writer) {
			Writer.Write(PacketHashNum);

			Writer.Write((byte)Type);
			Writer.Write((ushort)Payload.Length);
			Writer.Write(Payload);
		}

		public void Deserialize(BinaryReader Reader) {
			PacketValid = true;
			long StartPos = Reader.BaseStream.Position;

			if (Reader.ReadInt32() != PacketHashNum) {
				PacketValid = false;
				return;
			}

			Type = (PacketType)Reader.ReadByte();

			ushort PayloadLen = Reader.ReadUInt16();
			if (PayloadLen > NetWreck.MaxDataSize) {
				PacketValid = false;
				return;
			}

			Payload = Reader.ReadBytes(PayloadLen);

			int Len = (int)(Reader.BaseStream.Position - StartPos);
			if (Len > NetWreck.MaxDataSize)
				PacketValid = false;
		}
	}

	public unsafe class NetWreckClient {
		public IPEndPoint SenderEndPoint;

		public ClientState State {
			get;
			internal set;
		}

		internal double TimeSent;
		internal double TimeReceived;

		internal NetWreckClient(IPEndPoint SenderEndPoint) {
			this.SenderEndPoint = SenderEndPoint;
			TimeSent = double.NegativeInfinity;
		}

		public override string ToString() {
			return SenderEndPoint.ToString();
		}
	}

	public delegate void OnClientConnectingFunc(NetWreckClient Cli, NetPacket Packet);
	public delegate void OnClientConnectedFunc(NetWreckClient Cli);
	public delegate void OnClientDisconnectedFunc(NetWreckClient Cli);
	public delegate void OnPacketReceivedFunc(NetPacket Packet);

	public unsafe class NetWreck {
		internal const int MaxDataSize = 1024;

		public static IPEndPoint CreateEndPoint(string IP, int Port) {
			return new IPEndPoint(IPAddress.Parse(IP), Port);
		}

		Stopwatch SWatch;

		ConcurrentQueue<NetPacket> PacketPool;
		ArrayPool<byte> ByteArrayPool;
		List<NetWreckClient> ServerClientList;

		UdpClient UDP;
		int Port;

		bool IsServer;
		NetWreckClient ServerConnectionClient;

		public event OnClientConnectingFunc OnClientConnecting;
		public event OnClientConnectedFunc OnClientConnected;
		public event OnClientDisconnectedFunc OnClientDisconnected;
		public event OnPacketReceivedFunc OnPacketReceived;

		public NetWreck(int Port, bool IsServer = false, int PacketPoolSize = 128) {
			SWatch = Stopwatch.StartNew();

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

		double Timestamp() {
			return SWatch.Elapsed.TotalMilliseconds;
		}

		public double LastSendTime(NetWreckClient Cli) {
			return Timestamp() - Cli.TimeSent;
		}

		public double LastReceiveTime(NetWreckClient Cli) {
			return Timestamp() - Cli.TimeReceived;
		}

		public void ConnectToServer(IPEndPoint Server) {
			DebugPrint("Starting connection to server");

			ServerConnectionClient = new NetWreckClient(Server);
			ServerConnectionClient.State = ClientState.Connecting;
		}

		void UpdateServer() {
			IPEndPoint Sender = null;

			if (ReceivePacket(ref Sender, out NetPacket Packet)) {
				NetWreckClient Cli = FindOrCreateClient(Sender);

				Cli.TimeReceived = Timestamp();
				Packet.Sender = Cli;

				switch (Cli.State) {
					case ClientState.Connecting: {
							if (OnClientConnecting != null)
								OnClientConnecting(Cli, Packet);
							else
								AcceptConnection(Cli);
							break;
						}

					case ClientState.Connected: {
							if (Packet.Type == PacketType.ConnectionRequest) {
								AcceptConnection(Cli);
								break;
							}

							OnPacketReceived?.Invoke(Packet);
							break;
						}

					case ClientState.Disconnected: {
							Disconnect(Cli);
							break;
						}

					default:
						throw new NotImplementedException();
				}

				FreePacket(Packet);
			}

			NetWreckClient[] Clients = ServerClientList.ToArray();
			foreach (var C in Clients) {
				double LastReceived = LastReceiveTime(C);

				if (LastReceived > 10000)
					ServerClientList.Remove(C);

				if (LastReceived > 2000)
					Disconnect(C);
			}
		}

		void UpdateClient() {
			if (ServerConnectionClient == null)
				return;

			if (ReceivePacket(ref ServerConnectionClient.SenderEndPoint, out NetPacket Packet)) {
				ServerConnectionClient.TimeReceived = Timestamp();
				Packet.Sender = ServerConnectionClient;

				switch (ServerConnectionClient.State) {
					case ClientState.Connecting:
						if (Packet.Type == PacketType.ConnectionAccept) {
							ServerConnectionClient.State = ClientState.Connected;
							OnClientConnected?.Invoke(ServerConnectionClient);
							break;
						}

						break;

					case ClientState.Connected:
						if (Packet.Type == PacketType.ConnectionAccept)
							break;

						if (Packet.Type == PacketType.Disconnect) {
							Disconnect(ServerConnectionClient);
							break;
						}

						OnPacketReceived?.Invoke(Packet);
						break;

					case ClientState.Disconnected:
						break;

					default:
						throw new NotImplementedException();
				}

				FreePacket(Packet);
			}

			if (ServerConnectionClient.State == ClientState.Connecting) {
				// If last packet sent was more than half a second ago, try again
				if (LastSendTime(ServerConnectionClient) > 500) {
					NetPacket P = AllocPacket();
					P.PacketNum = 0;
					P.Payload = new byte[0];
					P.Type = PacketType.ConnectionRequest;
					SendPacket(P, ServerConnectionClient);
					FreePacket(P);
				}
			}
		}

		public void AcceptConnection(NetWreckClient Cli) {
			NetPacket P = AllocPacket();
			P.PacketNum = 0;
			P.Payload = new byte[0];
			P.Type = PacketType.ConnectionAccept;
			SendPacket(P, Cli);
			FreePacket(P);

			if (Cli.State != ClientState.Connected) {
				Cli.State = ClientState.Connected;
				OnClientConnected?.Invoke(Cli);
			}
		}

		public void RejectConnection(NetWreckClient Cli) {
			NetPacket P = AllocPacket();
			P.PacketNum = 0;
			P.Payload = new byte[0];
			P.Type = PacketType.ConnectionReject;
			SendPacket(P, Cli);
			FreePacket(P);
		}

		public void Disconnect(NetWreckClient Cli) {
			NetPacket P = AllocPacket();
			P.PacketNum = 0;
			P.Payload = new byte[0];
			P.Type = PacketType.Disconnect;
			SendPacket(P, Cli);
			FreePacket(P);

			if (Cli.State != ClientState.Disconnected) {
				Cli.State = ClientState.Disconnected;
				OnClientDisconnected?.Invoke(Cli);
			}
		}

		NetWreckClient FindOrCreateClient(IPEndPoint EndPoint) {
			foreach (var C in ServerClientList) {
				if (C.SenderEndPoint.Equals(EndPoint))
					return C;
			}

			NetWreckClient Cli = new NetWreckClient(EndPoint);
			Cli.State = ClientState.Connecting;
			ServerClientList.Add(Cli);
			return Cli;
		}

		/*public void DisconnectClient(NetWreckClient Cli) {
			Cli.State = ClientState.Disconnected;

			NetPacket P = AllocPacket();
			P.PacketNum = 0;
			P.Payload = new byte[0];
			P.Type = PacketType.Disconnect;
			SendPacket(P, Cli);
			FreePacket(P);
		}*/

		public void StartUpdateLoop() {
			Thread UpdateThread = new Thread(() => {
				while (true) {
					if (IsServer)
						UpdateServer();
					else
						UpdateClient();

					Thread.Sleep(0);
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

		public void SendRaw(byte[] Data, int Length, NetWreckClient Cli) {
			SendRaw(Data, Length, Cli.SenderEndPoint);
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

		bool ReceiveRaw(ref IPEndPoint Sender, out byte[] Data) {
			if (Sender == null)
				Sender = new IPEndPoint(IPAddress.Any, Port);

			if (UDP.Available > 0) {
				try {
					Data = UDP.Receive(ref Sender);
					return true;
				} catch (SocketException E) {
				}
			}

			Data = null;
			return false;
		}

		bool ReceivePacket(ref IPEndPoint Sender, out NetPacket Packet) {
			if (ReceiveRaw(ref Sender, out byte[] Raw)) {
				Packet = AllocPacket();
				WreckUtils.Deserialize(Raw, ref Packet);

				if (!Packet.PacketValid) {
					DebugPrint("Dropping invalid packet");

					FreePacket(Packet);
					Packet = null;
					return false;
				}

				return true;
			}

			Packet = null;
			return false;
		}

		public void SendPacket(NetPacket Packet, NetWreckClient Cli) {
			byte[] Arr = ByteArrayPool.Rent(MaxDataSize);
			int Len = WreckUtils.Serialize(Packet, Arr, MaxDataSize);

			SendRaw(Arr, Len, Cli);
			Cli.TimeSent = Timestamp();

			ByteArrayPool.Return(Arr);
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

		static void DebugPrint(string Str) {
			Console.WriteLine("[DBG] {0}", Str);
		}

		static void DebugPrint(string Fmt, params object[] Args) {
			DebugPrint(string.Format(Fmt, Args));
		}
	}
}
