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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace BerkeleyDb
{
  /// <summary>Represents Berkeley DB database. See
  /// <see href="http://www.sleepycat.com/docs/api_c/db_class.html"/>.</summary>
  /// <remarks>Wraps a <see cref="DB"/> handle. Can be made thread-safe by specifying
  /// the <c>Db.OpenFlags.ThreadSafe</c> flag when opening the database. However, as we
  /// synchronize all calls it does not appear necessary to specify this flag. It could be
  /// used as a safe-guard since it does not impact performance much.</remarks>
  public unsafe class Db: IDisposable
  {
    protected readonly Env env;
    DbFile dbf = null;
    bool noSync = false;

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
    volatile DB* dbp = null;
    GCHandle instanceHandle;

    Set<DbCursor> cursors = new Set<DbCursor>();
    Set<Sequence> sequences = new Set<Sequence>();

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    internal DbRetVal AllocateHandle(DB_ENV* evp, DbCreateFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp;
        ret = LibDb.db_create(out dbp, evp, flags);
        if (ret == DbRetVal.SUCCESS) {
          this.dbp = dbp;
          dbp->api_internal = (IntPtr)instanceHandle;
        }
      }
      return ret;
    }

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    DbRetVal ReleaseUnmanagedResources() {
      UInt32 flags = noSync ? unchecked((UInt32)DbConst.DB_NOSYNC) : 0;
      DB* dbp = this.dbp;
      if (dbp == null)
        return DbRetVal.SUCCESS;
      // DB->Close() could be a lengthy call, so we call Disposed() first, and the
      // CER ensures that we reach DB->Close() without external interruption.
      // This is OK because one must not use the handle after DB->Close() was called,
      // regardless of the return value.
      Disposed();
      DbRetVal ret = dbp->Close(dbp, flags);
      return ret;
    }

    // requires synchronization lock on rscLock
    internal bool InsertSequence(Sequence seq) {
      return sequences.Insert(seq);
    }

    // requires synchronization lock on rscLock
    internal bool RemoveSequence(Sequence seq) {
      return sequences.Remove(seq);
    }

    // requires synchronization lock on rscLock
    internal bool InsertCursor(DbCursor dbc) {
      return cursors.Insert(dbc);
    }

    // requires synchronization lock on rscLock
    internal bool RemoveCursor(DbCursor dbc) {
      return cursors.Remove(dbc);
    }

    #endregion

    #region Construction, Finalization

    // also called from environment
    internal Db(Env env) {
      this.env = env;
      // so that callbacks can refer back to the Db instance
      instanceHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
    }

    public Db(DbCreateFlags flags) {
      // Db will have private environment
      env = new Env();
      // so that callbacks can refer back to the Db instance
      instanceHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
      DbRetVal ret;
      // do not need to lock environment, as it is not publicly visible yet
      RuntimeHelpers.PrepareConstrainedRegions();
      try { }
      finally {
        ret = AllocateHandle(null, flags);
        if (ret == DbRetVal.SUCCESS)
          env.SetOwnerDatabase(this);
      }
      Util.CheckRetVal(ret);
    }

    public const string disposedStr = "Database handle closed.";

    [CLSCompliant(false)]
    protected internal DB* CheckDisposed() {
      // avoid multiple volatile memory access
      DB* dbp = this.dbp;
      if (dbp == null)
        throw new ObjectDisposedException(disposedStr);
      return dbp;
    }

    // does not check for dbp == null!
    void Disposed() {
      dbp = null;
      // unregister with resource manager
      env.RemoveDatabase(this);
    }

    public bool IsDisposed {
      get { return dbp == null; }
    }

    internal void DisposeCursors(bool disposing) {
      if (cursors != null) {
        int iter = cursors.StartIter();
        while (cursors.MoveNext(ref iter)) {
          DbCursor dbc = cursors.Get(iter);
          // dbc removes itself from cursors
          dbc.Dispose(disposing);
          if (disposing)
            GC.SuppressFinalize(dbc);
        }
      }
    }

    void DisposeDependents(bool disposing) {
      // release sequences
      if (sequences != null) {
        int iter = sequences.StartIter();
        while (sequences.MoveNext(ref iter)) {
          Sequence seq = sequences.Get(iter);
          // seq removes itself from sequences
          seq.Dispose(disposing);
          if (disposing)
            GC.SuppressFinalize(seq);
        }
      }
      // release cursors
      DisposeCursors(disposing);
    }

    // when overriding, call base method at end (using finally clause)
    internal protected virtual void Dispose(bool disposing) {
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
      // always lock environment first to avoid deadlocks
      lock (env.rscLock) {
        Dispose(true);
      }
      GC.SuppressFinalize(this);
    }

    #endregion

    #region Helpers

    // for synchronizing access to instance variables while in a callback
    protected internal object callBackLock = new object();
    // callback instance variables
    protected internal byte[] callBackBuffer1 = new byte[0];
    protected internal byte[] callBackBuffer2 = new byte[0];

    // for synchronize access to instance variables used in a call
    protected internal object callLock = new object();
    // instance variables used in Db calls
    protected internal byte[] callBuffer = new byte[0];

    internal DB_ENV* GetEvp() {
      return dbp->GetEnv(dbp);
    }

    #endregion

    #region Public Operations & Properties

    public Env GetEnv() {
      return env;
    }

    /// <summary>Do not flush cached information to disk when closing database.</summary>
    public bool NoSync {
      get { return noSync; }
      set { noSync = value; }
    }

    public bool IsSecondary {
      get {
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          return dbp->IsSecondary;
        }
      }
    }

    public DbFile Dbf {
      get { return dbf; }
    }

    DbFile Open(
      DB_TXN* txp,
      byte[] fBytes,
      byte[] dBytes,
      DbType type,
      OpenFlags flags,
      int mode)
    {
      lock (rscLock) {
        if (dbf != null)  // database already open
          return null;
        DB* dbp = CheckDisposed();
        DbRetVal ret;
        fixed (byte* fp = fBytes, dp = dBytes) {
          ret = dbp->Open(dbp, txp, fp, dp, type, unchecked((UInt32)flags), mode);
        }
        Util.CheckRetVal(ret);
        if (type == DbType.Unknown)
          type = GetDbType(dbp);
        switch (type) {
          case DbType.BTree:
            dbf = new DbBTree(this);
            break;
          case DbType.Hash:
            dbf = new DbHash(this);
            break;
          case DbType.Queue:
            dbf = new DbQueue(this);
            break;
          case DbType.Recno:
            dbf = new DbRecNo(this);
            break;
          default:
            throw new BdbException(
              string.Format("Unrecognized database type: '{0}'.", type.ToString()));
        }
        return dbf;
      }
    }

    public DbFile Open(
      Txn txn,
      string file,
      string database,
      DbType type,
      OpenFlags flags,
      int mode) 
    {
      byte[] fBytes = null;
      Util.StrToUtf8(file, ref fBytes);
      byte[] dBytes = null;
      Util.StrToUtf8(database, ref dBytes);

      DbFile result;
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          result = Open(txp, fBytes, dBytes, type, flags, mode);
        }
      }
      else
        result = Open(null, fBytes, dBytes, type, flags, mode);
      if (result == null)
        throw new BdbException(string.Format("Database already open: {0}.", file));
      return result;
    }

    public void Close() {
      Dispose();
    }

    // use for checking the type of an unknown open database
    [CLSCompliant(false)]
    public static DbType GetDbType(DB* dbp) {
      if (dbp == null)
        throw new ArgumentNullException("dbp");
      DbType result;
      DbRetVal ret = dbp->GetDbType(dbp, out result);
      Util.CheckRetVal(ret);
      return result;
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
        DB* dbp = this.dbp;
        if (dbp == null)
          return;
        dbp->Err(dbp, errno, errStr);
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
        // don't lock, if the call-back comes from a different thread we might dead-lock
        DB* dbp = this.dbp;
        if (dbp == null)
          return;
        dbp->Errx(dbp, errStr);
      }
    }

    DbCacheFile cacheFile = null;

    public DbCacheFile CacheFile {
      get {
        if (cacheFile == null) {
          DbRetVal ret;
          DB_MPOOLFILE* mpf;
          uint pageSize;
          DbCacheFile dbcf = new DbCacheFile(this);
          lock (rscLock) {
            DB* dbp = CheckDisposed();
#if BDB_4_3_29
            mpf = dbp->mpf;
#endif
#if BDB_4_5_20
            mpf = dbp->GetMpf(dbp);
#endif
            ret = dbp->GetPageSize(dbp, out pageSize);
          }
          Util.CheckRetVal(ret);
          dbcf.mpf = mpf;
          dbcf.pageSize = pageSize;
          cacheFile = dbcf;
        }
        return cacheFile;
      }
    }

    public void Remove(string file, string database) {
      byte[] fBytes = null;
      Util.StrToUtf8(file, ref fBytes);
      byte[] dBytes = null;
      Util.StrToUtf8(database, ref dBytes);
      
      
      DbRetVal ret;
      // always lock environment first to avoid deadlocks
      lock (env.rscLock) {
        lock (rscLock) {
          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DB* dbp = CheckDisposed();
            // DB->Remove() could be a lengthy call, so we call Disposed() first, and the
            // CER ensures that we reach DB->Remove() without external interruption.
            // This is OK because one must not use the handle after DB->Remove() was called,
            // regardless of the return value.
            Disposed();  // there are no dependents to be disposed of
            fixed (byte* fp = fBytes, dp = dBytes) {
              ret = dbp->Remove(dbp, fp, dp, 0);
            }
          }
        }
      }
      GC.SuppressFinalize(this);
      Util.CheckRetVal(ret);
    }

    public void Rename(string file, string database, string newName) {
      byte[] fBytes = null;
      Util.StrToUtf8(file, ref fBytes);
      byte[] dBytes = null;
      Util.StrToUtf8(database, ref dBytes);
      byte[] nBytes = null;
      Util.StrToUtf8(newName, ref nBytes);

      DbRetVal ret;
      // always lock environment first to avoid deadlocks
      lock (env.rscLock) {
        lock (rscLock) {
          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DB* dbp = CheckDisposed();
            // DB->Rename() could be a lengthy call, so we call Disposed() first, and the
            // CER ensures that we reach DB->Rename() without external interruption.
            // This is OK because one must not use the handle after DB->Rename() was called,
            // regardless of the return value.
            Disposed();  // there are no dependents to be disposed of
            fixed (byte* fp = fBytes, dp = dBytes, np = nBytes) {
              ret = dbp->Rename(dbp, fp, dp, np, 0);
            }
          }
        }
      }
      GC.SuppressFinalize(this);
      Util.CheckRetVal(ret);
    }

    // Maybe it is better to use the Upgrade utility instead.
    public void Upgrade(string file, UpgradeFlags flags) {
      byte[] fBytes = null;
      Util.StrToUtf8(file, ref fBytes);
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        fixed (byte* fp = fBytes) {
          ret = dbp->Upgrade(dbp, fp, (UInt32)flags);
        }
      }
      Util.CheckRetVal(ret);
    }

    #region Verify

    public delegate DbRetVal VerifyCallFcn(Db db, string arg);

    // keep stream & delegate alive
    Stream verifyStream = null;
    VerifyCallback verifyCall = null;

    static DbRetVal VerifyWrapStream(IntPtr handle, byte* arg) {
      try {
        GCHandle gch = (GCHandle)handle;
        Db db = (Db)gch.Target;

        int argSize = Util.ByteStrLen(arg);
        lock (db.callBackLock) {
          // copy into byte buffer
          if (argSize > db.callBackBuffer1.Length)
            db.callBackBuffer1 = new byte[argSize];
          if (arg != null)
            Marshal.Copy((IntPtr)arg, db.callBackBuffer1, 0, argSize);
          // write to verify stream
          db.verifyStream.Write(db.callBackBuffer1, 0, argSize);
        }
        return DbRetVal.SUCCESS;
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Verify Call");
        return DbRetVal.VERIFY_FAILED;
      }
    }

    public void Verify(string file, string database, Stream dumpStream, VerifyFlags flags) {
      byte[] fBytes = null;
      Util.StrToUtf8(file, ref fBytes);
      byte[] dBytes = null;
      Util.StrToUtf8(database, ref dBytes);
      IntPtr cb = IntPtr.Zero;

      DbRetVal ret;
      // always lock environment first to avoid deadlocks
      lock (env.rscLock) {
        lock (rscLock) {
          verifyStream = dumpStream;
          if (dumpStream != null) {
            verifyCall = VerifyWrapStream;
            cb = Marshal.GetFunctionPointerForDelegate(verifyCall);
          }
          else
            verifyCall = null;

          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DB* dbp = CheckDisposed();
            // LibDb.db_verify_internal() could be a lengthy call, so we call Disposed()
            // first, and the CER ensures that we reach LibDb.Verify() without external
            // interruption. This is OK because one must not use the handle after
            // LibDb.db_verify_internal() was called, regardless of the return value.
            Disposed();  // there are no dependents to be disposed of
            fixed (byte* fp = fBytes, dp = dBytes) {
              ret = LibDb.db_verify_internal(dbp, fp, dp, (IntPtr)instanceHandle, cb, unchecked((UInt32)flags));
            }
          }
          verifyStream = null;
        }
      }
      GC.SuppressFinalize(this);
      Util.CheckRetVal(ret);
    }

    #endregion Verify

    // TODO do we need the capability to set custom memory allocation functions for a database

    #endregion

    #region Public General Configuration

    public DbFlags GetFlags() {
      uint value;
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->GetFlags(dbp, out value);
      }
      Util.CheckRetVal(ret);
      return unchecked((DbFlags)value);
    }

    public void SetFlags(DbFlags value) {
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->SetFlags(dbp, unchecked((uint)value));
      }
      Util.CheckRetVal(ret);
    }

    public CacheSize CacheSize {
      get {
        CacheSize value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetCacheSize(dbp, out value.gigaBytes, out value.bytes, out value.numCaches);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetCacheSize(dbp, value.gigaBytes, value.bytes, value.numCaches);
        }
        Util.CheckRetVal(ret);
      }
    }

    public int PageSize {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetPageSize(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetPageSize(dbp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int LOrder {
      get {
        int value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetLOrder(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetLOrder(dbp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public EncryptMode EncryptFlags {
      get {
        UInt32 value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetEncryptFlags(dbp, out value);
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
        DB* dbp = CheckDisposed();
        fixed (byte* pwd = pwdBytes) {
          ret = dbp->SetEncrypt(dbp, pwd, (UInt32)mode);
        }
      }
      Util.CheckRetVal(ret);
    }

    #region DupCompare Callback

    [CLSCompliant(false)]
    public delegate int DupCompareFastFcn(Db db, ref DBT appData, ref DBT dbData);
    public delegate int DupCompareFcn(Db db, ref DbEntry appdata, ref DbEntry dbData);

    // keep delegates alive
    DupCompareFastFcn dupCompareFast = null;
    DupCompareFcn dupCompareCLS = null;
    DB.DupCompareFcn dupCompare = null;

    void SetDupCompare(DB.DupCompareFcn value) {
      IntPtr dupCmp = IntPtr.Zero;
      if (value != null)
        dupCmp = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->SetDupCompare(dbp, dupCmp);
      }
      Util.CheckRetVal(ret);
    }

    static int DupCompareWrapFast(DB* dbp, ref DBT appData, ref DBT dbData) {
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        return db.dupCompareFast(db, ref appData, ref dbData);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "DupCompare");
        if (db != null)
          db.Error((int)DbRetVal.DUPCOMP_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.DUPCOMP_FAILED, null);
        return 0;
      }
    }

    static int DupCompareWrapCLS(DB* dbp, ref DBT appData, ref DBT dbData) {
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        lock(db.callBackLock) {
          // construct DbEntry for appData
          int size = unchecked((int)appData.size);
          if (size > db.callBackBuffer1.Length)
            db.callBackBuffer1 = new byte[size];
          Marshal.Copy((IntPtr)appData.data, db.callBackBuffer1, 0, size);
          DbEntry appEntry = DbEntry.InOut(db.callBackBuffer1, 0, size);
          // appEntry.dbt.flags = appData->flags; // not used
          // appEntry.dbt.dlen = appData->dlen;   // not used
          // appEntry.dbt.doff = appData->doff;   // not used

          // construct DbEntry for dbData
          size = unchecked((int)dbData.size);
          if (size > db.callBackBuffer2.Length)
            db.callBackBuffer2 = new byte[size];
          Marshal.Copy((IntPtr)dbData.data, db.callBackBuffer2, 0, size);
          DbEntry dbEntry = DbEntry.InOut(db.callBackBuffer2, 0, size);
          // dbEntry.dbt.flags = dbData->flags; // not used
          // dbEntry.dbt.dlen = dbData->dlen;   // not used
          // dbEntry.dbt.doff = dbData->doff;   // not used

          // call CLS compliant delegate - we assume it is not null
          return db.dupCompareCLS(db, ref appEntry, ref dbEntry);
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "DupCompare");
        if (db != null)
          db.Error((int)DbRetVal.DUPCOMP_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.DUPCOMP_FAILED, null);
        return 0;
      }
    }

    // delegate should be thread-safe
    [CLSCompliant(false)]
    public DupCompareFastFcn DupCompareFast {
      get { return dupCompareFast; }
      set {
        DB.DupCompareFcn dupComp = null;
        if (value != null)
          dupComp = DupCompareWrapFast;
        SetDupCompare(dupComp);
        dupCompareFast = value;
        dupCompareCLS = null;
        dupCompare = dupComp;
      }
    }

    // delegate should be thread-safe
    public DupCompareFcn DupCompare {
      get { return dupCompareCLS; }
      set {
        DB.DupCompareFcn dupComp = null; 
        if (value != null)
          dupComp = DupCompareWrapCLS;
        SetDupCompare(dupComp);
        dupCompareFast = null;
        dupCompareCLS = value;
        dupCompare = dupComp;
      }
    }

    #endregion DupCompare Callback

    public string ErrorPrefix {
      get { return env.ErrorPrefix; }
      set { env.ErrorPrefix = value; }
    }

    [CLSCompliant(false)]
    public Env.ErrCallFastFcn ErrorCallFast {
      get { return env.ErrorCallFast; }
      set { env.ErrorCallFast = value; }
    }

    public Env.ErrCallFcn ErrorCall {
      get { return env.ErrorCall; }
      set { env.ErrorCall = value; }
    }

    public Stream ErrorStream {
      get { return env.ErrorStream; }
      set { env.ErrorStream = value; }
    }

    [CLSCompliant(false)]
    public Env.MsgCallFastFcn MessageCallFast {
      get { return env.MessageCallFast; }
      set { env.MessageCallFast = value; }
    }

    public Env.MsgCallFcn MessageCall {
      get { return env.MessageCall; }
      set { env.MessageCall = value; }
    }

    public Stream MessageStream {
      get { return env.MessageStream; }
      set { env.MessageStream = value; }
    }

    #region Feedback Call

    public delegate void FeedbackFcn(Db db, int opcode, int percent);

    // keep delegates alive
    FeedbackFcn feedbackCLS = null;
    DB.FeedbackFcn feedback = null;

    void SetFeedback(DB.FeedbackFcn value) {
      IntPtr fb = IntPtr.Zero;
      if (value != null)
        fb = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->SetFeedback(dbp, fb);
      }
      Util.CheckRetVal(ret);
    }

    static void FeedbackWrapCLS(DB* dbp, int opcode, int percent) {
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        db.feedbackCLS(db, opcode, percent);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "Feedback");
        if (db != null)
          db.Error((int)DbRetVal.FEEDBACK_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.FEEDBACK_FAILED, null);
      }
    }

    // delegate should be thread-safe
    public FeedbackFcn FeedbackCall {
      get { return feedbackCLS; }
      set {
        DB.FeedbackFcn fdback = null;
        if (value != null)
          fdback = FeedbackWrapCLS;
        SetFeedback(fdback);
        feedbackCLS = value;
        feedback = fdback;
      }
    }

    #endregion

#if BDB_4_3_29

    public Env.PanicCallFcn PanicCall {
      get { return env.PanicCall; }
      set { env.PanicCall = value; }
    }

#endif

    #endregion Public General Configuration

    #region Public BTree/RecNo Configuration

    #region BTreeCompare Callback

    [CLSCompliant(false)]
    public delegate int BtCompareFastFcn(Db db, ref DBT dbt1, ref DBT dbt2);
    public delegate int BtCompareFcn(Db db, ref DbEntry entry1, ref DbEntry entry2);

    // keep delegates alive
    BtCompareFastFcn btCompareFast = null;
    BtCompareFcn btCompareCLS = null;
    DB.BtCompareFcn btCompare = null;

    void SetBtCompare(DB.BtCompareFcn value) {
      IntPtr btCmp = IntPtr.Zero;
      if (value != null)
        btCmp = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->SetBtCompare(dbp, btCmp);
      }
      Util.CheckRetVal(ret);
    }

    static int BtCompareWrapFast(DB* dbp, ref DBT dbt1, ref DBT dbt2) {
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        return db.btCompareFast(db, ref dbt1, ref dbt2);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "BtCompare");
        if (db != null)
          db.Error((int)DbRetVal.BTCOMP_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.BTCOMP_FAILED, null);
        return 0;
      }
    }

    static int BtCompareWrapCLS(DB* dbp, ref DBT dbt1, ref DBT dbt2) {
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        lock(db.callBackLock) {
          // construct DbEntry for dbt1
          int size = unchecked((int)dbt1.size);
          if (size > db.callBackBuffer1.Length)
            db.callBackBuffer1 = new byte[size];
          Marshal.Copy((IntPtr)dbt1.data, db.callBackBuffer1, 0, size);
          DbEntry entry1 = DbEntry.InOut(db.callBackBuffer1, 0, size);

          // construct DbEntry for dbData
          size = unchecked((int)dbt2.size);
          if (size > db.callBackBuffer2.Length)
            db.callBackBuffer2 = new byte[size];
          Marshal.Copy((IntPtr)dbt2.data, db.callBackBuffer2, 0, size);
          DbEntry entry2 = DbEntry.InOut(db.callBackBuffer2, 0, size);

          // call CLS compliant delegate - we assume it is not null
          return db.btCompareCLS(db, ref entry1, ref entry2);
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "BtCompare");
        if (db != null)
          db.Error((int)DbRetVal.BTCOMP_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.BTCOMP_FAILED, null);
        return 0;
      }
    }

    // delegate should be thread-safe
    [CLSCompliant(false)]
    public BtCompareFastFcn BTreeCompareFast {
      get { return btCompareFast; }
      set {
        DB.BtCompareFcn btComp = null;
        if (value != null)
          btComp = BtCompareWrapFast;
        SetBtCompare(btComp);
        btCompareFast = value;
        btCompareCLS = null;
        btCompare = btComp;
      }
    }

    // delegate should be thread-safe
    public BtCompareFcn BTreeCompare {
      get { return btCompareCLS; }
      set {
        DB.BtCompareFcn btComp = null;
        if (value != null)
          btComp = BtCompareWrapCLS;
        SetBtCompare(btComp);
        btCompareFast = null;
        btCompareCLS = value;
        btCompare = btComp;
      }
    }

    #endregion BTreeCompare Callback

    #region BTreePrefix Callback

    [CLSCompliant(false)]
    public delegate uint BtPrefixFastFcn(Db db, ref DBT dbt1, ref DBT dbt2);
    public delegate int BtPrefixFcn(Db db, ref DbEntry entry1, ref DbEntry entry2);

    // keep delegates alive
    BtPrefixFastFcn btPrefixFast = null;
    BtPrefixFcn btPrefixCLS = null;
    DB.BtPrefixFcn btPrefix = null;

    void SetBtPrefix(DB.BtPrefixFcn value) {
      IntPtr btPrefix = IntPtr.Zero;
      if (value != null)
        btPrefix = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->SetBtPrefix(dbp, btPrefix);
      }
      Util.CheckRetVal(ret);
    }

    static uint BtPrefixWrapFast(DB* dbp, ref DBT dbt1, ref DBT dbt2) {
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        return db.btPrefixFast(db, ref dbt1, ref dbt2);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "BtPrefix");
        if (db != null)
          db.Error((int)DbRetVal.BTPREFIX_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.BTPREFIX_FAILED, null);
        return 0;
      }
    }

    static uint BtPrefixWrapCLS(DB* dbp, ref DBT dbt1, ref DBT dbt2) {
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        int ret;
        lock(db.callBackLock) {
          // construct DbEntry for dbt1
          DbEntry entry1;
          int size = unchecked((int)dbt1.size);
          if (size > db.callBackBuffer1.Length)
            db.callBackBuffer1 = new byte[size];
          if (dbt1.data != null && dbt1.size != 0) {
            Marshal.Copy((IntPtr)dbt1.data, db.callBackBuffer1, 0, size);
            entry1 = DbEntry.InOut(db.callBackBuffer1, 0, size);
          }
          else
            entry1 = DbEntry.InOut(db.callBackBuffer1, 0, 0);


          // construct DbEntry for dbData
          DbEntry entry2;
          size = unchecked((int)dbt2.size);
          if (size > db.callBackBuffer2.Length)
            db.callBackBuffer2 = new byte[size];
          if (dbt2.data != null && dbt2.size != 0) {
            Marshal.Copy((IntPtr)dbt2.data, db.callBackBuffer2, 0, size);
            entry2 = DbEntry.InOut(db.callBackBuffer2, 0, size);
          }
          else
            entry2 = DbEntry.InOut(db.callBackBuffer2, 0, 0);

          // call CLS compliant delegate - we assume it is not null
          ret = db.btPrefixCLS(db, ref entry1, ref entry2);
        }
        return unchecked((uint)ret);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "BtPrefix");
        if (db != null)
          db.Error((int)DbRetVal.BTPREFIX_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.BTPREFIX_FAILED, null);
        return 0;
      }
    }

    // delegate should be thread-safe
    [CLSCompliant(false)]
    public BtPrefixFastFcn BTreePrefixFast {
      get { return btPrefixFast; }
      set {
        DB.BtPrefixFcn btPrefix = null;
        if (value != null)
          btPrefix = BtPrefixWrapFast;
        SetBtPrefix(btPrefix);
        btPrefixFast = value;
        btPrefixCLS = null;
        this.btPrefix = btPrefix;
      }
    }

    // delegate should be thread-safe
    public BtPrefixFcn BTreePrefix {
      get { return btPrefixCLS; }
      set {
        DB.BtPrefixFcn btPrefix = null;
        if (value != null)
          btPrefix = BtPrefixWrapCLS;
        SetBtPrefix(btPrefix);
        btPrefixFast = null;
        btPrefixCLS = value;
        this.btPrefix = btPrefix;
      }
    }

    #endregion BTreePrefix Callback

    public int BTreeMinKey {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetBtMinKey(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetBtMinKey(dbp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

#if BDB_4_3_29

    public int BTreeMaxKey {
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetBtMaxKey(dbp, unchecked((UInt32)value));
        }
        Util.CheckRetVal(ret);
      }
    }

#endif

    #endregion

    #region Public RecNo/Queue Configuration

    #region AppendRecno Call

    [CLSCompliant(false)]
    public delegate AppendStatus AppendRecnoFastFcn(Db db, ref DBT data, UInt32 recno);
    public delegate AppendStatus AppendRecnoFcn(Db db, ref DbEntry data, int recno);

    // keep delegates alive
    AppendRecnoFastFcn appendRecnoFast = null;
    AppendRecnoFcn appendRecnoCLS = null;
    DB.AppendRecnoFcn appendRecno = null;

    void SetAppendRecno(DB.AppendRecnoFcn value) {
      IntPtr ar = IntPtr.Zero;
      if (value != null)
        ar = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->SetAppendRecno(dbp, ar);
      }
      Util.CheckRetVal(ret);
    }

    static DbRetVal AppendRecnoWrapFast(DB* dbp, ref DBT data, UInt32 recno) {
      try {
        Db db = Util.GetDb(dbp);
        AppendStatus aps = db.appendRecnoFast(db, ref data, recno);
        return aps == AppendStatus.Failure ? DbRetVal.APPEND_RECNO_FAILED : DbRetVal.SUCCESS;
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "AppendRecno");
        return DbRetVal.APPEND_RECNO_FAILED;
      }
    }

    static DbRetVal AppendRecnoWrapCLS(DB* dbp, ref DBT data, UInt32 recno) {
      try {
        Db db = Util.GetDb(dbp);
        DbEntry dataEntry;
        AppendStatus aps;
        lock(db.callBackLock) {
          int size = unchecked((int)data.size);
          if (size > db.callBackBuffer1.Length)
            db.callBackBuffer1 = new byte[size];
          Marshal.Copy((IntPtr)data.data, db.callBackBuffer1, 0, size);
          dataEntry = DbEntry.InOut(db.callBackBuffer1, 0, size);
          dataEntry.dbt.flags = data.flags;
          dataEntry.dbt.dlen = data.dlen;
          dataEntry.dbt.doff = data.doff;

          // call CLS compliant delegate - we assume it is not null
          aps = db.appendRecnoCLS(db, ref dataEntry, unchecked((int)recno));
        }
        switch (aps) {
          case AppendStatus.DataUnchanged:
            return DbRetVal.SUCCESS;
          case AppendStatus.Failure:
            return DbRetVal.APPEND_RECNO_FAILED;
          case AppendStatus.DataModified: {
              // copy result back
              void* retData = data.data;
              // if the return data are larger than the input data, allocate new buffer
              if (data.size <= dataEntry.dbt.size) {
                DbRetVal ret = LibDb.os_umalloc(null, dataEntry.dbt.size, out retData);
                if (ret != DbRetVal.SUCCESS)
                  return ret;
                data.data = retData;
                data.flags |= DBT.DB_DBT_APPMALLOC;  // tell LibDb to free that memory
              }
              Marshal.Copy(dataEntry.Buffer, dataEntry.Start, (IntPtr)retData, dataEntry.Size);
              return DbRetVal.SUCCESS;
            }
          default:
            return DbRetVal.APPEND_RECNO_FAILED;
        }
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "AppendRecno");
        return DbRetVal.APPEND_RECNO_FAILED;
      }
    }

    [CLSCompliant(false)]
    public AppendRecnoFastFcn AppendRecNoFast {
      get { return appendRecnoFast; }
      set {
        DB.AppendRecnoFcn apRecno = null;
        if (value != null)
          apRecno = AppendRecnoWrapFast;
        SetAppendRecno(apRecno);
        appendRecnoFast = value;
        appendRecnoCLS = null;
        appendRecno = apRecno;
      }
    }

    // delegate should be thread-safe
    public AppendRecnoFcn AppendRecno {
      get { return appendRecnoCLS; }
      set {
        DB.AppendRecnoFcn apRecno = null;
        if (value != null)
          apRecno = AppendRecnoWrapCLS;
        SetAppendRecno(apRecno);
        appendRecnoFast = null;
        appendRecnoCLS = value;
        appendRecno = apRecno;
      }
    }

    #endregion AppendRecno Call

    public int RecDelim {
      get {
        int value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetReDelim(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetReDelim(dbp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public int RecLen {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetReLen(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetReLen(dbp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int RecPad {
      get {
        int value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetRePad(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetRePad(dbp, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public string RecSource {
      get {
        string value;
        lock (rscLock) {
          byte* source;
          DB* dbp = CheckDisposed();
          DbRetVal ret = dbp->GetReSource(dbp, out source);
          Util.CheckRetVal(ret);
          value = Util.Utf8PtrToString(source);
        }
        return value;
      }
      set {
        byte[] sBytes = null;
        Util.StrToUtf8(value, ref sBytes);
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          fixed (byte* sp = sBytes) {
            ret = dbp->SetReSource(dbp, sp);
          }
        }
        Util.CheckRetVal(ret);
      }
    }

    public int QueueExtentSize {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetQExtentSize(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetQExtentSize(dbp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    #endregion

    #region Public Hash Configuration

    #region Hash Callback

    [CLSCompliant(false)]
    public delegate UInt32 HHashFastFcn(Db db, byte* bytes, UInt32 length);
    public delegate int HHashFcn(Db db, byte[] bytes, int length);

    // keep delegates alive
    HHashFastFcn hHashFast = null;
    HHashFcn hHashCLS = null;
    DB.HHashFcn hHash = null;

    void SetHHash(DB.HHashFcn value) {
      IntPtr hHash = IntPtr.Zero;
      if (value != null)
        hHash = Marshal.GetFunctionPointerForDelegate(value);
      DbRetVal ret;
      lock (rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->SetHHash(dbp, hHash);
      }
      Util.CheckRetVal(ret);
    }

    static uint HHashWrapFast(DB* dbp, void* bytes, UInt32 length) {
      if (bytes == null || length == 0)
        return 0;
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        return db.hHashFast(db, (byte*)bytes, length);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "HHash");
        if (db != null)
          db.Error((int)DbRetVal.HHASH_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.HHASH_FAILED, null);
        return 0;
      }
    }

    static uint HHashWrapCLS(DB* dbp, void* bytes, UInt32 length) {
      if (bytes == null || length == 0)
        return 0;
      Db db = null;
      try {
        db = Util.GetDb(dbp);
        // copy to buffer
        int hashValue;
        lock(db.callBackLock) {
          if (length > db.callBackBuffer1.Length)
            db.callBackBuffer1 = new byte[(int)length];
          Marshal.Copy((IntPtr)bytes, db.callBackBuffer1, 0, (int)length);
          // call CLS compliant delegate - we assume it is not null
          hashValue = db.hHashCLS(db, db.callBackBuffer1, (int)length);
        }
        return unchecked((uint)hashValue);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "HHash");
        if (db != null)
          db.Error((int)DbRetVal.HHASH_FAILED, ex.Message);
        else
          dbp->Err(dbp, (int)DbRetVal.HHASH_FAILED, null);
        return 0;
      }
    }

    // delegate should be thread-safe
    [CLSCompliant(false)]
    public HHashFastFcn HashFuncFast {
      get { return hHashFast; }
      set {
        DB.HHashFcn hh = null;
        if (value != null)
          hh = HHashWrapFast;
        SetHHash(hh);
        hHashFast = value;
        hHashCLS = null;
        hHash = hh;
      }
    }

    // delegate should be thread-safe
    public HHashFcn HashFunc {
      get { return hHashCLS; }
      set {
        DB.HHashFcn hh = null;
        if (value != null)
          hh = HHashWrapCLS;
        SetHHash(hh);
        hHashFast = null;
        hHashCLS = value;
        hHash = hh;
      }
    }

    #endregion Hash Callback

    public int HashFillFactor {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetHFFactor(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetHFFactor(dbp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public int HashNumElements {
      get {
        uint value;
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetHNelem(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->SetHNelem(dbp, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    #endregion

    #region Nested Types

    [Flags]
    public enum OpenFlags: int
    {
      None = 0,
      AutoCommit = DbConst.DB_AUTO_COMMIT,
      Create = DbConst.DB_CREATE,
      Exclusive = DbConst.DB_EXCL,
#if BDB_4_5_20
      MultiVersion = DbConst.DB_MULTIVERSION,
#endif
      NoMemoryMap = DbConst.DB_NOMMAP,
      ReadOnly = DbConst.DB_RDONLY,
#if BDB_4_3_29
      DirtyRead = DbConst.DB_DIRTY_READ,
#endif
#if BDB_4_5_20
      ReadUncommitted = DbConst.DB_READ_UNCOMMITTED,
#endif
      ThreadSafe = DbConst.DB_THREAD,
      Truncate = DbConst.DB_TRUNCATE,
    }

    public enum JoinMode: int
    {
      None = 0,
      NoSort = DbConst.DB_JOIN_NOSORT
    }

    [Flags]
    public enum UpgradeFlags: int
    {
      None = 0,
      DupSort = DbFlags.DupSort
    }

    [Flags]
    public enum VerifyFlags: int
    {
      None = 0,
      DbSalvage = DbConst.DB_SALVAGE,
      Aggressive = DbConst.DB_AGGRESSIVE,
      NoOrderChk = DbConst.DB_NOORDERCHK,
      OrderChkOnly = DbConst.DB_ORDERCHKONLY,
      Printable = DbConst.DB_PRINTABLE,
    }

    public enum AppendStatus
    {
      DataModified = DbRetVal.SUCCESS,
      DataUnchanged,
      Failure
    }

    /// <summary>Abstract base class for Berkeley DB databases when opened for file access.</summary>
    public abstract unsafe class DbFileBase
    {
      protected static DbType type = DbType.Unknown;

      protected readonly Db db;

      protected DbFileBase(Db db) {
        this.db = db;
      }

      [CLSCompliant(false)]
      protected DB* CheckDisposed() {
        return db.CheckDisposed();
      }

      [CLSCompliant(false)]
      protected DB* dbp {
        get { return db.dbp; }
      }

      protected bool InsertSequence(Sequence seq) {
        return db.InsertSequence(seq);
      }

      protected bool InsertCursor(DbCursor dbc) {
        return db.InsertCursor(dbc);
      }

      public Db GetDb() {
        return db;
      }
    }

    #endregion
  }

  /// <summary>Intermediate base class for Berkeley DB databases when opened for file access.</summary>
  public abstract unsafe class DbFile: Db.DbFileBase
  {
    // store delegates for frequently used function pointer calls
    [CLSCompliant(false)]
    protected DB.GetFcn DbGet;
    [CLSCompliant(false)]
    protected DB.PGetFcn DbPGet;
    [CLSCompliant(false)]
    protected DB.PutFcn DbPut;
    [CLSCompliant(false)]
    protected DB.DelFcn DbDel;
    [CLSCompliant(false)]
    protected DB.KeyRangeFcn DbKeyRange;

    // initialize function pointer delegates
    protected void InitDelegates() {
      DbGet = dbp->Get;
      DbPGet = dbp->PGet;
      DbPut = dbp->Put;
      DbDel = dbp->Del;
      DbKeyRange = dbp->KeyRange;
    }

    internal DbFile(Db db) : base(db) {
      InitDelegates();
    }

    #region Helpers

    [CLSCompliant(false)]
    protected ReadStatus Get(DB_TXN* txp, ref DbEntry key, ref DbEntry data, uint flags) {
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer, dataBufP = data.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          data.dbt.data = dataBufP + data.Start;
          ret = DbGet(dbp, txp, ref key.dbt, ref data.dbt, flags);
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
      data.SetReturnType(type, flags);
      return (ReadStatus)ret;
    }

    [CLSCompliant(false)]
    protected ReadStatus PGet(DB_TXN* txp, ref DbEntry key, ref DbEntry pkey, ref DbEntry data, uint flags) {
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer, pkeyBufP = pkey.Buffer, dataBufP = data.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          pkey.dbt.data = pkeyBufP + pkey.Start;
          data.dbt.data = dataBufP + data.Start;
          ret = DbPGet(dbp, txp, ref key.dbt, ref pkey.dbt, ref data.dbt, flags);
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
      data.SetReturnType(type, flags);
      return (ReadStatus)ret;
    }

    [CLSCompliant(false)]
    protected WriteStatus Put(DB_TXN* txp, ref DbEntry key, ref DbEntry data, uint flags) {
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer, dataBufP = data.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          data.dbt.data = dataBufP + data.Start;
          ret = DbPut(dbp, txp, ref key.dbt, ref data.dbt, flags);
        }
      }
      switch (ret) {
        case DbRetVal.NOTFOUND: break;  // should not happen, according to the docs
        case DbRetVal.KEYEXIST: break;
        default:
          Util.CheckRetVal(ret);
          break;
      }
      return (WriteStatus)ret;
    }

    [CLSCompliant(false)]
    protected DeleteStatus Delete(DB_TXN* txp, ref DbEntry key, uint flags) {
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          ret = DbDel(dbp, txp, ref key.dbt, flags);
        }
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

    #endregion

    #region Public Operations & Properties

    // fast - no call into LibDb
    public DbType DbType {
      get { return type; }
    }

    public void GetName(out string file, out string database) {
      byte* fp, dp;
      UTF8Encoding utf8 = new UTF8Encoding();
      byte[] nameBytes = null;
      int count;

      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        DbRetVal ret = dbp->GetDbName(dbp, out fp, out dp);
        Util.CheckRetVal(ret);
        count = Util.PtrToBuffer(fp, ref nameBytes);
        file = utf8.GetString(nameBytes, 0, count);
        count = Util.PtrToBuffer(dp, ref nameBytes);
      }
      database = utf8.GetString(nameBytes, 0, count);
    }

    public Db.OpenFlags DbOpenFlags {
      get {
        UInt32 value;
        DbRetVal ret;
        lock (db.rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetOpenFlags(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((Db.OpenFlags)value);
      }
    }

    public bool IsTransactional {
      get {
        lock (db.rscLock) {
          DB* dbp = CheckDisposed();
          int ret = dbp->GetTransactional(dbp);
          return ret != 0;
        }
      }
    }

    public bool ByteSwapped {
      get {
        int value;
        DbRetVal ret;
        lock (db.rscLock) {
          DB* dbp = CheckDisposed();
          ret = dbp->GetByteSwapped(dbp, out value);
        }
        Util.CheckRetVal(ret);
        return value != 0;
      }
    }

    #region Associate

    [CLSCompliant(false)]
    public delegate KeyGenStatus KeyGeneratorFastFcn(DbFile secondary, ref DBT key, ref DBT data, ref DBT result);
    public delegate KeyGenStatus 
    KeyGeneratorFcn(DbFile secondary, ref DbEntry key, ref DbEntry data, out DbEntry result);

    KeyGeneratorFastFcn keyGeneratorFast = null;
    KeyGeneratorFcn keyGeneratorCLS = null;
    DB.KeyGeneratorFcn keyGenerator = null;

    static unsafe DbRetVal KeyGeneratorWrapFast(DB* secondary, ref DBT key, ref DBT data, ref DBT result) {
      try {
        DbFile secDb = Util.GetDb(secondary).Dbf;
        return (DbRetVal)secDb.keyGeneratorFast(secDb, ref key, ref data, ref result);
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "KeyGenerator");
        return DbRetVal.KEYGEN_FAILED;
      }
    }

    static unsafe DbRetVal KeyGeneratorWrapCLS(DB* secondary, ref DBT key, ref DBT data, ref DBT result) {
      try {
        Db db = Util.GetDb(secondary);
        DbFile dbf = db.Dbf;
        DbEntry resultEntry;
        KeyGenStatus ks;
        lock(db.callBackLock) {
          // construct DbEntry for key
          int size = unchecked((int)key.size);
          if (size > db.callBackBuffer1.Length)
            db.callBackBuffer1 = new byte[size];
          Marshal.Copy((IntPtr)key.data, db.callBackBuffer1, 0, size);
          DbEntry keyEntry = DbEntry.InOut(db.callBackBuffer1, 0, size);
          keyEntry.dbt.flags = key.flags;
          keyEntry.dbt.dlen = key.dlen;
          keyEntry.dbt.doff = key.doff;

          // construct DbEntry for data
          size = unchecked((int)data.size);
          if (size > db.callBackBuffer2.Length)
            db.callBackBuffer2 = new byte[size];
          Marshal.Copy((IntPtr)data.data, db.callBackBuffer2, 0, size);
          DbEntry dataEntry = DbEntry.InOut(db.callBackBuffer2, 0, size);
          dataEntry.dbt.flags = data.flags;
          dataEntry.dbt.dlen = data.dlen;
          dataEntry.dbt.doff = data.doff;

          // call CLS compliant delegate - we assume it is not null
          ks = dbf.keyGeneratorCLS(dbf, ref keyEntry, ref dataEntry, out resultEntry);
        }
        if (ks != KeyGenStatus.Success)
          return (DbRetVal)ks;
        if (resultEntry.Buffer == null)
          return DbRetVal.KEYGEN_FAILED;

        // copy result back
        result.flags = resultEntry.dbt.flags & ~DBT.DB_DBT_USERMEM;
        void* retData;
        DbRetVal ret = LibDb.os_umalloc(null, resultEntry.dbt.size, out retData);
        if (ret != DbRetVal.SUCCESS)
          return ret;
        Marshal.Copy(resultEntry.Buffer, resultEntry.Start, (IntPtr)retData, resultEntry.Size);
        result.data = retData;
        result.flags |= DBT.DB_DBT_APPMALLOC;  // tell LibDb to free that memory
        result.size = resultEntry.dbt.size;
        return DbRetVal.SUCCESS;
      }
      catch (Exception ex) {
        Trace.WriteLine(ex.Message, "KeyGenerator");
        return DbRetVal.KEYGEN_FAILED;
      }
    }

    [CLSCompliant(false)]
    public void Associate(DbFile secondary, KeyGeneratorFastFcn callback, AssociateFlags flags) {
      KeyGeneratorFastFcn oldKeyGenFast = secondary.keyGeneratorFast;
      DB.KeyGeneratorFcn oldKeyGen = secondary.keyGenerator;

      secondary.keyGeneratorFast = callback;
      secondary.keyGenerator = KeyGeneratorWrapFast;
      IntPtr cb = Marshal.GetFunctionPointerForDelegate(secondary.keyGenerator);

      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        lock (secondary.db.rscLock) {
          DB* secDbp = secondary.CheckDisposed();
          ret = dbp->Associate(dbp, null, secDbp, cb, unchecked((UInt32)flags));
          if (ret != DbRetVal.SUCCESS) {
            secondary.keyGeneratorFast = oldKeyGenFast;
            secondary.keyGenerator = oldKeyGen;
          }
          // function pointers may have been updated in BDB
          secondary.InitDelegates();
        }
      }
      Util.CheckRetVal(ret);
      secondary.keyGeneratorCLS = null;
    }

    public void Associate(DbFile secondary, KeyGeneratorFcn callback, AssociateFlags flags) {
      KeyGeneratorFcn oldKeyGenCLS = secondary.keyGeneratorCLS;
      DB.KeyGeneratorFcn oldKeyGen = secondary.keyGenerator;

      secondary.keyGeneratorCLS = callback; 
      secondary.keyGenerator = KeyGeneratorWrapCLS;
      IntPtr cb = Marshal.GetFunctionPointerForDelegate(secondary.keyGenerator);

      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        lock (secondary.db.rscLock) {
          DB* secDbp = secondary.CheckDisposed();
          ret = dbp->Associate(dbp, null, secDbp, cb, unchecked((UInt32)flags));
          if (ret != DbRetVal.SUCCESS) {
            secondary.keyGeneratorCLS = oldKeyGenCLS;
            secondary.keyGenerator = oldKeyGen;
          }
        }
      }
      Util.CheckRetVal(ret);
      secondary.keyGeneratorFast = null;
    }

    #endregion

    public ReadStatus Get(Txn txn, ref DbEntry key, ref DbEntry data, ReadFlags rflags) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Get(txp, ref key, ref data, unchecked((uint)rflags));
        }
      }
      else
        return Get((DB_TXN*)null, ref key, ref data, unchecked((uint)rflags));
    }

    public ReadStatus GetExact(Txn txn, ref DbEntry key, ref DbEntry data, ReadFlags rflags) {
      uint flags = DbConst.DB_GET_BOTH | unchecked((uint)rflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Get(txp, ref key, ref data, flags);
        }
      }
      else
        return Get((DB_TXN*)null, ref key, ref data, flags);
    }

    public ReadStatus PGet(
      Txn txn,
      ref DbEntry key,
      ref DbEntry pkey,
      ref DbEntry data,
      ReadFlags rflags) 
    {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return PGet(txp, ref key, ref pkey, ref data, unchecked((uint)rflags));
        }
      }
      else
        return PGet((DB_TXN*)null, ref key, ref pkey, ref data, unchecked((uint)rflags));
    }

    public ReadStatus PGetExact(
      Txn txn,
      ref DbEntry key,
      ref DbEntry pkey,
      ref DbEntry data,
      ReadFlags rflags)
    {
      uint flags = DbConst.DB_GET_BOTH | unchecked((uint)rflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return PGet(txp, ref key, ref pkey, ref data, flags);
        }
      }
      else
        return PGet((DB_TXN*)null, ref key, ref pkey, ref data, flags);
    }

    public WriteStatus Put(Txn txn, ref DbEntry key, ref DbEntry data, WriteFlags wflags) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Put(txp, ref key, ref data, unchecked((uint)wflags));
        }
      }
      else
        return Put((DB_TXN*)null, ref key, ref data, unchecked((uint)wflags));
    }

    public WriteStatus Put(Txn txn, ref DbEntry key, ref DbEntry data) {
      return Put(txn, ref key, ref data, WriteFlags.None);
    }

    // does not overwrite data if key already exists
    public WriteStatus PutNew(Txn txn, ref DbEntry key, ref DbEntry data, WriteFlags wflags) {
      uint flags = DbConst.DB_NOOVERWRITE | unchecked((uint)wflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Put(txp, ref key, ref data, flags);
        }
      }
      else
        return Put((DB_TXN*)null, ref key, ref data, flags);
    }

    public WriteStatus PutNew(Txn txn, ref DbEntry key, ref DbEntry data) {
      return PutNew(txn, ref key, ref data, WriteFlags.None);
    }

    public DeleteStatus Delete(Txn txn, ref DbEntry key, WriteFlags flags) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Delete(txp, ref key, unchecked((uint)flags));
        }
      }
      else
        return Delete((DB_TXN*)null, ref key, unchecked((uint)flags));
    }

    public DeleteStatus Delete(Txn txn, ref DbEntry key) {
      return Delete(txn, ref key, WriteFlags.None);
    }

    public DbJoinCursor Join(DbFileCursor[] cursors, Db.JoinMode mode, int timeout) {
      int curLen = cursors.Length;
      // create null terminated list of DBC*
      DBC** curPtrList = stackalloc DBC*[curLen + 1];
      curPtrList[curLen] = null;

      DbJoinCursor joinDbc = new DbJoinCursor();
      int lockedIndx = 0;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        // no need to lock joinDbc - not visible yet
        try {
          // fill the DBC* list, lock the cursors involved and set their "join owner"
          for (; lockedIndx < curLen; lockedIndx++)
            curPtrList[lockedIndx] = cursors[lockedIndx].JoinLock(timeout);
          // lockedIndx now holds the number of successfully locked cursors

          RuntimeHelpers.PrepareConstrainedRegions();
          try { }
          finally {
            DBC* dbcp;
            DbRetVal ret = dbp->Join(dbp, curPtrList, out dbcp, (UInt32)mode);
            Util.CheckRetVal(ret);
            joinDbc.Initialize(db, dbcp, cursors);
            InsertCursor(joinDbc);
            for (int indx = 0; indx < curLen; indx++)
              cursors[indx].owner = joinDbc;
          }
        }
        finally {
          // In case of a JoinLock() exception, unlock only those that were locked (< curIndx)
          for (int indx = 0; indx < lockedIndx; indx++)
            cursors[indx].JoinUnlock();
        }
      }
      return joinDbc;
    }

    public Sequence CreateSequence() {
      Sequence seq = new Sequence(db);
      DbRetVal ret;
      lock (db.rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DB* dbp = CheckDisposed();
          ret = seq.AllocateHandle(dbp, 0);
          if (ret == DbRetVal.SUCCESS)
            InsertSequence(seq);
        }
      }
      Util.CheckRetVal(ret);
      return seq;
    }

    public void PrintStats(StatPrintFlags flags) {
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->StatPrint(dbp, unchecked((UInt32)flags));
      }
      Util.CheckRetVal(ret);
    }

    public void Sync() {
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->Sync(dbp, 0);
      }
      Util.CheckRetVal(ret);
    }

    int Truncate(DB_TXN* txp, uint flags) {
      UInt32 count;
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->Truncate(dbp, txp, out count, flags);
      }
      Util.CheckRetVal(ret);
      return unchecked((int)count);
    }

#if BDB_4_3_29

    public int Truncate(Txn txn, TruncateMode mode) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Truncate(txp, unchecked((uint)mode));
        }
      }
      else
        return Truncate((DB_TXN*)null, unchecked((uint)mode));
    }

#endif

#if BDB_4_5_20

    public int Truncate(Txn txn) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Truncate(txp, 0);
        }
      }
      else
        return Truncate((DB_TXN*)null, 0);
    }

#endif

    #endregion

    #region Nested Types

    [Flags]
    public enum AssociateFlags: int
    {
      None = 0,
#if BDB_4_3_29
      AutoCommit = DbConst.DB_AUTO_COMMIT,
#endif
      Create = DbConst.DB_CREATE,
#if BDB_4_5_20
      ImmutableKey = DbConst.DB_IMMUTABLE_KEY
#endif
    }

    public enum KeyGenStatus
    {
      Success = DbRetVal.SUCCESS,
      DoNotIndex = DbRetVal.DONOTINDEX,
      Failure = DbRetVal.KEYGEN_FAILED
    }

    [Flags]
    public enum ReadFlags: int
    {
      None = 0,
#if BDB_4_3_29
      AutoCommit = DbConst.DB_AUTO_COMMIT,
      Degree2 = DbConst.DB_DEGREE_2,
      DirtyRead = DbConst.DB_DIRTY_READ,
#endif
#if BDB_4_5_20
      ReadCommitted = DbConst.DB_READ_COMMITTED,
      ReadUncommitted = DbConst.DB_READ_UNCOMMITTED,
#endif
      Multiple = DbConst.DB_MULTIPLE,
      RMW = DbConst.DB_RMW
    }

    [Flags]
    public enum WriteFlags: int
    {
      None = 0,
#if BDB_4_3_29
      AutoCommit = DbConst.DB_AUTO_COMMIT
#endif
    }

    public enum StatFlags: int
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
      FastStat = DbConst.DB_FAST_STAT
    }

    [Flags]
    public enum StatPrintFlags: int
    {
      None = 0,
      All = DbConst.DB_STAT_ALL,
      Clear = DbConst.DB_STAT_CLEAR,
#if BDB_4_5_20
      Fast = DbConst.DB_FAST_STAT
#endif
    }

#if BDB_4_3_29

    public enum TruncateMode: int
    {
      None = 0,
      AutoCommit = DbConst.DB_AUTO_COMMIT
    }

#endif

    #endregion
  }

  /// <summary>Intermediate class needed for associating cursor classes with database classes.</summary>
  /// <typeparam name="D">Type of <see cref="DbFile"></see> subclass.</typeparam>
  /// <typeparam name="C">Type of <see cref="DbFileCursor"></see> subclass.</typeparam>
  public abstract unsafe class DbFile<D, C>: DbFile
    where D: DbFile<D, C>
    where C: DbFileCursor<C, D>, new()
  {
    protected DbFile(Db db) : base(db) { }

    C OpenCursor(DB_TXN* txp, DbFileCursor<C, D>.CreateFlags flags) {
      C dbc = new C();
      DbRetVal ret;
      lock (db.rscLock) {
        // do not need to lock dbc, as it is not publicly visible yet
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DBC* dbcp;
          DB* dbp = CheckDisposed();
          ret = dbp->Cursor(dbp, txp, out dbcp, unchecked((UInt32)flags));
          if (ret == DbRetVal.SUCCESS) {
            dbc.Initialize(db, dbcp);
            InsertCursor(dbc);
          }
        }
      }
      Util.CheckRetVal(ret);
      return dbc;
    }

    public C OpenCursor(Txn txn, DbFileCursor<C, D>.CreateFlags flags) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return OpenCursor(txp, flags);
        }
      }
      else
        return OpenCursor((DB_TXN*)null, flags);
    }
  }

  /// <summary>Berkeley DB database opened for the Hash access method.</summary>
  public unsafe class DbHash: DbFile<DbHash, DbHashCursor>
  {
    static DbHash() {
      type = DbType.Hash;
    }

    internal DbHash(Db db) : base(db) { }

    #region Public Operations & Properties

    // does not overwrite entry if key+data already exist
    public WriteStatus PutUnique(Txn txn, ref DbEntry key, ref DbEntry data, WriteFlags wflags) {
      uint flags = DbConst.DB_NODUPDATA | unchecked((uint)wflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Put(txp, ref key, ref data, flags);
        }
      }
      else
        return Put((DB_TXN*)null, ref key, ref data, flags);
    }

    // does not overwrite entry if key+data already exist
    public WriteStatus PutUnique(Txn txn, ref DbEntry key, ref DbEntry data) {
      return PutUnique(txn, ref key, ref data, WriteFlags.None);
    }

    Stats GetStats(DB_TXN* txp, StatFlags mode) {
      Stats value;
      DB_HASH_STAT* sp;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = dbp->HashStat(dbp, txp, out sp, unchecked((UInt32)mode));
          Util.CheckRetVal(ret);
          value.hashStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public Stats GetStats(Txn txn, StatFlags mode) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return GetStats(txp, mode);
        }
      }
      else
        return GetStats((DB_TXN*)null, mode);
    }

    #endregion

    #region Nested Types

    // CLS compliant wrapper for DB_HASH_STAT
    public struct Stats
    {
      internal DB_HASH_STAT hashStats;

      /* Magic number. */
      public int Magic {
        get { return unchecked((int)hashStats.hash_magic); }
      }

      /* Version number. */
      public int Version {
        get { return unchecked((int)hashStats.hash_version); }
      }

      /* Metadata flags. */
      public int MetaFlags {
        get { return unchecked((int)hashStats.hash_metaflags); }
      }

      /* Number of unique keys. */
      public int NumKeys {
        get { return unchecked((int)hashStats.hash_nkeys); }
      }

      /* Number of data items. */
      public int NumData {
        get { return unchecked((int)hashStats.hash_ndata); }
      }

      /* Page size. */
      public int PageSize {
        get { return unchecked((int)hashStats.hash_pagesize); }
      }

      /* Fill factor specified at create. */
      public int FillFactor {
        get { return unchecked((int)hashStats.hash_ffactor); }
      }

      /* Number of hash buckets. */
      public int Buckets {
        get { return unchecked((int)hashStats.hash_buckets); }
      }

      /* Pages on the free list. */
      public int FreePages {
        get { return unchecked((int)hashStats.hash_free); }
      }

      /* Bytes free on bucket pages. */
      public int BucketsFree {
        get { return unchecked((int)hashStats.hash_bfree); }
      }

      /* Number of big key/data pages. */
      public int BigPages {
        get { return unchecked((int)hashStats.hash_bigpages); }
      }

      /* Bytes free on big item pages. */
      public int BigPagesFree {
        get { return unchecked((int)hashStats.hash_big_bfree); }
      }

      /* Number of overflow pages. */
      public int OverflowPages {
        get { return unchecked((int)hashStats.hash_overflows); }
      }

      /* Bytes free on ovfl pages. */
      public int OverflowPagesFree {
        get { return unchecked((int)hashStats.hash_ovfl_free); }
      }

      /* Number of dup pages. */
      public int DuplicatePages {
        get { return unchecked((int)hashStats.hash_dup); }
      }

      /* Bytes free on duplicate pages. */
      public int DuplicatePagesFree {
        get { return unchecked((int)hashStats.hash_dup_free); }
      }
    }

    #endregion
  }

  /// <summary>Berkeley DB database opened for the Queue access method.</summary>
  public unsafe class DbQueue: DbFile<DbQueue, DbQueueCursor>
  {
    static DbQueue() {
      type = DbType.Queue;
    }

    internal DbQueue(Db db): base(db) { }

    #region Public Operations & Properties

    // does not need to handle multiple return records - because of DB_CONSUME flags
    public ReadStatus Consume(Txn txn, ref DbEntry key, ref DbEntry data, ReadFlags rflags, bool wait) {
      uint flags;
      if (wait)
        flags = DbConst.DB_CONSUME_WAIT;
      else
        flags = DbConst.DB_CONSUME;
      flags |= unchecked((uint)rflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Get(txp, ref key, ref data, flags);
        }
      }
      else
        return Get((DB_TXN*)null, ref key, ref data, flags);
    }

    public WriteStatus Append(Txn txn, ref DbEntry key, ref DbEntry data, WriteFlags wflags) {
      uint flags = DbConst.DB_APPEND | unchecked((uint)wflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Put(txp, ref key, ref data, flags);
        }
      }
      else
        return Put((DB_TXN*)null, ref key, ref data, flags);
    }

    public WriteStatus Append(Txn txn, ref DbEntry key, ref DbEntry data) {
      return Append(txn, ref key, ref data, WriteFlags.None);
    }

    Stats GetStats(DB_TXN* txp, StatFlags mode) {
      Stats value;
      DB_QUEUE_STAT* sp;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = dbp->QueueStat(dbp, txp, out sp, unchecked((UInt32)mode));
          Util.CheckRetVal(ret);
          value.qsStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public Stats GetStats(Txn txn, StatFlags mode) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return GetStats(txp, mode);
        }
      }
      else
        return GetStats((DB_TXN*)null, mode);
    }

    #endregion

    #region Nested Types

    // CLS compliant wrapper for DB_QUEUE_STAT
    public struct Stats
    {
      internal DB_QUEUE_STAT qsStats;

      /* Magic number. */
      public int Magic {
        get { return unchecked((int)qsStats.qs_magic); }
      }

      /* Version number. */
      public int Version {
        get { return unchecked((int)qsStats.qs_version); }
      }

      /* Metadata flags. */
      public int MetaFlags {
        get { return unchecked((int)qsStats.qs_metaflags); }
      }

      /* Number of unique keys. */
      public int NumKeys {
        get { return unchecked((int)qsStats.qs_nkeys); }
      }

      /* Number of data items. */
      public int NumData {
        get { return unchecked((int)qsStats.qs_ndata); }
      }

      /* Page size. */
      public int PageSize {
        get { return unchecked((int)qsStats.qs_pagesize); }
      }

      /* Pages per extent. */
      public int ExtentSize {
        get { return unchecked((int)qsStats.qs_extentsize); }
      }

      /* Data pages. */
      public int Pages {
        get { return unchecked((int)qsStats.qs_pages); }
      }

      /* Fixed-length record length. */
      public int RecordLength {
        get { return unchecked((int)qsStats.qs_re_len); }
      }

      /* Fixed-length record pad. */
      public int RecordPad {
        get { return unchecked((int)qsStats.qs_re_pad); }
      }

      /* Bytes free in data pages. */
      public int PagesFree {
        get { return unchecked((int)qsStats.qs_pgfree); }
      }

      /* First not deleted record. */
      public int FirstRecno {
        get { return unchecked((int)qsStats.qs_first_recno); }
      }

      /* Next available record number. */
      public int CurrentRecno {
        get { return unchecked((int)qsStats.qs_cur_recno); }
      }
    }

    #endregion
  }

  /// <summary>Base class for Berkeley DB databases opened for the RecNo or BTree access method.</summary>
  /// <typeparam name="C">Type of <see cref="DbFileCursor"></see> subclass.</typeparam>
  /// <typeparam name="D">Type of <see cref="DbBtreeRecno{D, C}"></see> subclass.</typeparam>
  public abstract unsafe class DbBtreeRecno<D, C>: DbFile<D, C>
    where D: DbBtreeRecno<D, C>
    where C: DbFileCursor<C, D>, new()
  {
    protected DbBtreeRecno(Db handle) : base(handle) { }

    #region Public Operations & Properties

    Stats GetStats(DB_TXN* txp, StatFlags mode) {
      Stats value;
      DB_BTREE_STAT* sp;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          DbRetVal ret = dbp->BTreeStat(dbp, txp, out sp, unchecked((UInt32)mode));
          Util.CheckRetVal(ret);
          value.btStats = *sp;
          LibDb.os_ufree(null, sp);
        }
      }
      return value;
    }

    public Stats GetStats(Txn txn, StatFlags mode) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return GetStats(txp, mode);
        }
      }
      else
        return GetStats((DB_TXN*)null, mode);
    }

#if BDB_4_5_20

    void Compact(
      DB_TXN* txp,
      DbEntry start,
      DbEntry stop,
      ref CompactData cdata,
      uint flags, 
      out DbEntry end)
    {
      DbRetVal ret;
      end = new DbEntry();
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        fixed (byte* startP = start.Buffer, stopP = stop.Buffer) {
          fixed (DB_COMPACT* cdataPtr = &cdata.dbCompact) {
            start.dbt.data = startP + start.Start;
            stop.dbt.data = stopP + stop.Start;
            ret = dbp->Compact(dbp, txp, &start.dbt, &stop.dbt, cdataPtr, flags, out end.dbt);
          }
        }
      }
      Util.CheckRetVal(ret);
      // this will modify end.dbt.ulen to be the same as end.Size
      end.ResizeBuffer(end.Size);
      Marshal.Copy((IntPtr)end.dbt.data, end.Buffer, 0, end.Size);
    }

    public DbEntry Compact(
      Txn txn, 
      ref DbEntry start,
      ref DbEntry stop,
      ref CompactData cdata,
      CompactFlags flags)
    {
      DbEntry end;
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          Compact(txp, start, stop, ref cdata, unchecked((uint)flags), out end);
        }
      }
      else
        Compact(null, start, stop, ref cdata, unchecked((uint)flags), out end);
      return end;
    }

    void Compact(DB_TXN* txp, uint flags) {
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        ret = dbp->Compact(dbp, txp, null, null, null, flags, out *((DBT*)null));
      }
      Util.CheckRetVal(ret);
    }

    public void Compact(Txn txn, CompactFlags flags) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          Compact(txp, unchecked((uint)flags));
        }
      }
      else
        Compact(null, unchecked((uint)flags));
    }

#endif

    #endregion

    #region Nested Types

    // CLS compliant wrapper for DB_BTREE_STAT
    public struct Stats
    {
      internal DB_BTREE_STAT btStats;

      /* Magic number. */
      public int Magic {
        get { return unchecked((int)btStats.bt_magic); }
      }

      /* Version number. */
      public int Version {
        get { return unchecked((int)btStats.bt_version); }
      }

      /* Metadata flags. */
      public int MetaFlags {
        get { return unchecked((int)btStats.bt_metaflags); }
      }

      /* Number of unique keys. */
      public int NumKeys {
        get { return unchecked((int)btStats.bt_nkeys); }
      }

      /* Number of data items. */
      public int NumData {
        get { return unchecked((int)btStats.bt_ndata); }
      }

      /* Page size. */
      public int PageSize {
        get { return unchecked((int)btStats.bt_pagesize); }
      }

#if BDB_4_3_29
      /* Maxkey value. */
      public int MaxKey {
        get { return unchecked((int)btStats.bt_maxkey); }
      }
#endif

      /* Minkey value. */
      public int MinKey {
        get { return unchecked((int)btStats.bt_minkey); }
      }

      /* Fixed-length record length. */
      public int RecordLength {
        get { return unchecked((int)btStats.bt_re_len); }
      }

      /* Fixed-length record pad. */
      public int RecordPad {
        get { return unchecked((int)btStats.bt_re_pad); }
      }

      /* Tree levels. */
      public int Levels {
        get { return unchecked((int)btStats.bt_levels); }
      }

      /* Internal pages. */
      public int InternalPages {
        get { return unchecked((int)btStats.bt_int_pg); }
      }

      /* Leaf pages. */
      public int LeafPages {
        get { return unchecked((int)btStats.bt_leaf_pg); }
      }

      /* Duplicate pages. */
      public int DuplicatePages {
        get { return unchecked((int)btStats.bt_dup_pg); }
      }

      /* Overflow pages. */
      public int OverflowPages {
        get { return unchecked((int)btStats.bt_over_pg); }
      }

      /* Empty pages. */
      public int EmptyPages {
        get { return unchecked((int)btStats.bt_empty_pg); }
      }

      /* Pages on the free list. */
      public int FreePages {
        get { return unchecked((int)btStats.bt_free); }
      }

      /* Bytes free in internal pages. */
      public int InternalPagesFree {
        get { return unchecked((int)btStats.bt_int_pgfree); }
      }

      /* Bytes free in leaf pages. */
      public int LeafPagesFree {
        get { return unchecked((int)btStats.bt_leaf_pgfree); }
      }

      /* Bytes free in duplicate pages. */
      public int DuplicatePagesFree {
        get { return unchecked((int)btStats.bt_dup_pgfree); }
      }

      /* Bytes free in overflow pages. */
      public int OverflowPagesFree {
        get { return unchecked((int)btStats.bt_over_pgfree); }
      }
    }

#if BDB_4_5_20
    public enum CompactFlags: int
    {
      None = 0,
      FreeListOnly = DbConst.DB_FREELIST_ONLY,
      FreeSpace = DbConst.DB_FREE_SPACE
    }

    // CLS compliant wrapper for DB_COMPACT
    public struct CompactData
    {
      internal DB_COMPACT dbCompact;

      /* Input Parameters. */

      /* Desired fillfactor: 1-100 */
      public int FillPercent {
        get { return unchecked((int)dbCompact.compact_fillpercent); }
        set { dbCompact.compact_fillpercent = unchecked((uint)value); }
      }

      /* Lock timeout in microseconds. */
      public int Timeout {
        get { return unchecked((int)dbCompact.compact_timeout); }
        set { dbCompact.compact_timeout = unchecked((uint)value); }
      }

      /* Max pages to process. */
      public int Pages {
        get { return unchecked((int)dbCompact.compact_pages); }
        set { dbCompact.compact_pages = unchecked((uint)value); }
      }

      /* Output Stats. */

      /* Number of pages freed. */
      public int PagesFreed {
        get { return unchecked((int)dbCompact.compact_pages_free); }
      }

      /* Number of pages examine. */
      public int PagesExamined {
        get { return unchecked((int)dbCompact.compact_pages_examine); }
      }

      /* Number of levels removed. */
      public int LevelsRemoved {
        get { return unchecked((int)dbCompact.compact_levels); }
      }

      /* Number of deadlocks. */
      public int Deadlocks {
        get { return unchecked((int)dbCompact.compact_deadlock); }
      }

      /* Pages returned to OS. */
      public int PagesTruncated {
        get { return unchecked((int)dbCompact.compact_pages_truncated); }
      }
    }
#endif

    #endregion
  }

  /// <summary>Berkeley DB database opened for the RecNo access method.</summary>
  public unsafe class DbRecNo: DbBtreeRecno<DbRecNo, DbRecNoCursor>
  {
    static DbRecNo() {
      type = DbType.Recno;
    }

    internal DbRecNo(Db db): base(db) { }

    #region Public Operations & Properties

    public WriteStatus Append(Txn txn, ref DbEntry key, ref DbEntry data, WriteFlags wflags) {
      uint flags = DbConst.DB_APPEND | unchecked((uint)wflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Put(txp, ref key, ref data, flags);
        }
      }
      else
        return Put((DB_TXN*)null, ref key, ref data, flags);
    }

    public WriteStatus Append(Txn txn, ref DbEntry key, ref DbEntry data) {
      return Append(txn, ref key, ref data, WriteFlags.None);
    }

    #endregion
  }

  /// <summary>Berkeley DB database opened for the BTree access method.</summary>
  public unsafe class DbBTree: DbBtreeRecno<DbBTree, DbBTreeCursor>
  {
    static DbBTree() {
      type = DbType.BTree;
    }

    internal DbBTree(Db db): base(db) { }

    #region Public Operations & Properties

    // key.Data must contain a record number (UInt32) on input
    public ReadStatus GetAtRecNo(Txn txn, ref DbEntry key, ref DbEntry data, ReadFlags rflags) {
      uint flags = DbConst.DB_SET_RECNO | unchecked((uint)rflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Get(txp, ref key, ref data, flags);
        }
      }
      else
        return Get((DB_TXN*)null, ref key, ref data, flags);
    }

    // does not overwrite entry if key+data already exist
    public WriteStatus PutUnique(Txn txn, ref DbEntry key, ref DbEntry data, WriteFlags wflags) {
      uint flags = DbConst.DB_NODUPDATA | unchecked((uint)wflags);
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return Put(txp, ref key, ref data, flags);
        }
      }
      else
        return Put((DB_TXN*)null, ref key, ref data, flags);
    }

    public WriteStatus PutUnique(Txn txn, ref DbEntry key, ref DbEntry data) {
      return PutUnique(txn, ref key, ref data, WriteFlags.None);
    }

    KeyRange GetKeyRange(DB_TXN* txp, ref DbEntry key) {
      KeyRange value;
      DbRetVal ret;
      lock (db.rscLock) {
        DB* dbp = CheckDisposed();
        fixed (byte* keyBufP = key.Buffer) {
          key.dbt.data = keyBufP + key.Start;
          ret = DbKeyRange(dbp, txp, ref key.dbt, out value.keyRange, 0);
        }
      }
      Util.CheckRetVal(ret);
      return value;
    }

    public KeyRange GetKeyRange(Txn txn, ref DbEntry key) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return GetKeyRange(txp, ref key);
        }
      }
      else
        return GetKeyRange((DB_TXN*)null, ref key);
    }

    #endregion

    #region Nested Types

    // wrapper for DB_KEY_RANGE - just for naming convention purposes
    public struct KeyRange
    {
      internal DB_KEY_RANGE keyRange;

      public double Less {
        get { return keyRange.less; }
      }

      public double Equal {
        get { return keyRange.equal; }
      }

      public double Greater {
        get { return keyRange.greater; }
      }
    }

    #endregion
  }
}
