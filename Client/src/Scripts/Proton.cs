using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Proton.Stream;
using Proton.Network;
using Proton.Structures;
using Proton.GlobalStates;
using Proton.Packet.Handler;
using Proton.Callbacks;
using UnityEngine;

namespace Proton
{
    public static class ProtonEngine
    {
        public static GameObject ProtonHandlerPrefab;
        public static GameObject ProtonHandlerObject;
        public static List<object> CallbacksTargets = new List<object>();
        public static List<object[]> CallbacksStack = new List<object[]>();
        public static Room CurrentRoom;
        public static List<Player> CachedRoomPlayers = new List<Player>();
        public static Player LocalPlayer;
        public static Vector3 LocalCameraPosition;
        public static float StreamZone;
        public static float SendMultiplier;
        public static bool AutoStreamZoneUpdate = true;
        public static string NickName;

        public static void Connect(string nickname, string IP, int port)
        {
            if (ProtonGlobalStates.PeerState == PeerStates.Disconnected)
            {
                ProtonHandlerPrefab = Resources.Load("ProtonHandler", typeof(GameObject)) as GameObject;
                ProtonHandlerObject = GameObject.Instantiate(ProtonHandlerPrefab);
                ProtonEngine.NickName = nickname;
                ProtonNetwork.Connect(IP, port, nickname, Application.version);
            }
            else
            {
                Debug.LogError("Подключение возможно только если PeerState == PeerStates.Disconnected. Текущий PeerState: " + ProtonGlobalStates.PeerState.ToString());
            }
        }
        public static void Disconnect()
        {
            ProtonNetwork.Disconnect();
            GameObject.Destroy(ProtonHandlerObject);
            if (CurrentRoom != null)
            {
                foreach (KeyValuePair<uint, GameObject> gameObject in CurrentRoom.GameobjectPool)
                {
                    GameObject.Destroy(gameObject.Value);
                }
            }
            CurrentRoom = null;
            LocalPlayer = null;
        }
        public static void CreateRoom(RoomSettings roomSettings)
        {
            ProtonPacketHandler.SendCreateRoom(roomSettings);
        }
        public static void JoinRoom(string roomName)
        {
            ProtonPacketHandler.SendJoinRoom(roomName);
        }
        public static void GetRoomsList()
        {
            ProtonPacketHandler.SendGetRoomsList();
        }
        public static void SendRPC(params object[] values)
        {
            if (values.Length == 0)
            {
                Debug.LogError("Для отправки RPC нужно указать имя!");
                return;
            }
            else if (values.Length == 1)
            {
                Debug.LogError("Для отправки RPC нужно указать отправителя!");
                return;
            }

            if (!IsConnected())
            {
                Debug.LogError("Для отправки RPC нужно подключиться к серверу!");
                return;
            }

            ProtonPacketHandler.SendRPC(values);
        }
        public static Player GetPlayerByID(uint playerID)
        {
            if (CurrentRoom == null)
            {
                return null;
            }

            foreach (Player player in CurrentRoom.PlayersList)
            {
                if (player.ID == playerID)
                {
                    return player;
                }
            }

            return null;
        }
        public static Player GetPlayerByNickname(string playerNickname)
        {
            if (CurrentRoom == null)
            {
                Debug.LogError("Для поиска игрока нужно зайти в комнату!");
                return null;
            }

            foreach (Player player in CurrentRoom.PlayersList)
            {
                if (player.NickName == playerNickname)
                {
                    return player;
                }
            }

            return null;
        }
        public static GameObject GetGameObjectByID(uint gameObjectID)
        {
            if (CurrentRoom == null)
            {
                Debug.LogError("Для поиска объекта нужно зайти в комнату!");
                return null;
            }

            if (CurrentRoom.GameobjectPool.ContainsKey(gameObjectID))
            {
                return CurrentRoom.GameobjectPool[gameObjectID];
            }
            else
            {
                return null;
            }
        }
        public static uint AllocateGameobjectID()
        {
            return (uint) Random.Range((uint) 0x00000000, (uint) 0xFFFFFFFF);
        }
        public static GameObject Instantiate(string gameobjectName, Vector3 position, Quaternion rotation)
        {
            if (!IsConnected())
            {
                Debug.LogError("Для создания объекта нужно подключиться к серверу!");
                return null;
            }
            if (CurrentRoom == null)
            {
                Debug.LogError("Для создания объекта нужно подключиться к комнате!");
                return null;
            }

            GameObject loadedGameobject = (GameObject) Resources.Load(gameobjectName, typeof(GameObject));
            if (loadedGameobject.GetComponent<ProtonView>() == null)
            {
                Debug.LogError("Созданный объект должен иметь компонент ProtonView!");
                return null;
            }
            ProtonView loadedObjectProtonView = loadedGameobject.GetComponent<ProtonView>();
            uint ID = AllocateGameobjectID();

            loadedObjectProtonView.Init(ProtonEngine.LocalPlayer.ID, true, ID);

            GameObject spawnedObject = GameObject.Instantiate(loadedGameobject, position, rotation);

            CurrentRoom.GameobjectPool[ID] = spawnedObject;
            CurrentRoom.MineGameobjectPool[ID] = spawnedObject;
            ProtonPacketHandler.NetworkInstantiate(gameobjectName, position, rotation, ID);

            return spawnedObject;
        }
        public static void Destroy(GameObject destroyedGameobject)
        {
            if (!IsConnected())
            {
                Debug.LogError("Для удаления объекта нужно подключиться к серверу!");
                return;
            }
            if (CurrentRoom == null)
            {
                Debug.LogError("Для удаления объекта нужно подключиться к комнате!");
                return;
            }

            if (destroyedGameobject == null)
            {
                Debug.LogError("Вы пытаетесь удалить несуществующий объект!");
                return;
            }

            ProtonView destroyedGameobjectProtonView = destroyedGameobject.GetComponent<ProtonView>();
            if (destroyedGameobjectProtonView == null)
            {
                Debug.LogError("Удаляемый объект должен иметь компонент ProtonView!");
            }

            ProtonPacketHandler.NetworkDestroy(destroyedGameobjectProtonView.ID);
        }
        public static void SendChatMessage(string message)
        {
            if (!IsConnected())
            {
                Debug.LogError("Для отправки сообщения нужно подключиться к серверу!");
                return;
            }
            if (CurrentRoom == null)
            {
                Debug.LogError("Для отправки сообщения нужно подключиться к комнате!");
                return;
            }

            ProtonPacketHandler.SendChatMessage(message);
        }
        public static void KickPlayer(uint ID)
        {
            if (!IsConnected())
            {
                Debug.LogError("Для кика игрока нужно подключиться к серверу!");
                return;
            }
            if (CurrentRoom == null)
            {
                Debug.LogError("Для кика игрока нужно подключиться к комнате!");
                return;
            }
            if (CurrentRoom.Host != LocalPlayer)
            {
                Debug.LogError("Для кика игрока нужно быть хостом комнаты!");
                return;
            }

            ProtonPacketHandler.SendKickPlayer(ID);
        }
        public static void UpdateRoomSendrate()
        {
            if (CurrentRoom == null)
            {
                return;
            }

            CurrentRoom.UpdateSendRate();
        }
        public static void UpdateStreamZome(Vector3 position)
        {
            LocalCameraPosition = position;
            ProtonPacketHandler.SendUpdateCameraPosition(position);
        }

        public static void SendTransformSync(uint objectID, Vector3 position, Quaternion rotation)
        {
            ProtonPacketHandler.SendTransformSync(objectID, position, rotation);
        }
        public static void SendRigidbodySync(RigidbodyPacket rigidbodyData)
        {
            ProtonPacketHandler.SendRigidbodySync(rigidbodyData);
        }

        public static void AddCallbacksTarget(object targetScript)
        {
            CallbacksTargets.Add(targetScript);
        }
        public static void InvokeCallback(string callbackName, object[] arguments)
        {
            ProtonCallbacks.InvokeCallback(callbackName, arguments);
        }

        public static bool IsConnected()
        {
            return !(ProtonGlobalStates.PeerState == PeerStates.Disconnected || ProtonGlobalStates.PeerState == PeerStates.RequestEncryptionKeysExchangePermission || ProtonGlobalStates.PeerState == PeerStates.ReceivedEncryptionKey || ProtonGlobalStates.PeerState == PeerStates.RequestSecureConnection);
        }
    }

    public class Room : RoomSettings
    {
        public List<Player> PlayersList = new List<Player>();
        public Dictionary<uint, GameObject> GameobjectPool = new Dictionary<uint, GameObject>();
        public Dictionary<uint, ProtonTransformView> TransformViewPool = new Dictionary<uint, ProtonTransformView>();
        public Dictionary<uint, GameObject> MineGameobjectPool = new Dictionary<uint, GameObject>();
        public Dictionary<uint, GameObject> MovingObjects = new Dictionary<uint, GameObject>();
        public Player Host;
        public float SendRate = 4.0f;

        public void Init(RoomInfoPacket roomInfo)
        {
            this.RoomName = roomInfo.RoomName;
            this.MapName = roomInfo.MapName;
            this.GamemodeName = roomInfo.GamemodeName;
            this.MaxPlayers = roomInfo.MaxPlayers;
            this.CurrentPlayers = roomInfo.CurrentPlayers;
            this.HasPassword = roomInfo.HasPassword;
            this.Password = roomInfo.Password;
        }
        public void AddOrUpdatePlayer(Player newPlayer)
        {
            foreach (Player player in PlayersList)
            {
                if (player == newPlayer)
                {
                    PlayersList.Remove(player);
                    PlayersList.Add(newPlayer);
                    if (newPlayer.IsHost == true)
                    {
                        Host = newPlayer;
                    }
                    return;
                }
            }
            PlayersList.Add(newPlayer);
        }
        public void RemovePlayer(Player removedPlayer)
        {
            foreach (Player player in PlayersList)
            {
                if (player == removedPlayer)
                {
                    PlayersList.Remove(player);
                    return;
                }
            }
        }
        public void RemovePlayer(uint removedId)
        {
            foreach (Player player in PlayersList)
            {
                if (player.ID == removedId)
                {
                    PlayersList.Remove(player);
                    return;
                }
            }
        }
        public void UpdateSendRate()
        {
            if (MovingObjects.Count <= 50)
            {
                SendRate = UnknownConvert(MovingObjects.Count, 1, 50, 5, 1);
            }

            if (MovingObjects.Count > 50)
            {
                SendRate = 50.0f / (float) MovingObjects.Count;
            }
        }
        private float UnknownConvert(float value, float in_min, float in_max, float out_min, float out_max)
        {
            if (value > in_max)
            {
                value = in_max;
            }
            if (value < in_min)
            {
                value = in_min;
            }

            return (value - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }
    public class RoomSettings : RoomInfoPacket {}
    public class Player : PlayerInfoPacket
    {
        public static bool operator ==(Player player1, Player player2)
        {
            return player1.ID == player2.ID;
        }
        public static bool operator !=(Player player1, Player player2)
        {
            return player1.ID != player2.ID;
        }
        public override bool Equals(object object2)
        {
            Player player2 = (Player) object2;
            return this.ID == player2.ID;
        }
        public override int GetHashCode()
        {
            return (int) (this.ID / 2);
        }
        public override string ToString()
        {
            return "(" + this.NickName + ") (" + this.ID + ")";
        }
    }
}