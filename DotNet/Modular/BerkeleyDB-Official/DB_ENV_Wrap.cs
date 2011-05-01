/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.CompilerServices;

namespace BerkeleyDb
{
  /// <summary>Represents Berkeley DB environment.</summary>
  /// <remarks>Wraps a <see cref="DB_ENV"/> handle. Can be made thread-safe by specifying
  /// the <c>Env.OpenFlags.ThreadSafe</c> flag when opening the environment. However, as we
  /// synchronize all calls it does not appear necessary to specify this flag. It could be
  /// used as a safe-guard since it does not impact performance much.</remarks>
  public unsafe class Env: CriticalFinalizerObject, IDisposable
  {
    #region Unmanaged Resources

    protected internal readonly object rscLock = new object();
    DbRetVal releaseVal;

    // access to properly aligned types of size "native int" is atomic!
    volatile DB_ENV* evp = null;
    GCHandle instanceHandle;
    bool isOwnedByDb = false;

    byte* errpfx = null;
    Set<Txn> transactions = new Set<Txn>();
    Set<Db> databases = new Set<Db>();
    Set<LogCursor> logCursors = new Set<LogCursor>();
#if BDB_FULL_MPOOL_API
    Set<EnvCacheFile> cacheFiles = new Set<EnvCacheFile>();
#endif

    /*             Notes about dependencies and finalization
     * 
     * The proper order of closing resources to avoid Db recovery would be:
     * 1) Close all join cursors
     * 2) Close all other cursors
     * 3) Close all sequences
     * 4) Close all secondary databases
     * 5) Close all primary databases
     * 6) Close all root transactions (children are closed implicitly)
     * 7) Close environment
     * 
     * However, we are mostly concerned about resource leaks.
     * 
     * Now, since we have dependencies and therefore must ensure a proper order
     * of releasing resources, we must concentrate the release of a cluster of
     * dependent resources into *one* finalizer, as the CLR does not guarantee
     * the order in which finalizers are called.
     * 
     * So, our basic approach will be:
     * - Environment tracks transactions and databases
     * - Database tracks cursors and sequences
     * - All these resources are released in the proper order in the Environment's
     *   finalizer.
     */

    /*             Notes about resource protection
     * 
     * As we are using references to unmanaged structs most of the time,
     * we need to make sure these are properly released and that we do not
     * have unmanaged access violations.
     * 
     * The proper order of disposing/finalization described above should ensure
     * that we do not use a non-null reference to a disposed unmanaged struct.
     * 
     * The fact that the references to the unmanaged structs are properly aligned
     * types of size "native int" means that the CLI guarantees that access to
     * them is atomic. Additionally we have declared them as volatile, so that
     * their values are never cached, and we are synchronizing access to the
     * unmanaged references using the lock() statment in Dispose(bool disposing).
     * 
     * This should guarantee that once an unmanaged struct is disposed, the
     * reference to it is set to null atomically, and when accessing it again,
     * an ObjectDisposedException should be thrown. 
     */

    /*             Notes about synchronization
     *
     * We should always "lock" in the same order, which prevents deadlocks.
     * For instance, calling Txn.Commit() will first lock the environment,
     * and then lock the transaction. Calling Env.Dispose() will also first
     * lock the environment, and then eventually call Txn.Dispose(false), which
     * locks the transaction.
     * 
     * The lock order occurrences we have here:
     * 
     * Env:
     *   Env
     *   Env -> Txn
     *   Env -> Db
     * 
     * Txn:
     *   Txn
     *   Env -> Txn
     * 
     * Db:
     *   Db
     *   Env -> Db
     *   Txn -> Db
     *   Db -> Sequence
     *   Db -> DbcJoin
     *   Db -> DbcFile
     * 
     * Sequence:
     *   Sequence
     *   Txn -> Sequence
     *   Db -> Sequence
     *   Txn -> Db -> Sequence
     * 
     * Dbc:
     *   Dbc
     *   Db -> DbcJoin
     *   Db -> DbcJoin ->DbcFile
     *   Db -> DbcFile
     * 
     * It appears these do not allow for a cyclic dependency,
     * so we should not experience deadlocks.
     */

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    DbRetVal ReleaseUnmanagedResources() {
      DbRetVal ret = DbRetVal.SUCCESS;
      DB_ENV* evp = this.evp;
      if (evp == null)
        return ret;
      // DB_ENV->Close() could be a lengthy call, so we call Disposed() first, and the
      // CER ensures that we reach DB_ENV->Close() without external interruption.
      // This is OK because one must not use the handle after DB_ENV->Close() was called,
      // regardless of the return value.
      Disposed();
      if (!isOwnedByDb)
        ret = evp->Close(evp, 0);
      return ret;
    }

    // requires synchronization lock on rscLock
    internal bool RemoveTransaction(Txn txn) {
      return transactions.Remove(txn);
    }

    // requires synchronization lock on rscLock
    internal bool RemoveDatabase(Db db) {
      return databases.Remove(db);
    }

    // requires synchronization lock on rscLock
    internal bool RemoveLogCursor(LogCursor logc) {
      return logCursors.Remove(logc);
    }

#if BDB_FULL_MPOOL_API
    // requires synchronization lock on rscLock
    internal bool RemoveCacheFile(EnvCacheFile mpf) {
      return cacheFiles.Remove(mpf);
    }
#endif

    internal bool IsOwnedByDb {
      get { return isOwnedByDb; }
    }

    #endregion

    #region Construction, Finalization

    // called when creating wrapper for private (internal) environment of DB;
    internal Env() {
      // so that we can refer back to the Env instance
      instanceHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
    }

    // called when creating wrapper for private (internal) environment of DB;
    // call must be synchronized on rscLock; db.Evp must be != null, not checked;
    internal void SetOwnerDatabase(Db db) {
      evp = db.GetEvp();
      databases.Insert(db);
      isOwnedByDb = true;
      // so that callbacks can refer back to the DbEnv instance
      instanceHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
      evp->api_internal = (IntPtr)instanceHandle;
    }

    public Env(EnvCreateFlags flags): this() {
      DbRetVal ret;
      lock (rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DB_ENV* evp;
          ret = LibDb.db_env_create(out evp, flags);
          if (ret == DbRetVal.SUCCESS) {
            this.evp = evp;
            evp->api_internal = (IntPtr)instanceHandle;
          }
        }
      }
      Util.CheckRetVal(ret);
    }

    ~Env() {
      Dispose(false);
    }

    public const string disposedStr = "Environment handle closed.";

    [CLSCompliant(false)]
    protected DB_ENV* CheckDisposed() {
      // avoid multiple volatile memory access
      DB_ENV* evp = this.evp;
      if (evp == null)
        throw new ObjectDisposedException(disposedStr);
      return evp;
    }

    void Disposed() {
      evp = null;
    }

    public bool IsDisposed {
      get { return evp == null; }
    }

    void DisposeDependents(bool disposing) {
#if BDB_FULL_MPOOL_API
      // dispose memory pool file handles
      if (cacheFiles != null && cacheFiles.Count > 0) {
        int iter = cacheFiles.StartIter();
        while (cacheFiles.MoveNext(ref iter)) {
          EnvCacheFile mpf = cacheFiles.Get(iter);
          // mpf removes itself from cacheFiles
          mpf.Dispose(disposing);
          if (disposing)
            GC.SuppressFinalize(mpf);
        }
      }
#endif

      // dispose log cursors
      if (logCursors != null && logCursors.Count > 0) {
        int iter = logCursors.StartIter();
        while (logCursors.MoveNext(ref iter)) {
          LogCursor logc = logCursors.Get(iter);
          // logc removes itself from logCursors
          logc.Dispose(disposing);
          if (disposing)
            GC.SuppressFinalize(logc);
        }
      }

      // dispose cursors, needs to be done before aborting transactions
      if (databases != null && databases.Count > 0) {
        int iter = databases.StartIter();
        while (databases.MoveNext(ref iter)) {
          Db db = databases.Get(iter);
          db.DisposeCursors(disposing);
        }
      }

      // dispose transactions
      if (transactions != null && transactions.Count > 0) {
        int iter = transactions.StartIter();
        while (transactions.MoveNext(ref iter)) {
          Txn txn = transactions.Get(iter);
          // txn removes itself from transactions
          txn.Dispose(disposing);
          if (disposing) 
            GC.SuppressFinalize(txn);
        }
      }

      // dispose databases, we do not differentiate between primary and
      // secondary databases, as the internal BDB code seems to handle that
      if (databases != null && databases.Count > 0) {
        int iter = databases.StartIter();
        while (databases.MoveNext(ref iter)) {
          Db db = databases.Get(iter);
          // db removes itself from databases
          db.Dispose(disposing);
          if (disposing)
            GC.SuppressFinalize(db);
        }
      }

      if (errpfx != null) {
        LibDb.os_ufree(null, errpfx);
        errpfx = null;
      }
    }

    // when overriding, call base method at end (using finally clause)
    protected virtual void Dispose(bool disposing) {
      lock (rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DisposeDependents(disposing);
          releaseVal = ReleaseUnmanagedResources();
        }
        if (instanceHandle.IsAllocated)
          instanceHandle.Free();
      }
    }

    public DbRetVal ReleaseVal {
      get { return releaseVal; }
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #endregion

    #region Helpers

    // for internal use - make sure access is synchronized
    protected object callBackLock = new object();
    protected byte[] callBackBuffer1 = new byte[0];
    protected byte[] callBackBuffer2 = new byte[0];
    protected static byte[] utf8nl = new UTF8Encoding().GetBytes(Environment.NewLine);

    internal DB_ENV* Evp {
      get { return evp; }
    }

    // allocates (and re-allocates) external memory for errpfx
    internal unsafe byte* AllocateErrPfx(string value) {
      byte[] buffer = null;
      int count = Util.StrToUtf8(value, ref buffer);
      Util.BufferToPtr(buffer, 0, count, ref errpfx);
      return errpfx;
    }

    protected int GetTimeout(TimeoutKind flags) {
      UInt32 value;
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->GetTimeout(evp, out value, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
      return unchecked((int)value);
    }

    protected void SetTimeout(int timeout, TimeoutKind flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetTimeout(evp, unchecked((UInt32)timeout), unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

    #endregion

    #region Public Operations & Properties

    public void Close() {
      Dispose();
    }

    public void Open(string home, OpenFlags flags, int mode) {
      byte[] hBytes = null;
      Util.StrToUtf8(home, ref hBytes);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* hp = hBytes) {
          ret = evp->Open(evp, hp, unchecked((UInt32)flags), mode);
        }
      }
      Util.CheckRetVal(ret);
    }

    public string Home {
      get {
        byte* homep;
        byte[] homeBuf = null;
        int count;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          DbRetVal ret = evp->GetHome(evp, out homep);
          Util.CheckRetVal(ret);
          count = Util.PtrToBuffer(homep, ref homeBuf);
        }
        return new UTF8Encoding().GetString(homeBuf, 0, count);
      }
    }

    public OpenFlags EnvOpenFlags {
      get {
        UInt32 value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetOpenFlags(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((OpenFlags)value);
      }
    }

    public Db CreateDatabase(DbCreateFlags flags) {
      if (IsOwnedByDb)
        throw new InvalidOperationException("Must not use private environment.");
      Db db = new Db(this);
      DbRetVal ret;
      lock (rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DB_ENV* evp = CheckDisposed();
          ret = db.AllocateHandle(evp, flags);
          if (ret == DbRetVal.SUCCESS)
            databases.Insert(db);
        }
      }
      Util.CheckRetVal(ret);
      return db;
    }

    DbRetVal DbRemove(DB_TXN* txp, byte[] fBytes, byte[] dBytes, WriteFlags flags) {
      DB_ENV* evp = CheckDisposed();
      fixed (byte* fp = fBytes, dp = dBytes) {
        return evp->DbRemove(evp, txp, fp, dp, unchecked((UInt32)flags));
      }
    }

    public void DbRemove(Txn txn, string file, string database, WriteFlags flags) {
      byte[] fBytes = null;
      Util.StrToUtf8(file, ref fBytes);
      byte[] dBytes = null;
      Util.StrToUtf8(database, ref dBytes);

      DbRetVal ret;
      // lock environement first, to avoid deadlocks
      lock (rscLock) {
        if (txn != null) {
          lock (txn.rscLock) {
            DB_TXN* txp = txn.CheckDisposed();
            ret = DbRemove(txp, fBytes, dBytes, flags);
          }
        }
        else
          ret = DbRemove(null, fBytes, dBytes, flags);
      }
      Util.CheckRetVal(ret);
    }

    DbRetVal DbRename(DB_TXN* txp, byte[] fBytes, byte[] dBytes, byte[] nBytes, WriteFlags flags) {
      DB_ENV* evp = CheckDisposed();
      fixed (byte* fp = fBytes, dp = dBytes, np = nBytes) {
        return evp->DbRename(evp, txp, fp, dp, np, unchecked((UInt32)flags));
      }
    }

    public void DbRename(Txn txn, string file, string database, string newname, WriteFlags flags) {
      byte[] fBytes = null;
      Util.StrToUtf8(file, ref fBytes);
      byte[] dBytes = null;
      Util.StrToUtf8(database, ref dBytes);
      byte[] nBytes = null;
      Util.StrToUtf8(newname, ref nBytes);

      DbRetVal ret;
      // lock environement first, to avoid deadlocks
      lock (rscLock) {
        if (txn != null) {
          lock (txn.rscLock) {
            DB_TXN* txp = txn.CheckDisposed();
            ret = DbRename(txp, fBytes, dBytes, nBytes, flags);
          }
        }
        else
          ret = DbRename(null, fBytes, dBytes, nBytes, flags);
      }
      Util.CheckRetVal(ret);
    }

    public void Remove(string home, RemoveFlags flags) {
      byte[] hBytes = null;
      Util.StrToUtf8(home, ref hBytes);

      DbRetVal ret;
      lock (rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DB_ENV* evp = CheckDisposed();
          DisposeDependents(true);
          // DB_ENV->Remove() could be a lengthy call, so we call Disposed() first, and the
          // CER ensures that we reach DB_ENV->Remove() without external interruption.
          // This is OK because one must not use the handle after DB_ENV->Remove() was called,
          // regardless of the return value.
          Disposed();
          fixed (byte* hp = hBytes) {
            ret = evp->Remove(evp, hp, unchecked((UInt32)flags));
          }
        }
      }
      GC.SuppressFinalize(this);
      Util.CheckRetVal(ret);
    }

    // likely called from call-back - must not throw exception
    public void Error(int errno, string errMsg) {
      byte[] errBytes = null;
      try {
        Util.StrToUtf8(errMsg, ref errBytes);
      }
      catch { }
      fixed (byte* errStr = errBytes) {
        // don't lock, if the call-back comes from a different thread we might dead-lock
        DB_ENV* evp = this.evp;
        if (evp == null)
          return;
        evp->Err(evp, errno, errStr);
      }
    }

    // likely called from call-back - must not throw exception
    public void Error(string errMsg) {
      byte[] errBytes = null;
      try {
        Util.StrToUtf8(errMsg, ref errBytes);
      }
      catch { }
      fixed (byte* errStr = errBytes) {
        DB_ENV* evp = this.evp;
        if (evp == null)
          return;
        evp->Errx(evp, errStr);
      }
    }

#if BDB_4_5_20

    public void ThreadFailCheck() {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->FailChk(evp, 0);
      }
      Util.CheckRetVal(ret);
    }

    public void SetThreadCount(int value) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetThreadCount(evp, unchecked((UInt32)value));
      }
      Util.CheckRetVal(ret);
    }

    public void FileIdReset(string file, bool encrypt) {
      DbRetVal ret;
      byte[] fileBytes = null;
      Util.StrToUtf8(file, ref fileBytes);
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* fp = fileBytes) {
          ret = evp->FileIdReset(evp, fp, encrypt ? (UInt32)DbFlags.Encrypt : 0);
        }
      }
      Util.CheckRetVal(ret);
    }

    public void LsnReset(string file, bool encrypt) {
      DbRetVal ret;
      byte[] fileBytes = null;
      Util.StrToUtf8(file, ref fileBytes);
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* fp = fileBytes) {
          ret = evp->LsnReset(evp, fp, encrypt ? (UInt32)DbFlags.Encrypt : 0);
        }
      }
      Util.CheckRetVal(ret);
    }

#endif

    #region Transaction Related

    // must be called under an environment and parent lock
    DbRetVal TxnBegin(DB_TXN* parxp, Txn txn, Txn.BeginFlags flags) {
      DbRetVal ret;
      RuntimeHelpers.PrepareConstrainedRegions();
      try { }
      finally {
        DB_TXN* txp;
        DB_ENV* evp = CheckDisposed();
        ret = evp->TxnBegin(evp, parxp, out txp, unchecked((UInt32)flags));
        if (ret == DbRetVal.SUCCESS) {
          txn.Initialize(txp);
          transactions.Insert(txn);
        }
      }
      return ret;
    }

    public Txn TxnBegin(Txn parent, Txn.BeginFlags flags) {
      Txn txn = new Txn(this);
      DbRetVal ret;
      // lock environement first, to avoid deadlocks
      lock (rscLock) {
        if (parent != null) {
          lock (parent.rscLock) {
            DB_TXN* parxp = parent.CheckDisposed();
            ret = TxnBegin(parxp, txn, flags);
          }
        }
        else
          ret = TxnBegin(null, txn, flags);
      }
      Util.CheckRetVal(ret);
      return txn;
    }

    public void TxnCheckpoint(int kBytes, int minutes, Txn.CheckpointMode mode) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        unchecked {
          ret = evp->TxnCheckpoint(evp, (UInt32)kBytes, (UInt32)minutes, (UInt32)mode);
        }
      }
      Util.CheckRetVal(ret);
    }

    public int TxnTimeout {
      get { return GetTimeout(TimeoutKind.TxnTimeout); }
      set { SetTimeout(value, TimeoutKind.TxnTimeout); }
    }

    public int TxnMaxActive {
      get {
        UInt32 value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetTxMax(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetTxMax(evp, unchecked((UInt32)value));
        }
        Util.CheckRetVal(ret);
      }
    }

#if _USE_32BIT_TIME_T
    public int TxnTimestamp {
      get {
        int value;
#else
    public long TxnTimestamp {
      get {
        long value;
#endif
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetTxTimestamp(evp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetTxTimestamp(evp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public TxnStats GetTxnStats(StatFlags flags) {
      TxnStats value;
      DB_TXN_STAT* sp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->TxnStat(evp, out sp, unchecked((UInt32)flags));
          Util.CheckRetVal(ret);
          try {
            value = new TxnStats(sp);
          }
          finally {
            LibDb.os_ufree(null, sp);
          }
        }
      }
      return value;
    }

    public void PrintTxnStats(BerkeleyDb.StatPrintFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->TxnStatPrint(evp, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

    // returns number of array members filled in
    public int TxnRecover(PreparedTxn[] prepList, TxnRecoverMode mode) {
      int retCount = 0;
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (void* prepListP = prepList) {
          ret = evp->TxnRecover(evp, 
            (DB_PREPLIST*)prepListP, prepList.Length, out retCount, unchecked((UInt32)mode));
        }
      }
      Util.CheckRetVal(ret);
      // Convert DB_TXN* to IntPtr (GCHandle), to avoid access violations though
      // de-referencing unmanaged pointers after they have been released.
      for (int indx = 0; indx < retCount; indx++)
        prepList[indx].FlipToGCHandle();
      return retCount;
    }

    #endregion Transaction Related

    #region Logging Related

    DbRetVal OpenLogCursor(LogCursor logc, int flags) {
      DbRetVal ret;
      RuntimeHelpers.PrepareConstrainedRegions();
      try { }
      finally {
        DB_LOGC* logcp;
        DB_ENV* evp = CheckDisposed();
        ret = evp->LogCursor(evp, out logcp, unchecked((UInt32)flags));
        if (ret == DbRetVal.SUCCESS) {
          logc.Initialize(logcp);
          logCursors.Insert(logc);
        }
      }
      return ret;
    }

    public LogCursor OpenLogCursor() {
      LogCursor logc = new LogCursor(this);
      DbRetVal ret;
      // lock environement first, to avoid deadlocks
      lock (rscLock) {
        ret = OpenLogCursor(logc, 0);
      }
      Util.CheckRetVal(ret);
      return logc;
    }

    public int LogBufSize {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetLogBufSize(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetLogBufSize(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int MaxLogFileSize {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetMaxLogFileSize(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetMaxLogFileSize(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int MaxLogRegionSize {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetMaxLogRegionSize(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetMaxLogRegionSize(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public string LogDir {
      get {
        byte* dirp;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          DbRetVal ret = evp->GetLogDir(evp, out dirp);
          Util.CheckRetVal(ret);
          return Util.Utf8PtrToString(dirp);
        }
      }
      set {
        byte[] dirBytes = null;
        Util.StrToUtf8(value, ref dirBytes);
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          fixed (byte* dirp = dirBytes) {
            ret = evp->SetLogDir(evp, dirp);
          }
        }
        Util.CheckRetVal(ret);
      }
    }

#if BDB_4_5_20

      public int LogFileMode {
      get {
        int value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetLogFileMode(evp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetLogFileMode(evp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

#endif

    // according to the docs, log file names are usually 10 characters long
    public const int MaxLogFileNameLen = 64;

    public string LogFile(Lsn lsn) {
      byte* filep = stackalloc byte[MaxLogFileNameLen];
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        DbRetVal ret = evp->LogFile(evp, ref lsn.lsn, filep, MaxLogFileNameLen);
        Util.CheckRetVal(ret);
        return Util.Utf8PtrToString(filep);
      }
    }

    public List<string> LogArchive(LogArchiveFlags flags) {
      List<string> files = new List<string>();
      byte** filesp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->LogArchive(evp, out filesp, unchecked((UInt32)flags));
          Util.CheckRetVal(ret);
          if (filesp != null) {
            byte[] buffer = null;
            byte* filep;
            try {
              while ((filep = *filesp) != null) {
                files.Add(Util.Utf8PtrToString(filep, ref buffer));
                filesp++;
              }
            }
            finally {
              LibDb.os_ufree(null, filesp);
            }
          }
        }
      }
      return files;
    }

    public void LogFlush(Lsn? lsn) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        DB_LSN dbLsn = lsn.GetValueOrDefault().lsn;
        ret = evp->LogFlush(evp, lsn == null ? null : &dbLsn);
      }
      Util.CheckRetVal(ret);
    }

    public Lsn LogPut(ref DbEntry data, LogPutFlags flags) {
      DbRetVal ret;
      Lsn lsn;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->LogPut(evp, out lsn.lsn, ref data.dbt, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
      return lsn;
    }

#if BDB_4_5_20

    void LogPrintFile(DB_TXN* txp, byte[] msg) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* msgPtr = msg) {
          ret = evp->LogPrintFile(evp, txp, msgPtr);
        }
      }
      Util.CheckRetVal(ret);
    }

    public void LogPrintFile(Txn txn, string msg) {
      byte[] msgBytes = null;
      int count = Util.StrToUtf8(msg, ref msgBytes);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          LogPrintFile(txp, msgBytes);
        }
      }
      else
        LogPrintFile(null, msgBytes);
    }

#endif

    public LogStats GetLogStats(StatFlags flags) {
      LogStats value;
      DB_LOG_STAT* sp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->LogStat(evp, out sp, unchecked((UInt32)flags));
          Util.CheckRetVal(ret);
          value.logStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public void PrintLogStats(BerkeleyDb.StatPrintFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->LogStatPrint(evp, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

    #endregion Logging Related

    #region Locking Related

    public byte[,] LockConflicts {
      get {
        DbRetVal ret;
        byte* lckp = null;
        int modes = 0;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetLockConflicts(evp, ref lckp, ref modes);
        }
        Util.CheckRetVal(ret);
        byte[,] lcks = new byte[modes, modes];
        for (int i = 0; i < modes; i++)
          for (int j = 0; j < modes; j++)
            lcks[i, j] = *lckp++;  // or lcks[j, i] - depends on array layout
        return lcks;
      }
      set {
        DbRetVal ret;
        if (value.Rank != 2)
          throw new BdbException("Not a two-dimensional array.");
        int modes = value.GetLength(0);
        if (modes != value.GetLength(1))
          throw new BdbException("Not a square array.");
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          fixed (byte* lckp = value) {
            ret = evp->SetLockConflicts(evp, lckp, modes);
          }
        }
        Util.CheckRetVal(ret);
      }
    }

    public LockDetectMode LockDetectMode {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetLockDetect(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((LockDetectMode)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetLockDetect(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int MaxLockers {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetMaxLockers(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetMaxLockers(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int MaxLocks {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetMaxLocks(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetMaxLocks(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int MaxLockObjects {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetMaxObjects(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetMaxObjects(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int LockTimeout {
      get { return GetTimeout(TimeoutKind.LockTimeout); }
      set { SetTimeout(value, TimeoutKind.LockTimeout); }
    }

    public int DetectDeadLocks(LockDetectMode mode) {
      DbRetVal ret;
      int aborted = 0;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->LockDetect(evp, unchecked((UInt32)mode), 0, ref aborted);
      }
      Util.CheckRetVal(ret);
      return aborted;
    }

    private void ThrowLockException(DbRetVal ret, int failedIndx) {
      string errStr;
      if (ret > Util.BdbLowError)
        errStr = LibDb.db_strerror(ret);
      else if (ret > Util.DotNetLowError)
        errStr = Util.DotNetStr(ret);
      else
        errStr = Util.UnknownStr(ret);
      throw new LockException(ret, failedIndx, errStr);
    }

    public Lock AcquireLock(int locker, LockFlags flags, ref DbEntry obj, LockMode mode) {
      DbRetVal ret;
      Lock lck = new Lock();
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* objBufP = obj.Buffer) {
          obj.dbt.data = objBufP + obj.Start;
          ret = evp->LockGet(evp, unchecked((uint)locker), unchecked((uint)flags), ref obj.dbt, (DB_LOCKMODE)mode, ref lck.dblck);
        }
      }
      if (ret != DbRetVal.SUCCESS)
        ThrowLockException(ret, -1);
      return lck;
    }

    public void ReleaseLock(ref Lock lck) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->LockPut(evp, ref lck.dblck);
      }
      Util.CheckRetVal(ret);
    }

    public int AcquireLockerId() {
      DbRetVal ret;
      uint lid;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->LockId(evp, out lid);
      }
      Util.CheckRetVal(ret);
      return unchecked((int)lid);
    }

    public void ReleaseLockerId(int lid) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->LockIdFree(evp, unchecked((uint)lid));
      }
      Util.CheckRetVal(ret);
    }

    public LockStats GetLockStats(StatFlags flags) {
      LockStats value;
      DB_LOCK_STAT* sp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->LockStat(evp, out sp, unchecked((UInt32)flags));
          Util.CheckRetVal(ret);
          value.lockStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public void PrintLockStats(LockStatPrintFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->LockStatPrint(evp, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

    public void LockVector(int locker, LockFlags flags, LockRequest[] list) {
      DbRetVal ret; 
      int failedIndx;

      // allocate the arguments we need to pass
      DBT* objs = stackalloc DBT[list.Length];
      DB_LOCKREQ* lockReqs = stackalloc DB_LOCKREQ[list.Length];
      // calculate object buffer size and allocate on stack
      int objSize = 0;
      for (int indx = 0; indx < list.Length; indx++)
        objSize += list[indx].Obj.Size;
      byte* objBuf = stackalloc byte[objSize];

      // configure all DB_LOCKREQ instances
      DBT* currObjp = objs;
      byte* currObjBuf = objBuf;
      for (int indx = 0; indx < list.Length; indx++) {
        lockReqs[indx] = list[indx].PrepareLockReq(currObjp, ref currObjBuf);
        currObjp++;
      }

      // perform call into BDB
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        DB_LOCKREQ* lastp;
        ret = evp->LockVector(evp, unchecked((uint)locker), unchecked((UInt32)flags),
          lockReqs, list.Length, out lastp);
        if (ret != DbRetVal.SUCCESS)
          failedIndx = (int)(lastp - lockReqs);
        else
          failedIndx = list.Length;
      }

      // assign the acquired locks back to the initial LockRequest instances
      for (int indx = 0; indx < failedIndx; indx++) {
        LockOperation op = list[indx].Op;
        if (op == LockOperation.Acquire || op == LockOperation.AcquireTimeout)
          list[indx].lck = new Lock(lockReqs[indx].dblock);
      }

      if (ret != DbRetVal.SUCCESS)
        ThrowLockException(ret, failedIndx);
    }

#if BDB_4_5_20

    public Txn CdsGroupBegin() {
      Txn txn = new Txn(this);
      DbRetVal ret;
      lock (rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DB_TXN* txp;
          DB_ENV* evp = CheckDisposed();
          ret = evp->CdsGroupBegin(evp, out txp);
          if (ret == DbRetVal.SUCCESS) {
            txn.Initialize(txp);
            transactions.Insert(txn);
          }
        }
      }
      Util.CheckRetVal(ret);
      return txn;
    }

#endif

    #endregion Locking Related

    #region Memory Pool (Cache) Related

#if BDB_FULL_MPOOL_API

    DbRetVal CacheFileCreate(EnvCacheFile ecf, int flags) {
      DbRetVal ret;
      RuntimeHelpers.PrepareConstrainedRegions();
      try { }
      finally {
        DB_MPOOLFILE* mpf;
        DB_ENV* evp = CheckDisposed();
        ret = evp->MemPoolFileCreate(evp, out mpf, unchecked((UInt32)flags));
        if (ret == DbRetVal.SUCCESS) {
          ecf.mpf = mpf; ;
          cacheFiles.Insert(ecf);
        }
      }
      return ret;
    }

    public EnvCacheFile CacheFileCreate() {
      EnvCacheFile ecf = new EnvCacheFile(this);
      DbRetVal ret;
      // lock environement first, to avoid deadlocks
      lock (rscLock) {
        ret = CacheFileCreate(ecf, 0);
      }
      Util.CheckRetVal(ret);
      return ecf;
    }

    public delegate CallbackStatus PageInOutFcn(Env env, CachePage page, IntPtr cookie);
#if false // for alternate definition of page cookie
    public delegate CallbackStatus PageInOutFcn(Env env, CachePage page, ref DbEntry pgcookie);
#endif

    // keep delegates alive
    PageInOutFcn pageInCLS = null;
    PageInOutFcn pageOutCLS = null;
    DB_ENV.PageInOutFcn pageIn = null;
    DB_ENV.PageInOutFcn pageOut = null;

    void CacheRegister(int fType, DB_ENV.PageInOutFcn pageIn, DB_ENV.PageInOutFcn pageOut) {
      IntPtr pgIn = IntPtr.Zero;
      if (pageIn != null)
        pgIn = Marshal.GetFunctionPointerForDelegate(pageIn);
      IntPtr pgOut = IntPtr.Zero;
      if (pageOut != null)
        pgOut = Marshal.GetFunctionPointerForDelegate(pageOut);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->MemPoolRegister(evp, fType, pgIn, pgOut);
      }
      Util.CheckRetVal(ret);
    }

    static CallbackStatus PageInOutWrapCLS(Env env, UInt32 pgno, void* pgaddr, ref DBT pgcookie, PageInOutFcn pgInOut) {
      IntPtr cookie = IntPtr.Zero;
      // extract IntPtr from DBT
      int size = unchecked((int)pgcookie.size);
      if (size != 0) {
        if (size != sizeof(IntPtr))
          throw new BdbException("Invalid page cookie.");
        cookie = *(IntPtr*)pgcookie.data;
      }
      // call CLS compliant delegate - we assume it is not null
      return pgInOut(env, new CachePage(pgno, (IntPtr)pgaddr), cookie);
    }

#if false // for alternate definition of page cookie
    static CallbackStatus PageInOutWrapCLS(Env env, UInt32 pgno, void* pgaddr, ref DBT pgcookie, PageInOutFcn pgInOut) {
      // construct DbEntry for pgcookie
      int size = unchecked((int)pgcookie.size);
      // we are not using a shared buffer - the call-back might take
      // a long time and we do not want to lock the buffer that long
      byte[] buffer = new byte[size];
      Marshal.Copy((IntPtr)pgcookie.data, buffer, 0, size);
      DbEntry cookieEntry = DbEntry.InOut(buffer, 0, size);
      cookieEntry.dbt.flags = pgcookie.flags;
      cookieEntry.dbt.dlen = pgcookie.dlen;
      cookieEntry.dbt.doff = pgcookie.doff;
      // call CLS compliant delegate - we assume it is not null
      return pgInOut(env, new CachePage(pgno, (IntPtr)pgaddr), ref cookieEntry);
    }
#endif

    static DbRetVal PageInWrapCLS(DB_ENV* evp, UInt32 pgno, void* pgaddr, ref DBT pgcookie) {
      Env env = null;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        CallbackStatus cs = PageInOutWrapCLS(env, pgno, pgaddr, ref pgcookie, env.pageInCLS);
        return cs == CallbackStatus.Success ? DbRetVal.SUCCESS : DbRetVal.PAGE_IN_FAILED;
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "PageIn");
        return DbRetVal.PAGE_IN_FAILED;
      }
    }

    static DbRetVal PageOutWrapCLS(DB_ENV* evp, UInt32 pgno, void* pgaddr, ref DBT pgcookie) {
      Env env = null;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        CallbackStatus cs = PageInOutWrapCLS(env, pgno, pgaddr, ref pgcookie, env.pageOutCLS);
        return cs == CallbackStatus.Success ? DbRetVal.SUCCESS : DbRetVal.PAGE_OUT_FAILED;
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "PageOut");
        return DbRetVal.PAGE_OUT_FAILED;
      }
    }

    public void CacheRegister(int fileType, PageInOutFcn pageIn, PageInOutFcn pageOut) {
      DB_ENV.PageInOutFcn pgIn = null;
      if (pageIn != null)
        pgIn = PageInWrapCLS;
      DB_ENV.PageInOutFcn pgOut = null;
      if (pageOut != null)
        pgOut = PageOutWrapCLS;
      CacheRegister(fileType, pgIn, pgOut);
      pageInCLS = pageIn;
      pageOutCLS = pageOut;
      this.pageIn = pgIn;
      this.pageOut = pgOut;
    }

#endif

    public int CacheMMapSize {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetMpMMapSize(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetMpMMapSize(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int CacheMaxOpenFd {
      get {
        int value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetMpMaxOpenFd(evp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetMpMaxOpenFd(evp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public void GetCacheMaxWrite(out int maxWrite, out int maxWriteSleep) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->GetMpMaxWrite(evp, out maxWrite, out maxWriteSleep);
      }
      Util.CheckRetVal(ret);
    }

    public void SetCacheMaxWrite(int maxWrite, int maxWriteSleep) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetMpMaxWrite(evp, maxWrite, maxWriteSleep);
      }
      Util.CheckRetVal(ret);
    }

    public void CacheSync(Lsn? lsn) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        DB_LSN dbLsn = lsn.GetValueOrDefault().lsn;
        ret = evp->MemPoolSync(evp, lsn == null ? null : &dbLsn);
      }
      Util.CheckRetVal(ret);
    }

    public int CacheTrickle(int percent) {
      DbRetVal ret;
      int numPagesWritten;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->MemPoolTrickle(evp, percent, out numPagesWritten);
      }
      Util.CheckRetVal(ret);
      return numPagesWritten;
    }

    public CacheStats GetCacheStats(StatFlags flags) {
      CacheStats value;
      DB_MPOOL_STAT* sp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->MemPoolStat(evp, &sp, null, unchecked((UInt32)flags));
          Util.CheckRetVal(ret);
          value.cStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public CacheFileStats[] GetCacheFileStats(StatFlags flags) {
      CacheFileStats[] value = null;
      DB_MPOOL_FSTAT** fspp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->MemPoolStat(evp, null, &fspp, unchecked((UInt32)flags));
          Util.CheckRetVal(ret);
          if (fspp != null) {
            int count = 0;
            DB_MPOOL_FSTAT** tmpp = fspp;
            try {
              while (*(tmpp++) != null) count++;
              value = new CacheFileStats[count];
              for (int indx = 0; indx < value.Length; indx++) {
                value[indx] = new CacheFileStats(*fspp);
                fspp++;
              }
            }
            finally {
              LibDb.os_ufree(null, fspp);
            }
          }
        }
      }
      return value;
    }

    public void PrintCacheStats(CacheStatPrintFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->MemPoolStatPrint(evp, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

    #endregion Memory Pool Related

    #region Replication Related

    public void RepStart(ref DbEntry cdata, RepStartMode mode) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepStart(evp, ref cdata.dbt, unchecked((uint)mode));
      }
      Util.CheckRetVal(ret);
    }

#if BDB_4_3_29

    public int RepElect(int numSites, int numVotes, int priority, int timeout) {
      int envId;
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepElect(evp, numSites, numVotes, priority, unchecked((UInt32)timeout), out envId, 0);
      }
      Util.CheckRetVal(ret);
      return envId;
    }
    
#endif

#if BDB_4_5_20

    public int RepElect(int numSites, int numVotes) {
      int envId;
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepElect(evp, numSites, numVotes, out envId, 0);
      }
      Util.CheckRetVal(ret);
      return envId;
    }

#endif

    public RepStatus RepProcessMessage(ref DbEntry control, ref DbEntry rec, ref int envId, ref Lsn retLsn) {
      DbRetVal ret;
      DB_LSN lsn;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepProcessMessage(evp, ref control.dbt, ref rec.dbt, ref envId, out lsn);
      }
      switch (ret) {
        case DbRetVal.SUCCESS:
          break;
        case DbRetVal.REP_DUPMASTER:
          break;
        case DbRetVal.REP_HOLDELECTION:
          break;
#if BDB_4_5_20
        case DbRetVal.REP_IGNORE:
          break;
#endif
        case DbRetVal.REP_ISPERM:
          retLsn.lsn = lsn;
          break;
#if BDB_4_5_20
        case DbRetVal.REP_JOIN_FAILURE:
          break;
#endif
        case DbRetVal.REP_NEWMASTER:
          break;
        case DbRetVal.REP_NEWSITE:
          break;
        case DbRetVal.REP_NOTPERM:
          retLsn.lsn = lsn;
          break;
#if BDB_4_3_29
        case DbRetVal.REP_STARTUPDONE:
          break;
#endif
        default: 
          Util.CheckRetVal(ret);
          break;
      }
      return (RepStatus)ret;
    }

    public RepStats GetRepStats(StatFlags flags) {
      RepStats value;
      DB_REP_STAT* sp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->RepStat(evp, out sp, unchecked((UInt32)flags));
          Util.CheckRetVal(ret);
          value.repStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public void PrintRepStats(BerkeleyDb.StatPrintFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepStatPrint(evp, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

#if BDB_4_5_20

    public void RepSync() {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepSync(evp, 0);
      }
      Util.CheckRetVal(ret);
    }

    public void RepSetConfig(RepConfig which, bool onoff) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepSetConfig(evp, unchecked((uint)which), onoff ? 1 : 0);
      }
      Util.CheckRetVal(ret);
    }

    public bool RepGetConfig(RepConfig which) {
      DbRetVal ret;
      int onoff;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepGetConfig(evp, unchecked((uint)which), out onoff);
      }
      Util.CheckRetVal(ret);
      return onoff == 0 ? false : true;
    }

    public int RepNumSites {
      get {
        DbRetVal ret;
        int value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->RepGetNSites(evp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->RepSetNSites(evp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public int RepPriority {
      get {
        DbRetVal ret;
        int value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->RepGetPriority(evp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->RepSetPriority(evp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public void RepSetTimeout(RepTimeoutKind which, int timeout) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepSetTimeout(evp, unchecked((uint)which), unchecked((uint)timeout));
      }
      Util.CheckRetVal(ret);
    }

    public int RepGetTimeout(RepTimeoutKind which) {
      DbRetVal ret;
      uint timeout;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepGetTimeout(evp, unchecked((uint)which), out timeout);
      }
      Util.CheckRetVal(ret);
      return unchecked((int)timeout);
    }

#endif

    public DataSize RepLimit {
      get {
        DataSize value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
#if BDB_4_3_29
          ret = evp->GetRepLimit(evp, out value.gigaBytes, out value.bytes);
#endif
#if BDB_4_5_20
          ret = evp->RepGetLimit(evp, out value.gigaBytes, out value.bytes);
#endif
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
#if BDB_4_3_29
          ret = evp->SetRepLimit(evp, value.gigaBytes, value.bytes);
#endif
#if BDB_4_5_20
          ret = evp->RepSetLimit(evp, value.gigaBytes, value.bytes);
#endif
        }
        Util.CheckRetVal(ret);
      }
    }

    [CLSCompliant(false)]
    public delegate CallbackStatus RepSendFastFcn(Env env, ref DBT control, ref DBT rec, DB_LSN* lsnp, int envId, RepSendFlags flags);
    public delegate CallbackStatus RepSendFcn(Env env, ref DbEntry control, ref DbEntry rec, Lsn? lsn, int envId, RepSendFlags flags);

    // keep delegates alive
    RepSendFastFcn repSendFast = null;
    RepSendFcn repSendCLS = null;
    DB_ENV.RepSendFcn repSend = null;

    void RepSetTransport(int envId, DB_ENV.RepSendFcn value) {
      IntPtr repSnd = IntPtr.Zero;
      if (value != null)
        repSnd = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
#if BDB_4_3_29
        ret = evp->SetRepTransport(evp, envId, repSnd);
#endif
#if BDB_4_5_20
        ret = evp->RepSetTransport(evp, envId, repSnd);
#endif
      }
      Util.CheckRetVal(ret);
    }

    static DbRetVal RepSendWrapFast(
      DB_ENV* evp, 
      ref DBT control,
      ref DBT rec,
      DB_LSN* lsnp,
      int envid,
      uint flags)
    {
      Env env = null;
      try {
        env = Util.GetEnv(evp);
        CallbackStatus cs = env.repSendFast(
          env, ref control, ref rec, lsnp, envid, unchecked((RepSendFlags)flags));
        return cs == CallbackStatus.Success ? DbRetVal.SUCCESS : DbRetVal.REPSEND_FAILED;
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "RepSend");
        if (env != null)
          env.Error((int)DbRetVal.REPSEND_FAILED, ex.Message);
        else
          evp->Err(evp, (int)DbRetVal.REPSEND_FAILED, null);
        return DbRetVal.REPSEND_FAILED;
      }
    }

    static DbRetVal RepSendWrapCLS(
      DB_ENV* evp,
      ref DBT control,
      ref DBT rec,
      DB_LSN* lsnp,
      int envid,
      uint flags)
    {
      Env env = null;
      try {
        env = Util.GetEnv(evp);
        lock (env.callBackLock) {
          // construct DbEntry for control
          int size = unchecked((int)control.size);
          if (size > env.callBackBuffer1.Length)
            env.callBackBuffer1 = new byte[size];
          Marshal.Copy((IntPtr)control.data, env.callBackBuffer1, 0, size);
          DbEntry ctrlEntry = DbEntry.InOut(env.callBackBuffer1, 0, size);
          // construct DbEntry for rec
          size = unchecked((int)rec.size);
          if (size > env.callBackBuffer2.Length)
            env.callBackBuffer2 = new byte[size];
          Marshal.Copy((IntPtr)rec.data, env.callBackBuffer2, 0, size);
          DbEntry recEntry = DbEntry.InOut(env.callBackBuffer2, 0, size);
          // construct Lsn
          Lsn? lsn;
          if (lsnp == null) lsn = null; else lsn = new Lsn(*lsnp);
          // call CLS compliant delegate - we assume it is not null
          CallbackStatus cs = env.repSendCLS(
            env, ref ctrlEntry, ref recEntry, lsn, envid, unchecked((RepSendFlags)flags));
          return cs == CallbackStatus.Success ? DbRetVal.SUCCESS : DbRetVal.REPSEND_FAILED;
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "RepSend");
        if (env != null)
          env.Error((int)DbRetVal.REPSEND_FAILED, ex.Message);
        else
          evp->Err(evp, (int)DbRetVal.REPSEND_FAILED, null);
        return DbRetVal.REPSEND_FAILED;
      }
    }

    [CLSCompliant(false)]
#if BDB_4_3_29
    public void SetRepTransport(int envId, RepSendFastFcn repTransport) {
#endif
#if BDB_4_5_20
    public void RepSetTransport(int envId, RepSendFastFcn repTransport) {
#endif      
      repSendFast = repTransport;
      repSendCLS = null;
      repSend = RepSendWrapFast;
      RepSetTransport(envId, repSend);
    }

#if BDB_4_3_29
    public void SetRepTransport(int envId, RepSendFcn repTransport) {
#endif
#if BDB_4_5_20
    public void RepSetTransport(int envId, RepSendFcn repTransport) {
#endif      
      repSendFast = null;
      repSendCLS = repTransport;
      repSend = RepSendWrapCLS;
      RepSetTransport(envId, repSend);
    }

#if BDB_4_5_20

    public int RepMgrAddRemoteSite(string host, int port, RepSiteFlags flags) {
      byte[] hostBytes = null;
      Util.StrToUtf8(host, ref hostBytes);
      int envId;
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* hostPtr = hostBytes) {
          ret = evp->RepMgrAddRemoteSite(evp, hostPtr, unchecked((uint)port), out envId, unchecked((uint)flags));
        }
      }
      Util.CheckRetVal(ret);
      return envId;
    }

    public RepAckPolicy RepMgrAckPolicy {
      get {
        int value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->RepMgrGetAckPolicy(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((RepAckPolicy)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->RepMgrSetAckPolicy(evp, unchecked((int)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public void RepMgrSetLocalSite(string host, int port) {
      byte[] hostBytes = null;
      Util.StrToUtf8(host, ref hostBytes);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* hostPtr = hostBytes) {
          ret = evp->RepMgrSetLocalSite(evp, hostPtr, unchecked((uint)port), 0);
        }
      }
      Util.CheckRetVal(ret);
    }

    public RepMgrSite[] RepMgrSiteList() {
      RepMgrSite[] siteList;
      uint count;
      DB_REPMGR_SITE* rsp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->RepMgrSiteList(evp, out count, out rsp);
          Util.CheckRetVal(ret);
          try {
            siteList = new RepMgrSite[count];
            for (int indx = 0; indx < siteList.Length; indx++) {
              siteList[indx] = new RepMgrSite(rsp);
              rsp++;
            }
          }
          finally {
            LibDb.os_ufree(null, rsp);
          }
        }
      }
      return siteList;
    }

    public void RepMgrStart(int numThreads, RepStartFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->RepMgrStart(evp, numThreads, unchecked((uint)flags));
      }
      Util.CheckRetVal(ret);
    }

#endif

    #endregion Replication Related
    
    #region Mutex Related

#if BDB_4_3_29

    public int TasSpins {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetTasSpins(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetTasSpins(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }
    
#endif

#if BDB_4_5_20

    public int MutexAlloc(MutexAllocFlags flags) {
      DbRetVal ret;
      uint value;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->MutexAlloc(evp, unchecked((uint)flags), out value);
      }
      Util.CheckRetVal(ret);
      return unchecked((int)value);
    }

    public void MutexFree(int mutex) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->MutexFree(evp, unchecked((uint)mutex));
      }
      Util.CheckRetVal(ret);
    }

    public int MutexAlign {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->MutexGetAlign(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set { 
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->MutexSetAlign(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int MutexIncrement {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->MutexGetIncrement(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->MutexSetIncrement(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int MutexMax {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->MutexGetMax(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set { 
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->MutexSetMax(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int MutexTasSpins {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->MutexGetTasSpins(evp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->MutexSetTasSpins(evp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public void MutexLock(int mutex) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->MutexLock(evp, unchecked((uint)mutex));
      }
      Util.CheckRetVal(ret);
    }

    public MutexStats GetMutexStats(StatFlags flags) {
      MutexStats value;
      DB_MUTEX_STAT* sp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = evp->MutexStat(evp, out sp, unchecked((uint)flags));
          Util.CheckRetVal(ret);
          value.mtxStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public void MutexStatPrint(BerkeleyDb.StatPrintFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->MutexStatPrint(evp, unchecked((uint)flags));
      }
      Util.CheckRetVal(ret);
    }

    public void MutexUnlock(int mutex) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->MutexUnlock(evp, unchecked((uint)mutex));
      }
      Util.CheckRetVal(ret);
    }

#endif

    #endregion Mutex Related    

    #endregion

    #region Public General Configuration

    // TODO do we need the capability to set custom memory allocation functions for an environment

    public string ErrorPrefix {
      get {
        byte* errpfx;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          evp->GetErrPfx(evp, out errpfx);
          return Util.Utf8PtrToString(errpfx);
        }
      }
      set {
        lock (rscLock) {
          byte* errpfx = AllocateErrPfx(value);
          DB_ENV* evp = CheckDisposed();
          evp->SetErrPfx(evp, errpfx);
        }
      }
    }

    #region Error Callback

    [CLSCompliant(false)]
    public delegate void ErrCallFastFcn(Env env, byte* errpfx, byte* msg);
    public delegate void ErrCallFcn(Env env, string errpfx, string msg);

    // keep delegates alive
    object errCallProcessor = null;
    DB_ENV.ErrCallFcn errCall = null;

    void SetErrCall(DB_ENV.ErrCallFcn value) {
      IntPtr errCall = IntPtr.Zero;
      if (value != null)
        errCall = Marshal.GetFunctionPointerForDelegate(value);
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        evp->SetErrCall(evp, errCall);
      }
    }

    static void ErrCallWrapFast(DB_ENV* evp, byte* errpfx, byte* msg) {
      try {
        Env env = (Env)((GCHandle)evp->api_internal).Target;
        ((ErrCallFastFcn)env.errCallProcessor)(env, errpfx, msg);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Error Call");
      }
    }

    static void ErrCallWrapCLS(DB_ENV* evp, byte* errpfx, byte* msg) {
      try {
        Env env = (Env)((GCHandle)evp->api_internal).Target;
        UTF8Encoding utf8 = new UTF8Encoding();
        string prefix, message;
        lock (env.callBackLock) {
          // build msg string
          int size = Util.PtrToBuffer(msg, ref env.callBackBuffer1);
          message = utf8.GetString(env.callBackBuffer1, 0, size);

          // build errpfx string
          size = Util.PtrToBuffer(errpfx, ref env.callBackBuffer1);
          prefix = utf8.GetString(env.callBackBuffer1, 0, size);

          // call CLS compliant delegate - we assume it is not null
          ((ErrCallFcn)env.errCallProcessor)(env, prefix, message);
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Error Call");
      }
    }

    static void ErrCallWrapStream(DB_ENV* evp, byte* errpfx, byte* msg) {
      try {
        Env env = (Env)((GCHandle)evp->api_internal).Target;

        int pfxSize = Util.ByteStrLen(errpfx);
        int msgSize = Util.ByteStrLen(msg);
        // add necessary space for ": " and line break
        int size;
        if (pfxSize > 0)
          size = pfxSize + 2 + msgSize + utf8nl.Length;
        else
          size = msgSize + utf8nl.Length;

        lock (env.callBackLock) {
          // copy into byte buffer
          if (size > env.callBackBuffer1.Length)
            env.callBackBuffer1 = new byte[size];
          if (errpfx != null) {
            Marshal.Copy((IntPtr)errpfx, env.callBackBuffer1, 0, pfxSize);
            env.callBackBuffer1[pfxSize++] = (byte)':';
            env.callBackBuffer1[pfxSize++] = (byte)' ';
          }
          if (msg != null) {
            Marshal.Copy((IntPtr)msg, env.callBackBuffer1, pfxSize, msgSize);
            pfxSize += msgSize;
          }
          // add line break
          for (int indx = 0; indx < utf8nl.Length; indx++)
            env.callBackBuffer1[pfxSize++] = utf8nl[indx];

          // write to error stream
          ((Stream)env.errCallProcessor).Write(env.callBackBuffer1, 0, size);
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Error Call");
      }
    }

    // delegate should be thread-safe
    [CLSCompliant(false)]
    public ErrCallFastFcn ErrorCallFast {
      get { return errCallProcessor as ErrCallFastFcn; }
      set {
        DB_ENV.ErrCallFcn errCall = ErrCallWrapFast;
        SetErrCall(value == null ? null : errCall);
        errCallProcessor = value;
        this.errCall = errCall;
      }
    }

    // delegate should be thread-safe
    public ErrCallFcn ErrorCall {
      get { return errCallProcessor as ErrCallFcn; }
      set {
        DB_ENV.ErrCallFcn errCall = ErrCallWrapCLS;
        SetErrCall(value == null ? null : errCall);
        errCallProcessor = value;
        this.errCall = errCall;
      }
    }

    // TODO Document that ErrorStream replaces DB->set_errfile() from the C API.

    public Stream ErrorStream {
      get { return errCallProcessor as Stream; }
      set {
        DB_ENV.ErrCallFcn errCall = ErrCallWrapStream;
        SetErrCall(value == null ? null : errCall);
        errCallProcessor = value;
        this.errCall = errCall;
      }
    }

    #endregion Error Callback

    #region Message Callback

    [CLSCompliant(false)]
    public delegate void MsgCallFastFcn(Env env, byte* msg);
    public delegate void MsgCallFcn(Env env, string msg);

    // keep delegates alive
    object msgCallProcessor = null;
    DB_ENV.MsgCallFcn msgCall = null;

    void SetMsgCall(DB_ENV.MsgCallFcn value) {
      IntPtr msgCall = IntPtr.Zero;
      if (value != null)
        msgCall = Marshal.GetFunctionPointerForDelegate(value);
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        evp->SetMsgCall(evp, msgCall);
      }
    }

    static void MsgCallWrapFast(DB_ENV* evp, byte* msg) {
      try {
        Env env = (Env)((GCHandle)evp->api_internal).Target;
        ((MsgCallFastFcn)env.msgCallProcessor)(env, msg);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Message Call");
      }
    }

    static void MsgCallWrapCLS(DB_ENV* evp, byte* msg) {
      try {
        Env env = (Env)((GCHandle)evp->api_internal).Target;
        UTF8Encoding utf8 = new UTF8Encoding();
        string message;
        lock (env.callBackLock) {
          // build msg string
          int size = Util.PtrToBuffer(msg, ref env.callBackBuffer1);
          message = utf8.GetString(env.callBackBuffer1, 0, size);

          // call CLS compliant delegate - we assume it is not null
          ((MsgCallFcn)env.msgCallProcessor)(env, message);
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Message Call");
      }
    }

    static void MsgCallWrapStream(DB_ENV* evp, byte* msg) {
      try {
        Env env = (Env)((GCHandle)evp->api_internal).Target;

        int msgSize = Util.ByteStrLen(msg);
        // add necessary space for line break
        int size = msgSize + utf8nl.Length;
        lock (env.callBackLock) {
          // copy into byte buffer
          if (size > env.callBackBuffer1.Length)
            env.callBackBuffer1 = new byte[size];
          if (msg != null)
            Marshal.Copy((IntPtr)msg, env.callBackBuffer1, 0, msgSize);
          // add line break
          for (int indx = 0; indx < utf8nl.Length; indx++)
            env.callBackBuffer1[msgSize++] = utf8nl[indx];

          // write to message stream
          ((Stream)env.msgCallProcessor).Write(env.callBackBuffer1, 0, size);
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Message Call");
      }
    }

    // TODO Document that MessageStream replaces DB->set_msgfile() from the C API.

    // delegate should be thread-safe
    [CLSCompliant(false)]
    public MsgCallFastFcn MessageCallFast {
      get { return msgCallProcessor as MsgCallFastFcn; }
      set {
        DB_ENV.MsgCallFcn msgCall = MsgCallWrapFast;
        SetMsgCall(value == null ? null : msgCall);
        msgCallProcessor = value;
        this.msgCall = msgCall;
      }
    }

    // delegate should be thread-safe
    public MsgCallFcn MessageCall {
      get { return msgCallProcessor as MsgCallFcn; }
      set {
        DB_ENV.MsgCallFcn msgCall = MsgCallWrapCLS;
        SetMsgCall(value == null ? null : msgCall);
        msgCallProcessor = value;
        this.msgCall = msgCall;
      }
    }

    public Stream MessageStream {
      get { return msgCallProcessor as Stream; }
      set {
        DB_ENV.MsgCallFcn msgCall = MsgCallWrapStream;
        SetMsgCall(value == null ? null : msgCall);
        msgCallProcessor = value;
        this.msgCall = msgCall;
      }
    }

    #endregion Message Callback

    #region Feedback Call

    public delegate void FeedbackFcn(Env env, int opcode, int percent);

    // keep delegates alive
    FeedbackFcn feedbackCLS = null;
    DB_ENV.FeedbackFcn feedback = null;

    void SetFeedback(DB_ENV.FeedbackFcn value) {
      IntPtr fb = IntPtr.Zero;
      if (value != null)
        fb = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetFeedback(evp, fb);
      }
      Util.CheckRetVal(ret);
    }

    static void FeedbackWrapCLS(DB_ENV* evp, int opcode, int percent) {
      Env env = null;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        env.feedbackCLS(env, opcode, percent);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Feedback");
        if (env != null)
          env.Error((int)DbRetVal.FEEDBACK_FAILED, ex.Message);
        else
          evp->Err(evp, (int)DbRetVal.FEEDBACK_FAILED, null);
      }
    }

    // delegate should be thread-safe
    public FeedbackFcn FeedbackCall {
      get { return feedbackCLS; }
      set {
        DB_ENV.FeedbackFcn fdback = null;
        if (value != null)
          fdback = FeedbackWrapCLS;
        SetFeedback(fdback);
        feedbackCLS = value;
        feedback = fdback;
      }
    }

    #endregion Feedback Call

#if BDB_4_3_29

    #region Panic Call

    public delegate void PanicCallFcn(Env env, int errval);

    // keep delegates alive
    PanicCallFcn panicCallCLS = null;
    DB_ENV.PanicCallFcn panicCall = null;

    void SetPanicCall(DB_ENV.PanicCallFcn value) {
      IntPtr pc = IntPtr.Zero;
      if (value != null)
        pc = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetPanicCall(evp, pc);
      }
      Util.CheckRetVal(ret);
    }

    static void PanicCallWrapCLS(DB_ENV* evp, int errval) {
      Env env = null;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        env.panicCallCLS(env, errval);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Panic Call");
        if (env != null)
          env.Error((int)DbRetVal.PANICCALL_FAILED, ex.Message);
        else
          evp->Err(evp, (int)DbRetVal.PANICCALL_FAILED, null);
      }
    }

    // delegate should be thread-safe
    public PanicCallFcn PanicCall {
      get { return panicCallCLS; }
      set {
        DB_ENV.PanicCallFcn pCall = null;
        if (value != null)
          pCall = PanicCallWrapCLS;
        SetPanicCall(pCall);
        panicCallCLS = value;
        panicCall = pCall;
      }
    }

    #endregion Panic Call

#endif

    #region App Dispatch Call

    [CLSCompliant(false)]
    public delegate CallbackStatus AppRecoverFastFcn(Env env, ref DBT logrec, DB_LSN* lsnp, RecoveryOps op);
    public delegate CallbackStatus AppRecoverFcn(Env env, ref DbEntry logrec, Lsn? lsn, RecoveryOps op);

    // keep delegates alive
    AppRecoverFastFcn appRecoverFast = null;
    AppRecoverFcn appRecoverCLS = null;
    DB_ENV.AppRecoverFcn appRecover = null;

    void SetAppDispatch(DB_ENV.AppRecoverFcn value) {
      IntPtr txRec = IntPtr.Zero;
      if (value != null)
        txRec = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetAppDispatch(evp, txRec);
      }
      Util.CheckRetVal(ret);
    }

    static DbRetVal AppRecoverWrapFast(DB_ENV* evp, ref DBT logrec, DB_LSN* lsnp, DB_RECOPS op) {
      Env env = null;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        CallbackStatus cs = env.appRecoverFast(env, ref logrec, lsnp, (RecoveryOps)op);
        return cs == CallbackStatus.Success ? DbRetVal.SUCCESS : DbRetVal.APP_RECOVER_FAILED;
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "TxnRecover");
        return DbRetVal.APP_RECOVER_FAILED;
      }
    }

    static DbRetVal AppRecoverWrapCLS(DB_ENV* evp, ref DBT logrec, DB_LSN* lsnp, DB_RECOPS op) {
      Env env = null;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;

        // construct DbEntry for log_rec
        int size = unchecked((int)logrec.size);
        // we are not using a shared buffer - the call-back might take
        // a long time and we do not want to lock the buffer that long
        byte[] buffer = new byte[size];
        Marshal.Copy((IntPtr)logrec.data, buffer, 0, size);
        DbEntry logEntry = DbEntry.InOut(buffer, 0, size);
        logEntry.dbt.flags = logrec.flags;
        logEntry.dbt.dlen = logrec.dlen;
        logEntry.dbt.doff = logrec.doff;
        // construct Lsn
        Lsn? lsn;
        if (lsnp == null) lsn = null; else lsn = new Lsn(*lsnp);
        // call CLS compliant delegate - we assume it is not null
        CallbackStatus cs =  env.appRecoverCLS(env, ref logEntry, lsn, (RecoveryOps)op);
        return cs == CallbackStatus.Success ? DbRetVal.SUCCESS : DbRetVal.APP_RECOVER_FAILED;
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "TxnRecover");
        return DbRetVal.APP_RECOVER_FAILED;
      }
    }

    // delegate should be thread-safe
    [CLSCompliant(false)]
    public AppRecoverFastFcn AppRecoverFast {
      get { return appRecoverFast; }
      set {
        DB_ENV.AppRecoverFcn txRec = null;
        if (value != null)
          txRec = AppRecoverWrapFast;
        SetAppDispatch(txRec);
        appRecoverFast = value;
        appRecoverCLS = null;
        appRecover = txRec;
      }
    }

    // delegate should be thread-safe
    public AppRecoverFcn AppRecover {
      get { return appRecoverCLS; }
      set {
        DB_ENV.AppRecoverFcn txRec = null;
        if (value != null)
          txRec = AppRecoverWrapCLS;
        SetAppDispatch(txRec);
        appRecoverFast = null;
        appRecoverCLS = value;
        appRecover = txRec;
      }
    }

    #endregion App Dispatch Call

#if BDB_4_5_20

    #region Event Notification

    public delegate void EventNotifyFcn(Env env, EventType eventType, object eventInfo);

    // keep delegates alive
    EventNotifyFcn eventNotifyCLS = null;
    DB_ENV.EventNotifyFcn eventNotify = null;

    void SetEventNotify(DB_ENV.EventNotifyFcn value) {
      IntPtr en = IntPtr.Zero;
      if (value != null)
        en = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetEventNotify(evp, en);
      }
      Util.CheckRetVal(ret);
    }

    static void EventNotifyWrapCLS(DB_ENV* evp, UInt32 evnt, void* event_info) {
      Env env = null;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        object eventInfo = null;
        if (evnt == DbConst.DB_EVENT_REP_NEWMASTER)
          eventInfo = *((int*)event_info);
        env.eventNotifyCLS(env, (EventType)evnt, eventInfo);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Event Notification");
        if (env != null)
          env.Error((int)DbRetVal.EVENT_NOTIFY_FAILED, ex.Message);
        else
          evp->Err(evp, (int)DbRetVal.EVENT_NOTIFY_FAILED, null);
      }
    }

    // delegate should be thread-safe
    public void SetEventNotification(EventNotifyFcn value) {
      DB_ENV.EventNotifyFcn evNotify = null;
      if (value != null)
        evNotify = EventNotifyWrapCLS;
      SetEventNotify(evNotify);
      eventNotifyCLS = value;
      eventNotify = evNotify;
    }

    #endregion Event Notification

    #region IsAlive Callback

    public delegate bool IsAliveFcn(Env env, int pid, int tid, IsAliveFlags flags);

    // keep delegates alive
    IsAliveFcn isAliveCLS = null;
    DB_ENV.IsAliveFcn isAlive = null;

    void SetIsAlive(DB_ENV.IsAliveFcn value) {
      IntPtr ial = IntPtr.Zero;
      if (value != null)
        ial = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetIsAlive(evp, ial);
      }
      Util.CheckRetVal(ret);
    }

    static int IsAliveWrapCLS(DB_ENV* evp, int pid, UInt32 tid, UInt32 flags) {
      Env env = null;
      bool result;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        result = env.isAliveCLS(env, pid, unchecked((int)tid), unchecked((IsAliveFlags)flags));
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "IsAlive");
        if (env != null)
          env.Error((int)DbRetVal.ISALIVE_FAILED, ex.Message);
        else
          evp->Err(evp, (int)DbRetVal.ISALIVE_FAILED, null);
        result = false;
      }
      return result ? 1 : 0;
    }

    // delegate should be thread-safe
    public void SetIsAliveCall(IsAliveFcn value) {
      DB_ENV.IsAliveFcn ial = null;
      if (value != null)
        ial = IsAliveWrapCLS;
      SetIsAlive(ial);
      isAliveCLS = value;
      isAlive = ial;
    }

    #endregion

    #region ThreadId Callback

    public delegate void ThreadIdFcn(Env env, out int pid, out int tid);

    // keep delegates alive
    ThreadIdFcn threadIdCLS = null;
    DB_ENV.ThreadIdFcn threadId = null;

    void SetThreadId(DB_ENV.ThreadIdFcn value) {
      IntPtr ti = IntPtr.Zero;
      if (value != null)
        ti = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetThreadId(evp, ti);
      }
      Util.CheckRetVal(ret);
    }

    static void ThreadIdWrapCLS(DB_ENV* evp, out int pid, out UInt32 tid) {
      Env env = null;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        int tidCLS;
        env.threadIdCLS(env, out pid, out tidCLS);
        tid = unchecked((UInt32)tidCLS);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "ThreadId");
        if (env != null)
          env.Error((int)DbRetVal.THREADID_FAILED, ex.Message);
        else
          evp->Err(evp, (int)DbRetVal.THREADID_FAILED, null);
        pid = 0;
        tid = 0;
      }
    }

    // delegate should be thread-safe
    public void SetThreadIdCall(ThreadIdFcn value) {
      DB_ENV.ThreadIdFcn ti = null;
      if (value != null)
        ti = ThreadIdWrapCLS;
      SetThreadId(ti);
      threadIdCLS = value;
      threadId = ti;
    }

    #endregion ThreadId Callback

    #region ThreadIdString Callback

    public delegate string ThreadIdStringFcn(Env env, int pid, int tid);

    // keep delegates alive
    ThreadIdStringFcn threadIdStringCLS = null;
    DB_ENV.ThreadIdStringFcn threadIdString = null;

    void SetThreadIdString(DB_ENV.ThreadIdStringFcn value) {
      IntPtr ti = IntPtr.Zero;
      if (value != null)
        ti = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetThreadIdString(evp, ti);
      }
      Util.CheckRetVal(ret);
    }

    static byte* ThreadIdStringWrapCLS(DB_ENV* evp, int pid, UInt32 tid, byte* buf) {
      Env env = null;
      int count = 0;
      try {
        env = (Env)((GCHandle)evp->api_internal).Target;
        string tidStr = env.threadIdStringCLS(env, pid, unchecked((int)tid));
        fixed (char* tidChars = tidStr) {
          count = new UTF8Encoding().GetBytes(tidChars, tidStr.Length, buf, DbConst.DB_THREADID_STRLEN - 1);
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "ThreadIdString");
        if (env != null)
          env.Error((int)DbRetVal.THREADID_STRING_FAILED, ex.Message);
        else
          evp->Err(evp, (int)DbRetVal.THREADID_STRING_FAILED, null);
      }
      buf[count] = 0;
      return buf;
    }

    // delegate should be thread-safe
    public void SetThreadIdStringCall(ThreadIdStringFcn value) {
      DB_ENV.ThreadIdStringFcn tis = null;
      if (value != null)
        tis = ThreadIdStringWrapCLS;
      SetThreadIdString(tis);
      threadIdStringCLS = value;
      threadIdString = tis;
    }

    #endregion ThreadIdString Callback

#endif

    public void PrintStats(StatPrintFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->StatPrint(evp, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

    public EnvFlags GetFlags() {
      EnvFlags value;
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->GetFlags(evp, out value);
      }
      Util.CheckRetVal(ret);
      return value;
    }

    public void SetFlags(EnvFlags value, bool on) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetFlags(evp, value, on ? 1 : 0);
      }
      Util.CheckRetVal(ret);
    }

    public CacheSize CacheSize {
      get {
        CacheSize value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetCacheSize(evp, out value.gigaBytes, out value.bytes, out value.numCaches);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetCacheSize(evp, value.gigaBytes, value.bytes, value.numCaches);
        }
        Util.CheckRetVal(ret);
      }
    }

    public EncryptMode EncryptFlags {
      get {
        UInt32 value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetEncryptFlags(evp, out value);
        }
        Util.CheckRetVal(ret);
        return (EncryptMode)value;
      }
    }

    public void SetEncryption(string password, EncryptMode mode) {
      byte[] pwdBytes = null;
      Util.StrToUtf8(password, ref pwdBytes);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* pwd = pwdBytes) {
          ret = evp->SetEncrypt(evp, pwd, mode);
        }
      }
      Util.CheckRetVal(ret);
    }

    public List<string> GetDataDirs() {
      List<string> dirs = new List<string>();
      byte** dirsp;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        DbRetVal ret = evp->GetDataDirs(evp, out dirsp);
        Util.CheckRetVal(ret);
        byte[] buffer = null;
        byte* dirp;
        while ((dirp = *dirsp) != null) {
          dirs.Add(Util.Utf8PtrToString(dirp, ref buffer));
          dirsp++;
        }
      }
      return dirs;
    }

    public void SetDataDir(string value) {
      byte[] dirBytes = null;
      Util.StrToUtf8(value, ref dirBytes);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* dirp = dirBytes) {
          ret = evp->SetDataDir(evp, dirp);
        }
      }
      Util.CheckRetVal(ret);
    }

    public string TmpDir {
      get {
        byte* dirp;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          DbRetVal ret = evp->GetTmpDir(evp, out dirp);
          Util.CheckRetVal(ret);
          return Util.Utf8PtrToString(dirp);
        }
      }
      set {
        byte[] dirBytes = null;
        Util.StrToUtf8(value, ref dirBytes);
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          fixed (byte* dirp = dirBytes) {
            ret = evp->SetTmpDir(evp, dirp);
          }
        }
        Util.CheckRetVal(ret);
      }
    }

    public int SharedMemoryKey {
      get {
        int value;
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->GetShmKey(evp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_ENV* evp = CheckDisposed();
          ret = evp->SetShmKey(evp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public bool GetVerbose(DbVerb which) {
      int onoff;
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->GetVerbose(evp, unchecked((uint)which), out onoff);
      }
      Util.CheckRetVal(ret);
      return onoff != 0;
    }

    public void SetVerbose(DbVerb which, bool on) {
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        ret = evp->SetVerbose(evp, unchecked((uint)which), on ? 1 : 0);
      }
      Util.CheckRetVal(ret);
    }

    public void SetRpcServer(IntPtr client, string host, int clTimeout, int svTimeout) {
      byte[] hostBytes = null;
      Util.StrToUtf8(host, ref hostBytes);
      DbRetVal ret;
      lock (rscLock) {
        DB_ENV* evp = CheckDisposed();
        fixed (byte* hostp = hostBytes) {
          ret = evp->SetRpcServer(evp, (void*)client, hostp, clTimeout, svTimeout, 0);
        }
      }
      Util.CheckRetVal(ret);
    }

    #endregion

    #region Nested Types

    [Flags]
    public enum OpenFlags: int
    {
      None = 0,
      /* subsystem initialization */
#if BDB_4_3_29
      JoinEnv = DbConst.DB_JOINENV,
#endif
      InitCDB = DbConst.DB_INIT_CDB,
      InitLock = DbConst.DB_INIT_LOCK,
      InitLog = DbConst.DB_INIT_LOG,
      InitMPool = DbConst.DB_INIT_MPOOL,
      InitRep = DbConst.DB_INIT_REP,
      InitTxn = DbConst.DB_INIT_TXN,
      /* recovery */
      Recover = DbConst.DB_RECOVER,
      RecoverFatal = DbConst.DB_RECOVER_FATAL,  // must set DB_CREATE as well
      /* file naming */
      UseEnviron = DbConst.DB_USE_ENVIRON,
      UseEnvironRoot = DbConst.DB_USE_ENVIRON_ROOT,
      /* additional */
      Create = DbConst.DB_CREATE,
      LockDown = DbConst.DB_LOCKDOWN,
      Private = DbConst.DB_PRIVATE,
#if BDB_4_5_20
      Register = DbConst.DB_REGISTER,
#endif
      SystemMem = DbConst.DB_SYSTEM_MEM,
      ThreadSafe = DbConst.DB_THREAD,
    }

    [Flags]
    public enum WriteFlags: int
    {
      None = 0,
      AutoCommit = DbConst.DB_AUTO_COMMIT
    }

    [Flags]
    public enum RemoveFlags: int
    {
      None = 0,
      Force = DbConst.DB_FORCE,
      UseEnviron = DbConst.DB_USE_ENVIRON,
      UseEnvironRoot = DbConst.DB_USE_ENVIRON_ROOT
    }

    public enum RecoveryOps: int
    {
      Abort = DB_RECOPS.TXN_ABORT,
      Apply = DB_RECOPS.TXN_APPLY,
      BackwardRoll = DB_RECOPS.TXN_BACKWARD_ROLL,
      ForwardRoll = DB_RECOPS.TXN_FORWARD_ROLL,
      Print = DB_RECOPS.TXN_PRINT
    }

    public enum TxnRecoverMode: int
    {
      First = DbConst.DB_FIRST,
      Next = DbConst.DB_NEXT
    }

#if BDB_4_5_20

    public enum EventType: int
    {
      Panic = DbConst.DB_EVENT_PANIC,
      RepClient = DbConst.DB_EVENT_REP_CLIENT,
      RepMaster = DbConst.DB_EVENT_REP_MASTER,
      RepNewmaster = DbConst.DB_EVENT_REP_NEWMASTER,
      RepStartupDone = DbConst.DB_EVENT_REP_STARTUPDONE,
      WriteFailed = DbConst.DB_EVENT_WRITE_FAILED
    }

    public enum IsAliveFlags: int
    {
      None = 0,
      ProcessOnly = DbConst.DB_MUTEX_PROCESS_ONLY
    }

    public enum RepConfig: int
    {
      Bulk = DbConst.DB_REP_CONF_BULK,
      DelayClient = DbConst.DB_REP_CONF_DELAYCLIENT,
      NoAutoInit = DbConst.DB_REP_CONF_NOAUTOINIT,
      NoWait = DbConst.DB_REP_CONF_NOWAIT
    }

    public enum RepTimeoutKind: int
    {
      AckTimeout = DbConst.DB_REP_ACK_TIMEOUT,
      ElectionTimeout = DbConst.DB_REP_ELECTION_TIMEOUT,
      ElectionRetry = DbConst.DB_REP_ELECTION_RETRY,
      ConnectionRetry = DbConst.DB_REP_CONNECTION_RETRY
    }

    [Flags]
    public enum RepSiteFlags: int
    {
      None = 0,
      Peer = DbConst.DB_REPMGR_PEER
    }

    public enum RepAckPolicy: int
    {
      All = DbConst.DB_REPMGR_ACKS_ALL,
      AllPeers = DbConst.DB_REPMGR_ACKS_ALL_PEERS,
      None = DbConst.DB_REPMGR_ACKS_NONE,
      One = DbConst.DB_REPMGR_ACKS_ONE,
      OnePeer = DbConst.DB_REPMGR_ACKS_ONE_PEER,
      Quorum = DbConst.DB_REPMGR_ACKS_QUORUM
    }

    public enum RepStartFlags: int
    {
      Client = DbConst.DB_REP_CLIENT,
      Master = DbConst.DB_REP_MASTER,
      Election = DbConst.DB_REP_ELECTION,
      FullElection = DbConst.DB_REP_FULL_ELECTION,
    }

#endif

    [Flags]
    public enum StatPrintFlags: int
    {
      None = 0,
      All = DbConst.DB_STAT_ALL,
      Clear = DbConst.DB_STAT_CLEAR,
      SubSystem = DbConst.DB_STAT_SUBSYSTEM
    }

    public enum RepStartMode: int
    {
      Client = DbConst.DB_REP_CLIENT,
      Master = DbConst.DB_REP_MASTER
    }

    [Flags]
    public enum RepSendFlags: int
    {
      NoBuffer = DbConst.DB_REP_NOBUFFER,
      Permanent = DbConst.DB_REP_PERMANENT,
#if BDB_4_5_20
      Anywhere = DbConst.DB_REP_ANYWHERE,
      ReRequest = DbConst.DB_REP_REREQUEST
#endif
    }

    public enum RepStatus: int
    {
      Success = DbRetVal.SUCCESS,
      DupMaster = DbRetVal.REP_DUPMASTER,
      HoldElection = DbRetVal.REP_HOLDELECTION,
#if BDB_4_5_20
      Ignore = DbRetVal.REP_IGNORE,
#endif
      IsPerm = DbRetVal.REP_ISPERM,
#if BDB_4_5_20
      JoinFailure = DbRetVal.REP_JOIN_FAILURE,
#endif
      NewMaster = DbRetVal.REP_NEWMASTER,
      NewSite = DbRetVal.REP_NEWSITE,
      NotPerm = DbRetVal.REP_NOTPERM,
#if BDB_4_3_29
      StartupDone = DbRetVal.REP_STARTUPDONE
#endif
    }

    [Flags]
    public enum DbVerb: int
    {
      DeadLock = DbConst.DB_VERB_DEADLOCK,        /* Deadlock detection information. */
      Recovery = DbConst.DB_VERB_RECOVERY,        /* Recovery information. */
#if BDB_4_5_20
      Register = DbConst.DB_VERB_REGISTER,        /* DB_REGISTER support information. */
#endif
      Replication = DbConst.DB_VERB_REPLICATION,  /* Replication information. */
      WaitsFor = DbConst.DB_VERB_WAITSFOR         /* Dump waits-for table. */
    }

    // CLS compliant wrapper for DB_REP_STAT
    public struct RepStats
    {
      internal DB_REP_STAT repStats;

      /* Current replication status. */
      public int Status {
        get { return unchecked((int)repStats.st_status); }
      }

      /* Next LSN to use or expect. */
      public Lsn NextLsn {
        get { return new Lsn(repStats.st_next_lsn); }
      }

      /* LSN we're awaiting, if any. */
      public Lsn WaitingLsn {
        get { return new Lsn(repStats.st_waiting_lsn); }
      }

      /* Next pg we expect. (typedef  u_int32_t  db_pgno_t;) */
      public int NextPage {
        get { return unchecked((int)repStats.st_next_pg); }
      }

      /* pg we're awaiting, if any. (typedef  u_int32_t  db_pgno_t;) */
      public int WaitingPage {
        get { return unchecked((int)repStats.st_waiting_pg); }
      }

      /* # of times a duplicate master condition was detected.+ */
      public int DupMasters {
        get { return unchecked((int)repStats.st_dupmasters); }
      }

      /* Current environment ID. */
      public int EnvId {
        get { return repStats.st_env_id; }
      }

      /* Current environment priority. */
      public int EnvPriority {
        get { return repStats.st_env_priority; }
      }

#if BDB_4_5_20
      /* Bulk buffer fills. */
      public int BulkFills {
        get { return unchecked((int)repStats.st_bulk_fills); }
      }

      /* Bulk buffer overflows. */
      public int BulkOverflows {
        get { return unchecked((int)repStats.st_bulk_overflows); }
      }

      /* Bulk records stored. */
      public int BulkRecords {
        get { return unchecked((int)repStats.st_bulk_records); }
      }

      /* Transfers of bulk buffers. */
      public int BulkTransfers {
        get { return unchecked((int)repStats.st_bulk_transfers); }
      }

      /* Number of forced rerequests. */
      public int ClientReRequests {
        get { return unchecked((int)repStats.st_client_rerequests); }
      }

      /* Number of client service requests received by this client. */
      public int ClientSvcRequests {
        get { return unchecked((int)repStats.st_client_svc_req); }
      }

      /* Number of client service requests missing on this client. */
      public int ClientSvcReqsMissing {
        get { return unchecked((int)repStats.st_client_svc_miss); }
      }
#endif

      /* Current generation number. */
      public int CurGenNum {
        get { return unchecked((int)repStats.st_gen); }
      }

      /* Current election gen number. */
      public int CurElectionGenNum {
        get { return unchecked((int)repStats.st_egen); }
      }

      /* Log records received multiply.+ */
      public int LogDuplicated {
        get { return unchecked((int)repStats.st_log_duplicated); }
      }

      /* Log records currently queued.+ */
      public int LogQueued {
        get { return unchecked((int)repStats.st_log_queued); }
      }

      /* Max. log records queued at once.+ */
      public int LogQueuedMax {
        get { return unchecked((int)repStats.st_log_queued_max); }
      }

      /* Total # of log recs. ever queued.+ */
      public int LogQueuedTotal {
        get { return unchecked((int)repStats.st_log_queued_total); }
      }

      /* Log records received and put.+ */
      public int LogRecords {
        get { return unchecked((int)repStats.st_log_records); }
      }

      /* Log recs. missed and requested.+ */
      public int LogRequested {
        get { return unchecked((int)repStats.st_log_requested); }
      }

      /* Env. ID of the current master. */
      public int MasterId {
        get { return repStats.st_master; }
      }

      /* # of times we've switched masters. */
      public int MasterChanges {
        get { return unchecked((int)repStats.st_master_changes); }
      }

      /* Messages with a bad generation #.+ */
      public int MsgsBadGenNum {
        get { return unchecked((int)repStats.st_msgs_badgen); }
      }

      /* Messages received and processed.+ */
      public int MsgsProcessed {
        get { return unchecked((int)repStats.st_msgs_processed); }
      }

      /* Messages ignored because this site was a client in recovery.+ */
      public int MsgsWhileRecovering {
        get { return unchecked((int)repStats.st_msgs_recover); }
      }

      /* # of failed message sends.+ */
      public int MsgsSendFailed {
        get { return unchecked((int)repStats.st_msgs_send_failures); }
      }

      /* # of successful message sends.+ */
      public int MsgsSent {
        get { return unchecked((int)repStats.st_msgs_sent); }
      }

      /* # of NEWSITE msgs. received.+ */
      public int NewSites {
        get { return unchecked((int)repStats.st_newsites); }
      }

      /* Current number of sites we will assume during elections. */
      public int NumSites {
        get { return repStats.st_nsites; }
      }

      /* # of times we were throttled. */
      public int NumThrottles {
        get { return unchecked((int)repStats.st_nthrottles); }
      }

      /* # of times we detected and returned an OUTDATED condition.+ */
      public int NumOutdated {
        get { return unchecked((int)repStats.st_outdated); }
      }

      /* Pages received multiply.+ */
      public int PagesDuplicated {
        get { return unchecked((int)repStats.st_pg_duplicated); }
      }

      /* Pages received and stored.+ */
      public int PageRecords {
        get { return unchecked((int)repStats.st_pg_records); }
      }

      /* Pages missed and requested.+ */
      public int PagesRequested {
        get { return unchecked((int)repStats.st_pg_requested); }
      }

      /* Site completed client sync-up. */
      public int StartupComplete {
        get { return unchecked((int)repStats.st_startup_complete); }
      }

      /* # of transactions applied.+ */
      public int TxnsApplied {
        get { return unchecked((int)repStats.st_txns_applied); }
      }

      /* Elections generally. */

      /* # of elections held.+ */
      public int Elections {
        get { return unchecked((int)repStats.st_elections); }
      }

      /* # of elections won by this site.+ */
      public int ElectionsWon {
        get { return unchecked((int)repStats.st_elections_won); }
      }

      /* Statistics about an in-progress election. */

      /* Current front-runner. */
      public int ElectionCurWinner {
        get { return repStats.st_election_cur_winner; }
      }

      /* Election generation number. */
      public int ElectionGenNum {
        get { return unchecked((int)repStats.st_election_gen); }
      }

      /* Max. LSN of current winner. */
      public Lsn ElectionLsn {
        get { return new Lsn(repStats.st_election_lsn); }
      }

      /* # of "registered voters". */
      public int ElectionSites {
        get { return repStats.st_election_nsites; }
      }

      /* # of "registered voters" needed. */
      public int ElectionSitesNeeded {
        get { return repStats.st_election_nvotes; }
      }

      /* Current election priority. */
      public int ElectionPriority {
        get { return repStats.st_election_priority; }
      }

      /* Current election status. */
      public int ElectionStatus {
        get { return repStats.st_election_status; }
      }

      /* Election tiebreaker value. */
      public int ElectionTieBreaker {
        get { return unchecked((int)repStats.st_election_tiebreaker); }
      }

      /* Votes received in this round. */
      public int ElectionVotes {
        get { return repStats.st_election_votes; }
      }
#if BDB_4_5_20
      /* Last election time seconds. */
      public int ElectionSeconds {
        get { return unchecked((int)repStats.st_election_sec); }
      }

      /* Last election time useconds. */
      public int ElectionMicroSeconds {
        get { return unchecked((int)repStats.st_election_usec); }
      }
#endif
    }
    
#if BDB_4_5_20

    public enum MutexAllocFlags: int
    {
      None = 0,
      ProcessOnly = DbConst.DB_MUTEX_PROCESS_ONLY,
      SelfBlock = DbConst.DB_MUTEX_SELF_BLOCK
    }

    //  CLS compliant wrapper for DB_MUTEX_STAT
    public struct MutexStats
    {
      internal DB_MUTEX_STAT mtxStats;

      /* Mutex alignment */
      public int Alignment {
        get { return unchecked((int)mtxStats.st_mutex_align); }
      }

      /* Mutex test-and-set spins */
      public int TasSpins {
        get { return unchecked((int)mtxStats.st_mutex_tas_spins); }
      }

      /* Mutex count */
      public int Count {
        get { return unchecked((int)mtxStats.st_mutex_cnt); }
      }

      /* Available mutexes */
      public int Free {
        get { return unchecked((int)mtxStats.st_mutex_free); }
      }

      /* Mutexes in use */
      public int InUse {
        get { return unchecked((int)mtxStats.st_mutex_inuse); }
      }

      /* Maximum mutexes ever in use */
      public int MaxInUse {
        get { return unchecked((int)mtxStats.st_mutex_inuse_max); }
      }

      /* Region lock granted after wait. */
      public int RegionWaits {
        get { return unchecked((int)mtxStats.st_region_wait); }
      }
      
      /* Region lock granted without wait. */
      public int RegionNoWaits {
        get { return unchecked((int)mtxStats.st_region_nowait); }
      }
      
      /* Region size. (typedef uintptr_t roff_t;) */
      public long RegionSize {
        get { return mtxStats.st_regsize.ToInt64(); }
      }
    }

    public enum RepMgrSiteStatus: int
    {
      Connected = DbConst.DB_REPMGR_CONNECTED,
      Disconnected = DbConst.DB_REPMGR_DISCONNECTED
    }

    // CLS compliant wwrapper for DB_REPMGR_SITE
    public struct RepMgrSite
    {
      private DB_REPMGR_SITE repMgrSite;
      private string host;

      internal unsafe RepMgrSite(DB_REPMGR_SITE* rsp) {
        repMgrSite = *rsp;
        host = Util.Utf8PtrToString(repMgrSite.host);
      }

      public int EnvId {
        get { return repMgrSite.eid; }
      }

      public string Host {
        get { return host; }
      }

      public int Port {
        get { return unchecked((int)repMgrSite.port); }
      }

      public RepMgrSiteStatus Status {
        get { return unchecked((RepMgrSiteStatus)repMgrSite.status); }
      }
    }

#endif

    #endregion
  }
}
