/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Runtime.InteropServices;

namespace Kds.Serialization.Buffer
{
  /// <summary>Serialization support routines, mainly for numeric and text types.</summary>
  /// <remarks>The purpose is to serialize with big-endian byte order, such that
  /// lexical sorting will work as expected.</remarks>
  public static class BEConverter
  {
    #region Not CLS-Compliant

    [CLSCompliant(false)]
    public const UInt16 ByteLoMask = 0x00FF;
    [CLSCompliant(false)]
    public const UInt32 Byte1Mask = 0x000000FF;
    [CLSCompliant(false)]
    public const UInt32 Byte2Mask = 0x0000FF00;
    [CLSCompliant(false)]
    public const UInt32 Byte3Mask = 0x00FF0000;
    [CLSCompliant(false)]
    public const UInt64 IntLoMask = 0x00000000FFFFFFFF;

    /// <summary>Serializes <c>UInt16</c> values.</summary>
    /// <remarks>Use <c>unchecked((UInt16)&lt;argument>)</c> to pass signed values.
    /// Use <c>(UInt16)&lt;argument></c> to pass character values.</remarks>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public static void ToBytes(UInt16 num, byte[] bytes, ref int index) {
      unchecked {
        bytes[index++] = (byte)(num >> 8);
        bytes[index++] = (byte)(num & ByteLoMask);
      }
    }

    /// <summary>Deserializes <c>UInt16</c> values.</summary>
    /// <remarks>Use <c>unchecked((Int16)ToUInt16(&lt;bytes>, ref &lt;index>))</c> to return signed
    /// results. Use <c>(Char)ToUInt16(&lt;bytes>, ref &lt;index>)</c> to return character results.</remarks>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>UInt16</c> value.</returns>
    [CLSCompliant(false)]
    public static UInt16 ToUInt16(byte[] bytes, ref int index) {
      int indx = index;
      UInt16 result;
      unchecked {
        result = (UInt16)(bytes[indx++] << 8);
      }
      result |= bytes[indx++];
      index = indx;
      return result;
    }

    /// <summary>Deserializes <c>UInt16</c> values.</summary>
    /// <remarks>Use <c>unchecked((Int16)ToUInt16(&lt;bytes>))</c> to return signed
    /// results. Use <c>(Char)ToUInt16(&lt;bytes>)</c> to return character results.</remarks>
    /// <param name="bytes">Buffer pointer to read from.</param>
    /// <returns>Deserialized <c>UInt16</c> value.</returns>
    [CLSCompliant(false)]
    public static unsafe UInt16 ToUInt16(ref byte* bytes) {
      UInt16 result;
      unchecked {
        result = (UInt16)(*bytes++ << 8);
      }
      result |= (*bytes++);
      return result;
    }

    /// <summary>Serializes <c>UInt16</c> array.</summary>
    /// <param name="numArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public static void
    ToBytes(UInt16[] numArray, int start, int count, byte[] bytes, ref int index) {
      int end = start + count;
      int byteIndex = index;
      for (; start != end; start++) {
        unchecked {
          UInt16 num = numArray[start];
          bytes[byteIndex++] = (byte)(num >> 8);
          bytes[byteIndex++] = (byte)(num & ByteLoMask);
        }
      }
      index = byteIndex;
    }

    /// <summary>Deserializes <c>UInt16</c> array.</summary>
    /// <param name="numArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    [CLSCompliant(false)]
    public static void
    ToUInt16Array(UInt16[] numArray, int start, int count, byte[] bytes, ref int index) {
      int end = start + count;
      int byteIndex = index;
      for (; start != end; start++) {
        UInt16 num;
        unchecked {
          num = (UInt16)(bytes[byteIndex++] << 8);
        }
        num |= bytes[byteIndex++];
        numArray[start] = num;
      }
      index = byteIndex;
    }

    /// <summary>Serializes <c>UInt32</c> values.</summary>
    /// <remarks>Use <c>unchecked((UInt32)&lt;argument>)</c> to pass signed values.</remarks>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public static void ToBytes(UInt32 num, byte[] bytes, ref int index) {
      int byteIndex = index;
      unchecked {
        bytes[byteIndex++] = (byte)(num >> 24);
        bytes[byteIndex++] = (byte)((num & Byte3Mask) >> 16);
        bytes[byteIndex++] = (byte)((num & Byte2Mask) >> 8);
        bytes[byteIndex++] = (byte)(num & Byte1Mask);
      }
      index = byteIndex;
    }

    /// <summary>Deserializes <c>UInt32</c> values.</summary>
    /// <remarks>Use <c>unchecked((Int32)ToUInt32(&lt;bytes>, ref &lt;index>))</c>
    /// to return signed results.</remarks>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>UInt32</c> value.</returns>
    [CLSCompliant(false)]
    public static UInt32 ToUInt32(byte[] bytes, ref int index) {
      int byteIndex = index;
      UInt32 result;
      unchecked {
        result = (UInt32)bytes[byteIndex++] << 24;
        result |= (UInt32)bytes[byteIndex++] << 16;
        result |= (UInt32)bytes[byteIndex++] << 8;
      }
      result |= bytes[byteIndex++];
      index = byteIndex;
      return result;
    }

    /// <summary>Deserializes <c>UInt32</c> values.</summary>
    /// <remarks>Use <c>unchecked((Int32)ToUInt32(&lt;bytes>))</c>
    /// to return signed results.</remarks>
    /// <param name="bytes">Buffer pointer to read from.</param>
    /// <returns>Deserialized <c>UInt32</c> value.</returns>
    [CLSCompliant(false)]
    public static unsafe UInt32 ToUInt32(ref byte* bytes) {
      UInt32 result;
      unchecked {
        result = (UInt32)(*bytes++) << 24;
        result |= (UInt32)(*bytes++) << 16;
        result |= (UInt32)(*bytes++) << 8;
      }
      result |= (*bytes++);
      return result;
    }

    /// <summary>Serializes <c>UInt32</c> array.</summary>
    /// <param name="numArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public static void
    ToBytes(UInt32[] numArray, int start, int count, byte[] bytes, ref int index) {
      int end = start + count;
      int byteIndex = index;
      for (; start != end; start++) {
        unchecked {
          UInt32 num = numArray[start];
          bytes[byteIndex++] = (byte)(num >> 24);
          bytes[byteIndex++] = (byte)((num & Byte3Mask) >> 16);
          bytes[byteIndex++] = (byte)((num & Byte2Mask) >> 8);
          bytes[byteIndex++] = (byte)(num & Byte1Mask);
        }
      }
      index = byteIndex;
    }

    /// <summary>Deserializes <c>UInt32</c> array.</summary>
    /// <param name="numArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    [CLSCompliant(false)]
    public static void
    ToUInt32Array(UInt32[] numArray, int start, int count, byte[] bytes, ref int index) {
      int end = start + count;
      int byteIndex = index;
      for (; start != end; start++) {
        UInt32 num;
        unchecked {
          num = (UInt32)bytes[byteIndex++] << 24;
          num |= (UInt32)bytes[byteIndex++] << 16;
          num |= (UInt32)bytes[byteIndex++] << 8;
        }
        num |= bytes[byteIndex++];
        numArray[start] = num;
      }
      index = byteIndex;
    }

    /// <summary>Serializes <c>UInt64</c> values.</summary>
    /// <remarks>Use <c>unchecked((UInt64)&lt;argument>)</c> to pass signed values.</remarks>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public static void ToBytes(UInt64 num, byte[] bytes, ref int index) {
      UInt32 part;
      unchecked { part = (UInt32)(num >> 32); }
      ToBytes(part, bytes, ref index);
      unchecked { part = (UInt32)(num & IntLoMask); }
      ToBytes(part, bytes, ref index);
    }

    /// <summary>Deserializes <c>UInt64</c> values.</summary>
    /// <remarks>Use <c>unchecked((Int64)ToUInt64(&lt;bytes>, ref &lt;index>))</c>
    /// to return signed results.</remarks>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>UInt64</c> value.</returns>
    [CLSCompliant(false)]
    public static UInt64 ToUInt64(byte[] bytes, ref int index) {
      UInt64 hiPart = ToUInt32(bytes, ref index);
      UInt64 result = ToUInt32(bytes, ref index);
      unchecked { result |= hiPart << 32; }
      return result;
    }

    #endregion

    #region CLS-Compliant

    /// <summary>Serializes <c>Int16</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public static void ToBytes(Int16 num, byte[] bytes, ref int index) {
      unchecked { ToBytes((UInt16)num, bytes, ref index); }
    }

    /// <summary>Deserializes <c>Int16</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Int16</c> value.</returns>
    public static Int16 ToInt16(byte[] bytes, ref int index) {
      unchecked { return (Int16)ToUInt16(bytes, ref index); }
    }

    /// <summary>Serializes <c>Int16</c> array.</summary>
    /// <param name="numArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public static void
    ToBytes(Int16[] numArray, int start, int count, byte[] bytes, ref int index) {
      UShortUnion union = new UShortUnion(numArray);
      ToBytes(union.UShortValue, start, count, bytes, ref index);
   }

    /// <summary>Deserializes <c>Int16</c> array.</summary>
    /// <param name="numArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    public static void
    ToInt16Array(Int16[] numArray, int start, int count, byte[] bytes, ref int index) {
      UShortUnion union = new UShortUnion(numArray);
      ToUInt16Array(union.UShortValue, start, count, bytes, ref index);
    }

    /// <summary>Serializes <c>Int32</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public static void ToBytes(Int32 num, byte[] bytes, ref int index) {
      unchecked { ToBytes((UInt32)num, bytes, ref index); }
    }

    /// <summary>Deserializes <c>Int32</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Int32</c> value.</returns>
    public static Int32 ToInt32(byte[] bytes, ref int index) {
      unchecked { return (Int32)ToUInt32(bytes, ref index); }
    }

    /// <summary>Serializes <c>Int32</c> array.</summary>
    /// <param name="numArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public static void
    ToBytes(Int32[] numArray, int start, int count, byte[] bytes, ref int index) {
      UIntUnion union = new UIntUnion(numArray);
      ToBytes(union.UIntValue, start, count, bytes, ref index);
    }

    /// <summary>Deserializes <c>Int32</c> array.</summary>
    /// <param name="numArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    public static void
    ToInt32Array(Int32[] numArray, int start, int count, byte[] bytes, ref int index) {
      UIntUnion union = new UIntUnion(numArray);
      ToUInt32Array(union.UIntValue, start, count, bytes, ref index);
    }

    /// <summary>Serializes <c>Int64</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public static void ToBytes(Int64 num, byte[] bytes, ref int index) {
      unchecked { ToBytes((UInt64)num, bytes, ref index); }
    }

    /// <summary>Deserializes <c>Int64</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Int64</c> value.</returns>
    public static Int64 ToInt64(byte[] bytes, ref int index) {
      unchecked { return (Int64)ToUInt64(bytes, ref index); }
    }

    /// <summary>Serializes <c>Single</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public static void ToBytes(Single num, byte[] bytes, ref int index) {
      UInt32SingleUnion union = new UInt32SingleUnion(num);
      ToBytes(union.UIntValue, bytes, ref index);
    }

    /// <summary>Deserializes <c>Single</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Single</c> value.</returns>
    public static Single ToSingle(byte[] bytes, ref int index) {
      UInt32SingleUnion union = new UInt32SingleUnion(ToUInt32(bytes, ref index));
      return union.SingleValue;
    }

    /// <summary>Serializes <c>Double</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public static void ToBytes(Double num, byte[] bytes, ref int index) {
      Int64 doubleBits = BitConverter.DoubleToInt64Bits(num);
      ToBytes(unchecked((UInt64)doubleBits), bytes, ref index);
    }

    /// <summary>Deserializes <c>Double</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Double</c> value.</returns>
    public static Double ToDouble(byte[] bytes, ref int index) {
      UInt64 doubleBits = ToUInt64(bytes, ref index);
      return BitConverter.Int64BitsToDouble(unchecked((Int64)doubleBits));
    }

    /// <summary>Serializes <c>Decimal</c> values.</summary>
    /// <remarks>Processes a <c>Decimal</c> value in reverse part order, where
    /// each part is a <c>Int32</c> value. This is a .NET specific type.</remarks>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public static void ToBytes(Decimal num, byte[] bytes, ref int index) {
      Int32[] parts = Decimal.GetBits(num);
      for (int indx = parts.Length - 1; indx >= 0; indx--)
        ToBytes(parts[indx], bytes, ref index);
    }

    /// <summary>Deserializes <c>Decimal</c> values.</summary>
    /// <remarks>Processes the buffer as four <c>Int32</c> values which form the
    /// parts of a <c>Decimal</c> value in reverse order. This is .NET specific.</remarks>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Decimal</c> value.</returns>
    public static Decimal ToDecimal(byte[] bytes, ref int index) {
      Int32[] parts = new Int32[4];
      for (int indx = parts.Length - 1; indx >= 0; indx--)
        parts[indx] = ToInt32(bytes, ref index);
      return new Decimal(parts);
    }

    #endregion
  }

  /// <summary>Union used solely for re-interpreting <c>UInt16[]</c> as <c>Int16[]</c> and vice versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct UShortUnion
  {
    /// <summary><c>UInt16[]</c> version of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt16[] UShortValue;

    /// <summary><c>Int16[]</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Int16[] ShortValue;

    /// <summary>Constructor taking a <c>UInt16[]</c> value.</summary>
    [CLSCompliant(false)]
    public UShortUnion(UInt16[] value) {
      ShortValue = null;  // keeps the compiler happy
      UShortValue = value;
    }

    /// <summary>Constructor taking an <c>Int16[]</c> value.</summary>
    public UShortUnion(Int16[] value) {
      UShortValue = null;  // keeps the compiler happy
      ShortValue = value;
    }
  }

  /// <summary>Union used solely for re-interpreting <c>UInt32[]</c> as <c>Int32[]</c> and vice versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct UIntUnion
  {
    /// <summary><c>UInt32[]</c> version of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt32[] UIntValue;

    /// <summary><c>Int32[]</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Int32[] IntValue;

    /// <summary>Constructor taking a <c>UInt32[]</c> value.</summary>
    [CLSCompliant(false)]
    public UIntUnion(UInt32[] value) {
      IntValue = null;  // keeps the compiler happy
      UIntValue = value;
    }

    /// <summary>Constructor taking an <c>Int32[]</c> value.</summary>
    public UIntUnion(Int32[] value) {
      UIntValue = null;  // keeps the compiler happy
      IntValue = value;
    }
  }

  /// <summary>Union used solely for re-interpreting <c>Single</c> as <c>Int32</c> and vice versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct UInt32SingleUnion
  {
    /// <summary><c>Int32</c> version of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt32 UIntValue;

    /// <summary><c>Single</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Single SingleValue;

    /// <summary>Constructor taking a <c>UInt32</c> value.</summary>
    [CLSCompliant(false)]
    public UInt32SingleUnion(uint value) {
      SingleValue = 0;  // just to keep the compiler happy
      UIntValue = value;
    }

    /// <summary>Constructor taking a <c>Single</c> value.</summary>
    public UInt32SingleUnion(float value) {
      UIntValue = 0;  // just to keep the compiler happy
      SingleValue = value;
    }
  }
}
