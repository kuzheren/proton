using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Proton.Crypto
{
    public static class ProtonCrypto
    {
        public static byte EncryptByte(byte value)
        {
            uint temp = value;
            uint rotated = (temp << 5 | temp >> 3) & 0x000000FF;
            byte rotatedByte = (byte) rotated;
            byte xoredByte = (byte) (rotatedByte ^ 165);

            return rotatedByte;
        }
        public static byte DecryptByte(byte value)
        {
            byte xoredByte = (byte) (value ^ 165);
            uint temp = xoredByte;
            uint rotated = (temp << 3 | temp >> 5) & 0x000000FF;
            byte rotatedByte = (byte) rotated;

            return rotatedByte;
        }
        public static byte[] XorByteArray(byte[] array, byte key)
        {
            List<byte> xoredBytes = new List<byte>();
            foreach (byte value in array)
            {
                xoredBytes.Add((byte) (value ^ key));
            }
            return xoredBytes.ToArray();
        }
    }
}