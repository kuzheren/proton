import struct

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
        string_bytes = value.encode("utf-8", "replace")

        string_length = len(string_bytes)
        if string_length > 255:
            string_length = 255

        self.write_byte(string_length)
        for i in range(string_length):
            self.write_byte(string_bytes[i])

    def write_string16(self, value):
        string_bytes = value.encode("utf-8", "replace")

        string_length = len(string_bytes)
        if string_length > 65535:
            string_length = 65535

        self.write_uint16(string_length)
        for i in range(string_length):
            self.write_byte(string_bytes[i])

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
        return bytes(string_bytes).decode("utf-8")

    def read_string16(self):
        string_length = self.read_uint16()
        string_bytes = []
        for i in range(string_length):
            string_bytes.append(self.read_byte())
        return bytes(string_bytes).decode("utf-8")

    def read_bool(self):
        return self.read_byte() == 1