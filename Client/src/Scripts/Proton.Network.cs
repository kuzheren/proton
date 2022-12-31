using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Proton.Stream;
using Proton.Packet.Handler;
using Proton.Packet.ID;
using Proton.GlobalStates;
using Proton.Crypto;
using Proton;

namespace Proton.Network
{
    public static class ProtonNetwork
    {
        private static Socket ProtonPeer;
        private static IPEndPoint IP;
        private static Thread SendThread;
        private static List<ProtonStream> SendDataQueue = new List<ProtonStream>();

        private static bool Active;

        public static void Connect(string adress, int port, string nickname)
        {
            if (nickname == "")
            {
                nickname = "Player_" + UnityEngine.Random.Range(10000, 99999).ToString();
            }
            IP = new IPEndPoint(IPAddress.Parse(adress), port);
            ProtonPeer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ProtonPeer.SendTimeout = 1;
            ProtonPeer.ReceiveTimeout = 1;

            Active = true;

            Thread encryptionInitializationThread = new Thread(new ParameterizedThreadStart(InitializeEncryption));
            encryptionInitializationThread.Start(nickname);
        }
        public static void Disconnect()
        {
            Active = false;

            ProtonGlobalStates.PeerState = PeerStates.Disconnected;
            ProtonGlobalStates.XorEncryptionKey = 0;
            ProtonGlobalStates.ConnectionEncrypted = false;

            if (ProtonPeer != null)
            {
                ProtonPeer.Close();
            }
        }
        public static void SendQueuedData() // unused
        {
            while (true)
            {
                Thread.Sleep(1);
                if (SendDataQueue.Count > 0)
                {
                    ProtonStream ps = SendDataQueue[0];
                    try
                    {
                        if (ProtonGlobalStates.ConnectionEncrypted)
                        {
                            ProtonPeer.SendTo(ProtonCrypto.XorByteArray(ps.Bytes.ToArray(), ProtonGlobalStates.XorEncryptionKey), IP);
                        }
                        else
                        {
                            ProtonPeer.SendTo(ps.Bytes.ToArray(), IP);
                        }
                    }
                    catch {}

                    SendDataQueue.RemoveAt(0);
                }
            }
        }
        public static void Send(ProtonStream ps)
        {
            try
            {
                if (ProtonGlobalStates.ConnectionEncrypted)
                {
                    ProtonPeer.SendTo(ProtonCrypto.XorByteArray(ps.Bytes.ToArray(), ProtonGlobalStates.XorEncryptionKey), IP);
                }
                else
                {
                    ProtonPeer.SendTo(ps.Bytes.ToArray(), IP);
                }
            }
            catch {}
        }
        public static void SendPacket(ProtonStream ps)
        {
            ps.Bytes.Insert(0, ProtonPacketID.PACKET);
            Send(ps);
        }
        public static void SendRPC(ProtonStream ps)
        {
            ps.Bytes.Insert(0, ProtonPacketID.RPC);
            Send(ps);
        }
        public static void Receive()
        {
            if (Active == false)
            {
                return;
            }
            
            try
            {
                byte[] receiveBuffer = new byte[65535];
                ProtonPeer.Receive(receiveBuffer);

                ProtonStream ps = new ProtonStream();
                ps.Bytes = new List<byte>(receiveBuffer);

                if (ProtonGlobalStates.ConnectionEncrypted)
                {
                    ps.Bytes = new List<byte>(ProtonCrypto.XorByteArray(ps.Bytes.ToArray(), ProtonGlobalStates.XorEncryptionKey));

                    ProtonPacketHandler.OnReceivePacket(ps);
                }
                else
                {
                    byte dataType = ps.ReadByte();
                    if (dataType == ProtonPacketID.PACKET)
                    {
                        byte packetID = ps.ReadByte();
                        if (packetID == ProtonPacketID.XOR_PUBLIC_KEY)
                        {
                            ProtonGlobalStates.PeerState = PeerStates.ReceivedEncryptionKey;

                            byte encryptedKeyLocation = ProtonCrypto.DecryptByte(ps.ReadByte());
                            byte encryptedKey = ProtonCrypto.DecryptByte(ps.Bytes[encryptedKeyLocation + 2]);

                            ProtonGlobalStates.XorEncryptionKey = encryptedKey;
                            ProtonGlobalStates.PeerState = PeerStates.ReceivedEncryptionKey;
                        }
                        else if (packetID == ProtonPacketID.PACKET_ENCRYPTED_CONNECTION_REQUEST)
                        {
                            ProtonGlobalStates.PeerState = PeerStates.Connected;
                            ProtonEngine.StreamZone = ps.ReadFloat();
                            ProtonEngine.SendMultiplier = ps.ReadFloat();
                        }
                        else if (packetID == ProtonPacketID.PACKET_INTERNAL_ERROR)
                        {
                            uint errorCode = ps.ReadUInt32();
                            string errorText = ps.ReadString8();

                            if (errorCode == 1 || errorCode == 5 || errorCode == 6)
                            {
                                ProtonEngine.Disconnect();
                            }

                            ProtonEngine.InvokeCallback("OnServerError", new object[] {errorCode, errorText});
                        }
                    }
                }
            }
            catch (SocketException error)
            {
                ProtonEngine.InvokeCallback("OnSocketException", new object[] {error.ErrorCode, error.ToString()});

                switch (error.ErrorCode)
                {
                    case 10054:
                        ProtonEngine.Disconnect();
                        ProtonEngine.InvokeCallback("OnKicked", new object[] {true});
                        break;
                    case 10060:
                        return;
                }
            }
        }
        public static void InitializeEncryption(object nickname)
        {
            ProtonGlobalStates.PeerState = PeerStates.RequestEncryptionKeysExchangePermission;

            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.ENCRYPTION_START_REQUEST);
            ps.WriteString8((string) nickname);
            SendPacket(ps);

            while (ProtonGlobalStates.PeerState != PeerStates.ReceivedEncryptionKey)
            {
                Thread.Sleep(10);
            }

            ProtonGlobalStates.PeerState = PeerStates.RequestSecureConnection;

            ProtonStream ps2 = new ProtonStream();
            ps2.WriteByte(ProtonPacketID.PACKET_ENCRYPTED_CONNECTION_REQUEST);
            SendPacket(ps2);

            while (ProtonGlobalStates.PeerState != PeerStates.Connected)
            {
                Thread.Sleep(10);
            }

            ProtonGlobalStates.ConnectionEncrypted = true;

            List<object> argumentsList = new List<object>();
            argumentsList.Insert(0, (object) ("OnConnected"));
            ProtonEngine.CallbacksStack.Add(argumentsList.ToArray());   
        }
    }
}