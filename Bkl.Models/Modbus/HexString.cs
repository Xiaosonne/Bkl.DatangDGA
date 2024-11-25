using System;
using System.Collections.Generic;
using System.Linq;


namespace Bkl.Models.Std
{
    public class HexString
    {
        public string Text;
        public ushort[] Raw;
        public HexString(ushort[] data)
        {
            Raw = data;
            Text = string.Join("", data.Select(s => s.ToString("x4")).ToArray());
        }
        public override string ToString()
        {
            return Text;
        }
        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public Int16 GetInt16(ModbusByteDataOrder order)
        {
            switch (order)
            {
                case ModbusByteDataOrder.AB:
                case ModbusByteDataOrder.ABCD:
                case ModbusByteDataOrder.CDAB:
                case ModbusByteDataOrder.None:
                    return (short)Raw[0];
                case ModbusByteDataOrder.BA:
                case ModbusByteDataOrder.DCBA:
                case ModbusByteDataOrder.BADC:
                    return (short)((Raw[0] & 0xff00) >> 8 | ((Raw[0] & 0xff) << 8));
                default:
                    throw new ArgumentException($"order:{order}");
            }
        }
        public UInt16 GetUInt16(ModbusByteDataOrder order)
        {
            switch (order)
            {
                case ModbusByteDataOrder.AB:
                case ModbusByteDataOrder.ABCD:
                case ModbusByteDataOrder.CDAB:
                case ModbusByteDataOrder.None:
                    return (ushort)Raw[0];
                case ModbusByteDataOrder.BA:
                case ModbusByteDataOrder.DCBA:
                case ModbusByteDataOrder.BADC:
                    return (ushort)((Raw[0] & 0xff00) >> 8 | ((Raw[0] & 0xff) << 8));
                default:
                    throw new ArgumentException($"order:{order}");
            }
        }
        public float GetFloat(ModbusByteDataOrder order)
        {
            return Convert<float>(order, BitConverter.ToSingle);

        }
        public Int32 GetInt32(ModbusByteDataOrder order)
        {
            return Convert<Int32>(order, BitConverter.ToInt32);
        }
        public UInt32 GetUInt32(ModbusByteDataOrder order)
        {
            return Convert<UInt32>(order, BitConverter.ToUInt32);
        }
        private T Convert<T>(ModbusByteDataOrder order, Func<byte[], int, T> convert)
        {
            switch (order)
            {
                case ModbusByteDataOrder.ABCD:
                case ModbusByteDataOrder.BADC:
                case ModbusByteDataOrder.CDAB:
                case ModbusByteDataOrder.DCBA:
                    int[] orderArr = new int[] { (int)order / 1000 - 1, ((int)order % 1000) / 100 - 1, ((int)order % 100) / 10 - 1, (int)order % 10 - 1 };
                    byte[] arr = new byte[Raw.Length * 2];
                    for (int i = 0; i < Raw.Length; i++)
                    {
                        ////00286713 13672800
                        //arr[2 * i] = (byte)((Raw[i] & 0xff00) >> 8);
                        //arr[2 * i+1] = (byte)((Raw[i] & 0xff));

                        //07174528 45280717
                        Array.Copy(BitConverter.GetBytes(Raw[i]), 0, arr, 2 * i, 2);
                    }
                    byte[] arrOrdered = new byte[arr.Length];
                    for (int i = 0; i < arrOrdered.Length; i++)
                    {
                        arrOrdered[orderArr[i]] = arr[i];
                    }
                    return convert(arrOrdered, 0);
                default:
                    throw new ArgumentException($"order:{order}");
            }
        }
        public static implicit operator HexString(string value)
        {
            if (value == null)
                return default(HexString);
            return new HexString(_ushortConvert(value).ToArray());
        }
        public static implicit operator HexString(ushort[] value)
        {
            return new HexString(value);
        }
        static IEnumerable<ushort> _ushortConvert(string value)
        {
            int len = value.Length / 4;
            for (int i = 0; i < len; i++)
            {
                var temp = value.Substring(i * 4, 4);
                yield return ushort.Parse(temp, System.Globalization.NumberStyles.HexNumber);
            }
            yield break;
        }
    }
}
