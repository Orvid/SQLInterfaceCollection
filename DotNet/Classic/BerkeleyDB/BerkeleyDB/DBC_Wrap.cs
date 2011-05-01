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
using System.Runtime.CompilerServices;
using System.Threading;

namespace BerkeleyDb
{
  /// <summary>Represents a Berkeley DB database cursor.</summary>
  /// <remarks>Wraps a <see cref="DBC"/> handle. This class is not thread-safe, not even
  /// within Berkeley Db. If multiple threads use this class, access must be synchronized
  /// by the application. Cursors may not span transactions; that is, each cursor must be
  /// opened and closed within a single transaction.</remarks>
  public abstract unsafe class DbCursor: IDisposable
  {
    protected static DbType dbType = DbType.Unknown;
    protected Db db;

    // store delegates for frequently used function pointer calls
    [CLSCompliant(false)]
    protected DBC.GetFcn DbcGet;
    [CLSCompliant(false)]
    protected DBC.PGetFcn DbcPGet;
    [CLSCompliant(false)]
    protected DBC.PutFcn DbcPut;
    [CLSCompliant(false)]
    protected DBC.DelFcn DbcDel;
    [CLSCompliant(false)]
    protected DBC.CountFcn DbcCount;

    #region Hash Code

    static Random rand = new Random();

    static int GetNextHashCode() {
      lock (rand) {
        return rand.Next();
      }
    }

    int hashCode = GetNextHashCode();

    public override int GetHashCode() {
      return hashCode;
    }

    #endregion

    #region Unmanaged Resources

    protected internal readonly object rscLock = new object();
    DbRetVal releaseVal;

    // access to properly aligned types of size "native int" is atomic!
    internal volatile DBC* dbcp = null;

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    DbRetVal ReleaseUnmanagedResources() {
      DBC* dbcp = this.dbcp;
      if (dbcp == null)
        return DbRetVal.SUCCESS;
      // DBC->Close() could be a lengthy call, so we call Disposed() first, and the
      // CER ensures that we reach DBC->Close() without external interruption.
      // This is OK because one must not use the handle after DBC->Close() was called,
      // regardless of the return value.
      Disposed();
      DbRetVal ret = dbcp->Close(dbcp);
      return ret;
    }

    #endregion

    #region Construction, Finalization

    public const string disposedStr = "Cursor handle closed.";

    [CLSCompliant(false)]
    protected DBC* CheckDisposed() {
      // avoid multiple volatile memory access
      DBC* dbcp = this.dbcp;
      if (dbcp == null)
        throw new ObjectDisposedException(disposedStr);
      return dbcp;
    }

    // when overriding, call base method at end (using finally clause)
    protected internal virtual void Dispose(bool disposing) {
      lock (rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          releaseVal = ReleaseUnmanagedResources();
        }
      }
    }

    // does not check for dbcp == null!
    void Disposed() {
      dbcp = null;
      if (db != null)
        db.RemoveCursor(this);
    }

    public bool IsDisposed {
      get { return dbcp == null; }
    }

    public DbRetVal ReleaseVal {
      get { return releaseVal; }
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
      lock (db.rscLock) {
        Dispose(true);
      }
      GC.SuppressFinalize(this);
    }

    #endregion

    #region Public Operations & Properties

    public Db GetDb() {
      return db;
    }

    public void Close() {
      Dispose();
    }

    public bool IsTransactional {
      get {
        lock (rscLock) {
          DBC* dbcp = CheckDisposed();
          return dbcp->txn != null;
        }
      }
    }

    #endregion
  }

  /// <summary>Cursor for joins.</summary>
  /// <remarks>Can only be iterated over once. Therefore it is recommended to use it
  /// with a foreach loop, as this calls <see cref="IDisposable.Dispose"/> at the end.</remarks>
  public unsafe class DbJoinCursor: DbCursor, IEnumerable<KeyDataPair>, IEnumerator<KeyDataPair>
  {
    DbFileCursor[] cursors = null;

    #region Construction, Finalization

    // parameters must all be != null - not checked!; call should be synchronized on rscLock
    internal void Initialize(Db db, DBC* dbcp, DbFileCursor[] cursors) {
      this.db = db;
      this.dbcp = dbcp;
      this.cursors = cursors;
      DbcGet = dbcp->Get;
    }

    // when overriding, call base method at end (using finally clause)
    protected internal override void Dispose(bool disposing) {
      lock (rscLock) {
        if (disposing && cursors != null) {
          // remove "join" flag from joined cursors
          for (int indx = 0; indx < cursors.Length; indx++) {
            DbFileCursor cursor = cursors[indx];
            lock (cursor.rscLock) {
              cursor.owner = null;
            }
          }
        }
        base.Dispose(disposing);
      }
    }

    #endregion

    #region Public Operations & Properties

    // This looks the same as the helper method in DbcFile, but if we moved
    // it up to the base class, we would have to make Dbc.CheckDisposed() virtual
    // and override it in DbcFile, affecting almost every call.
    public ReadStatus Get(ref DbEntry key, ref DbEntry data, GetMode gmode, ReadFlags rflags)
    {
      uint flags = unchecked((uint)gmode | (uint)rflags);
      DbRetVal ret;
      lock (rscLock) {
        DBC* dbcp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer, dataBufP = data.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          data.dbt.data = dataBufP + data.Start;
          ret = DbcGet(dbcp, ref key.dbt, ref data.dbt, flags);
        }
      }
      switch (ret) {
        case DbRetVal.NOTFOUND: break;
        case DbRetVal.KEYEMPTY: break;
        case DbRetVal.BUFFER_SMALL: break;
        default:
          Util.CheckRetVal(ret);
          break;
      }
      data.SetReturnType(dbType, flags);
      return (ReadStatus)ret;
    }

    /// <summary>Returns the <see cref="IEnumerable&lt;KeyDataPair>"/> interface, with the
    /// added option of configuring the enumerator flags.</summary>
    /// <param name="gmode"><see cref="GetMode"/> value to be passed to <see cref="Get"/>.</param>
    /// <param name="rflags"><see cref="ReadFlags"/> value to be passed to <see cref="Get"/>.</param>
    /// <returns><see cref="IEnumerable&lt;KeyDataPair>"/> interface.</returns>
    public IEnumerable<KeyDataPair> Items(GetMode gmode, ReadFlags rflags) {
      this.gmode = gmode;
      this.rflags = rflags;
      return this;
    }

    #endregion

    #region IEnumerable<KeyDataPair> Members

    public IEnumerator<KeyDataPair> GetEnumerator() {
      if (current.Key.Buffer == null) {
        current.Key = DbEntry.Out(new byte[16]);
        current.Data = DbEntry.Out(new byte[32]);
      }
      return this;
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    #endregion

    #region IEnumerator<KeyDataPair> Members

    readonly KeyDataPair current = new KeyDataPair();
    GetMode gmode = GetMode.None;
    ReadFlags rflags = ReadFlags.None;
    bool valid = false;
    bool isReset = true;

    // Current is only valid until the next call to MoveNext
    public KeyDataPair Current {
      get {
        if (valid)
          return current;
        else
          throw new InvalidOperationException("Enumerator position invalid.");
      }
    }

    #endregion

    #region IEnumerator Members

    object System.Collections.IEnumerator.Current {
      get { return Current; }
    }

    public bool MoveNext() {
      if (valid || isReset) {
        isReset = false;
        ReadStatus status = Get(ref current.Key, ref current.Data, gmode, rflags);
        // BDB checks only one argument's buffer size per call
        while (status == ReadStatus.BufferSmall) {
          // no key passed into join cursor - simply replace buffer if too small
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key = DbEntry.Out(new byte[current.Key.Size]);
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data = DbEntry.Out(new byte[current.Data.Size]);
          status = Get(ref current.Key, ref current.Data, gmode, rflags);
        }
        // it looks as if ReadStatus.KeyEmpty should not happen with join cursors
        valid = status == ReadStatus.Success;
      }
      return valid;
    }

    public void Reset() {
      isReset = true;
      valid = false;
    }

    #endregion

    #region Nested Types

    public enum GetMode: int
    {
      None = 0,
      JoinItem = DbConst.DB_JOIN_ITEM
    }

    [Flags]
    public enum ReadFlags: int
    {
      None = 0,
#if BDB_4_3_29
      DirtyRead = DbConst.DB_DIRTY_READ,
#endif
#if BDB_4_5_20
      ReadUncommitted = DbConst.DB_READ_UNCOMMITTED,
#endif
      RMW = DbConst.DB_RMW
    }

    #endregion
  }

  /// <summary>Abstract base class for Berkeley DB database cursor when opened for file access.</summary>
  public abstract unsafe class DbFileCursor: DbCursor, IEnumerable<KeyDataPair>
  {
    internal DbJoinCursor owner = null;

    #region Construction, Finalization

    // parameters must all be != null - not checked!; call should be synchronized on rscLock
    internal void Initialize(Db db, DBC* dbcp) {
      this.db = db;
      this.dbcp = dbcp;
      // initialize function pointer delegates
      DbcGet = dbcp->Get;
      DbcPGet = dbcp->PGet;
      DbcPut = dbcp->Put;
      DbcDel = dbcp->Del;
      DbcCount = dbcp->Count;
    }

    public const string joinedStr = "Cursor used in join.";

    [CLSCompliant(false)]
    protected new DBC* CheckDisposed() {
      // avoid multiple volatile memory access
      DBC* dbcp = this.dbcp;
      if (dbcp == null)
        throw new ObjectDisposedException(disposedStr);
      if (owner != null)
        throw new BdbException(joinedStr);
      return dbcp;
    }

    protected internal override void Dispose(bool disposing) {
      lock (rscLock) {
        // It seems to be more reasonable to throw an exception here than
        // to close the owner implicitly, as it should be considered an error.
        if (disposing && owner != null)
          throw new BdbException(joinedStr);
        base.Dispose(disposing);
      }
    }

    #endregion

    #region Helpers

    internal DBC* JoinLock(int timeout) {
      if (!Monitor.TryEnter(rscLock, timeout))
        throw new BdbException("Cursor busy");
      DBC* dbcp = this.dbcp;  // access volatile field only once
      if (dbcp == null) {
        Monitor.Exit(rscLock);
        throw new ObjectDisposedException(disposedStr);
      }
      if (owner != null) {
        Monitor.Exit(rscLock);
        throw new BdbException(joinedStr);
      }
      return dbcp;
    }

    internal void JoinUnlock() {
      Monitor.Exit(rscLock);
    }

    [CLSCompliant(false)]
    protected ReadStatus Get(ref DbEntry key, ref DbEntry data, uint flags) {
      DbRetVal ret;
      lock (rscLock) {
        DBC* dbcp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer, dataBufP = data.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          data.dbt.data = dataBufP + data.Start;
          ret = DbcGet(dbcp, ref key.dbt, ref data.dbt, flags);
        }
      }
      switch (ret) {
        case DbRetVal.NOTFOUND: break;
        case DbRetVal.KEYEMPTY: break;
        case DbRetVal.BUFFER_SMALL: break;
        default:
          Util.CheckRetVal(ret);
          break;
      }
      data.SetReturnType(dbType, flags);
      return (ReadStatus)ret;
    }

    [CLSCompliant(false)]
    protected ReadStatus PGet(ref DbEntry key, ref DbEntry pkey, ref DbEntry data, uint flags) {
      DbRetVal ret;
      lock (rscLock) {
        DBC* dbcp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer, pkeyBufP = pkey.Buffer, dataBufP = data.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          pkey.dbt.data = pkeyBufP + pkey.Start;
          data.dbt.data = dataBufP + data.Start;
          ret = DbcPGet(dbcp, ref key.dbt, ref pkey.dbt, ref data.dbt, flags);
        }
      }
      switch (ret) {
        case DbRetVal.NOTFOUND: break;
        case DbRetVal.KEYEMPTY: break;
        case DbRetVal.BUFFER_SMALL: break;
        default:
          Util.CheckRetVal(ret);
          break;
      }
      data.SetReturnType(dbType, flags);
      return (ReadStatus)ret;
    }

    [CLSCompliant(false)]
    protected WriteStatus Put(ref DbEntry key, ref DbEntry data, uint flags)
    {
      DbRetVal ret;
      lock (rscLock) {
        DBC* dbcp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer, dataBufP = data.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          data.dbt.data = dataBufP + data.Start;
          ret = DbcPut(dbcp, ref key.dbt, ref data.dbt, flags);
        }
      }
      switch (ret) {
        case DbRetVal.NOTFOUND: break;
        case DbRetVal.KEYEXIST: break;
        default:
          Util.CheckRetVal(ret);
          break;
      }
      return (WriteStatus)ret;
    }

    #endregion

    #region Public Operations & Properties

    public int Count() {
      UInt32 result = 0;
      DbRetVal ret;
      lock (rscLock) {
        DBC* dbcp = CheckDisposed();
        ret = DbcCount(dbcp, out result, 0);
      }
      Util.CheckRetVal(ret);
      return unchecked((int)result);
    }

    // flags currently unused, must be 0
    public DeleteStatus Delete() {
      DbRetVal ret;
      lock (rscLock) {
        DBC* dbcp = CheckDisposed();
        ret = DbcDel(dbcp, 0);
      }
      switch (ret) {
        case DbRetVal.NOTFOUND: break;
        case DbRetVal.KEYEMPTY: break;
        default:
          Util.CheckRetVal(ret);
          break;
      }
      return (DeleteStatus)ret;
    }

    public ReadStatus Get(
      ref DbEntry key,
      ref DbEntry data,
      GetMode gmode,
      ReadFlags rflags)
    {
      return Get(ref key, ref data, unchecked((uint)gmode | (uint)rflags));
    }

    public ReadStatus PGet(
      ref DbEntry key,
      ref DbEntry pkey,
      ref DbEntry data,
      GetMode gmode,
      ReadFlags rflags)
    {
      return PGet(ref key, ref pkey, ref data, unchecked((uint)gmode | (uint)rflags));
    }

    public ReadStatus GetAt(
      ref DbEntry key,
      ref DbEntry data,
      GetAtMode gmode,
      ReadFlags rflags)
    {
      return Get(ref key, ref data, unchecked((uint)gmode | (uint)rflags));
    }

    public ReadStatus PGetAt(
      ref DbEntry key,
      ref DbEntry pkey,
      ref DbEntry data,
      GetAtMode gmode,
      ReadFlags rflags)
    {
      return PGet(ref key, ref pkey, ref data, unchecked((uint)gmode | (uint)rflags));
    }

    public IEnumerable<KeyDataPair> ItemsForward(bool distinct, ReadFlags rflags, Usage usage) {
      if (GetDb().IsSecondary)
        return new SecondaryEnumerator(this, true, distinct, rflags, usage);
      else
        return new Enumerator(this, true, distinct, rflags, usage);
    }

    public IEnumerable<KeyDataPair> ItemsForward(bool distinct, ReadFlags rflags) {
      return ItemsForward(distinct, rflags, Usage.Close);
    }

    public IEnumerable<KeyDataPair> ItemsBackward(bool distinct, ReadFlags rflags, Usage usage) {
      if (GetDb().IsSecondary)
        return new SecondaryEnumerator(this, false, distinct, rflags, usage);
      else
        return new Enumerator(this, false, distinct, rflags, usage);
    }

    public IEnumerable<KeyDataPair> ItemsBackward(bool distinct, ReadFlags rflags) {
      return ItemsBackward(distinct, rflags, Usage.Close);
    }

    public IEnumerable<KeyDataPair> ItemsAt(ref DbEntry key, ReadFlags rflags, Usage usage) {
      if (GetDb().IsSecondary)
        return new SecondaryKeyDupEnumerator(this, ref key, rflags, usage);
      else
        return new KeyDupEnumerator(this, ref key, rflags, usage);
    }

    public IEnumerable<KeyDataPair> ItemsAt(ref DbEntry key, ReadFlags rflags) {
      return ItemsAt(ref key, rflags, Usage.Close);
    }

    public IEnumerable<KeyDataPair> ItemsAt(ref DbEntry key, ref DbEntry data, ReadFlags rflags, Usage usage) {
      if (GetDb().IsSecondary)
        return new SecondaryKeyDataDupEnumerator(this, ref key, ref data, rflags, usage);
      else
        return new KeyDataDupEnumerator(this, ref key, ref data, rflags, usage);
    }

    public IEnumerable<KeyDataPair> ItemsAt(ref DbEntry key, ref DbEntry data, ReadFlags rflags) {
      return ItemsAt(ref key, ref data, rflags, Usage.Close);
    }

    #endregion

    #region IEnumerable<KeyDataPair> Members

    public IEnumerator<KeyDataPair> GetEnumerator() {
      if (GetDb().IsSecondary)
        return new SecondaryEnumerator(this, true, false, ReadFlags.None, Usage.Close);
      else
        return new Enumerator(this, true, false, ReadFlags.None, Usage.Close);
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    #endregion

    #region Nested Types

    public enum Position
    {
      Initial,
      Valid,
      Final
    }

    private abstract class BaseEnumerator: IEnumerable<KeyDataPair>, IEnumerator<KeyDataPair>
    {
      protected readonly DbFileCursor cursor;
      protected readonly KeyDataPair current;
      protected readonly ReadFlags rflags;
      protected readonly GetMode nextMode;
      protected Position pos;
      protected readonly Usage usage;

      protected BaseEnumerator(DbFileCursor cursor, bool forward, bool distinct, ReadFlags rflags, Usage usage) {
        this.cursor = cursor;
        this.rflags = rflags;
        if (forward)
          nextMode = distinct ? GetMode.NextNoDup : GetMode.Next;
        else
          nextMode = distinct ? GetMode.PrevNoDup : GetMode.Prev;
        this.usage = usage;
        current = new KeyDataPair();
        current.Key = DbEntry.Out(new byte[16]);
        current.Data = DbEntry.Out(new byte[32]);
        Reset();
      }

      #region IEnumerable<KeyDataPair> Members

      public IEnumerator<KeyDataPair> GetEnumerator() {
        return this;
      }

      #endregion

      #region IEnumerable Members

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }

      #endregion

      #region IEnumerator<KeyDataPair> Members

      public KeyDataPair Current {
        get {
          if (pos == Position.Valid)
            return current;
          else
            throw new InvalidOperationException("Enumerator position invalid.");
        }
      }

      #endregion

      #region IDisposable Members

      public void Dispose() {
        if (usage == Usage.Close)
          cursor.Dispose();
      }

      #endregion

      #region IEnumerator Members

      object System.Collections.IEnumerator.Current {
        get { return Current; }
      }

      protected abstract ReadStatus GetFirst();

      protected abstract ReadStatus GetNext(); 

      public bool MoveNext() {
        ReadStatus status = ReadStatus.Success;
        switch (pos) {
          case Position.Initial:
            status = GetFirst();
            if (status == ReadStatus.KeyEmpty)
              status = GetNext();
            break;
          case Position.Valid:
            status = GetNext();
            break;
          case Position.Final:
            return false;
        }
        if (status == ReadStatus.Success) {
          pos = Position.Valid;
          return true;
        }
        else {
          pos = Position.Final;
          return false;
        }
      }

      public void Reset() {
        pos = Position.Initial;
      }

      #endregion
    }

    private class Enumerator: BaseEnumerator
    {
      public Enumerator(DbFileCursor cursor, bool forward, bool distinct, ReadFlags rflags, Usage usage)
        : base(cursor, forward, distinct, rflags, usage) { }

      protected override ReadStatus GetFirst() {
        GetMode firstMode;
        if (nextMode == GetMode.Next || nextMode == GetMode.NextNoDup)
          firstMode = GetMode.First;
        else
          firstMode = GetMode.Last;
        ReadStatus status = cursor.Get(ref current.Key, ref current.Data, firstMode, rflags);
        while (status == ReadStatus.BufferSmall) {
          // no key passed into forward cursor - simply replace buffer if too small
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key = DbEntry.Out(new byte[current.Key.Size]);
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data = DbEntry.Out(new byte[current.Data.Size]);
          status = cursor.Get(ref current.Key, ref current.Data, firstMode, rflags);
        }
        return status;
      }

      protected override ReadStatus GetNext() {
        ReadStatus status = cursor.Get(ref current.Key, ref current.Data, nextMode, rflags);
        while (status == ReadStatus.KeyEmpty)
          status = cursor.Get(ref current.Key, ref current.Data, nextMode, rflags);
        while (status == ReadStatus.BufferSmall) {
          // no key passed into forward cursor - simply replace buffer if too small
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key = DbEntry.Out(new byte[current.Key.Size]);
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data = DbEntry.Out(new byte[current.Data.Size]);
          status = cursor.Get(ref current.Key, ref current.Data, nextMode, rflags);
        }
        return status;
      }
    }

    private class SecondaryEnumerator: BaseEnumerator
    {
      public SecondaryEnumerator(DbFileCursor cursor, bool forward, bool distinct, ReadFlags rflags, Usage usage)
        : base(cursor, forward, distinct, rflags, usage)
      {
        current.PKey = DbEntry.Out(new byte[16]);
      }

      protected override ReadStatus GetFirst() {
        GetMode firstMode;
        if (nextMode == GetMode.Next || nextMode == GetMode.NextNoDup)
          firstMode = GetMode.First;
        else
          firstMode = GetMode.Last;
        ReadStatus status = cursor.PGet(ref current.Key, ref current.PKey, ref current.Data, firstMode, rflags);
        while (status == ReadStatus.BufferSmall) {
          // no key passed into forward cursor - simply replace buffer if too small
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key = DbEntry.Out(new byte[current.Key.Size]);
          if (current.PKey.Buffer.Length < current.PKey.Size)
            current.PKey = DbEntry.Out(new byte[current.PKey.Size]);
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data = DbEntry.Out(new byte[current.Data.Size]);
          status = cursor.PGet(ref current.Key, ref current.PKey, ref current.Data, firstMode, rflags);
        }
        return status;
      }

      protected override ReadStatus GetNext() {
        ReadStatus status = cursor.PGet(ref current.Key, ref current.PKey, ref current.Data, nextMode, rflags);
        while (status == ReadStatus.KeyEmpty)
          status = cursor.PGet(ref current.Key, ref current.PKey, ref current.Data, nextMode, rflags);
        while (status == ReadStatus.BufferSmall) {
          // no key passed into forward cursor - simply replace buffer if too small
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key = DbEntry.Out(new byte[current.Key.Size]);
          if (current.PKey.Buffer.Length < current.PKey.Size)
            current.PKey = DbEntry.Out(new byte[current.PKey.Size]);
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data = DbEntry.Out(new byte[current.Data.Size]);
          status = cursor.PGet(ref current.Key, ref current.PKey, ref current.Data, nextMode, rflags);
        }
        return status;
      }
    }

    private abstract class BaseDupEnumerator: IEnumerable<KeyDataPair>, IEnumerator<KeyDataPair>
    {
      protected readonly DbFileCursor cursor;
      protected readonly DbEntry key;
      protected readonly KeyDataPair current;
      protected readonly GetAtMode atMode;
      protected readonly ReadFlags rflags;
      protected Position pos;
      protected readonly Usage usage;

      protected BaseDupEnumerator(DbFileCursor cursor, ref DbEntry key, GetAtMode atMode, ReadFlags rflags, Usage usage) {
        if (key.Buffer == null)
          throw new ArgumentException("Missing key buffer.", "key");
        this.cursor = cursor;
        this.key = key;
        this.atMode = atMode;
        this.rflags = rflags;
        this.usage = usage;
        current = new KeyDataPair();
        current.Key = DbEntry.Out(new byte[key.Size < 16 ? 16 : key.Size]);
        Reset();
      }

      #region IEnumerable<KeyDataPair> Members

      public IEnumerator<KeyDataPair> GetEnumerator() {
        return this;
      }

      #endregion

      #region IEnumerable Members

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }

      #endregion

      #region IEnumerator<KeyDataPair> Members

      public KeyDataPair Current {
        get {
          if (pos == Position.Valid)
            return current;
          else
            throw new InvalidOperationException("Enumerator position invalid.");
        }
      }

      #endregion

      #region IDisposable Members

      public void Dispose() {
        if (usage == Usage.Close)
          cursor.Dispose();
      }

      #endregion

      #region IEnumerator Members

      object System.Collections.IEnumerator.Current {
        get { return Current; }
      }

      // In the case of Queue and Recno databases, this may return ReadStatus.KeyEmpty,
      // but as this enumerator does not cover range searches, this should be interpreted
      // the same as ReadStatus.NotFound.
      protected abstract ReadStatus GetAt();

      // Needs to be overridden for secondary databases. It looks as if this
      // will never return ReadStatus.KeyEmpty, so we don't check for it.
      protected virtual ReadStatus GetNext() {
        ReadStatus status;
        do {
          status = cursor.Get(ref current.Key, ref current.Data, GetMode.NextDup, rflags);
          while (status == ReadStatus.BufferSmall) {
            // no key passed into forward cursor - simply replace buffer if too small
            if (current.Key.Buffer.Length < current.Key.Size)
              current.Key = DbEntry.Out(new byte[current.Key.Size]);
            if (current.Data.Buffer.Length < current.Data.Size)
              current.Data = DbEntry.Out(new byte[current.Data.Size]);
            status = cursor.Get(ref current.Key, ref current.Data, GetMode.NextDup, rflags);
          }
        } while (status == ReadStatus.KeyEmpty);
        return status;
      }

      public bool MoveNext() {
        ReadStatus status = ReadStatus.Success;
        switch (pos) {
          case Position.Initial:
            status = GetAt();
            break;
          case Position.Valid:
            status = GetNext();
            break;
          case Position.Final:
            return false;
        }
        if (status == ReadStatus.Success) {
          pos = Position.Valid;
          return true;
        }
        else {
          pos = Position.Final;
          return false;
        }
      }

      public void Reset() {
        pos = Position.Initial;
      }

      #endregion
    }

    private abstract class SecondaryBaseDupEnumerator: BaseDupEnumerator
    {
      protected SecondaryBaseDupEnumerator(DbFileCursor cursor, ref DbEntry key, GetAtMode atMode, ReadFlags rflags, Usage usage)
        : base(cursor, ref key, atMode, rflags, usage) 
      {
        current.Data = DbEntry.Out(new byte[32]);
      }

      protected override ReadStatus GetNext() {
        ReadStatus status;
        do {
          status = cursor.PGet(ref current.Key, ref current.PKey, ref current.Data, GetMode.NextDup, rflags);
          while (status == ReadStatus.BufferSmall) {
            // no key passed into forward cursor - simply replace buffer if too small
            if (current.Key.Buffer.Length < current.Key.Size)
              current.Key = DbEntry.Out(new byte[current.Key.Size]);
            if (current.PKey.Buffer.Length < current.PKey.Size)
              current.PKey = DbEntry.Out(new byte[current.PKey.Size]);
            if (current.Data.Buffer.Length < current.Data.Size)
              current.Data = DbEntry.Out(new byte[current.Data.Size]);
            status = cursor.PGet(ref current.Key, ref current.PKey, ref current.Data, GetMode.NextDup, rflags);
          }
        } while (status == ReadStatus.KeyEmpty);
        return status;
      }
    }

    private class KeyDupEnumerator: BaseDupEnumerator
    {
      public KeyDupEnumerator(DbFileCursor cursor, ref DbEntry key, ReadFlags rflags, Usage usage)
        : base(cursor, ref key, GetAtMode.Set, rflags, usage)
      {
        current.Data = DbEntry.Out(new byte[32]);
      }

      protected override ReadStatus GetAt() {
        // copy key into current.Key and make it into an in-out DbEntry
        byte[] buffer = current.Key.Buffer;
        Buffer.BlockCopy(key.Buffer, key.Start, buffer, 0, key.Size);
        current.Key = DbEntry.InOut(buffer, 0, key.Size);
        // set cursor to position indicated by key
        ReadStatus status = cursor.GetAt(ref current.Key, ref current.Data, atMode, rflags);
        while (status == ReadStatus.BufferSmall) {
          // a key was passed in - need to preserve the buffer content
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key.ResizeBuffer(current.Key.Size);
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data = DbEntry.Out(new byte[current.Data.Size]);
          status = cursor.GetAt(ref current.Key, ref current.Data, atMode, rflags);
        }
        return status;
      }
    }

    private class SecondaryKeyDupEnumerator: SecondaryBaseDupEnumerator
    {
      public SecondaryKeyDupEnumerator(DbFileCursor cursor, ref DbEntry key, ReadFlags rflags, Usage usage)
        : base(cursor, ref key, GetAtMode.Set, rflags, usage) 
      {
        current.PKey = DbEntry.Out(new byte[16]);
      }

      protected override ReadStatus GetAt() {
        // copy key into current.Key and make it into an in-out DbEntry
        byte[] buffer = current.Key.Buffer;
        Buffer.BlockCopy(key.Buffer, key.Start, buffer, 0, key.Size);
        current.Key = DbEntry.InOut(buffer, 0, key.Size);
        // set cursor to position indicated by key
        ReadStatus status = cursor.PGetAt(ref current.Key, ref current.PKey, ref current.Data, atMode, rflags);
        while (status == ReadStatus.BufferSmall) {
          // a key was passed in - need to preserve the buffer content
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key.ResizeBuffer(current.Key.Size);
          if (current.PKey.Buffer.Length < current.PKey.Size)
            current.PKey = DbEntry.Out(new byte[current.PKey.Size]);
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data = DbEntry.Out(new byte[current.Data.Size]);
          status = cursor.PGetAt(ref current.Key, ref current.PKey, ref current.Data, atMode, rflags);
        }
        return status;
      }
    }

    private class KeyDataDupEnumerator: BaseDupEnumerator
    {
      protected readonly DbEntry data;

      public KeyDataDupEnumerator(DbFileCursor cursor, ref DbEntry key, ref DbEntry data, ReadFlags rflags, Usage usage)
        : base(cursor, ref key, GetAtMode.GetBoth, rflags, usage) 
      {
        if (data.Buffer == null)
          throw new ArgumentException("Missing data buffer.", "data");
        this.data = data;
        current.Data = DbEntry.Out(new byte[data.Size < 32 ? 32 : data.Size]);
      }

      protected override ReadStatus GetAt() {
        // copy key int current.Key and make it into an in-out DbEntry
        byte[] buffer = current.Key.Buffer;
        Buffer.BlockCopy(key.Buffer, key.Start, buffer, 0, key.Size);
        current.Key = DbEntry.InOut(buffer, 0, key.Size);
        // copy data int current.Data and make it into an in-out DbEntry
        buffer = current.Data.Buffer;
        Buffer.BlockCopy(data.Buffer, data.Start, buffer, 0, data.Size);
        current.Data = DbEntry.InOut(buffer, 0, data.Size);
        // set cursor to position indicated by key and data
        ReadStatus status = cursor.GetAt(ref current.Key, ref current.Data, atMode, rflags);
        while (status == ReadStatus.BufferSmall) {
          // a key was passed in - need to preserve the buffer content
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key.ResizeBuffer(current.Key.Size);
          // data were passed in - need to preserve the buffer content
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data.ResizeBuffer(current.Data.Size);
          status = cursor.GetAt(ref current.Key, ref current.Data, atMode, rflags);
        }
        return status;
      }
    }

    private class SecondaryKeyDataDupEnumerator: SecondaryBaseDupEnumerator
    {
      protected readonly DbEntry pkey;

      public SecondaryKeyDataDupEnumerator(DbFileCursor cursor, ref DbEntry key, ref DbEntry pkey, ReadFlags rflags, Usage usage)
        : base(cursor, ref key, GetAtMode.GetBoth, rflags, usage)
      {
        if (pkey.Buffer == null)
          throw new ArgumentException("Missing primary key buffer.", "pkey");
        this.pkey = pkey;
        current.PKey = DbEntry.Out(new byte[pkey.Size < 16 ? 16 : pkey.Size]);
      }

      protected override ReadStatus GetAt() {
        // copy key into current.Key and make it into an in-out DbEntry
        byte[] buffer = current.Key.Buffer;
        Buffer.BlockCopy(key.Buffer, key.Start, buffer, 0, key.Size);
        current.Key = DbEntry.InOut(buffer, 0, key.Size);
        // copy pkey int current.PKey and make it into an in-out DbEntry
        buffer = current.PKey.Buffer;
        Buffer.BlockCopy(pkey.Buffer, pkey.Start, buffer, 0, pkey.Size);
        current.PKey = DbEntry.InOut(buffer, 0, pkey.Size);
        // set cursor to position indicated by key and pkey
        ReadStatus status = cursor.PGetAt(ref current.Key, ref current.PKey, ref current.Data, atMode, rflags);
        while (status == ReadStatus.BufferSmall) {
          // a key was passed in - need to preserve the buffer content
          if (current.Key.Buffer.Length < current.Key.Size)
            current.Key.ResizeBuffer(current.Key.Size);
          // a primary key was passed in - need to preserve the buffer content
          if (current.PKey.Buffer.Length < current.PKey.Size)
            current.PKey.ResizeBuffer(current.PKey.Size);
          if (current.Data.Buffer.Length < current.Data.Size)
            current.Data = DbEntry.Out(new byte[current.Data.Size]);
          status = cursor.PGetAt(ref current.Key, ref current.PKey, ref current.Data, atMode, rflags);
        }
        return status;
      }
    }

    [Flags]
    public enum CreateFlags: int
    {
      None = 0,
#if BDB_4_3_29
      Degree2 = DbConst.DB_DEGREE_2,
      DirtyRead = DbConst.DB_DIRTY_READ,
#endif
#if BDB_4_5_20
      ReadCommitted = DbConst.DB_READ_COMMITTED,
      ReadUncommitted = DbConst.DB_READ_UNCOMMITTED,
#endif
      WriteCursor = DbConst.DB_WRITECURSOR,
#if BDB_4_5_20
      TxnSnapshot = DbConst.DB_TXN_SNAPSHOT
#endif
    }

    public enum DupMode: int
    {
      None = 0,
      Position = DbConst.DB_POSITION
    }

    [Flags]
    public enum ReadFlags: int
    {
      None = 0,
#if BDB_4_3_29
      DirtyRead = DbConst.DB_DIRTY_READ,
#endif
#if BDB_4_5_20
      ReadUncommitted = DbConst.DB_READ_UNCOMMITTED,
#endif
      Multiple = DbConst.DB_MULTIPLE,
      MultipleKey = DbConst.DB_MULTIPLE_KEY,
      RMW = DbConst.DB_RMW
    }

    public enum GetMode: int
    {
      Current = DbConst.DB_CURRENT,
      First = DbConst.DB_FIRST,
      Last = DbConst.DB_LAST,
      Next = DbConst.DB_NEXT,
      NextDup = DbConst.DB_NEXT_DUP,
      NextNoDup = DbConst.DB_NEXT_NODUP,
      Prev = DbConst.DB_PREV,
      PrevNoDup = DbConst.DB_PREV_NODUP
    }

    public enum GetAtMode: int
    {
      Set = DbConst.DB_SET,
      SetRange = DbConst.DB_SET_RANGE,
      GetBoth = DbConst.DB_GET_BOTH,
      GetBothRange = DbConst.DB_GET_BOTH_RANGE
    }

    #endregion
  }

  /// <summary>Intermediate class needed for associating cursor classes with database classes.</summary>
  /// <typeparam name="C">Type of <see cref="DbFileCursor"></see> subclass.</typeparam>
  /// <typeparam name="D">Type of <see cref="DbFile"></see> subclass.</typeparam>
  public abstract unsafe class DbFileCursor<C, D>: DbFileCursor
    where C: DbFileCursor<C, D>, new()
    where D: DbFile<D, C>
  {
    #region Public Operations & Properties

    public C Dup(DupMode mode) {
      C result = new C();
      DbRetVal ret;
      // always lock Db first, to avoid deadlock
      lock (db.rscLock) {
        lock (rscLock) {
          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DBC* dbcp = CheckDisposed();
            DBC* dupDbc;
            ret = dbcp->Dup(dbcp, out dupDbc, unchecked((UInt32)mode));
            if (ret == DbRetVal.SUCCESS) {
              result.Initialize(db, dupDbc);
              db.InsertCursor(this);
            }
          }
        }
      }
      Util.CheckRetVal(ret);
      return result;
    }

    #endregion
  }

  /// <summary>Cursor for <see cref="DbQueue"/> database.</summary>
  public class DbQueueCursor: DbFileCursor<DbQueueCursor, DbQueue>
  {
    static DbQueueCursor() {
      dbType = DbType.Queue;
    }

    public DbQueueCursor() { }

    #region Public Operations & Properties

    public WriteStatus Put(ref DbEntry data) {
      DbEntry nullEntry = new DbEntry();
      return Put(ref nullEntry, ref data, DbConst.DB_CURRENT);
    }

    #endregion
  }

  /// <summary>Cursor for <see cref="DbRecNo"/> database.</summary>
  public class DbRecNoCursor: DbFileCursor<DbRecNoCursor, DbRecNo>
  {
    static DbRecNoCursor() {
      dbType = DbType.Recno;
    }

    public DbRecNoCursor() { }

    #region Public Operations & Properties

    public WriteStatus Put(ref DbEntry data) {
      DbEntry nullEntry = new DbEntry();
      return Put(ref nullEntry, ref data, DbConst.DB_CURRENT);
    }

    public WriteStatus Put(ref DbEntry key, ref DbEntry data, PutMode mode) {
      return Put(ref key, ref data, unchecked((uint)mode));
    }

    #endregion

    #region Nested Types

    public enum PutMode: int
    {
      After = DbConst.DB_AFTER,
      Before = DbConst.DB_BEFORE
    }

    #endregion
  }

  /// <summary>Base class for cursors on <see cref="DbBTree"/> and <see cref="DbHash"/> databases.</summary>
  /// <typeparam name="C">Type of <see cref="DbFileCursor"></see> subclass.</typeparam>
  /// <typeparam name="D">Type of <see cref="DbFile"></see> subclass.</typeparam>
  public abstract class DbKeyCursor<C, D>: DbFileCursor<C, D>
    where C: DbFileCursor<C, D>, new()
    where D: DbFile<D, C>
  {
    #region Public Operations & Properties

    public WriteStatus Put(ref DbEntry data, PutMode mode) {
      DbEntry nullEntry = new DbEntry();
      return Put(ref nullEntry, ref data, unchecked((uint)mode));
    }

    public WriteStatus Put(ref DbEntry key, ref DbEntry data, PutKeyMode mode) {
      return Put(ref key, ref data, unchecked((uint)mode));
    }

    #endregion

    #region Nested Types

    public enum PutMode: int
    {
      After = DbConst.DB_AFTER,
      Before = DbConst.DB_BEFORE,
      Current = DbConst.DB_CURRENT
    }

    public enum PutKeyMode: int
    {
      KeyFirst = DbConst.DB_KEYFIRST,
      KeyLast = DbConst.DB_KEYLAST,
      NoDupData = DbConst.DB_NODUPDATA
    }

    #endregion
  }

  /// <summary>Cursor for <see cref="DbBTree"/> database.</summary>
  public class DbBTreeCursor: DbKeyCursor<DbBTreeCursor, DbBTree>
  {
    static DbBTreeCursor() {
      dbType = DbType.BTree;
    }

    public DbBTreeCursor() { }

    #region Public Operations & Properties

    // does not need to handle multiple output - because of DB_GET_RECNO flag
    public unsafe int GetRecNo(ReadFlags rflags) {
      int recno;
      DBT data = new DBT(0, sizeof(UInt32));
      data.data = &recno;

      DbRetVal ret;
      lock (rscLock) {
        DBC* dbcp = CheckDisposed();
        ret = DbcGet(dbcp, ref *(DBT*)null, ref data, DbConst.DB_GET_RECNO | unchecked((UInt32)rflags));
      }
      Util.CheckRetVal(ret);
      return recno;
    }

    public unsafe void GetBothRecNos(ReadFlags rflags, out int primRecNo, out int secRecNo) {
      int pRecNo, sRecNo;
      DBT data = new DBT(0, sizeof(UInt32));
      data.data = &pRecNo;
      DBT pkey = new DBT(0, sizeof(UInt32));
      pkey.data = &sRecNo;

      DbRetVal ret;
      lock (rscLock) {
        DBC* dbcp = CheckDisposed();
        ret = DbcPGet(dbcp, ref *(DBT*)null, ref pkey, ref data, DbConst.DB_GET_RECNO | unchecked((UInt32)rflags));
      }
      Util.CheckRetVal(ret);
      primRecNo = pRecNo;
      secRecNo = sRecNo;
    }

    // moves to numbered record; key.Data must contain a record number (UInt32) on input
    public ReadStatus GetAtRecNo(ref DbEntry key, ref DbEntry data, ReadFlags rflags) {
      return Get(ref key, ref data, DbConst.DB_SET_RECNO | unchecked((uint)rflags));
    }

    #endregion

    // TODO Iterating over range in BTrees (using record numbers?)
  }

  /// <summary>Cursor for <see cref="DbHash"/> database.</summary>
  public class DbHashCursor: DbKeyCursor<DbHashCursor, DbHash>
  {
    static DbHashCursor() {
      dbType = DbType.Hash;
    }

    public DbHashCursor() { }
  }
}
