using System.Collections;
using System.Collections.Generic;
using Proton;

namespace Proton.GlobalStates
{
    public static class ProtonGlobalStates
    {
        public static int PeerState = PeerStates.Disconnected;
        public static byte XorEncryptionKey = 0;
        public static bool ConnectionEncrypted = false;
        public static long LastPingTime;
    }

    public static class PeerStates
    {
        public static readonly int Disconnected = 0;
        public static readonly int RequestEncryptionKeysExchangePermission = 1;
        public static readonly int ReceivedEncryptionKey = 2;
        public static readonly int RequestSecureConnection = 3;
        public static readonly int Connected = 4;
        public static readonly int JoiningToRoom = 5;
        public static readonly int ConnectedToRoom = 6;
    }
}