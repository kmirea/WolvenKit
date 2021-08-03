using System;
using System.Numerics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;


namespace WolvenKit.Modkit.RED4.GeneralStructs
{
    public class Converters
    {
        public static float hfconvert(UInt16 read)// for converting ushort representation of a Half float to a float32
        {
            String bin = Convert.ToString(read, 2).PadLeft(16, '0');
            UInt16 sp = Convert.ToUInt16(bin.Substring(6, 10), 2);
            UInt16 pow = Convert.ToUInt16(bin.Substring(1, 5), 2);
            UInt16 sign = Convert.ToUInt16(bin.Substring(0, 1));

            float value = 0f;
            if (pow == 0)
            {
                value = Convert.ToSingle(Math.Pow(2, -14)) * (sp / 1024f);
            }
            else if (pow == 31)
            {
                if (sp == 0)
                    value = float.PositiveInfinity;
                else
                    value = float.NaN;
            }
            else
            {
                value = Convert.ToSingle(Math.Pow(2, pow - 15)) * (1 + sp / 1024f);
            }

            if (sign == 1)
            {
                value = (-1) * value;
            }

            return value;
        }
        public static UInt16 converthf(float value) // a floating point to halffloat uint16 equivalent representation -65504 <= value <= 65504
        {
            UInt16 sign = 0;
            UInt16 sp = 0;
            UInt16 pow = 0;
            if (float.IsNegative(value) && !float.IsNaN(value))
            {
                sign = 32768;
                value = -1 * value;
            }
            if (value > 65504)
            {
                value = 65504;      // if number provided is > Half.Max or < Half.Min then normalized
            }
            if (value >= 0 && value <= (float)0.000060975552)
            {
                pow = 0;
                sp = Convert.ToUInt16(value * 1024 * Math.Pow(2, 14));
            }
            else if (float.IsNaN(value) || float.IsPositiveInfinity(value))
            {
                sp = 0;
                pow = 31744;
                if (float.IsNaN(value))
                    sp = 55; // sp can be anything in this case i randomly put 55
            }
            else if (value >= (float)0.00006103515625 && value <= (float)65504)
            {
                Int16 temp1 = 14;
                UInt64 temp2 = Convert.ToUInt64((value * Math.Pow(2, temp1) - 1) * 1024);
                for (; temp2 > 1023; temp1--)
                {
                    temp2 = Convert.ToUInt64((value * Math.Pow(2, temp1 - 1) - 1) * 1024);
                }
                sp = Convert.ToUInt16(temp2);
                UInt16 temp3 = Convert.ToUInt16((-1 * temp1) + 15);
                pow = Convert.ToUInt16(temp3 << 10);
            }
            UInt16 U16 = Convert.ToUInt16(sign | sp | pow);
            return U16;
        }
        public static Vector4 TenBitShifted(UInt32 U32)
        {
            Int16 X = Convert.ToInt16(U32 & 0x3ff);
            Int16 Y = Convert.ToInt16((U32 >> 10) & 0x3ff);
            Int16 Z = Convert.ToInt16((U32 >> 20) & 0x3ff);
            byte W = Convert.ToByte((U32) >> 30);
            return new Vector4((X - 511) / 512f, (Y - 511) / 512f, (Z - 511) / 512f, W / 3f);
        }
        public static Vector4 TenBitUnsigned(UInt32 U32)
        {
            Int16 X = Convert.ToInt16(U32 & 0x3ff);
            Int16 Y = Convert.ToInt16((U32 >> 10) & 0x3ff);
            Int16 Z = Convert.ToInt16((U32 >> 20) & 0x3ff);
            byte W = Convert.ToByte((U32) >> 30);

            return new Vector4(X / 1023f, Y / 1023f, Z / 1023f, W / 3f);
        }
        public static Vector4 TenBitsigned(UInt32 U32)
        {
            Int16 X = Convert.ToInt16(U32 & 0x3ff);
            Int16 Y = Convert.ToInt16((U32 >> 10) & 0x3ff);
            Int16 Z = Convert.ToInt16((U32 >> 20) & 0x3ff);
            byte W = Convert.ToByte((U32) >> 30);

            if (X > 511)
                X = (Int16)(-1 * (X - 512));
            if (Y > 511)
                Y = (Int16)(-1 * (Y - 512));
            if (Z > 511)
                Z = (Int16)(-1 * (Z - 512));
            return new Vector4(X / 512f, Y / 512f, Z / 512f, W / 3f);
        }
        public static UInt32 Vec4ToU32(Vector4 v) // reversing for 10bit nors and tans
        {
            if (v.X < -0.998046f)
                v.X = -0.998046f;
            if (v.Y < -0.998046f)
                v.Y = -0.998046f;
            if (v.Z < -0.998046f)
                v.Z = -0.998046f;

            UInt32 a = Convert.ToUInt32(v.X * 512 + 511);
            UInt32 b = Convert.ToUInt32(v.Y * 512 + 511) << 10;
            UInt32 c = Convert.ToUInt32(v.Z * 512 + 511) << 20;
            UInt32 d = 0; // for tangents in bits its 00000000000000000000000000000000
            if (v.W == 0)
                d = 1073741824;  // for normals in bits its 01000000000000000000000000000000
            UInt32 U32 = a | b | c | d;
            return U32;
        }
    }
    public class Manipulators
    {
        public static float CalculateRealPart(Quaternion Q)
        {
            float w;
            if ((Q.X * Q.X + Q.Y * Q.Y + Q.Z * Q.Z) >= 1f)
                w = (float)Math.Sqrt((Q.X * Q.X + Q.Y * Q.Y + Q.Z * Q.Z) - 1f);
            else
                w = (float)Math.Sqrt(1f - (Q.X * Q.X + Q.Y * Q.Y + Q.Z * Q.Z));
            return w;
        }
    }
    public static class ReaderExtensions
    {
        public static void seek(this BinaryReader br, long ptr = 0)
        {
            if ((ptr >= 0) && (ptr < br.BaseStream.Length))
                br.BaseStream.Position = ptr;
        }
        public static BinaryReader readBuffer(this byte[] b, long ptr = 0)
        {
            var br = new BinaryReader(new MemoryStream(b));
            if (ptr > 0)
                br.BaseStream.Position = ptr;
            return br;
        }
        public static byte[] getBytes(this WolvenKit.RED4.CR2W.Types.CArray<WolvenKit.RED4.CR2W.Types.CUInt8> byteArray)
        {
            return byteArray.Select(_ => _.Value).ToArray();
        }
        public static string ReadString(this byte[] b, long ptr = 0, int len = 0)
        {
            var s = ASCIIEncoding.ASCII.GetString(b, (int)ptr, len);
            return s;
        }
        public static uint ReadUInt16(this byte[] b, long ptr = 0)
        {
            using var br = new BinaryReader(new MemoryStream(b));
            if (ptr > 0)
                br.BaseStream.Position = ptr;
            var n = br.ReadUInt16();
            return n;
        }
        public static uint ReadUInt32(this byte[] b, long ptr = 0)
        {
            using var br = new BinaryReader(new MemoryStream(b));
            if (ptr > 0)
                br.BaseStream.Position = ptr;
            var n = br.ReadUInt32();
            return n;
        }
        public static List<int> ReadUInt16(this BinaryReader br, int len)
        {
            var vals = new List<int>();
            for (int i = 0; i < len; i++)
            {
                vals.Add(br.ReadUInt16());
            }
            return vals;
        }
        public static List<uint> ReadUInt32(this BinaryReader br, int len)
        {
            var vals = new List<uint>();
            for (int i = 0; i < len; i++)
            {
                vals.Add(br.ReadUInt32());
            }
            return vals;
        }
        public static float readFloat16(this BinaryReader br)
        {
            ushort f = br.ReadUInt16();
            if (f == 0)
                return 1.0f;
            return (float)((32767.0f - f) * (1 / 32768.0f));
        }
        /// <summary>
        /// Packed 48 Bit Quaternion
        /// </summary>
        public static Quaternion readQuat48(this BinaryReader br, bool SignedW = false)
        {
            ushort X = br.ReadUInt16();
            ushort Y = br.ReadUInt16();
            ushort Z = br.ReadUInt16();

            float FX = ((int)X - 32767) / 32767.0f;
            float FY = ((int)Y - 32767) / 32767.0f;
            float FZ = ((int)Z - 32767) / 32767.0f;

            float d = (FX * FX) + (FY * FY) + (FZ * FZ);
            float s = (float)Math.Sqrt(2.0f - d);
            FX *= s;
            FY *= s;
            FZ *= s;
            float W = 1.0f - d;
            W = (SignedW) ? -W : W;
            return new Quaternion(FX, FY, FZ, W);
        }
        /// <summary>
        /// XYZ stripped W
        /// </summary>
        public static Quaternion readQuatXYZ(this BinaryReader br, bool SignedW = false)
        {

            float X = br.ReadSingle();
            float Y = br.ReadSingle();
            float Z = br.ReadSingle();
            float d = (X * X) + (Y * Y) + (Z * Z);
            float s = (float)Math.Sqrt(2.0f - d);
            X *= s;
            Y *= s;
            Z *= s;
            float W = 1.0f - d;
            W = (SignedW) ? -W : W;
            return new Quaternion(X, Y, Z, W);
        }
        public static Quaternion readQuat(this BinaryReader br)
        {
            return new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }
        public static Vector3 readVec3(this BinaryReader br)
        {
            float X = br.ReadSingle();
            float Y = br.ReadSingle();
            float Z = br.ReadSingle();
            return new Vector3(X, Y, Z);
        }
        /// <summary>
        /// Anim Keyframe Time
        /// </summary>
        public static float readDeltaTime(this BinaryReader br, float duration)
        {
            var dtime2 = (int)(br.ReadUInt16());
            return ((float)dtime2 / 0xFFFF) * duration;
        }
        public static Quaternion toQuat(this WolvenKit.RED4.CR2W.Types.Quaternion q)
        {
            return new Quaternion(q.I.Value, q.J.Value, q.K.Value, q.R.Value);
        }
        public static Vector3 toVector3(this WolvenKit.RED4.CR2W.Types.Vector3 v)
        {
            return new Vector3(v.X.Value, v.Y.Value, v.Z.Value);
        }
    }
 }
