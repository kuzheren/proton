from struct import Struct
import socket
import struct
from threading import Thread

IP, PORT = "127.0.0.1", 50001

ENCRYPTION_START_REQUEST =                 0
PACKET_ENCRYPTED_CONNECTION_REQUEST =      11
PACKET_CONSOLE_MESSAGE =                   32
PACKET_ROOM_LIST =                         15
PACKET =                                   33
PING =                                     35
PONG =                                     36
PACKET_GET_ROOM_LIST_REQUEST =             14
PACKET_JOIN_ROOM_REQUEST =                 16
PACKET_CHAT =                              21

class BitStream:
    def __init__(self):
        self.bytes = []
        self.read_offset = 0

    def set_bytes(self, bytes):
        self.bytes = bytes
        self.read_offset = 0

    def get_bytes(self):
        return bytes(self.bytes)

    def write_byte(self, value):
        self.bytes.append(value)

    def write_int16(self, value):
        short_bytes = (value).to_bytes(2, byteorder="little", signed=True)
        for i in range(2):
            self.write_byte(short_bytes[i])

    def write_uint16(self, value):
        short_bytes = (value).to_bytes(2, byteorder="little", signed=False)
        for i in range(2):
            self.write_byte(short_bytes[i])

    def write_int32(self, value):
        long_bytes = (value).to_bytes(4, byteorder="little", signed=True)
        for i in range(4):
            self.write_byte(long_bytes[i])

    def write_uint32(self, value):
        long_bytes = (value).to_bytes(4, byteorder="little", signed=False)
        for i in range(4):
            self.write_byte(long_bytes[i])

    def write_float(self, value):
        float_bytes = bytes(struct.pack("f", value))
        for i in range(4):
            self.write_byte(float_bytes[i])

    def write_string8(self, value):
        string_length = len(value)
        if string_length > 255:
            string_length = 255
        self.string_bytes = bytes(value, "cp1251", "replace")

        self.write_byte(string_length)
        for i in range(string_length):
            self.write_byte(self.string_bytes[i])

    def write_string16(self, value):
        string_length = len(value)
        if string_length > 65535:
            string_length = 65535
        self.string_bytes = bytes(value, "cp1251", "replace")

        self.write_uint16(string_length)
        for i in range(string_length):
            self.write_byte(self.string_bytes[i])

    def write_bool(self, value):
        self.write_byte(1 if value else 0)

    def read_byte(self):
        self.read_offset += 1
        return self.bytes[self.read_offset - 1]

    def read_int16(self):
        short_bytes = []
        for i in range(2):
            short_bytes.append(self.read_byte())
        return int.from_bytes(short_bytes, byteorder="little", signed=True)

    def read_uint16(self):
        short_bytes = []
        for i in range(2):
            short_bytes.append(self.read_byte())
        return int.from_bytes(short_bytes, byteorder="little", signed=False)

    def read_int32(self):
        int_bytes = []
        for i in range(4):
            int_bytes.append(self.read_byte())
        return int.from_bytes(int_bytes, byteorder="little", signed=True)

    def read_uint32(self):
        int_bytes = []
        for i in range(4):
            int_bytes.append(self.read_byte())
        return int.from_bytes(int_bytes, byteorder="little", signed=False)

    def read_float(self):
        float_bytes = []
        for i in range(4):
            float_bytes.append(self.read_byte())
        return struct.unpack("f", bytes(float_bytes))[0]

    def read_string8(self):
        string_length = self.read_byte()
        string_bytes = []
        for i in range(string_length):
            string_bytes.append(self.read_byte())
        return bytes(string_bytes).decode("cp1251", "replace")

    def read_string16(self):
        string_length = self.read_uint16()
        string_bytes = []
        for i in range(string_length):
            string_bytes.append(self.read_byte())
        return bytes(string_bytes).decode("cp1251", "replace")

    def read_bool(self):
        return self.read_byte() == 1

class Console:
    def __init__(self):
        self.socket = None
        self.adress = None

    def connect(self, ip, port):
        print("Hi, you are using the ProtonEngine server console! This is a test message.")
        print("List of commands: /help")
        print(f"You have connected to the server at {ip}:{port}")
        print("")

        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.adress = (ip, port)

        bs = BitStream()
        bs.write_byte(PACKET)
        bs.write_byte(ENCRYPTION_START_REQUEST)
        bs.write_string8("CONSOLE")
        bs.write_bool(True)
        self.socket.sendto(bs.get_bytes(), self.adress)

        Thread(target=self.listen).start()

        bs = BitStream()
        bs.write_byte(PACKET)
        bs.write_byte(PACKET_ENCRYPTED_CONNECTION_REQUEST)
        self.socket.sendto(bs.get_bytes(), self.adress)

        self.send()

    def process_packet(self, id, bs):
        if id == PACKET_CONSOLE_MESSAGE:
            msg = bs.read_string8()
            print(msg)
        if id == PACKET_ROOM_LIST:
            room_name = bs.read_string8()
            map_name = bs.read_string8()
            gamemode_name = bs.read_string8()
            max_players = bs.read_byte()
            current_players = bs.read_byte()
            have_password = bs.read_bool()
            password = bs.read_string8()
            print(f"Name({room_name}), map({map_name}), mode({gamemode_name}), online({current_players}/{max_players}), password({password})")
        if id == PACKET_CHAT:
            msg = bs.read_string16()
            print("[Chat]: " + str(msg))
    
    def listen(self):
        while True:
            content = self.socket.recv(65535)
            data = list(content)

            bs = BitStream()
            bs.set_bytes(data)
            data_type = bs.read_byte()
            if data_type == PACKET:
                data_id = bs.read_byte()
                self.process_packet(data_id, bs)
            if data_type == PING:
                bs = BitStream()
                bs.write_byte(PONG)
                bs.write_byte(PONG)
                self.socket.sendto(bs.get_bytes(), self.adress)

    def send(self):
        while True:
            cmd = input()
            if cmd == "/help":
                print("/help, /rooms, /join {room.name}, /chat {message}")
            if cmd == "/rooms":
                bs = BitStream()
                bs.write_byte(PACKET)
                bs.write_byte(PACKET_GET_ROOM_LIST_REQUEST)
                self.socket.sendto(bs.get_bytes(), self.adress)
            if "/join " in cmd:
                room = cmd[6:]
                bs = BitStream()
                bs.write_byte(PACKET)
                bs.write_byte(PACKET_JOIN_ROOM_REQUEST)
                bs.write_string8(room)
                bs.write_string8("")
                self.socket.sendto(bs.get_bytes(), self.adress)
            if "/chat " in cmd:
                msg = cmd[6:]
                bs = BitStream()
                bs.write_byte(PACKET)
                bs.write_byte(PACKET_CHAT)
                bs.write_string16(msg)
                self.socket.sendto(bs.get_bytes(), self.adress)
                

if __name__ == "__main__":
    console = Console()
    console.connect(IP, PORT)
