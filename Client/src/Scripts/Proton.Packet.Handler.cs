using System.Collections;
using System.Collections.Generic;
using Proton.Stream;
using Proton.GlobalStates;
using Proton.Packet.ID;
using Proton.Network;
using Proton.Structures;
using System.Reflection;
using System;
using UnityEngine;

namespace Proton.Packet.Handler
{
    public static class ProtonPacketHandler
    {
        public static void OnReceivePacket(ProtonStream ps)
        {
            try
            {
                if (ProtonGlobalStates.ConnectionEncrypted == true)
                {
                    byte dataType = ps.ReadByte();
                    if (dataType == ProtonPacketID.PACKET)
                    {
                        byte packetID = ps.ReadByte();
                        ProcessPacket(packetID, ps);
                    }
                    else if (dataType == ProtonPacketID.RPC)
                    {
                        uint senderID = ps.ReadUInt32();
                        string RPCName = ps.ReadString8();
                        ProcessRPC(RPCName, senderID, ps);
                    }
                    else if (dataType == ProtonPacketID.PING)
                    {
                        SendPongPacket();
                    } 
                }
            }
            catch (Exception error)
            {
                Debug.LogError(error);
            }
        }
        public static void ProcessPacket(byte packetID, ProtonStream ps)
        {
            if (packetID == ProtonPacketID.PACKET_ROOM_LIST)
            {
                RoomSettings roomInfo = (RoomSettings) ConvertBitStreamToStruct(ps, typeof(RoomSettings));
                ProtonEngine.InvokeCallback("OnRoomListUpdate", new object[] {roomInfo});
            }
            else if (packetID == ProtonPacketID.PACKET_CREATE_ROOM_REQUEST_ACCEPTED)
            {
                RoomInfoPacket roomInfo = (RoomInfoPacket) ConvertBitStreamToStruct(ps, typeof(RoomInfoPacket));
                Room createdRoom = new Room();
                createdRoom.Init(roomInfo);
                ProtonEngine.CurrentRoom = createdRoom;
                ProtonEngine.CurrentRoom.Host = ProtonEngine.LocalPlayer;
                ProtonEngine.CurrentRoom.AddOrUpdatePlayer(ProtonEngine.LocalPlayer);
                ProtonEngine.InvokeCallback("OnCreateRoom", new object[] {createdRoom.MapName});
            }
            else if (packetID == ProtonPacketID.PACKET_JOIN_ROOM_REQUEST_ACCEPTED)
            {
                RoomInfoPacket roomInfo = (RoomInfoPacket) ConvertBitStreamToStruct(ps, typeof(RoomInfoPacket));
                Room joinedRoom = new Room();
                joinedRoom.Init(roomInfo);
                ProtonEngine.CurrentRoom = joinedRoom;
                ProtonEngine.CurrentRoom.AddOrUpdatePlayer(ProtonEngine.LocalPlayer);
                foreach (Player cachedPlayer in ProtonEngine.CachedRoomPlayers)
                {
                    if (cachedPlayer.IsHost)
                    {
                        ProtonEngine.CurrentRoom.Host = cachedPlayer;
                    }
                    ProtonEngine.CurrentRoom.AddOrUpdatePlayer(cachedPlayer);
                }
                ProtonEngine.CachedRoomPlayers = new List<Player>();
                ProtonEngine.InvokeCallback("OnJoinRoom", new object[] {joinedRoom.MapName});
            }
            else if (packetID == ProtonPacketID.PACKET_CREATE_PLAYER_CLASS)
            {
                bool isPlayerListInitialization = ps.ReadBool();
                bool isLocal = ps.ReadBool();
                
                Player newPlayer = (Player) ConvertBitStreamToStruct(ps, typeof(Player));

                if (isLocal == true)
                {
                    ProtonEngine.LocalPlayer = newPlayer;
                }
                else
                {
                    if (ProtonEngine.CurrentRoom == null)
                    {
                        ProtonEngine.CachedRoomPlayers.Add(newPlayer);
                    }
                    else
                    {
                        ProtonEngine.CurrentRoom.AddOrUpdatePlayer(newPlayer);
                        if (isPlayerListInitialization == false && newPlayer != ProtonEngine.LocalPlayer)
                        {
                            ProtonEngine.InvokeCallback("OnPlayerJoined", new object[] {newPlayer});
                        }
                    }
                }
            }
            else if (packetID == ProtonPacketID.PACKET_REMOVE_PLAYER_CLASS)
            {
                uint removedPlayerID = ps.ReadUInt32();
                Player quitedPlayer = ProtonEngine.GetPlayerByID(removedPlayerID);
                ProtonEngine.CurrentRoom.RemovePlayer(removedPlayerID);
                ProtonEngine.InvokeCallback("OnPlayerLeaved", new object[] {quitedPlayer});
            }
            else if (packetID == ProtonPacketID.PACKET_HOST_CHANGED)
            {
                uint newHostID = ps.ReadUInt32();
                ProtonEngine.CurrentRoom.Host = ProtonEngine.GetPlayerByID(newHostID);
                ProtonEngine.InvokeCallback("OnHostChanged", new object[] {ProtonEngine.CurrentRoom.Host});
            }
            else if (packetID == ProtonPacketID.PACKET_INSTANTIATE_GAMEOBJECT)
            {
                InstantiatePacket newGameobjectData = (InstantiatePacket) ConvertBitStreamToStruct(ps, typeof(InstantiatePacket));

                string gameobjectName = newGameobjectData.GameobjectName.ToString();
                Vector3 gameobjectPosition = newGameobjectData.Position;
                Quaternion gameobjectRotation = newGameobjectData.Rotation;
                uint gameobjectID = newGameobjectData.ID;
                uint ownerID = newGameobjectData.OwnerID;

                if (ProtonEngine.CurrentRoom.GameobjectPool.ContainsKey(gameobjectID))
                {
                    Debug.LogWarning("Предотвращена попытка спавна объекта с занятым ProtonView.ID");
                    return;
                }

                GameObject instantiatePrefab = (GameObject) Resources.Load(gameobjectName, typeof(GameObject));
                if (instantiatePrefab == null)
                {
                    Debug.LogError("Префаб с именем " + gameobjectName + " не существует. Убедитесь, чтобы префаб находился в папке Resources");
                }
                if (instantiatePrefab.GetComponent<ProtonView>() == null)
                {
                    Debug.LogError("Префаб с именем " + gameobjectName + " не имеет компонента ProtonView. Разрешено создавать объекты только с этим компонентом");
                }

                ProtonView loadedObjectProtonView = instantiatePrefab.GetComponent<ProtonView>();
                loadedObjectProtonView.Init(ownerID, false, gameobjectID);

                GameObject newGameobject = MonoBehaviour.Instantiate(instantiatePrefab, gameobjectPosition, gameobjectRotation);
                ProtonRigidbodyView newGameobjectRigidbodyView = newGameobject.GetComponent<ProtonRigidbodyView>();
                ProtonTransformView newGameobjectTransformView = newGameobject.GetComponent<ProtonTransformView>();

                if (newGameobjectRigidbodyView != null)
                {
                    newGameobjectRigidbodyView.targetPosition = gameobjectPosition;
                }
                if (newGameobjectTransformView != null)
                {
                    newGameobjectTransformView.targetPosition = gameobjectPosition;
                }
                ProtonEngine.CurrentRoom.GameobjectPool[gameobjectID] = newGameobject;
            }
            else if (packetID == ProtonPacketID.PACKET_DESTROY_GAMEOBJECT)
            {
                uint destroyedID = ps.ReadUInt32();

                if (!ProtonEngine.CurrentRoom.GameobjectPool.ContainsKey(destroyedID))
                {
                    return;
                }

                MonoBehaviour.Destroy(ProtonEngine.CurrentRoom.GameobjectPool[destroyedID]);
                ProtonEngine.CurrentRoom.GameobjectPool.Remove(destroyedID);

                if (ProtonEngine.CurrentRoom.MineGameobjectPool.ContainsKey(destroyedID))
                {
                    ProtonEngine.CurrentRoom.MineGameobjectPool.Remove(destroyedID);
                }
            }
            else if (packetID == ProtonPacketID.PACKET_TRANSFORM_SYNC)
            {
                TransformPacket transformData = (TransformPacket) ConvertBitStreamToStruct(ps, typeof(TransformPacket));
                uint gameobjectID = transformData.ID;

                if (!ProtonEngine.CurrentRoom.GameobjectPool.ContainsKey(gameobjectID))
                {
                    return;
                }

                GameObject transformedGameobject = ProtonEngine.CurrentRoom.GameobjectPool[gameobjectID];

                if (transformedGameobject == null)
                {
                    if (ProtonEngine.CurrentRoom.GameobjectPool.ContainsKey(gameobjectID))
                    {
                        ProtonEngine.CurrentRoom.GameobjectPool.Remove(gameobjectID);
                    }
                    return;
                }

                ProtonTransformView transformView = transformedGameobject.GetComponent<ProtonTransformView>();

                if (transformView == null)
                {
                    return;
                }

                transformView.NetworkSync(transformData, UnityEngine.Time.time);
            }
            else if (packetID == ProtonPacketID.PACKET_RIGIDBODY_SYNC)
            {
                RigidbodyPacket rigidbodyData = (RigidbodyPacket) ConvertBitStreamToStruct(ps, typeof(RigidbodyPacket));
                uint gameobjectID = rigidbodyData.ID;

                if (!ProtonEngine.CurrentRoom.GameobjectPool.ContainsKey(gameobjectID))
                {
                    return;
                }

                GameObject rigidbody = ProtonEngine.CurrentRoom.GameobjectPool[gameobjectID];

                if (rigidbody == null)
                {
                    if (ProtonEngine.CurrentRoom.GameobjectPool.ContainsKey(gameobjectID))
                    {
                        ProtonEngine.CurrentRoom.GameobjectPool.Remove(gameobjectID);
                    }
                    return;
                }

                ProtonRigidbodyView rigidbodyView = rigidbody.GetComponent<ProtonRigidbodyView>();

                if (rigidbodyView == null)
                {
                    return;
                }

                rigidbodyView.NetworkSync(rigidbodyData);
            }
            else if (packetID == ProtonPacketID.PACKET_CHAT)
            {
                string message = ps.ReadString16();
                
                ProtonEngine.InvokeCallback("OnChatMessage", new object[] {message});
            }
            else if (packetID == ProtonPacketID.PACKET_KICK_PLAYER)
            {
                ProtonEngine.Disconnect();

                ProtonEngine.InvokeCallback("OnKicked", new object[] {false});
            }
            else if (packetID == ProtonPacketID.PACKET_INTERNAL_ERROR)
            {
                uint errorCode = ps.ReadUInt32();
                string errorText = ps.ReadString8();

                ProtonEngine.InvokeCallback("OnServerError", new object[] {errorCode, errorText});
            }
            else if (packetID == ProtonPacketID.PACKET_GAMEOBJECT_TELEPORT)
            {
                uint gameobjectID = ps.ReadUInt32();
                Vector3 position = ps.ReadVector3();

                if (ProtonEngine.CurrentRoom == null)
                {
                    return;
                }

                if (!ProtonEngine.CurrentRoom.GameobjectPool.ContainsKey(gameobjectID))
                {
                    return;
                }

                ProtonEngine.InvokeCallback("OnTeleportGameObject", new object[] {ProtonEngine.CurrentRoom.GameobjectPool[gameobjectID], position});
            }
            else if (packetID == ProtonPacketID.PACKET_INITIALIZE_PRIORITY)
            {
                byte dataLength = ps.ReadByte();
                for (int i = 0; i < (int) dataLength; i++)
                {
                    string gameobjectName = ps.ReadString8();
                    float priority = ps.ReadFloat();

                    GameObject instantiatePrefab = (GameObject) Resources.Load(gameobjectName, typeof(GameObject));
                    if (instantiatePrefab == null)
                    {
                        return;
                    }
                    if (instantiatePrefab.GetComponent<ProtonView>() == null)
                    {
                        return;
                    }

                    instantiatePrefab.GetComponent<ProtonView>().Priority = priority;
                }
            }
        }
        public static void ProcessRPC(string RPCName, uint senderID, ProtonStream ps)
        {
            byte argumentsCount = ps.ReadByte();
            List<object> arguments = new List<object>();
            arguments.Add(senderID);

            for (int i = 0; i < argumentsCount; i++)
            {
                byte valueType = ps.ReadByte();

                if (valueType == ProtonTypes.BYTE)
                {
                    arguments.Add(ps.ReadByte());
                }
                else if (valueType == ProtonTypes.STRING8)
                {
                    arguments.Add(ps.ReadString8());
                }
                else if (valueType == ProtonTypes.UINT16)
                {
                    arguments.Add(ps.ReadUInt16());
                }
                else if (valueType == ProtonTypes.INT16)
                {
                    arguments.Add(ps.ReadInt16());
                }
                else if (valueType == ProtonTypes.UINT32)
                {
                    arguments.Add(ps.ReadUInt32());
                }
                else if (valueType == ProtonTypes.INT32)
                {
                    arguments.Add(ps.ReadInt32());
                }
                else if (valueType == ProtonTypes.FLOAT)
                {
                    arguments.Add(ps.ReadFloat());
                }
                else if (valueType == ProtonTypes.BOOL)
                {
                    arguments.Add(ps.ReadBool());
                }
            }

            ProtonEngine.InvokeCallback(RPCName, arguments.ToArray());
        }
        public static ProtonStream ConvertStructToBitStream(byte packetID, object targetStruct)
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(packetID);
            foreach (FieldInfo field in targetStruct.GetType().GetFields(bindingFlags))
            {
                if (field.FieldType == typeof(string))
                {
                    ps.WriteString8((string) field.GetValue(targetStruct));
                }
                else if (field.FieldType == typeof(byte))
                {
                    ps.WriteByte((byte) field.GetValue(targetStruct));
                }
                else if (field.FieldType == typeof(float))
                {
                    ps.WriteFloat((float) field.GetValue(targetStruct));
                }
                else if (field.FieldType == typeof(bool))
                {
                    ps.WriteBool((bool) field.GetValue(targetStruct));
                }
                else if (field.FieldType == typeof(uint))
                {
                    ps.WriteUInt32((uint) field.GetValue(targetStruct));
                }
                else if (field.FieldType == typeof(ushort))
                {
                    ps.WriteUInt16((ushort) field.GetValue(targetStruct));
                }
                else if (field.FieldType == typeof(Vector3))
                {
                    ps.WriteVector3((Vector3) field.GetValue(targetStruct));
                }
                else if (field.FieldType == typeof(Quaternion))
                {
                    ps.WriteQuaternion((Quaternion) field.GetValue(targetStruct));
                }
            }
            return ps;
        }
        public static object ConvertBitStreamToStruct(ProtonStream ps, Type targetStructPattern)
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            object newClass = System.Activator.CreateInstance(targetStructPattern);
            foreach (FieldInfo field in targetStructPattern.GetFields(bindingFlags))
            {
                string fieldName = field.Name;
                FieldInfo targetField = newClass.GetType().GetField(fieldName);

                if (field.FieldType == typeof(string))
                {
                    targetField.SetValue(newClass, ps.ReadString8());
                }
                else if (field.FieldType == typeof(byte))
                {
                    targetField.SetValue(newClass, ps.ReadByte());
                }
                else if (field.FieldType == typeof(float))
                {
                    targetField.SetValue(newClass, ps.ReadFloat());
                }
                else if (field.FieldType == typeof(bool))
                {
                    targetField.SetValue(newClass, ps.ReadBool());
                }
                else if (field.FieldType == typeof(uint))
                {
                    targetField.SetValue(newClass, ps.ReadUInt32());
                }
                else if (field.FieldType == typeof(ushort))
                {
                    targetField.SetValue(newClass, ps.ReadUInt16());
                }
                else if (field.FieldType == typeof(Vector3))
                {
                    targetField.SetValue(newClass, ps.ReadVector3());
                }
                else if (field.FieldType == typeof(Quaternion))
                {
                    targetField.SetValue(newClass, ps.ReadQuaternion());
                }
            }
            return newClass;
        }

        public static void NetworkInstantiate(string gameobjectName, Vector3 position, Quaternion rotation, uint ID)
        {
            InstantiatePacket instantiatePacket = new InstantiatePacket();
            instantiatePacket.OwnerID = ProtonEngine.LocalPlayer.ID;
            instantiatePacket.ID = ID;
            instantiatePacket.GameobjectName = gameobjectName;
            instantiatePacket.Position = position;
            instantiatePacket.Rotation = rotation;

            ProtonStream convertedProtonStream = ConvertStructToBitStream(ProtonPacketID.PACKET_INSTANTIATE_GAMEOBJECT, instantiatePacket);
            ProtonNetwork.SendPacket(convertedProtonStream);
        }
        public static void NetworkDestroy(uint destroyedGameobjectID)
        {
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PACKET_DESTROY_GAMEOBJECT);
            ps.WriteUInt32(destroyedGameobjectID);
            ProtonNetwork.SendPacket(ps);
        }
        
        public static void SendPongPacket()
        {
            ProtonGlobalStates.LastPingTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PONG);
            ps.WriteByte(ProtonPacketID.PONG);
            ProtonNetwork.Send(ps);
        }
        public static void SendChatMessage(string message)
        {
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PACKET_CHAT);
            ps.WriteString16(message);
            ProtonNetwork.SendPacket(ps);
        }
        public static void SendCreateRoom(RoomSettings roomSettings)
        {
            ProtonStream convertedProtonStream = ConvertStructToBitStream(ProtonPacketID.PACKET_CREATE_ROOM_REQUEST, roomSettings);
            ProtonNetwork.SendPacket(convertedProtonStream);
        }
        public static void SendJoinRoom(string roomName, string password = "")
        {
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PACKET_JOIN_ROOM_REQUEST);
            ps.WriteString8(roomName);
            ps.WriteString8(password);
            ProtonNetwork.SendPacket(ps);
        }
        public static void SendGetRoomsList()
        {
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PACKET_GET_ROOM_LIST_REQUEST);
            ProtonNetwork.SendPacket(ps);
        }
        public static void SendUpdateCameraPosition(Vector3 position)
        {
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PACKET_UPDATE_STREAM_ZONE);
            ps.WriteVector3(position);
            ProtonNetwork.SendPacket(ps);
        }
        public static void SendDesyncRigidbody(uint objectID, bool freeze)
        {
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PACKET_DESYNC_RIGIDBODY);
            ps.WriteUInt32(objectID);
            ps.WriteBool(freeze);
            ProtonNetwork.SendPacket(ps);
        }
        public static void SendKickPlayer(uint playerID)
        {
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PACKET_KICK_PLAYER);
            ps.WriteUInt32(playerID);
            ProtonNetwork.SendPacket(ps);
        }
        public static void SendCheckGameobjectExistance(uint gameobjectID)
        {
            ProtonStream ps = new ProtonStream();
            ps.WriteByte(ProtonPacketID.PACKET_CHECK_EXISTANCE);
            ps.WriteUInt32(gameobjectID);
            ProtonNetwork.SendPacket(ps);
        }

        public static void SendTransformSync(uint objectID, Vector3 position, Quaternion rotation)
        {
            TransformPacket transformPacket = new TransformPacket();
            transformPacket.ID = objectID;
            transformPacket.Position = position;
            transformPacket.Rotation = rotation;

            ProtonStream convertedProtonStream = ConvertStructToBitStream(ProtonPacketID.PACKET_TRANSFORM_SYNC, transformPacket);

            ProtonNetwork.SendPacket(convertedProtonStream);
        }
        public static void SendRigidbodySync(RigidbodyPacket rigidbodyPacket)
        {
            ProtonStream convertedProtonStream = ConvertStructToBitStream(ProtonPacketID.PACKET_RIGIDBODY_SYNC, rigidbodyPacket);

            ProtonNetwork.SendPacket(convertedProtonStream);
        }
        

        public static void SendRPC(object[] values)
        {
            string RPCName = (string) values[0];
            uint targetID = 0;

            if (values[1].GetType() == typeof(uint))
            {
                targetID = (uint) values[1];
            }
            else if (values[1].GetType() == typeof(Player))
            {
                targetID = ((Player) values[1]).ID;
            }
            else
            {
                Debug.LogError("Для отправки RPC нужно выбрать ID или класс отправителя!");
                return;
            }

            ProtonStream ps = new ProtonStream();

            byte argumentsCount = (byte) (values.Length - 2);

            ps.WriteUInt32(targetID);
            ps.WriteString8(RPCName);
            ps.WriteByte(argumentsCount);

            for (int i = 2; i < values.Length; i++)
            {
                object value = values[i];

                if (value == null)
                {
                    Debug.LogError("Предотвращена попытка отправки RPC с null аргументом! Функция: " + RPCName + ". Индекс аргумента: " + (i - 1));
                    return;
                }

                if (value.GetType() == typeof(byte))
                {
                    ps.WriteByte(ProtonTypes.BYTE);
                    ps.WriteByte((byte) value);
                }
                else if (value.GetType() == typeof(string))
                {
                    ps.WriteByte(ProtonTypes.STRING8);
                    ps.WriteString8((string) value);
                }
                else if (value.GetType() == typeof(ushort))
                {
                    ps.WriteByte(ProtonTypes.UINT16);
                    ps.WriteUInt16((ushort) value);
                }
                else if (value.GetType() == typeof(short))
                {
                    ps.WriteByte(ProtonTypes.INT16);
                    ps.WriteInt16((short) value);
                }
                else if (value.GetType() == typeof(uint))
                {
                    ps.WriteByte(ProtonTypes.UINT32);
                    ps.WriteUInt32((uint) value);
                }
                else if (value.GetType() == typeof(int))
                {
                    ps.WriteByte(ProtonTypes.INT32);
                    ps.WriteInt32((int) value);
                }
                else if (value.GetType() == typeof(float))
                {
                    ps.WriteByte(ProtonTypes.FLOAT);
                    ps.WriteFloat((float) value);
                }
                else if (value.GetType() == typeof(bool))
                {
                    ps.WriteByte(ProtonTypes.BOOL);
                    ps.WriteBool((bool) value);
                }
            }

            ProtonNetwork.SendRPC(ps);
        }
    }
}