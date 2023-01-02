using System.Collections;
using System.Collections.Generic;

namespace Proton.Packet.ID
{
    public static class ProtonPacketID
    {
        public static readonly byte PACKET =                                   33;
        public static readonly byte RPC =                                      34;
        public static readonly byte PING =                                     35;
        public static readonly byte PONG =                                     36;
        public static readonly byte PACKET_SUCCESS =                           37;

        public static readonly byte ENCRYPTION_START_REQUEST =                  0;
        public static readonly byte XOR_PUBLIC_KEY =                            1;

        public static readonly byte PACKET_ENCRYPTED_CONNECTION_REQUEST =      11;
        public static readonly byte PACKET_TRANSFORM_SYNC =                    12;
        public static readonly byte PACKET_RIGIDBODY_SYNC =                    13;
        public static readonly byte PACKET_GET_ROOM_LIST_REQUEST =             14;
        public static readonly byte PACKET_ROOM_LIST =                         15;
        public static readonly byte PACKET_JOIN_ROOM_REQUEST =                 16;
        public static readonly byte PACKET_JOIN_ROOM_REQUEST_ACCEPTED =        17;
        public static readonly byte PACKET_CREATE_ROOM_REQUEST =               18;
        public static readonly byte PACKET_CREATE_ROOM_REQUEST_ACCEPTED =      19;
        public static readonly byte PACKET_DISCONNECT =                        20;
        public static readonly byte PACKET_CHAT =                              21;
        public static readonly byte PACKET_CREATE_PLAYER_CLASS =               22;
        public static readonly byte PACKET_REMOVE_PLAYER_CLASS =               23;
        public static readonly byte PACKET_HOST_CHANGED =                      24;
        public static readonly byte PACKET_INTERNAL_ERROR =                    25;
        public static readonly byte PACKET_INSTANTIATE_GAMEOBJECT =            26;
        public static readonly byte PACKET_DESTROY_GAMEOBJECT =                27;
        public static readonly byte PACKET_GAMEOBJECT_TELEPORT =               28;
        public static readonly byte PACKET_UPDATE_STREAM_ZONE =                29;
        public static readonly byte PACKET_DESYNC_RIGIDBODY =                  30;
        public static readonly byte PACKET_KICK_PLAYER =                       31;
        public static readonly byte PACKET_INITIALIZE_PRIORITY =               33;
        public static readonly byte PACKET_CHECK_EXISTANCE =                   34;
    }
    public static class Error
    {
        public static readonly byte ERROR_SERVER_MAX_CONNECTIONS =              1;
        public static readonly byte ERROR_NICKNAME_TAKEN =                      2;
    }
    public static class RpcTarget
    {
        public static readonly uint ROOM =                                      1;
        public static readonly uint HOST =                                      2;
        public static readonly uint GLOBAL =                                    3;
        public static readonly uint SERVER =                                    4;
    }
    public static class ProtonTypes
    {
        public static readonly byte BYTE =                                      1;
        public static readonly byte UINT16 =                                    2;
        public static readonly byte INT16 =                                     3;
        public static readonly byte UINT32 =                                    4;
        public static readonly byte INT32 =                                     5;
        public static readonly byte FLOAT =                                     6;
        public static readonly byte STRING8 =                                   7;
        public static readonly byte STRING16 =                                  8;
        public static readonly byte BOOL =                                      9;
    }
    public static class SendPriority
    {
        public static readonly float LOW =                                      0.5f;
        public static readonly float DEFAULT =                                  1.0f;
        public static readonly float HIGH =                                     2.0f;
    }
}