/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using System.Security;
using System.Runtime.InteropServices;

namespace BerkeleyDb
{
  /* Key/data structure -- a Data-Base Thang. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DBT
  {
    /*
     * data/size must be fields 1 and 2 for DB 1.85 compatibility.
     */
    public void* data;       /* Key/data */
    public UInt32 size;      /* key/data length */

    public UInt32 ulen;      /* RO: length of user buffer. */
    public UInt32 dlen;      /* RO: get/put record length. */
    public UInt32 doff;      /* RO: get/put record offset. */

#if BDB_4_5_20
    public IntPtr app_data;
#endif

    public const UInt32 DB_DBT_APPMALLOC = 0x001;   /* Callback allocated memory. */
    public const UInt32 DB_DBT_ISSET = 0x002;       /* Lower level calls set value. */
    public const UInt32 DB_DBT_MALLOC = 0x004;      /* Return in malloc'd memory. */
    public const UInt32 DB_DBT_PARTIAL = 0x008;     /* Partial put/get. */
    public const UInt32 DB_DBT_REALLOC = 0x010;     /* Return in realloc'd memory. */
#if BDB_4_3_29
    public const UInt32 DB_DBT_USERMEM = 0x020;     /* Return in user's memory. */
    public const UInt32 DB_DBT_DUPOK = 0x040;       /* Insert if duplicate. */
#endif
#if BDB_4_5_20
    public const UInt32 DB_DBT_USERCOPY = 0x020;    /* Use the user-supplied callback. */
    public const UInt32 DB_DBT_USERMEM = 0x040;     /* Return in user's memory. */
    public const UInt32 DB_DBT_DUPOK = 0x080;       /* Insert if duplicate. */
#endif
    public UInt32 flags;

    // for CLSCompliant initialization
    public DBT(int size, int ulen) {
      data = null;
      this.size = unchecked((UInt32)size);
      this.ulen = unchecked((UInt32)ulen);
      dlen = 0;
      doff = 0;
#if BDB_4_5_20
      app_data = IntPtr.Zero;
#endif
      flags = DB_DBT_USERMEM;  // we usually manage memory ourselves
    }

    // for CLSCompliant initialization
    public DBT(int size, int ulen, int dlen, int doff) {
      data = null;
      this.size = unchecked((UInt32)size);
      this.ulen = unchecked((UInt32)ulen);
      this.dlen = unchecked((UInt32)dlen);
      this.doff = unchecked((UInt32)doff);
#if BDB_4_5_20
      app_data = IntPtr.Zero;
#endif
      flags = DB_DBT_USERMEM | DB_DBT_PARTIAL;
    }
  }

  /// <summary>Wrapper for the <see cref="DBT">Key/Data Structure.</see></summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct DbEntry:
    IEnumerable<DbEntry.DataItem>,
    IEnumerable<DbEntry.KeyDataItem>,
    IEnumerable<DbEntry.RecNoDataItem>
  {
    internal DBT dbt;
    internal ReturnType retType;
    byte[] buffer;
    int start;

    #region Static Construction

    /// <summary>Returns a <c>DbEntry</c> instance configured for output of a partial record.</summary>
    /// <param name="buffer">Byte buffer to fill. Must be unsigned integer aligned
    /// (rightmost 2 bits of address must be 0). For bulk retrieval, buffer size must
    /// be multiple of 1024 and larger than page size.</param>
    /// <param name="chunkLength">Length of partial record.</param>
    /// <param name="chunkOffset">Offset where partial record starts.</param>
    /// <returns><c>DbEntry</c> instance configured for receiving a partial record.</returns>
    public static DbEntry Out(byte[] buffer, int chunkLength, int chunkOffset) {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      DbEntry result;
      result.dbt = new DBT(0, buffer.Length, chunkLength, chunkOffset);
      result.buffer = buffer;
      result.start = 0;
      result.retType = ReturnType.Single;
      return result;
    }

    /// <summary>Returns a <c>DbEntry</c> instance configured for output.</summary>
    /// <param name="buffer">Byte buffer to fill. Must be unsigned integer aligned
    /// (rightmost 2 bits of address must be 0). For bulk retrieval, buffer size must
    /// be multiple of 1024 and larger than page size.</param>
    /// <returns><c>DbEntry</c> instance configured for receiving a data record.</returns>
    public static DbEntry Out(byte[] buffer) {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      DbEntry result;
      result.dbt = new DBT(0, buffer.Length);
      result.buffer = buffer;
      result.start = 0;
      result.retType = ReturnType.Single;
      return result;
    }

    /// <summary>Returns a <c>DbEntry</c> instance configured for input and output of a partial record.</summary>
    /// <param name="buffer">Byte buffer to fill. Must be unsigned integer aligned
    /// (rightmost 2 bits of address must be 0). For bulk retrieval, buffer size must
    /// be multiple of 1024 and larger than page size.</param>
    /// <param name="start">Start index of input data in buffer.</param>
    /// <param name="size">Size of input data in buffer.</param>
    /// <param name="chunkLength">Length of partial record.</param>
    /// <param name="chunkOffset">Offset where partial record starts.</param>
    /// <returns><c>DbEntry</c> instance configured with input data, for receiving a partial record.</returns>
    public static DbEntry InOut(
      byte[] buffer,
      int start,
      int size,
      int chunkLength,
      int chunkOffset) 
    {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      if (size < 0 || start < 0 || start + size > buffer.Length)
        throw new ArgumentException("Out of bounds.", "start, size");
      DbEntry result;
      result.dbt = new DBT(size, buffer.Length, chunkLength, chunkOffset);
      result.buffer = buffer;
      result.start = start;
      result.retType = ReturnType.Single;
      return result;
    }

    /// <summary>Returns a <c>DbEntry</c> instance configured for input and output.</summary>
    /// <param name="buffer">Byte buffer to fill. Must be unsigned integer aligned
    /// (rightmost 2 bits of address must be 0). For bulk retrieval, buffer size must
    /// be multiple of 1024 and larger than page size.</param>
    /// <param name="start">Start index of input data in buffer.</param>
    /// <param name="size">Size of input data in buffer.</param>
    /// <returns><c>DbEntry</c> instance configured with input data, for receiving a data record.</returns>
    public static DbEntry InOut(byte[] buffer, int start, int size) {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      if (size < 0 || start < 0 || start + size > buffer.Length)
        throw new ArgumentException("Out of bounds.", "start, size");
      DbEntry result;
      result.dbt = new DBT(size, buffer.Length);
      result.buffer = buffer;
      result.start = start;
      result.retType = ReturnType.Single;
      return result;
    }

    /// <summary>Returns a <c>DbEntry</c> instance configured for input and output.</summary>
    /// <param name="buffer">Byte buffer to fill. Must be unsigned integer aligned
    /// (rightmost 2 bits of address must be 0). For bulk retrieval, buffer size must
    /// be multiple of 1024 and larger than page size.</param>
    /// <returns><c>DbEntry</c> instance configured with input data, for receiving a data record.</returns>
    public static DbEntry InOut(byte[] buffer) {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      DbEntry result;
      result.dbt = new DBT(buffer.Length, buffer.Length);
      result.buffer = buffer;
      result.start = 0;
      result.retType = ReturnType.Single;
      return result;
    }

    /// <summary>Returns a <c>DbEntry</c> instance configured for the case when
    /// no data are to be retrieved.</summary>
    /// <remarks>Used for checking if a record exists, without actually retrieving it.</remarks>
    /// <returns>An empty <c>DbEntry</c> instance.</returns>
    public static DbEntry EmptyOut() {
      DbEntry result = new DbEntry();        // initializes all fields to 0
      result.dbt.flags = DBT.DB_DBT_PARTIAL;  // required to return no data
      // result.retType = ReturnType.Single;
      return result;
    }

    #endregion

    #region Helpers

    /// <summary>Sets return type based on access method and BDB bulk retrieval flags.</summary>
    /// <param name="dbType">Access method, e.g. DbType.BTree.</param>
    /// <param name="flags">Bulk retrieval flags, e.g. DB_MULTIPLE, DB_MULTIPLE_KEY.</param>
    [CLSCompliant(false)]
    public void SetReturnType(DbType dbType, uint flags) {
      retType = ReturnType.Single;
      if ((DbConst.DB_MULTIPLE & flags) != 0)
        retType = ReturnType.Multiple;
      else if ((DbConst.DB_MULTIPLE_KEY & flags) != 0) {
        switch (dbType) {
          case DbType.Recno:
            goto case DbType.Queue;
          case DbType.Queue:
            retType = ReturnType.MultipleRecNo;
            break;
          case DbType.BTree:
            goto case DbType.Hash;
          case DbType.Hash:
            retType = ReturnType.MultipleKey;
            break;
        }
      }
    }

    #endregion

    #region Public Operations & Properties

    /// <summary>Resizes buffer, preserving existing data.</summary>
    /// <param name="newLength">New buffer length, must not cause truncation of existing data.</param>
    public void ResizeBuffer(int newLength) {
      if (newLength < (start + Size))
        throw new ArgumentException("Must not truncate data in buffer.", "newLength");
      Array.Resize<byte>(ref buffer, newLength);
      dbt.ulen = unchecked((UInt32)newLength);
    }

    /// <summary>Byte buffer holding input and output data.</summary>
    public byte[] Buffer {
      get { return buffer; }
    }

    /// <summary>Start index of data in buffer.</summary>
    public int Start {
      get { return start; }
    }

    /// <summary>Size of input or output data.</summary>
    public int Size {
      get { return unchecked((int)dbt.size); }
    }

    /// <summary>Length of partial database record to update or retrieve.</summary>
    public int ChunkLength {
      get { return unchecked((int)dbt.dlen); }
    }

    /// <summary>Offset of partial database record to update or retrieve.</summary>
    public int ChunkOffset {
      get { return unchecked((int)dbt.doff); }
    }

    /// <summary>Indicates how records were retrieved (single or bulk).</summary>
    public ReturnType RetType {
      get { return retType; }
    }

    /// <summary>Bulk retrieval interface to iterate over multiple data records.</summary>
    public IEnumerable<DataItem> DataItems {
      get { return this; }
    }

    /// <summary>Bulk retrieval interface to iterate over multiple key/data pairs.</summary>
    public IEnumerable<KeyDataItem> KeyDataItems {
      get { return this; }
    }

    /// <summary>Bulk retrieval interface to iterate over multiple record number/data pairs.</summary>
    public IEnumerable<RecNoDataItem> RecNoDataItems {
      get { return this; }
    }

    #endregion

    #region IEnumerable, IEnumerable<T> Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      switch (retType) {
        case ReturnType.Single: return new SingleEnumerator(Size);
        case ReturnType.Multiple: return new DataEnumerator(buffer, buffer.Length);
        case ReturnType.MultipleKey: return new KeyDataEnumerator(buffer, buffer.Length);
        case ReturnType.MultipleRecNo: return new RecNoDataEnumerator(buffer, buffer.Length);
        default: return null;
      }
    }

    IEnumerator<DataItem> IEnumerable<DataItem>.GetEnumerator() {
      if (retType == ReturnType.Single)
          return new SingleEnumerator(Size);
      else if (retType == ReturnType.Multiple)
        return new DataEnumerator(buffer, buffer.Length);
      else
        throw new BdbException("No DataItems available.");
    }

    IEnumerator<KeyDataItem> IEnumerable<KeyDataItem>.GetEnumerator() {
      if (retType == ReturnType.MultipleKey)
        return new KeyDataEnumerator(buffer, buffer.Length);
      else
        throw new BdbException("No KeyDataItems available.");
    }

    IEnumerator<RecNoDataItem> IEnumerable<RecNoDataItem>.GetEnumerator() {
      if (retType == ReturnType.MultipleRecNo)
         return new RecNoDataEnumerator(buffer, buffer.Length);
      else
         throw new BdbException("No RecNoDataItems available.");
     }

    #endregion

    #region Nested Types

    /// <summary>Indicates if data were retrieved as single records or in bulk.</summary>
    public enum ReturnType
    {
      Single = 0,
      Multiple,
      MultipleKey,
      MultipleRecNo
    }

    /// <summary>Represents data record for bulk retrieval.</summary>
    public struct DataItem
    {
      internal int start;
      internal int size;
      internal bool deleted;

      public int Start {
        get { return start; }
      }

      public int Size {
        get { return size; }
      }

      public bool Deleted {
        get { return deleted; }
      }
    }

    /// <summary>Represents key/data pair for bulk retrieval.</summary>
    public struct KeyDataItem
    {
      internal int keyStart;
      internal int keySize;
      internal int dataStart;
      internal int dataSize;

       public int KeyStart {
        get { return keyStart; }
      }

      public int KeySize {
        get { return keySize; }
      }
      
      public int DataStart {
        get { return dataStart; }
      }

      public int DataSize {
        get { return dataSize; }
      }
    }

    /// <summary>Represents record number/data pair for bulk retrieval.</summary>
    public struct RecNoDataItem
    {
      internal int recNo;
      internal int start;
      internal int size;

      public int RecNo {
        get { return recNo; }
      }

      public int Start {
        get { return start; }
      }

      public int Size {
        get { return size; }
      }
    }

    private abstract class BaseEnumerator: IDisposable
    {
      #region IDisposable Members

      public void Dispose() { }

      #endregion
    }

    private class SingleEnumerator: BaseEnumerator, IEnumerator<DataItem>
    {
      DataItem current = new DataItem();
      bool valid = false;
      bool done = false;

      public SingleEnumerator(int size) {
        current.size = size;
      }

      #region IEnumerator<DataItem> Members

      public DataItem Current {
        get {
          if (valid)
            return current;
          else
            throw new InvalidOperationException("Enumerator position invalid.");
        }
      }

      public bool MoveNext() {
        if (done) {
          valid = false;
          return false;
        }
        else {
          done = true;
          valid = true;
          return true;
        }
      }

      #endregion

      #region IEnumerator Members

      object System.Collections.IEnumerator.Current {
        get { return Current; }
      }

      public void Reset() {
        valid = false;
        done = false;
      }

      #endregion
    }

    private abstract class MultipleEnumerator: BaseEnumerator
    {
      protected byte[] buffer;
      protected int iterIndex;
      protected bool valid = false;
      int dataLen;

      // buffer must be multiple of 1024, larger than page size and unsigned integer aligned
      public MultipleEnumerator(byte[] buffer, int dataLen) {
        this.buffer = buffer;
        this.dataLen = dataLen;
        iterIndex = dataLen - sizeof(Int32);
      }

      #region IEnumerator Members

      public void Reset() {
        valid = false;
        iterIndex = dataLen - sizeof(Int32);
      }

      #endregion
    }

    private class DataEnumerator: MultipleEnumerator, IEnumerator<DataItem>
    {
      DataItem current = new DataItem();

      public DataEnumerator(byte[] buffer, int len) : base(buffer, len) { }

      #region IEnumerator<DataItem> Members

      public DataItem Current {
        get {
          if (valid)
            return current;
          else
            throw new InvalidOperationException("Enumerator position invalid.");
        }
      }

      public bool MoveNext() {
        Int32 start = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        if (start == -1) {
          valid = false;
          return false;
        }
        Int32 size = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        current.deleted = size == 0 && start == 0;
        current.start = start;
        current.size = size;
        valid = true;
        return true;
      }

      #endregion

      #region IEnumerator Members

      object System.Collections.IEnumerator.Current {
        get { return Current; }
      }

      #endregion
}

    private class KeyDataEnumerator: MultipleEnumerator, IEnumerator<KeyDataItem>
    {
      KeyDataItem current = new KeyDataItem();

      public KeyDataEnumerator(byte[] buffer, int len) : base(buffer, len) { }

      #region IEnumerator<KeyDataItem> Members

      public KeyDataItem Current {
        get {
          if (valid)
            return current;
          else
            throw new InvalidOperationException("Enumerator position invalid.");
        }
      }

      public bool MoveNext() {
        Int32 start = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        if (start == -1) {
          valid = false;
          return false;
        }
        current.keyStart = start;
        current.keySize = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        current.dataStart = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        current.dataSize = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        valid = true;
        return true;
      }

      #endregion

      #region IEnumerator Members

      object System.Collections.IEnumerator.Current {
        get { return Current; }
      }

      #endregion
    }

    private class RecNoDataEnumerator: MultipleEnumerator, IEnumerator<RecNoDataItem>
    {
      RecNoDataItem current = new RecNoDataItem();

      public RecNoDataEnumerator(byte[] buffer, int len) : base(buffer, len) { }

      #region IEnumerator<RecNoDataItem> Members

      public RecNoDataItem Current {
        get {
          if (valid)
            return current;
          else
            throw new InvalidOperationException("Enumerator position invalid.");
        }
      }

      public unsafe bool MoveNext() {
        Int32 recno = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        if (recno == 0) {
          valid = false;
          return false;
        }
        current.recNo = recno;
        Int32 start = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        Int32 size = BitConverter.ToInt32(buffer, iterIndex);
        iterIndex -= sizeof(Int32);
        current.start = start;
        current.size = size;
        valid = true;
        return true;
      }

      #endregion

      #region IEnumerator Members

      object System.Collections.IEnumerator.Current {
        get { return Current; }
      }

      #endregion
    }

    #endregion
  }
}
