/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BerkeleyDb
{
  /// <summary>Represents a log file cursor.</summary>
  /// <remarks>The wrapped <see cref="DB_LOGC"/> handle is *not* free-threaded.</remarks>
  public unsafe class LogCursor: IDisposable
  {
    protected Env env;

    // store delegates for frequently used function pointer calls
    [CLSCompliant(false)]
    protected DB_LOGC.GetFcn LogcGet;

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
    internal volatile DB_LOGC* logcp = null;

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    DbRetVal ReleaseUnmanagedResources() {
      DB_LOGC* logcp = this.logcp;
      if (logcp == null)
        return DbRetVal.SUCCESS;
      // DB_LOGC->Close() could be a lengthy call, so we call Disposed() first, and the
      // CER ensures that we reach DB_LOGC->Close() without external interruption.
      // This is OK because one must not use the handle after DB_LOGC->Close() was called,
      // regardless of the return value.
      Disposed();
      DbRetVal ret = logcp->Close(logcp, 0);
      return ret;
    }

    #endregion

    #region Construction, Finalization

    // assumes that env != null
    internal LogCursor(Env env) {
      this.env = env;
    }

    // assumes that logcp != null!
    internal void Initialize(DB_LOGC* logcp) {
      this.logcp = logcp;
      LogcGet = logcp->Get;
    }

    public const string disposedStr = "Log cursor handle closed.";

    [CLSCompliant(false)]
    protected DB_LOGC* CheckDisposed() {
      // avoid multiple volatile memory access
      DB_LOGC* logcp = this.logcp;
      if (logcp == null)
        throw new ObjectDisposedException(disposedStr);
      return logcp;
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

    // does not check for logcp == null!
    void Disposed() {
      logcp = null;
      env.RemoveLogCursor(this);
    }

    public bool IsDisposed {
      get { return logcp == null; }
    }

    public DbRetVal ReleaseVal {
      get { return releaseVal; }
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
      lock (env.rscLock) {
        Dispose(true);
      }
      GC.SuppressFinalize(this);
    }

    #endregion

    #region Public Operations & Properties

    public void Close() {
      Dispose();
    }

    public ReadStatus Get(ref Lsn lsn, ref DbEntry data, GetMode mode) {
      DbRetVal ret;
      lock (rscLock) {
        DB_LOGC* logcp = CheckDisposed();
        fixed (byte* dataBufP = data.Buffer) {
          data.dbt.data = dataBufP + data.Start;
          ret = LogcGet(logcp, ref lsn.lsn, ref data.dbt, unchecked((UInt32)mode));
        }
      }
      switch (ret) {
        case DbRetVal.NOTFOUND: break;
        case DbRetVal.BUFFER_SMALL: break;
        default:
          Util.CheckRetVal(ret);
          break;
      }
      data.SetReturnType(DbType.Unknown, 0);
      return (ReadStatus)ret;
    }

#if BDB_4_5_20

    public int Version {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_LOGC* logcp = CheckDisposed();
          ret = logcp->Version(logcp, out value, 0);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
    }

#endif

    #endregion

    #region Nested types

    public enum GetMode: int
    {
      Current = DbConst.DB_CURRENT,
      First = DbConst.DB_FIRST,
      Last = DbConst.DB_LAST,
      Next = DbConst.DB_NEXT,
      Prev = DbConst.DB_PREV,
      Set = DbConst.DB_SET
    }

    #endregion
  }

  [Flags]
  public enum LogArchiveFlags: int
  {
    None = 0,
    Abs = DbConst.DB_ARCH_ABS,
    Data = DbConst.DB_ARCH_DATA,
    Log = DbConst.DB_ARCH_LOG,
    Remove = DbConst.DB_ARCH_REMOVE
  }

  public enum LogPutFlags: int
  {
    None = 0,
    Flush = DbConst.DB_FLUSH
  }

  // CLS compliant wrapper for DB_LOG_STAT
  public struct LogStats
  {
    internal DB_LOG_STAT logStats;

    /* Log file magic number. */
    public int Magic {
      get { return unchecked((int)logStats.st_magic); }
    }

    /* Log file version number. */
    public int Version {
      get { return unchecked((int)logStats.st_version); }
    }

    /* Log file mode. */
    public int Mode {
      get { return unchecked((int)logStats.st_mode); }
    }

    /* Log buffer size. */
    public int LogBufferSize {
      get { return unchecked((int)logStats.st_lg_bsize); }
    }

    /* Log file size. */
    public int LogSize {
      get { return unchecked((int)logStats.st_lg_size); }
    }

#if BDB_4_5_20
    /* Records entered into the log. */
    public int LogRecords {
      get { return unchecked((int)logStats.st_record); }
    }
#endif

    /* Bytes to log. */
    public int WriteBytes {
      get { return unchecked((int)logStats.st_w_bytes); }
    }
    
    /* Megabytes to log. */
    public int WriteMBytes {
      get { return unchecked((int)logStats.st_w_mbytes); }
    }

    /* Bytes to log since checkpoint. */
    public int WriteChkBytes {
      get { return unchecked((int)logStats.st_wc_bytes); }
    }

    /* Megabytes to log since checkpoint. */
    public int WriteChkMBytes {
      get { return unchecked((int)logStats.st_wc_mbytes); }
    }

    /* Total I/O writes to the log. */
    public int WriteCount {
      get { return unchecked((int)logStats.st_wcount); }
    }

    /* Overflow writes to the log. */
    public int WriteCountFill {
      get { return unchecked((int)logStats.st_wcount_fill); }
    }

#if BDB_4_5_20
    /* Total I/O reads from the log. */
    public int ReadCount {
      get { return unchecked((int)logStats.st_rcount); }
    }
#endif

    /* Total syncs to the log. */
    public int SyncCount {
      get { return unchecked((int)logStats.st_scount); }
    }

    /* Region lock granted after wait. */
    public int NumRegionWait {
      get { return unchecked((int)logStats.st_region_wait); }
    }

    /* Region lock granted without wait. */
    public int NumRegionNowait {
      get { return unchecked((int)logStats.st_region_nowait); }
    }

    /* Current log file number. */
    public int CurrentFile {
      get { return unchecked((int)logStats.st_cur_file); }
    }

    /* Current log file offset. */
    public int CurrentOffset {
      get { return unchecked((int)logStats.st_cur_offset); }
    }

    /* Known on disk log file number. */
    public int DiskFile {
      get { return unchecked((int)logStats.st_disk_file); }
    }

    /* Known on disk log file offset. */
    public int DiskOffset {
      get { return unchecked((int)logStats.st_disk_offset); }
    }

    /* Region size. (typedef uintptr_t roff_t;) */
    public long RegionSize {
      get { return logStats.st_regsize.ToInt64(); }
    }

    /* Max number of commits in a flush. */
    public int MaxCommitsPerFlush {
      get { return unchecked((int)logStats.st_maxcommitperflush); }
    }

    /* Min number of commits in a flush. */
    public int MinCommitsPerFlush {
      get { return unchecked((int)logStats.st_mincommitperflush); }
    }
  }
}
