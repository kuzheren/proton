from glob import glob
import time
import psutil
import os
import math
from threading import Thread

from bitstream import *
from enums import *
from rpcargument import *

server = None

def start(init):
    global server
    server = init
    OnGamemodeStarted()

def process_rpc(sender, target_id, rpc_name, args_count, bs, ip):
    args = []
    for i in range(args_count):
        type = bs.read_byte()
        if type == BYTE:
            args.append(bs.read_byte())
        elif type == STRING8:
            args.append(bs.read_string8())
        elif type == UINT16:
            args.append(bs.read_uint16())
        elif type == INT16:
            args.append(bs.read_int16())
        elif type == UINT32:
            args.append(bs.read_uint32())
        elif type == INT32:
            args.append(bs.read_int32())
        elif type == FLOAT:
            args.append(bs.read_float())
        elif type == BOOL:
            args.append(bs.read_bool())
    return OnReceiveRPC(sender, target_id, rpc_name, args, bs)

def log(text):
    server.add_console_message("[Gamemode]: " + str(text))

    print("[Gamemode]:", text)

def logerror(text):
    server.add_console_message("[Gamemode Error]: " + str(text))

    print("[Gamemode Error]:", text)

##################################################

def GetDistance(pos1, pos2):
    return math.sqrt((pos2[0] - pos1[0])**2 + (pos2[1] - pos1[1])**2 + (pos2[2] - pos1[2])**2)

def GetPlayerRoom(player):
    return player.room

def GetRoomPlayers(room):
    return room.players

def GetRoomList():
    return server.rooms

def GetPlayerList():
    return server.users

def GetPlayerListFromRoom(room):
    if room == None:
        return None
    return room.players

def GetRoomGameobjects(room):
    if room == None:
        return None
    return room.gameobjects

def GetPlayerGameobjects(player):
    if player == None:
        return None
    if player.room == None:
        return None
    gameobjects = []
    for gameobject in player.room.gameobjects:
        if gameobject.owner == player:
            gameobjects.append(gameobject)
    return gameobjects

def GetPlayerById(id):
    for player in server.users:
        if player.id == id:
            return player
    return None

def GetGameobjectById(id, room=None):
    if room == None:
        for room in server.rooms:
            for gameobject in room.gameobjects:
                if gameobject.id == id:
                    return gameobject
        return
    else:
        for gameobject in room.gameobjects:
            if gameobject.id == id:
                return gameobject
        return None

##################################################

def DestroyObject(object):
    object.room.destroy_gameobject(object)

def KickPlayer(player):
    player.room.kick_player(player)

def SendError(player, error):
    player.send_error(error)

def TeleportGameobject(player, gameobject, position):
    player.send_teleport_gameobject(gameobject, position)

def AddChatMessage(player, message):
    player.send_chat_message(message)

def AddRoomChatMessage(room, message):
    room.send_chat_message(message)

def AddGlobalChatMessage(message):
    for player in server.users:
        player.send_chat_message(message)

def SetPlayerProperty(player, property_name, property):
    player.properties[property_name] = property

def GetPlayerProperty(player, property_name):
    if not property_name in player.properties:
        return None
    return player.properties[property_name]

def SendRPC(player, name, args):
    bs = BitStream()
    bs.write_byte(RPC)
    bs.write_uint32(TARGET_SERVER)
    bs.write_string8(name)
    bs.write_byte(len(args))
    for arg in args:
        bs.write_byte(arg.type)
        if arg.type == BYTE:
            bs.write_byte(arg.value)
        elif arg.type == STRING8:
            bs.write_string8(arg.value)
        elif arg.type == UINT16:
            bs.write_uint16(arg.value)
        elif arg.type == INT16:
            bs.write_int16(arg.value)
        elif arg.type == UINT32:
            bs.write_uint32(arg.value)
        elif arg.type == INT32:
            bs.write_int32(arg.value)
        elif arg.type == FLOAT:
            bs.write_float(arg.value)
        elif arg.type == BOOL:
            bs.write_bool(arg.value)
    player.send_bitstream(bs)

def SendRoomRPC(room, name, args):
    for player in room.players:
        SendRPC(player, name, args)

def SendGlobalRPC(name, args):
    for player in server.users:
        SendRPC(player, name, args)

##################################################

def OnGamemodeStarted():
    log(f"Gamemode started. Server version: {server.version}")

def OnPlayerConnected(player):
    pass

def OnPlayerDisconnected(player, reason):
    pass

def OnRoomCreateRequest(player, room_name, map_name, gamemode_name, max_players, password):
    return True

def OnRoomJoinRequest(player, room):
    if len(room.players) >= room.max_players and player.console == False:
        return False
    return True

def OnPlayerLeavedRoom(player, room):
    pass

def OnRoomCreated(room):
    pass

def OnRoomClosed(room):
    pass

def OnInstantiateObject(player, object):
    return True

def OnDestroyObject(player, object):
    return True

def OnTransformSync(player, object, position, rotation):
    return True

def OnRigidbodySync(player, object, mass, drag, angular_drag, has_gravity, is_kinematic, position, velocity, angular_velocity, rotation):
    return True

def OnUpdateStreamZone(player, position):
    return True

def OnChatMessage(player, message):
    return True

def OnChatCommand(player, cmd):
    if cmd == "help":
        AddChatMessage(player, "/help, /status")
    elif cmd == "status":
        process = psutil.Process(os.getpid())
        memory_info = process.memory_info()
        memory_usage_in_bytes = memory_info.rss
        memory_usage_in_megabytes = memory_usage_in_bytes / 1024 / 1024
        cpu = process.cpu_percent()
        ram = round(memory_usage_in_megabytes, 2)

        status_string = f"Server Status: version {server.version}, CPU: {cpu}%, RAM: {ram} MB, rooms: {len(server.rooms)}, players: {len(server.users)}, MD5 fingerprint: {server.fingerprint}"
        AddChatMessage(player, status_string)
    else:
        AddChatMessage(player, "Unknown command. Type /help for a list of commands")

def OnPlayerKicked(sender_player, kicked_player):
    return True

def OnReceiveRPC(player, target_id, rpc_name, args, bs):
    return True

def OnReceivePacket(player, id, bs, ip):
    return True

##################################################
