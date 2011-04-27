/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace BerkeleyDb
{
  /// <summary>Represents a sequence of numbers guaranteed to not produce duplicates.</summary>
  /// <remarks>Wraps a <see cref="DB_SEQUENCE"/> handle. Can be made thread-safe
  /// by specifying the <c>Sequence.OpenFlags.ThreadSafe</c> flag when opening the
  /// sequence. However, as we synchronize all calls it does not appear necessary to
  /// specify this flag. It could be used as a safe-guard since it does not impact
  /// performance much.</remarks>
  public unsafe class Sequence: IDisposable
  {
    protected readonly Db db;

    // store delegates for frequently used function pointer calls
    [CLSCompliant(false)]
    protected DB_SEQUENCE.GetFcn SeqGet = null;

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
    volatile DB_SEQUENCE* seqp = null;
    GCHandle instanceHandle;

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    internal DbRetVal AllocateHandle(DB* dbp, UInt32 flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp;
        ret = LibDb.db_sequence_create(out seqp, dbp, flags);
        if (ret == DbRetVal.SUCCESS) {
          this.seqp = seqp;
          seqp->api_internal = (IntPtr)instanceHandle;
        }
      }
      return ret;
    }

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    DbRetVal ReleaseUnmanagedResources() {
      DB_SEQUENCE* seqp = this.seqp;
      if (seqp == null)
        return DbRetVal.SUCCESS;
      // DB_SEQUENCE->Close() could be a lengthy call, so we call Disposed() first, and the
      // CER ensures that we reach DB_SEQUENCE->Close() without external interruption.
      // This is OK because one must not use the handle after DB_SEQUENCE->Close() was called,
      // regardless of the return value.
      Disposed();
      DbRetVal ret = seqp->Close(seqp, 0);
      return ret;
    }

    #endregion

    #region Construction, Finalization

    internal Sequence(Db db) {
      this.db = db;
      // so that we can refer back to the Sequence instance
      instanceHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
    }

    public const string disposedStr = "Sequence handle closed.";

    [CLSCompliant(false)]
    protected DB_SEQUENCE* CheckDisposed() {
      // avoid multiple volatile memory access
      DB_SEQUENCE* seqp = this.seqp;
      if (seqp == null)
        throw new ObjectDisposedException(disposedStr);
      return seqp;
    }

    // when overriding, call base method at end (using finally clause)
    protected internal virtual void Dispose(bool disposing) {
      lock (rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          releaseVal = ReleaseUnmanagedResources();
        }
        if (instanceHandle.IsAllocated)
          instanceHandle.Free();
      }
    }

    // does not check for seqp == null or db == null!
    void Disposed() {
      seqp = null;
      // unregister with resource manager
      db.RemoveSequence(this);
    }

    public bool IsDisposed {
      get { return seqp == null; }
    }

    public DbRetVal ReleaseVal {
      get { return releaseVal; }
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
      // always lock Db first, to avoid deadlock
      lock (db.rscLock) {
        Dispose(true);
      }
      GC.SuppressFinalize(this);
    }

    #endregion

    #region Helpers

    // does not check if disposed
    internal static Db GetDb(DB_SEQUENCE* seqp) {
      // DB* dbp;
      // DbRetVal ret = seqp->GetDb(seqp, out dbp);
      // Util.CheckRetVal(ret);
      // workaround for bug in GetDb - does not return dbp when sequence not open
      DB* dbp = seqp->seq_dbp;
      Db db = Util.GetDb(dbp);
      return db;
    }

    #endregion

    #region Public Operations & Properties

    public Db GetDb() {
      return db;
    }

    public void Close() {
      Dispose();
    }

    DbRetVal Open(DB_TXN* txp, ref DbEntry key, OpenFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          ret = seqp->Open(seqp, txp, ref key.dbt, unchecked((UInt32)flags));
        }
        // initialize function pointer delegates
        SeqGet = seqp->Get;
      }
      return ret;
    }

    public void Open(Txn txn, ref DbEntry key, OpenFlags flags) {
      DbRetVal ret;
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          ret = Open(txp, ref key, flags);
        }
      }
      else
        ret = Open((DB_TXN*)null, ref key, flags);
      Util.CheckRetVal(ret);
    }

    Int64 Get(DB_TXN* txp, Int32 delta, ReadFlags flags) {
      Int64 value;
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        if (SeqGet == null)
          throw new BdbException("Sequence must be open.");
        ret = SeqGet(seqp, txp, delta, out value, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
      return value;
    }

    public Int64 Get(Txn txn, Int32 delta, ReadFlags flags) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Get(txp, delta, flags);
        }
      }
      else
        return Get((DB_TXN*)null, delta, flags);
    }

    DbRetVal Remove(DB_TXN* txp, RemoveFlags flags) {
      DbRetVal ret;
      // always lock Db first, to avoid deadlock
      lock (db.rscLock) {
        lock (rscLock) {
          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DB_SEQUENCE* seqp = CheckDisposed();
            // DB_SEQUENCE->Remove() could be a lengthy call, so we call Disposed() first, and the
            // CER ensures that we reach DB_SEQUENCE->Remove() without external interruption.
            // This is OK because one must not use the handle after DB_SEQUENCE->Remove() was called,
            // regardless of the return value.
            Disposed();
            ret = seqp->Remove(seqp, txp, unchecked((UInt32)flags));
          }
        }
      }
      return ret;
    }

    public void Remove(Txn txn, RemoveFlags flags) {
      DbRetVal ret;
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          ret = Remove(txp, flags);
        }
      }
      else
        ret = Remove((DB_TXN*)null, flags);
      GC.SuppressFinalize(this);
      Util.CheckRetVal(ret);
    }

    public DbFile Dbf {
      get { return db.Dbf; }
    }

    public DbEntry Key {
      get {
        DbEntry keyEntry = new DbEntry();
        lock (rscLock) {
          DB_SEQUENCE* seqp = CheckDisposed();
          DbRetVal ret = seqp->GetKey(seqp, out keyEntry.dbt);
          Util.CheckRetVal(ret);
          // this will modify keyEntry.dbt.ulen to be the same as keyEntry.Size
          keyEntry.ResizeBuffer(keyEntry.Size);
          Marshal.Copy((IntPtr)keyEntry.dbt.data, keyEntry.Buffer, 0, keyEntry.Size);
        }
        return keyEntry;
      }
    }

    public SequenceStats GetStats(StatFlags flags) {
      SequenceStats value;
      DB_SEQUENCE_STAT* sp;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = seqp->Stat(seqp, out sp, unchecked((UInt32)flags));
          Util.CheckRetVal(ret);
          value.seqStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public void PrintStats(BerkeleyDb.StatPrintFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        ret = seqp->StatPrint(seqp, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

    #endregion

    #region Public Configuration

    public void SetInitialValue(Int64 value) {
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        ret = seqp->InitialValue(seqp, value);
      }
      Util.CheckRetVal(ret);
    }

    public SeqFlags GetFlags() {
      SeqFlags flags;
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        ret = seqp->GetFlags(seqp, out flags);
      }
      Util.CheckRetVal(ret);
      return flags;
    }

    public void SetFlags(SeqFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        ret = seqp->SetFlags(seqp, flags);
      }
      Util.CheckRetVal(ret);
    }

    public int CacheSize {
      get {
        Int32 value;
        DbRetVal ret;
        lock (rscLock) {
          DB_SEQUENCE* seqp = CheckDisposed();
          ret = seqp->GetCacheSize(seqp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_SEQUENCE* seqp = CheckDisposed();
          ret = seqp->SetCacheSize(seqp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public void GetRange(out Int64 min, out Int64 max) {
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        ret = seqp->GetRange(seqp, out min, out max);
      }
      Util.CheckRetVal(ret);
    }

    public void SetRange(Int64 min, Int64 max) {
      DbRetVal ret;
      lock (rscLock) {
        DB_SEQUENCE* seqp = CheckDisposed();
        ret = seqp->SetRange(seqp, min, max);
      }
      Util.CheckRetVal(ret);
    }

    #endregion

    #region Nested Types

    [Flags]
    public enum OpenFlags: int
    {
      None = 0,
#if BDB_4_3_29
      AutoCommit = DbConst.DB_AUTO_COMMIT,
#endif
      Create = DbConst.DB_CREATE,
      Exclusive = DbConst.DB_EXCL,
      ThreadSafe = DbConst.DB_THREAD
    }

    [Flags]
    public enum ReadFlags: int
    {
      None = 0,
#if BDB_4_3_29
      AutoCommit = DbConst.DB_AUTO_COMMIT,
#endif
      NoSync = DbConst.DB_TXN_NOSYNC
    }

    [Flags]
    public enum RemoveFlags: int
    {
      None = 0,
#if BDB_4_3_29
      AutoCommit = DbConst.DB_AUTO_COMMIT,
#endif
      NoSync = DbConst.DB_TXN_NOSYNC
    }

    #endregion
  }

  // CLS compliant wrapper for DB_SEQUENCE_STAT
  public struct SequenceStats
  {
    internal DB_SEQUENCE_STAT seqStats;

    public int NumWait {
      get { return unchecked((int)seqStats.st_wait); }
    }

    /* Sequence lock granted after wait. */
    public int NumNoWait {
      get { return unchecked((int)seqStats.st_nowait); }
    }

    /* Current value in db. (typedef int64_t db_seq_t;) */
    public Int64 Current {
      get { return seqStats.st_current; }
    }

    /* Current cached value. */
    public Int64 Value {
      get { return seqStats.st_value; }
    }

    /* Last cached value. */
    public Int64 LastValue {
      get { return seqStats.st_last_value; }
    }

    /* Minimum value. */
    public Int64 Min {
      get { return seqStats.st_min; }
    }

    /* Maximum value. */
    public Int64 Max {
      get { return seqStats.st_max; }
    }

    /* Cache size. */
    public int CacheSize {
      get { return seqStats.st_cache_size; }
    }

    /* Flag value. */
    public int Flags {
      get { return unchecked((int)seqStats.st_flags); }
    }
  }
}
