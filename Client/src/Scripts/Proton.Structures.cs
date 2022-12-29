using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Proton.Structures
{
    public class TransformPacket
    {
        public uint ID;
        public Vector3 Position;
        public Quaternion Rotation;
    }
    public class RigidbodyPacket
    {
        public uint ID;
        public ushort Mass = 0;
        public float Drag = 0;
        public float AngularDrag = 0;
        public bool Gravity = false;
        public bool Kinematic = false;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 AngularSpeed;
        public Quaternion Rotation;
    }
    public class InstantiatePacket
    {
        public uint OwnerID = 0;
        public uint ID = 0;
        public string GameobjectName = "";
        public Vector3 Position;
        public Quaternion Rotation;
    }
    public class RoomInfoPacket
    {
        public string RoomName = "";
        public string MapName = "";
        public string GamemodeName = "";
        public byte MaxPlayers = 0;
        public byte CurrentPlayers = 0;
        public bool HasPassword = false;
        public string Password = "";
    }
    public class PlayerInfoPacket
    {
        public string NickName = "";
        public uint ID = 0;
        public bool IsHost = false;
    }
}