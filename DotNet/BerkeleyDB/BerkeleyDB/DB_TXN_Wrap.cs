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
  /// <summary>Represents a transaction in a Berkeley DB environment.</summary>
  /// <remarks>Wraps a <see cref="DB_TXN"/> handle. Transactions may only span threads
  /// if they do so serially; that is, each transaction must be active in only a single
  /// thread of control at a time. This restriction holds for parents of nested transactions
  /// as well; not two children may be concurrently active in more than one thread of control
  /// at any one time.</remarks>
  public unsafe class Txn: IDisposable
  {
    protected readonly Env env;

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
    volatile DB_TXN* txp = null;
    GCHandle instanceHandle;

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    DbRetVal ReleaseUnmanagedResources() {
      DB_TXN* txp = this.txp;
      if (txp == null)
        return DbRetVal.SUCCESS;
      // DB_TXN->Abort() could be a lengthy call, so we call Disposed() first, and the
      // CER ensures that we reach DB_TXN->Abort() without external interruption.
      // This is OK because one must not use the handle after DB_TXN->Abort() was called,
      // regardless of the return value.
      Disposed();
      DbRetVal ret = txp->Abort(txp);
      return ret;
    }

    #endregion

    #region Construction, Finalization

    internal Txn(Env env) {
      this.env = env;
      // so that we can refer back to the Txn instance
      instanceHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
    }

    // assumes that env != null and txp != null!
    internal void Initialize(DB_TXN* txp) {
      this.txp = txp;
      txp->api_internal = (IntPtr)instanceHandle;
    }

    internal DB_TXN* Txp {
      get { return txp; }
    }

    public const string disposedStr = "Transaction handle closed.";

    [CLSCompliant(false)]
    protected internal DB_TXN* CheckDisposed() {
      // avoid multiple volatile memory access
      DB_TXN* txp = this.txp;
      if (txp == null)
        throw new ObjectDisposedException(disposedStr);
      return txp;
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

    void Disposed() {
      txp = null;
      // unregister with resource manager
      env.RemoveTransaction(this); ;
    }

    public bool IsDisposed {
      get { return txp == null; }
    }

    public DbRetVal ReleaseVal {
      get { return releaseVal; }
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
      // always lock environment first, to avoid deadlocks
      lock (env.rscLock) {
        Dispose(true);
      }
      GC.SuppressFinalize(this);
    }

    #endregion

#if BDB_4_3_29
    [CLSCompliant(false)]
    protected void SetBeginLsn(ref DB_LSN* lsnp) {
      DbRetVal ret;
      lock (rscLock) {
        DB_TXN* txp = CheckDisposed();
        ret = txp->SetBeginLsnp(txp, out lsnp);
      }
      Util.CheckRetVal(ret);
    }
#endif

    #region Public Operations & Properties

    public void Abort() {
      DbRetVal ret;
      // always lock environment first, to avoid deadlocks
      lock (env.rscLock) {
        lock (rscLock) {
          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DB_TXN* txp = CheckDisposed();
            // DB_TXN->Abort() could be a lengthy call, so we call Disposed() first, and the
            // CER ensures that we reach DB_TXN->Abort() without external interruption.
            // This is OK because one must not use the handle after DB_TXN->Abort() was called,
            // regardless of the return value.
            Disposed();
            ret = txp->Abort(txp);
          }
        }
      }
      GC.SuppressFinalize(this);
      Util.CheckRetVal(ret);
    }

    public void Commit(CommitMode mode) {
      DbRetVal ret;
      // always lock environment first, to avoid deadlocks
      lock (env.rscLock) {
        lock (rscLock) {
          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DB_TXN* txp = CheckDisposed();
            // DB_TXN->Commit() could be a lengthy call, so we call Disposed() first, and the
            // CER ensures that we reach DB_TXN->Commit() without external interruption.
            // This is OK because one must not use the handle after DB_TXN->Commit() was called,
            // regardless of the return value.
            Disposed();
            ret = txp->Commit(txp, unchecked((UInt32)mode));
          }
        }
      }
      GC.SuppressFinalize(this);
      Util.CheckRetVal(ret);
    }

    public void Discard() {
      DbRetVal ret;
      // always lock environment first, to avoid deadlocks
      lock (env.rscLock) {
        lock (rscLock) {
          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DB_TXN* txp = CheckDisposed();
            // DB_TXN->Discard() could be a lengthy call, so we call Disposed() first, and the
            // CER ensures that we reach DB_TXN->Discard() without external interruption.
            // This is OK because one must not use the handle after DB_TXN->Discard() was called,
            // regardless of the return value.
            Disposed();
            ret = txp->Discard(txp, 0);
          }
        }
      }
      GC.SuppressFinalize(this);
      Util.CheckRetVal(ret);
    }

    public int Id {
      get {
        lock (rscLock) {
          DB_TXN* txp = CheckDisposed();
          return unchecked((int)txp->Id(txp));
        }
      }
    }

    // gid must be a byte array of fixed size Const.DB_XIDDATASIZE
    public void Prepare(byte[] gid) {
      if (gid.Length != DbConst.DB_XIDDATASIZE) {
        string msg = string.Format("Size must be %d.", DbConst.DB_XIDDATASIZE);
        throw new ArgumentException(msg, "gid");
      }
      DbRetVal ret;
      lock (rscLock) {
        DB_TXN* txp = CheckDisposed();
        fixed (byte* gidp = gid) {
          ret = txp->Prepare(txp, gidp);
        }
      }
      Util.CheckRetVal(ret);
    }

    public void SetTimeout(int timeout, TimeoutKind flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_TXN* txp = CheckDisposed();
        ret = txp->SetTimeout(txp, unchecked((UInt32)timeout), unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

#if BDB_4_5_20

    public string Name {
      get {
        lock (rscLock) {
          DB_TXN* txp = CheckDisposed();
          byte* namep;
          DbRetVal ret = txp->GetName(txp, out namep);
          Util.CheckRetVal(ret);
          return Util.Utf8PtrToString(namep);
        }
      }
      set {
        DbRetVal ret;
        byte[] nameBytes = null;
        int count = Util.StrToUtf8(value, ref nameBytes);
        lock (rscLock) {
          DB_TXN* txp = CheckDisposed();
          fixed (byte* namep = nameBytes) {
            ret = txp->SetName(txp, namep);
          }
        }
        Util.CheckRetVal(ret);
      }
    }

#endif

    #endregion

    #region Nested Types

    [Flags]
    public enum BeginFlags: int
    {
      None = 0,
#if BDB_4_3_29
      Degree2 = DbConst.DB_DEGREE_2,
      DirtyRead = DbConst.DB_DIRTY_READ,
      NoSync = DbConst.DB_TXN_NOSYNC,
      NoWait = DbConst.DB_TXN_NOWAIT,
#endif
#if BDB_4_5_20
      ReadCommitted = DbConst.DB_READ_COMMITTED,
      ReadUncommitted = DbConst.DB_READ_UNCOMMITTED,
      NoSync = DbConst.DB_TXN_NOSYNC,
      NoWait = DbConst.DB_TXN_NOWAIT,
      Snapshot = DbConst.DB_TXN_SNAPSHOT,
#endif
      Sync = DbConst.DB_TXN_SYNC
    }

    public enum CommitMode: int
    {
      None = 0,
      NoSync = DbConst.DB_TXN_NOSYNC,
      Sync = DbConst.DB_TXN_SYNC
    }

    public enum CheckpointMode: int
    {
      None = 0,
      Force = DbConst.DB_FORCE
    }

    #endregion
  }

  // CLS compliant wrapper for DB_TXN_ACTIVE
  public struct ActiveTxn
  {
    internal DB_TXN_ACTIVE activeTxn;

    /* Transaction ID */
    public int TxnId {
      get { return unchecked((int)activeTxn.txnid); }
    }

    /* Transaction ID of parent */
    public int ParentId {
      get { return unchecked((int)activeTxn.parentid); }
    }

#if BDB_4_5_20
    /* Process owning txn ID */
    public int ProcessId {
      get { return activeTxn.pid; }
    }

    /* Thread owning txn ID */
    public int ThreadId {
      get { return unchecked((int)activeTxn.tid); }
    }
#endif

    /* LSN when transaction began */
    public Lsn Lsn {
      get { return new Lsn(activeTxn.lsn); }
    }

#if BDB_4_5_20
    /* Read LSN for MVCC */
    public Lsn ReadLsn {
      get { return new Lsn(activeTxn.read_lsn); }
    }

    /* MVCC reference count */
    public int MvccRefCount {
      get { return unchecked((int)activeTxn.mvcc_ref); }
    }

    /* Status of the transaction */
    public int Status {
      get { return unchecked((int)activeTxn.status); }
    }
#endif

    /* XA status */
    public int XaStatus {
      get { return unchecked((int)activeTxn.xa_status); }
    }

    /* XA global transaction ID */
    public unsafe byte[] XaId {
      get {
        byte[] result = new byte[DbConst.DB_XIDDATASIZE];
        fixed (byte* xidP = activeTxn.xid) {
          Marshal.Copy((IntPtr)xidP, result, 0, DbConst.DB_XIDDATASIZE);
        }
        return result;
      }
    }

#if BDB_4_5_20
    /* 50 bytes of name, nul termination */
    public unsafe string Name {
      get {
        string result;
        fixed (byte* namep = activeTxn.name) {
          result = Util.Utf8PtrToString(namep);
        }
        return result;
      }
    }
#endif
  }

  // CLS compliant wrapper for DB_PREPLIST
  public struct PreparedTxn
  {
    DB_PREPLIST prepList;

    // In order to avoid accessing an unmanaged pointer later on, we extract the GCHandle
    // for the Txn intance (from DB_TXN->api_internal) and store it instead of the DB_TXN*
    // pointer. Note: must be called *exactly* once right after prepList has been set
    // from unmanaged code.
    internal unsafe void FlipToGCHandle() {
      // flip IntPtr with DB_TXN*, assuming they have the same size
      IntPtr gcHandle = prepList.txn.txp->api_internal;
      prepList.txn.txnHandle = gcHandle;
    }

    // Based on the assumption that DB_TXN* has been converted to IntPtr GCHandle.
    // Must not be called before prepList has been assigned and ExtractGcHandle has been called.
    public Txn Txn {
      get {
        IntPtr th = prepList.txn.txnHandle;
        if (th == IntPtr.Zero)
          return null;
        else
          return (Txn)((GCHandle)th).Target;
      }
    }

    public unsafe byte[] Gid {
      get {
        byte[] result = new byte[DbConst.DB_XIDDATASIZE];
        fixed (byte* gidP = prepList.gid) {
          Marshal.Copy((IntPtr)gidP, result, 0, DbConst.DB_XIDDATASIZE);
        }
        return result;
      }
    }
  }

  // CLS compliant wrapper for DB_TXN_STAT
  public struct TxnStats
  {
    private DB_TXN_STAT txnStats;
    private ActiveTxn[] activeTxns;

    internal unsafe TxnStats(DB_TXN_STAT* sp) {
      txnStats = *sp;
      activeTxns = new ActiveTxn[txnStats.st_nactive];
      for (int indx = 0; indx < txnStats.st_nactive; indx++) {
        activeTxns[indx].activeTxn = *txnStats.st_txnarray;
        txnStats.st_txnarray++;
      }
    }

    /* lsn of the last checkpoint */
    public Lsn LastCheckpoint {
      get { return new Lsn(txnStats.st_last_ckp); }
    }

    /* time of last checkpoint */
#if _USE_32BIT_TIME_T
    public int LastCkpTime {
#else
    public long LastCkpTime {
#endif
      get { return txnStats.st_time_ckp; }
    }

    /* last transaction id given out */
    public int LastTxnId {
      get { return unchecked((int)txnStats.st_last_txnid); }
    }

    /* maximum txns possible */
    public int MaxTxns {
      get { return unchecked((int)txnStats.st_maxtxns); }
    }

    /* number of aborted transactions */
    public int NumAborts {
      get { return unchecked((int)txnStats.st_naborts); }
    }

    /* number of begun transactions */
    public int NumBegins {
      get { return unchecked((int)txnStats.st_nbegins); }
    }

    /* number of committed transactions */
    public int NumCommits {
      get { return unchecked((int)txnStats.st_ncommits); }
    }

    /* number of active transactions */
    public int NumActive {
      get { return unchecked((int)txnStats.st_nactive); }
    }

#if BDB_4_5_20
    /* number of snapshot transactions */
    public int NumSnapshots {
      get { return unchecked((int)txnStats.st_nsnapshot); }
    }
#endif

    /* number of restored transactions after recovery. */
    public int NumRestores {
      get { return unchecked((int)txnStats.st_nrestores); }
    }

    /* maximum active transactions */
    public int MaxNumActive {
      get { return unchecked((int)txnStats.st_maxnactive); }
    }

#if BDB_4_5_20
    /* maximum snapshot transactions */
    public int MaxNumSnapshots {
      get { return unchecked((int)txnStats.st_maxnsnapshot); }
    }
#endif

    /* array of active transactions */
    public ActiveTxn[] ActiveTxns {
      get { return activeTxns; }
    }

    /* Region lock granted after wait. */
    public int NumRegionWait {
      get { return unchecked((int)txnStats.st_region_wait); }
    }

    /* Region lock granted without wait. */
    public int NumRegionNoWait {
      get { return unchecked((int)txnStats.st_region_nowait); }
    }

    /* Region size. */
    public long RegionSize {
      get { return txnStats.st_regsize.ToInt64(); }
    }
  }
}
