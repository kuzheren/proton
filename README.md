# ProtonEngine - библиотека для разработки многопользовательских игр на Unity.

### Методы:
###### AddCallbacksTarget(object targetClass) - используется для добавления скрипта в список скриптов, слушающих коллбэки ProtonEngine
###### Connect(string nickname, string IP, int port) - используется для подключения к серверу
###### Disconnect() - используется для отключения от сервера
###### CreateRoom(RoomSettings roomSettings) - создает новую комнату и добавляет игрока в нее
###### JoinRoom(string roomName) - пытается зайти в комнату по ее названию
###### GetRoomsList() - получает список комнат
###### GetPlayerByID(uint ID) - возвращает класс игрока по его ID
###### UpdateStreamZome(Vector3 position) - обновляет на сервере положение камеры, исходя из которого обновляется зона прорисовки
###### SendChatMessage(string message) - отправляет сообщение всем игрокам в комнате
###### SendRPC(string callbackName, uint targetID/Player target, object[] params) - отправляет RPC указаной цели
###### Instantiate(string gameobjectName, Vector3 position, Quaternion rotation) - создает и возвращает сетевой объект
###### Destroy(GameObject destroyedGameobject) - удаляет сетевой объект
###### KickPlayer(Player kickedPlayer) - кикает игрока из комнаты 

### RPC:
#### Структура функции отправки:
###### SendRPC(string rpcCallbackName, uint targetID/Player target, object[]? args)
#### Структура коллбэка получения RPC:
###### public void RPC_Callback_Name(uint senderID, object[]? args)
>***В данный момент не поддерживается отправка массивов.***

>***Максимальное количество кастомных аргументов - 253***

>***senderID содержит ID игрока, отправившего RPC, но если RPC послан сервером, ID == 4 (RpcTarget.SERVER)***

### Типы аргументов цели RPC:
###### RpcTarget.ROOM - все игроки в комнате
###### RpcTarget.HOST - хост комнаты
###### RpcTarget.GLOBAL - все игроки, подключенные к серверу
###### RpcTarget.SERVER - сервер. Может использоваться для обработки в кастомном игровом моде

### Коллбэки:
###### OnConnected() - вызывается при успешном подключении к серверу
###### OnSocketException(int errorCode, string errorText) - вызывается при ошибке в сокете
###### OnRoomListUpdate(RoomSettings roomSettings) - вызывается после GetRoomsList() при получении списка комнат
###### OnCreateRoom(string mapName) - вызывается после успешного создания комнаты
###### OnJoinRoom(string mapName) - вызывается после успешного присоединения к комнате
###### OnPlayerJoined(Player joinedPlayer) - вызывается при подключении игрока в комнату
###### OnPlayerLeaved(Player leavedPlayer) - вызывается после выхода нелокального игрока из комнаты
###### OnHostChanged(Player newHost) - вызывается при смене хоста комнаты
###### OnChatMessage(string message) - вызывается при получении сообщения из чата
###### OnKicked(bool timeout) - вызывается при кике из комнаты
###### OnServerError(uint errorCode, string errorText) - вызывается при серверной ошибке
###### OnTeleportGameObject(GameObject teleportedObject, Vector3 position)

### Поля ProtonEngine:
###### string NickName - никнейм игрока (readonly)
###### Room CurrentRoom - текущая комната
###### Player LocalPlayer - текущий игрок
###### Vector3 LocalCameraPosition - положение камеры (readonly)
###### float StreamZone - зона прорисовки
###### bool AutoStreamZoneUpdate - флаг, отвечающий за автоматическое обновление позиции камеры на сервере. Если == false, то обновлять позицию нужно самостоятельно

### Поля Room:
###### List<Player> PlayersList - список игроков
###### Dictionary<uint, GameObject> GameobjectPool - список объектов, находящихся в зоне прорисовки игрока, а еще все локальные объекты
###### Dictionary<uint, GameObject> MineGameobjectPool - список локальных объектов
###### Dictionary<uint, GameObject> MovingObjects - список локальных движущихся объектов
###### Player Host - игрок, являющийся хостом комнаты
###### float SendRate - количество обновлений состояния локальных объектов в секунду (readonly)

### Поля Player:
###### string NickName - никнейм игрока
###### uint ID - идентификатор игрока
###### bool IsHost - флаг, указывающий на то, является ли игрок хостом

### Классы:
###### RoomSettings - класс, содержащий информацию о создаваемой/получаемой комнате. Наследуется от RoomInfoPacket
###### Room - класс, содержащий информацию о комнате. Наследуется от RoomSettings
###### Player - класс, содержащий информацию об определенном игроке. Наследуется от PlayerInfoPacket
