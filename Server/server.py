import socket
import random
from threading import Thread
import time
import math
import traceback
import hashlib

import gamemode
from bitstream import *
from enums import *
from config import *

console_messages = []
server = None

def log(text):
    server.add_console_message("[Server]: " + str(text))

    print("[Server]:", text)

def logerror(text):
    server.add_console_message("[Server Error]: " + str(text))

    print("[Server Error]:", text)

class GameObject:
    def __init__(self):
        self.id = 0
        self.name = ""
        self.owner = None
        self.room = None
        self.position = []
        self.rotation = []
    
    def __str__(self):
        return f"{self.name}[{self.id}]. Owner: {self.owner}"

class Player:
    def __init__(self):
        self.ip = None
        self.socket = None
        self.pings = 0
        self.room = None
        self.server = None
        self.room = None
        self.active = True
        self.crypto_key = 0
        self.encrypted = False
        self.nickname = ""
        self.id = 0
        self.position = [0, 0, 0]
        self.streamed_objects = []
        self.players_initializaed = False
        self.properties = {}
        self.console = False

        Thread(target=self.update_stream_zone).start()

    def __str__(self):
        return f"{self.nickname}[{self.id}]"

    def ping(self):
        while self.active:
            bs = BitStream()
            for i in range(16):
                bs.write_byte(PING)
            self.send_bitstream(bs)
            self.pings += 1
            if self.pings > 10:
                self.server.remove_udp_user(self, "timeout")
            time.sleep(1)

    def update_stream_zone(self):
        while self.active:
            time.sleep(0.1)
            if self.room == None:
                continue

            if self.players_initializaed == False:
                continue

            gameobjects = self.room.gameobjects
            for gameobject in gameobjects:
                if self.get_distance(self, gameobject) < STREAM_ZONE:
                    if not gameobject in self.streamed_objects:
                        self.streamed_objects.append(gameobject)
                        if gameobject.owner != self:
                            self.send_create_gameobject(gameobject)
                else:
                    if gameobject in self.streamed_objects:
                        self.streamed_objects.remove(gameobject)
                        if gameobject.owner != self:
                            self.send_destroy_gameobject(gameobject)

            for gameobject in self.streamed_objects:
                if gameobject not in self.room.gameobjects:
                    self.streamed_objects.remove(gameobject)
                    self.send_destroy_gameobject(gameobject)

    def send_allow_connection(self):
        bs = BitStream()
        bs.write_byte(PACKET_ENCRYPTED_CONNECTION_REQUEST)
        bs.write_float(STREAM_ZONE)
        bs.write_float(SEND_MULTIPLIER)
        self.send_packet(bs)
        if self.console == False:
            self.encrypted = True
        time.sleep(0.2)
        self.send_create_player_class(self, init=True, local=True)

    def send_create_player_class(self, player, init=False, local=False, host=False):
        bs = BitStream()
        bs.write_byte(PACKET_CREATE_PLAYER_CLASS)
        bs.write_bool(init)
        bs.write_bool(local)
        bs.write_string8(player.nickname)
        bs.write_uint32(player.id)
        bs.write_bool(host)
        self.send_packet(bs)

    def send_remove_player_class(self, player):
        bs = BitStream()
        bs.write_byte(PACKET_REMOVE_PLAYER_CLASS)
        bs.write_uint32(player.id)
        self.send_packet(bs, 1)

    def send_create_gameobject(self, gameobject):
        bs = BitStream()
        bs.write_byte(PACKET_INSTANTIATE_GAMEOBJECT)
        bs.write_uint32(gameobject.owner.id)
        bs.write_uint32(gameobject.id)
        bs.write_string8(gameobject.name)

        bs.write_float(gameobject.position[0])
        bs.write_float(gameobject.position[1])
        bs.write_float(gameobject.position[2])

        bs.write_float(gameobject.rotation[0])
        bs.write_float(gameobject.rotation[1])
        bs.write_float(gameobject.rotation[2])
        bs.write_float(gameobject.rotation[3])

        self.send_packet(bs, 2)

    def send_destroy_gameobject(self, gameobject):
        bs = BitStream()
        bs.write_byte(PACKET_DESTROY_GAMEOBJECT)
        bs.write_uint32(gameobject.id)
        self.send_packet(bs, 255)

    def send_transform_sync(self, object_id, position, rotation):
        bs = BitStream()
        bs.write_byte(PACKET_TRANSFORM_SYNC)
        bs.write_uint32(object_id)

        bs.write_float(position[0])
        bs.write_float(position[1])
        bs.write_float(position[2])

        bs.write_float(rotation[0])
        bs.write_float(rotation[1])
        bs.write_float(rotation[2])
        bs.write_float(rotation[3])

        self.send_packet(bs)

    def send_rigidbody_sync(self, object_id, mass, drag, angular_drag, gravity, kinematic, position, velocity, angular_velocity, rotation):
        bs = BitStream()
        bs.write_byte(PACKET_RIGIDBODY_SYNC)
        bs.write_uint32(object_id)

        bs.write_uint16(mass)
        bs.write_float(drag)
        bs.write_float(angular_drag)
        bs.write_bool(gravity)
        bs.write_bool(kinematic)

        bs.write_float(position[0])
        bs.write_float(position[1])
        bs.write_float(position[2])

        bs.write_float(velocity[0])
        bs.write_float(velocity[1])
        bs.write_float(velocity[2])

        bs.write_float(angular_velocity[0])
        bs.write_float(angular_velocity[1])
        bs.write_float(angular_velocity[2])

        bs.write_float(rotation[0])
        bs.write_float(rotation[1])
        bs.write_float(rotation[2])
        bs.write_float(rotation[3])

        self.send_packet(bs)

    def send_xor_key(self):
        raw_xor_key = random.randint(4, 254)
        raw_xor_key_location = random.randint(10, 250)

        xor_key = self.encrypt_byte(raw_xor_key)
        xor_key_location = self.encrypt_byte(raw_xor_key_location)

        key_bytes = [xor_key_location]

        for i in range(255):
            key_bytes.append(random.randint(0, 255))

        key_bytes[raw_xor_key_location] = xor_key

        bs = BitStream()
        bs.write_byte(XOR_PUBLIC_KEY)
        for key_byte in key_bytes:
            bs.write_byte(key_byte)
        self.send_packet(bs)

        self.crypto_key = raw_xor_key

    def encrypt_byte(self, value):
        return self.rotate_right(value, 3) ^ 165

    def decrypt_byte(self, value):
        return self.rotate_left(value ^ 165, 3)

    def rotate_right(self, n, rotations, width=8):
        return (2**width-1) & (n>>rotations|n<<(width-rotations))

    def rotate_left(self, n, rotations, width=8):
        rotations = width - rotations
        return (2**width-1) & (n>>rotations|n<<(width-rotations))

    def disconnect(self):
        self.active = False

    def get_distance(self, player, gameobject):
        pos1 = player.position
        pos2 = gameobject.position
        return math.sqrt((pos2[0] - pos1[0])**2 + (pos2[1] - pos1[1])**2 + (pos2[2] - pos1[2])**2)

    def send_players_info(self, joinedroom):
        for player in joinedroom.players:
            player.send_create_player_class(self)
            if player != self:
                self.send_create_player_class(player, init=True, local=player == self, host=player == joinedroom.host)
        time.sleep(1)
        player.players_initializaed = True

    def send_chat_message(self, message):
        bs = BitStream()
        bs.write_byte(PACKET_CHAT)
        bs.write_string16(message)
        self.send_packet(bs)
    
    def send_host_changed(self, newhost):
        bs = BitStream()
        bs.write_byte(PACKET_HOST_CHANGED)
        bs.write_uint32(newhost.id)
        self.send_packet(bs, 1)

    def send_create_room_accepted(self, newroom):
        bs = BitStream()
        bs.write_byte(PACKET_CREATE_ROOM_REQUEST_ACCEPTED)
        bs.write_string8(newroom.name)
        bs.write_string8(newroom.map)
        bs.write_string8(newroom.gamemode)
        bs.write_byte(newroom.max_players)
        bs.write_byte(len(newroom.players))
        bs.write_bool(newroom.password != None)
        bs.write_string8("")
        self.send_packet(bs)

        self.send_players_info(newroom)

    def send_join_room_accepted(self, joinedroom):
        self.send_players_info(joinedroom)

        bs = BitStream()
        bs.write_byte(PACKET_JOIN_ROOM_REQUEST_ACCEPTED)
        bs.write_string8(joinedroom.name)
        bs.write_string8(joinedroom.map)
        bs.write_string8(joinedroom.gamemode)
        bs.write_byte(joinedroom.max_players)
        bs.write_byte(len(joinedroom.players))
        bs.write_bool(joinedroom.password != None)
        bs.write_string8("")
        self.send_packet(bs)

    def send_kick_player(self):
        bs = BitStream()
        bs.write_byte(PACKET_KICK_PLAYER)
        self.send_packet(bs)

    def send_teleport_gameobject(self, gameobject, position):
        bs = BitStream()
        bs.write_byte(PACKET_GAMEOBJECT_TELEPORT)
        bs.write_uint32(gameobject.id)
        bs.write_float(position[0])
        bs.write_float(position[1])
        bs.write_float(position[2])
        self.send_packet(bs)

    def send_error(self, error):
        bs = BitStream()
        bs.write_byte(PACKET_INTERNAL_ERROR)
        bs.write_uint32(error[0])
        bs.write_string8(error[1])
        self.send_packet(bs, 1)

    def send_console_message(self, message):
        bs = BitStream()
        bs.write_byte(PACKET_CONSOLE_MESSAGE)
        bs.write_string8(message)
        self.send_packet(bs)

    def send_packet(self, bs, priority=0):
        bytes_list = list(bs.get_bytes())
        bytes_list.insert(0, PACKET)
        send_bs = BitStream()
        send_bs.set_bytes(bytes_list)
        self.send_bitstream(send_bs, priority)

    def send_rpc(self, bs, sender_id):
        bs.read_offset = 0
        info_bytes = bs.bytes[5:]

        send_bs = BitStream()
        send_bs.write_byte(RPC)
        send_bs.write_uint32(sender_id)
        for byte in info_bytes:
            send_bs.write_byte(byte)
        self.send_bitstream(send_bs)

    def send_bitstream(self, bs, priority=0):
        try:
            if self.encrypted == True:
                xored_bytes = self.server.xor_byte_list(bs.get_bytes(), self.crypto_key)
                self.socket.sendto(bytes(xored_bytes), self.ip)
            else:
                self.socket.sendto(bs.get_bytes(), self.ip)
        except Exception as error:
            logerror(traceback.format_exc())

class Room:
    def __init__(self):
        self.active = True
        self.name = None
        self.map = None
        self.gamemode = None
        self.max_players = 0
        self.password = None
        self.players = []
        self.host = None
        self.server = None
        self.gameobjects = []

        gamemode.OnRoomCreated(self)

    def __str__(self):
        return f"Name({self.name}), map({self.map}), mode({self.gamemode}), online({len(self.players)}/{self.max_players})"

    def add_player(self, player):
        self.players.append(player)
        player.room = self

    def get_gameobject_by_id(self, id):
        for gameobject in self.gameobjects:
            if gameobject.id == id:
                return gameobject
        return None

    def add_gameobject(self, gameobject):
        self.gameobjects.append(gameobject)
        gameobject.room = self

    def destroy_gameobject(self, destroyed_gameobject):
        if destroyed_gameobject != None:
            self.gameobjects.remove(destroyed_gameobject)
            if destroyed_gameobject.owner != None:
                destroyed_gameobject.owner.send_destroy_gameobject(destroyed_gameobject)
            del destroyed_gameobject

    def teleport_gameobject(self, teleported_gameobject, position):
        if teleported_gameobject != None:
            teleported_gameobject.owner.send_teleport_gameobject(teleported_gameobject, position)

    def send_transform_sync(self, sender, object_id, position, rotation):
        if self.get_gameobject_by_id(object_id) == None:
            return

        for player in self.players:
            if player != sender and self.get_gameobject_by_id(object_id) in player.streamed_objects:
                player.send_transform_sync(object_id, position, rotation)

    def send_rigidbody_sync(self, sender, object_id, mass, drag, angular_drag, gravity, kinematic, position, velocity, angular_velocity, rotation):
        if self.get_gameobject_by_id(object_id) == None:
            return

        for player in self.players:
            if player != sender and self.get_gameobject_by_id(object_id) in player.streamed_objects:
                player.send_rigidbody_sync(object_id, mass, drag, angular_drag, gravity, kinematic, position, velocity, angular_velocity, rotation)

    def send_desync_rigidbody(self, sender, object_id, freeze):
        if self.get_gameobject_by_id(object_id) == None:
            return

        for player in self.players:
            if player != sender and self.get_gameobject_by_id(object_id) in player.streamed_objects:
                player.send_desync_rigidbody(object_id, freeze)

    def send_create_gameobject_to_all(self, gameobject):
        for player in self.players:
            if player != gameobject.owner:
                player.send_create_gameobject(gameobject)

    def send_destroy_gameobject_to_all(self, gameobject):
        for player in self.players:
            player.send_destroy_gameobject(gameobject)

    def send_chat_message(self, message):
        for player in self.players:
            player.send_chat_message(message)

    def send_host_changed_to_all(self, newhost):
        for player in self.players:
            player.send_host_changed(newhost)

    def destroy_player_gameobjects(self, player):
        target_gameobjects = []

        for gameobject in self.gameobjects:
            if gameobject.owner == player:
                target_gameobjects.append(gameobject)

        for gameobject in target_gameobjects:
            time.sleep(0.02)
            self.destroy_gameobject(gameobject)

    def remove_player(self, player):
        if not player in self.players:
            return

        for room_player in self.players:
            if room_player != player:
                room_player.send_remove_player_class(player)
        
        if player == self.host:
            if len(self.players) > 1:
                self.host = self.players[1]
                self.send_host_changed_to_all(self.host)
            else:
                self.host = None

        self.players.remove(player)

        Thread(target=self.destroy_player_gameobjects, args=(player, )).start()

    def kick_player(self, player):
        player.send_kick_player()
        self.remove_player(player)
        self.server.remove_udp_user(player, "kicked")

    def send_rpc_to_all(self, bs, sender_id):
        for player in self.players:
            player.send_rpc(bs, sender_id)

    def send_rpc_to_host(self, bs, sender_id):
        self.host.send_rpc(bs, sender_id)

    def handle_room(self):
        while self.active:
            try:
                if len(self.players) == 0:
                    self.close_room()
                    break
                time.sleep(1)
            except Exception as error:
                logerror(traceback.format_exc())

    def close_room(self):
        if DEEP_LOGGING:
            log("Room closed: " + self.name)
        self.active = False

        gamemode.OnRoomClosed(self)

        self.server.rooms.remove(self)

class Server:
    def __init__(self):
        self.server = None
        self.users = []
        self.rooms = []
        self.version = SERVER_VERSION
        self.fingerprint = self.get_fingerprint()

    def add_console_message(self, message):
        global console_messages
        console_messages.append(message)
        if len(console_messages) > 20:
            console_messages.pop(0)

        for player in self.users:
            if player.console == True:
                player.send_console_message(str(message))

    def get_fingerprint(self):
        hash_obj = hashlib.new("md5")

        with open("server.py", "rb") as file1:
            file1_content = file1.read()
            hash_obj.update(file1_content)
        with open("gamemode.py", "rb") as file2:
            file2_content = file2.read()
            hash_obj.update(file2_content)

        return str(hash_obj.hexdigest())

    def start(self, ip, port):
        log("Server started on " + str((ip, port)))
        self.server = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.server.bind((ip, port))
        gamemode.start(self)
        self.listen()

    def remove_udp_user(self, player, reason):
        log("Player disconnected: " + player.nickname + ". Reason: " + reason)
        if not player in self.users:
            return

        self.remove_player_from_room(player)
        player.disconnect()
        self.users.remove(player)

        gamemode.OnPlayerDisconnected(player, reason)

        del player

    def remove_player_from_room(self, player):
        if not player in self.users:
            return

        if player.room != None:
            if DEEP_LOGGING:
                log("Player quit from room: " + player.nickname)
            gamemode.OnPlayerLeavedRoom(player, player.room)

            player.room.remove_player(player)
            player.room = None

    def xor_byte_list(self, array, key):
        xored_array = []
        for value in array:
            xored_array.append(value ^ key)
        return xored_array

    def get_player_by_ip(self, ip):
        for user in self.users:
            if user.ip == ip:
                return user
        return None

    def get_player_by_id(self, id):
        for user in self.users:
            if user.id == id:
                return user
        return None

    def get_room_by_name(self, name):
        for room in self.rooms:
            if room.name == name:
                return room
        return None

    def generate_id(self):
        return random.randint(0x00, 0xFFFFFFFF)

    def process_packet(self, id, bs, ip):
        sender = self.get_player_by_ip(ip)

        if gamemode.OnReceivePacket(sender, id, bs, ip) == False:
            return

        if id == ENCRYPTION_START_REQUEST:
            def error(arg):
                time.sleep(1)
                send_bs = BitStream()
                send_bs.write_byte(PACKET)
                send_bs.write_byte(PACKET_INTERNAL_ERROR)
                send_bs.write_uint32(arg[0])
                send_bs.write_string8(arg[1])
                self.server.sendto(send_bs.get_bytes(), ip)

            if len(self.users) >= MAX_PLAYERS:
                Thread(target=error, args=(ERROR_SERVER_MAX_CONNECTIONS, )).start()
                return

            same_ip = 0
            for player in self.users:
                if player.ip[0] == ip[0]:
                    same_ip += 1
            if same_ip >= MAX_PLAYERS_PER_IP:
                error(ERROR_MAX_PLAYER_PER_IP)
                return

            nickname = bs.read_string8()
            console = False
            try:
                console = bs.read_bool()
            except:
                pass

            if len(nickname) < 3 or len(nickname) > 30:
                error(ERROR_BAD_NICKNAME)
                return

            if "console" in nickname and console == False:
                error(ERROR_BAD_NICKNAME)
                return

            newuser = Player()
            newuser.ip = ip
            newuser.socket = self.server
            newuser.server = self
            newuser.nickname = nickname
            newuser.id = self.generate_id()
            newuser.console = console
            if console == False:
                newuser.send_xor_key()
            Thread(target=newuser.ping).start()
            self.users.append(newuser)

            gamemode.OnPlayerConnected(newuser)

            log("New player connected to server: " + newuser.nickname)
        elif id == PACKET_ENCRYPTED_CONNECTION_REQUEST:
            Thread(target=sender.send_allow_connection).start()

            for message in console_messages:
                sender.send_console_message(message)
        elif id == PACKET_DISCONNECT:
            self.remove_udp_user(sender, "quit")
        elif id == PACKET_CREATE_ROOM_REQUEST:
            room_name = bs.read_string8()
            map_name = bs.read_string8()
            gamemode_name = bs.read_string8()
            max_players = bs.read_byte()
            unused_current_players = bs.read_byte()
            unused_have_password = bs.read_bool()
            password = bs.read_string8()

            if room_name == "":
                room_name = "Room " + str(random.randint(1000, 9999))

            if sender.room != None:
                return
            
            if self.get_room_by_name(room_name) != None:
                sender.send_error(ERROR_ROOM_NAME_TAKEN)

            if gamemode.OnRoomCreateRequest(sender, room_name, map_name, gamemode_name, max_players, password) == False:
                return

            newroom = Room()
            newroom.name = room_name
            newroom.map = map_name
            newroom.gamemode = gamemode_name
            newroom.max_players = max_players
            newroom.host = sender
            newroom.server = self
            newroom.password = password

            newroom.add_player(sender)
            Thread(target=newroom.handle_room).start()
            self.rooms.append(newroom)
            sender.send_create_room_accepted(newroom)

            log("New room: " + room_name)
        elif id == PACKET_JOIN_ROOM_REQUEST:
            room_name = bs.read_string8()
            password = bs.read_string8()

            target_room = self.get_room_by_name(room_name)

            if sender.room != None:
                return

            if target_room == None:
                sender.send_error(ERROR_ROOM_NOT_EXIST)
                return

            if target_room.password != password and sender.console == False:
                sender.send_error(ERROR_ROOM_WRONG_PASSWORD)
                return

            if HANDLE_SAME_NICKNAMES_ON_ROOM == False:
                for player in target_room.players:
                    if player.nickname == sender.nickname:
                        sender.send_error(ERROR_NICKNAME_TAKEN)
                        return

            if gamemode.OnRoomJoinRequest(sender, target_room) == False:
                return

            target_room.add_player(sender)
            Thread(target=sender.send_join_room_accepted, args=(target_room, )).start()
            log("Player " + sender.nickname + " joined to room " + room_name)
        elif id == PACKET_GET_ROOM_LIST_REQUEST:
            for room in self.rooms:
                if len(room.players) < 1:
                    continue

                bs = BitStream()
                bs.write_byte(PACKET_ROOM_LIST)
                bs.write_string8(room.name)
                bs.write_string8(room.map)
                bs.write_string8(room.gamemode)
                bs.write_byte(room.max_players)
                bs.write_byte(len(room.players))
                bs.write_bool(room.password != None and room.password != "")
                if sender.console == True:
                    bs.write_string8(room.password)
                else:
                    bs.write_string8("")
                sender.send_packet(bs)
        elif id == PACKET_INSTANTIATE_GAMEOBJECT:
            if sender.room == None:
                return

            owner_id = bs.read_uint32()
            id = bs.read_uint32()
            name = bs.read_string8()
            position = [bs.read_float(), bs.read_float(), bs.read_float()]
            rotation = [bs.read_float(), bs.read_float(), bs.read_float(), bs.read_float()]

            new_object = GameObject()
            new_object.id = id
            new_object.name = name
            new_object.owner = sender
            new_object.position = position
            new_object.rotation = rotation

            if gamemode.OnInstantiateObject(sender, new_object) == False:
                sender.send_destroy_gameobject(new_object)
                return

            sender.room.add_gameobject(new_object)
        elif id == PACKET_DESTROY_GAMEOBJECT:
            if sender.room == None:
                return

            destroyed_id = bs.read_uint32()
            destroyed_object = sender.room.get_gameobject_by_id(destroyed_id)

            if destroyed_object == None:
                return

            if gamemode.OnDestroyObject(sender, destroyed_object) == False:
                return


            sender.room.destroy_gameobject(sender.room.get_gameobject_by_id(destroyed_id))
        elif id == PACKET_TRANSFORM_SYNC:
            if sender.room == None:
                return

            object_id = bs.read_uint32()
            position = [bs.read_float(), bs.read_float(), bs.read_float()]
            rotation = [bs.read_float(), bs.read_float(), bs.read_float(), bs.read_float()]

            gameobject = sender.room.get_gameobject_by_id(object_id)

            if gameobject == None:
                return

            if gameobject.owner != sender:
                return

            if gamemode.OnTransformSync(sender, gameobject, position, rotation) == False:
                return

            gameobject.position = position
            gameobject.rotation = rotation

            sender.room.send_transform_sync(sender, object_id, position, rotation)
        elif id == PACKET_RIGIDBODY_SYNC:
            object_id = bs.read_uint32()
            mass = bs.read_uint16()
            drag = bs.read_float()
            angular_drag = bs.read_float()
            gravity = bs.read_bool()
            kinematic = bs.read_bool()
            position = [bs.read_float(), bs.read_float(), bs.read_float()]
            velocity = [bs.read_float(), bs.read_float(), bs.read_float()]
            angular_velocity = [bs.read_float(), bs.read_float(), bs.read_float()]
            rotation = [bs.read_float(), bs.read_float(), bs.read_float(), bs.read_float()]

            gameobject = sender.room.get_gameobject_by_id(object_id)
            
            if gameobject == None:
                return

            if gameobject.owner != sender:
                return

            if gamemode.OnRigidbodySync(sender, gameobject, mass, drag, angular_drag, gravity, kinematic, position, velocity, angular_velocity, rotation) == False:
                return

            gameobject.position = position
            gameobject.rotation = rotation

            sender.room.send_rigidbody_sync(sender, object_id, mass, drag, angular_drag, gravity, kinematic, position, velocity, angular_velocity, rotation)
        if id == PACKET_UPDATE_STREAM_ZONE:
            camera_position = [bs.read_float(), bs.read_float(), bs.read_float()]

            if gamemode.OnUpdateStreamZone(sender, camera_position) == False:
                return

            sender.position = camera_position
        if id == PACKET_CHAT:
            if sender == None:
                return

            message = bs.read_string16()

            if len(message) == 0:
                return

            if message[0] == "/":
                gamemode.OnChatCommand(sender, message[1:])
                return

            if gamemode.OnChatMessage(sender, message) == False:
                return

            if sender.room == None:
                if sender.console == True:
                    gamemode.AddGlobalChatMessage(message)
                return

            sender.room.send_chat_message(sender.nickname + ": " + message)
        if id == PACKET_KICK_PLAYER:
            kicked_player_id = bs.read_uint32()

            kicked_player = self.get_player_by_id(kicked_player_id)

            if sender != sender.room.host or kicked_player == None:
                return

            if gamemode.OnPlayerKicked(sender, kicked_player) == False:
                return

            sender.room.kick_player(kicked_player)

    def process_rpc(self, bs, ip):
        sender = self.get_player_by_ip(ip)
        target_id = bs.read_uint32()
        rpc_name = bs.read_string8()
        args_count = bs.read_byte()

        if gamemode.process_rpc(sender, target_id, rpc_name, args_count, bs, ip) == False:
            return

        if target_id == TARGET_ROOM and sender.room != None:
            sender.room.send_rpc_to_all(bs, sender.id)
        elif target_id == TARGET_HOST and sender.room != None:
            sender.room.send_rpc_to_host(bs, sender.id)
        elif target_id == TARGET_GLOBAL:
            self.send_rpc_to_all_connected_players(bs, sender.id)
        else:
            player = self.get_player_by_id(target_id)
            if player != None:
                player.send_rpc(bs, sender.id)

    def send_rpc_to_all_connected_players(self, bs, sender_id):
        for player in self.users:
            player.send_rpc(bs, sender_id)

    def listen(self):
        while True:
            try:
                content, adress = self.server.recvfrom(65535)
                data = list(content)

                bs = BitStream()
                sender_user = self.get_player_by_ip(adress)
                if sender_user != None:
                    if sender_user.encrypted == True:
                        bs.set_bytes(self.xor_byte_list(data, sender_user.crypto_key))
                    else:
                        bs.set_bytes(data)
                else:
                    bs.set_bytes(data)

                data_type = bs.read_byte()

                if data_type == PACKET:
                    data_id = bs.read_byte()
                    self.process_packet(data_id, bs, adress)
                if data_type == RPC:
                    self.process_rpc(bs, adress)
                if data_type == PONG:
                    sender_user.pings = 0
            except IndexError:
                pass
            except ConnectionResetError:
                pass
            except Exception as error:
                logerror(traceback.format_exc())

if __name__ == "__main__":
    hostname = socket.gethostname()

    server = Server()
    server.start(IP, PORT)