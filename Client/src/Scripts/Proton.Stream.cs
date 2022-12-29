using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

namespace Proton.Stream
{
    public class ProtonStream
    {
        public List<byte> Bytes = new List<byte>();
        public short ReadOffset = 0;

        public void WriteByte(object value)
        {
            byte castedValue = (byte) value;
            Bytes.Add(castedValue);
        }
        public void WriteInt16(object value)
        {
            short castedValue = (short) value;
            byte[] shortBytes = BitConverter.GetBytes(castedValue);
            for (int i = 0; i < shortBytes.Length; i++)
            {
                WriteByte(shortBytes[i]);
            }
        }
        public void WriteUInt16(object value)
        {
            ushort castedValue = (ushort) value;
            byte[] ushortBytes = BitConverter.GetBytes(castedValue);
            for (int i = 0; i < ushortBytes.Length; i++)
            {
                WriteByte(ushortBytes[i]);
            }
        }
        public void WriteInt32(object value)
        {
            int castedValue = (int) value;
            byte[] longBytes = BitConverter.GetBytes(castedValue);
            for (int i = 0; i < longBytes.Length; i++)
            {
                WriteByte(longBytes[i]);
            }
        }
        public void WriteUInt32(object value)
        {
            uint castedValue = (uint) value;
            byte[] ulongBytes = BitConverter.GetBytes(castedValue);
            for (int i = 0; i < ulongBytes.Length; i++)
            {
                WriteByte(ulongBytes[i]);
            }
        }
        public void WriteFloat(float value)
        {
            byte[] floatBytes = BitConverter.GetBytes(value);
            for (int i = 0; i < floatBytes.Length; i++)
            {
                WriteByte(floatBytes[i]);
            }
        }
        public void WriteString8(string value)
        {
            byte length = (byte) value.Length;
            byte[] stringBytes = Encoding.GetEncoding("Windows-1251").GetBytes(value);

            WriteByte(length);
            for (int i = 0; i < length; i++)
            {
                WriteByte(stringBytes[i]);
            }
        }
        public void WriteString16(string value)
        {
            ushort length = (ushort) value.Length;
            byte[] stringBytes = Encoding.GetEncoding("Windows-1251").GetBytes(value);

            WriteUInt16(length);
            for (int i = 0; i < length; i++)
            {
                WriteByte(stringBytes[i]);
            }
        }
        public void WriteBool(bool value)
        {
            WriteByte((byte)  (value ? 1 : 0));
        }
        public void WriteVector3(UnityEngine.Vector3 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
        }
        public void WriteQuaternion(UnityEngine.Quaternion value)
        {
            WriteFloat(value[0]);
            WriteFloat(value[1]);
            WriteFloat(value[2]);
            WriteFloat(value[3]);
        }

        public byte ReadByte()
        {
            ReadOffset++;
            return Bytes[ReadOffset - 1];
        }
        public short ReadInt16()
        {
            List<byte> shortBytes = new List<byte>();
            for (int i = 0; i < 2; i++)
            {
                shortBytes.Add(ReadByte());
            }
            return BitConverter.ToInt16(shortBytes.ToArray(), 0);
        }
        public ushort ReadUInt16()
        {
            List<byte> ushortBytes = new List<byte>();
            for (int i = 0; i < 2; i++)
            {
                ushortBytes.Add(ReadByte());
            }
            return BitConverter.ToUInt16(ushortBytes.ToArray(), 0);
        }
        public int ReadInt32()
        {
            List<byte> intBytes = new List<byte>();
            for (int i = 0; i < 4; i++)
            {
                intBytes.Add(ReadByte());
            }
            return BitConverter.ToInt32(intBytes.ToArray(), 0);
        }
        public uint ReadUInt32()
        {
            List<byte> uintBytes = new List<byte>();
            for (int i = 0; i < 4; i++)
            {
                uintBytes.Add(ReadByte());
            }
            return BitConverter.ToUInt32(uintBytes.ToArray(), 0);
        }
        public float ReadFloat()
        {
            List<byte> floatBytes = new List<byte>();
            for (int i = 0; i < 4; i++)
            {
                floatBytes.Add(ReadByte());
            }
            return BitConverter.ToSingle(floatBytes.ToArray(), 0);
        }
        public string ReadString8()
        {
            byte length = ReadByte();
            List<byte> stringBytes = new List<byte>();

            for (int i = 0; i < length; i++)
            {
                stringBytes.Add(ReadByte());
            }

            return Encoding.GetEncoding("Windows-1251").GetString(stringBytes.ToArray());
        }
        public string ReadString16()
        {
            ushort length = ReadUInt16();
            List<byte> stringBytes = new List<byte>();

            for (int i = 0; i < length; i++)
            {
                stringBytes.Add(ReadByte());
            }
    
            return Encoding.GetEncoding("Windows-1251").GetString(stringBytes.ToArray());
        }
        public bool ReadBool()
        {
            return ReadByte() == 1;
        }
        public UnityEngine.Vector3 ReadVector3()
        {
            return new UnityEngine.Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }
        public UnityEngine.Quaternion ReadQuaternion()
        {
            UnityEngine.Quaternion quaternion = new UnityEngine.Quaternion();
            quaternion[0] = ReadFloat();
            quaternion[1] = ReadFloat();
            quaternion[2] = ReadFloat();
            quaternion[3] = ReadFloat();
            return quaternion;
        }
    }
}