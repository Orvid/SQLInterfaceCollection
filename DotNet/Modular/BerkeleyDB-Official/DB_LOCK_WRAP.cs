/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace BerkeleyDb
{
  /// <summary>CLS compliant wrapper for <see cref="DB_LOCK"/>.</summary>
  public struct Lock
  {
    internal DB_LOCK dblck;

    internal Lock(DB_LOCK dblck) {
      this.dblck = dblck;
    }
  }

  public enum LockMode: int
  {
    Read = DB_LOCKMODE.READ,
    Write = DB_LOCKMODE.WRITE,
    IWrite = DB_LOCKMODE.IWRITE,
    IRead = DB_LOCKMODE.IREAD,
    IntentWR = DB_LOCKMODE.IWR
  }

  public enum LockOperation: int
  {
    Acquire = DB_LOCKOP.GET,
    AcquireTimeout = DB_LOCKOP.GET_TIMEOUT,
    Release = DB_LOCKOP.PUT,
    ReleaseAll = DB_LOCKOP.PUT_ALL,
    ReleaseObj = DB_LOCKOP.PUT_OBJ,
    Timeout = DB_LOCKOP.TIMEOUT
  }

  /// <summary>CLS compliant substitute for <see cref="DB_LOCKREQ"/>.</summary>
  public struct LockRequest
  {
    LockOperation op;
    LockMode mode;
    int timeout;
    DbEntry obj;
    internal Lock lck;

    public static LockRequest ForAcquire(LockMode mode, ref DbEntry obj) {
      LockRequest lr =
        new LockRequest(LockOperation.Acquire, mode, 0, ref obj, new Lock());
      return lr;
    }

    public static LockRequest ForAcquireWithTimeout(LockMode mode, int timeout, ref DbEntry obj) {
      LockRequest lr =
        new LockRequest(LockOperation.AcquireTimeout, mode, timeout, ref obj, new Lock());
      return lr;
    }

    public static LockRequest ForRelease(Lock lck) {
      LockRequest lr = new LockRequest();
      lr.op = LockOperation.Release;
      lr.lck = lck;
      return lr;
    }

    public static LockRequest ForReleaseAll() {
      LockRequest lr = new LockRequest();
      lr.op = LockOperation.ReleaseAll;
      return lr;
    }

    public static LockRequest ForReleaseObj(ref DbEntry obj) {
      LockRequest lr = new LockRequest();
      lr.op = LockOperation.ReleaseObj;
      lr.obj = obj;
      return lr;
    }

    public static LockRequest ForTimeout() {
      LockRequest lr = new LockRequest();
      lr.op = LockOperation.Timeout;
      return lr;
    }

    public LockRequest(LockOperation op, LockMode mode, int timeout, ref DbEntry obj, Lock lck) {
      this.op = op;
      this.mode = mode;
      this.timeout = timeout;
      this.obj = obj;
      this.lck = lck;
    }

    public LockOperation Op {
      get { return op; }
    }

    public LockMode Mode {
      get { return mode; }
    }

    public int Timeout {
      get { return timeout; }
    }

    public DbEntry Obj {
      get { return obj; }
    }

    public Lock Lock {
      get { return lck; }
    }

    // constructs a DB_LOCKREQ instance based on this instance and an
    // externally (in unmanaged memory) allocated DBT and byte buffer;
    // bufp *must* point to memory large enough to hold obj.Size bytes
    internal unsafe DB_LOCKREQ PrepareLockReq(DBT* dbt, ref byte* bufp) {
      DB_LOCKREQ dblr = new DB_LOCKREQ((DB_LOCKOP)op, (DB_LOCKMODE)mode, unchecked((uint)timeout),
        (IntPtr)dbt, lck.dblck);
      dbt->data = bufp;
      dbt->size = obj.dbt.size;
      dbt->flags = DBT.DB_DBT_USERMEM;
      if (bufp != null)
        Marshal.Copy(obj.Buffer, obj.Start, (IntPtr)bufp, obj.Size);
      bufp += obj.dbt.size;
      return dblr;
    }
  }

  /// <summary>Exception class that indicates that a lock request failed.</summary>
  public class LockException: BdbException
  {
    private int failIndex;

    public LockException() { }

    public LockException(string message) : base(message) { }

    public LockException(string message, Exception e) : base(message, e) { }

    public LockException(DbRetVal error, int failIndex)
      : base(error) {
      this.failIndex = failIndex;
    }

    public LockException(DbRetVal error, int failIndex, string message)
      : base(error, message) {
      this.failIndex = failIndex;
    }

    /// <summary>Index of failed lock request when multiple requests (lock vector)
    /// were submitted.</summary>
    public int FailIndex {
      get { return failIndex; }
    }
  }

  public enum LockDetectMode: int
  {
    Default = DbConst.DB_LOCK_DEFAULT,
    Expire = DbConst.DB_LOCK_EXPIRE,
    MaxLocks = DbConst.DB_LOCK_MAXLOCKS,
    MaxWrite = DbConst.DB_LOCK_MAXWRITE,
    MinLocks = DbConst.DB_LOCK_MINLOCKS,
    MinWrite = DbConst.DB_LOCK_MINWRITE,
    Oldest = DbConst.DB_LOCK_OLDEST,
    Random = DbConst.DB_LOCK_RANDOM,
    Youngest = DbConst.DB_LOCK_YOUNGEST
  }

  public enum LockFlags: int
  {
    None = 0,
    NoWait = DbConst.DB_LOCK_NOWAIT
  }

  [Flags]
  public enum LockStatPrintFlags: int
  {
    None = 0,
    All = DbConst.DB_STAT_ALL,
    Conflict = DbConst.DB_STAT_LOCK_CONF,
    Lockers = DbConst.DB_STAT_LOCK_LOCKERS,
    Objects = DbConst.DB_STAT_LOCK_OBJECTS,
    Params = DbConst.DB_STAT_LOCK_PARAMS
  }

  // CLS compliant wrapper for DB_LOCK_STAT
  public struct LockStats
  {
    internal DB_LOCK_STAT lockStats;

    /* Last allocated locker ID. */
    public int LastId {
      get { return unchecked((int)lockStats.st_id); }
    }

    /* Current maximum unused ID. */
    public int CurrentMaxId {
      get { return unchecked((int)lockStats.st_cur_maxid); }
    }

    /* Maximum number of locks in table. */
    public int MaxLocks {
      get { return unchecked((int)lockStats.st_maxlocks); }
    }

    /* Maximum num of lockers in table. */
    public int MaxLockers {
      get { return unchecked((int)lockStats.st_maxlockers); }
    }

    /* Maximum num of objects in table. */
    public int MaxObjects {
      get { return unchecked((int)lockStats.st_maxobjects); }
    }

    /* Number of lock modes. */
    public int NumModes {
      get { return lockStats.st_nmodes; }
    }

    /* Current number of locks. */
    public int NumLocks {
      get { return unchecked((int)lockStats.st_nlocks); }
    }

    /* Maximum number of locks so far. */
    public int MaxNumLocks {
      get { return unchecked((int)lockStats.st_maxnlocks); }
    }

    /* Current number of lockers. */
    public int NumLockers {
      get { return unchecked((int)lockStats.st_nlockers); }
    }

    /* Maximum number of lockers so far. */
    public int MaxNumLockers {
      get { return unchecked((int)lockStats.st_maxnlockers); }
    }

    /* Current number of objects. */
    public int NumObjects {
      get { return unchecked((int)lockStats.st_nobjects); }
    }

    /* Maximum number of objects so far. */
    public int MaxNumObjects {
      get { return unchecked((int)lockStats.st_maxnobjects); }
    }

#if BDB_4_3_29
    /* Number of lock conflicts. */
    public int NumConflicts {
      get { return unchecked((int)lockStats.st_nconflicts); }
    }
#endif

    /* Number of lock gets. */
    public int NumRequests {
      get { return unchecked((int)lockStats.st_nrequests); }
    }

    /* Number of lock puts. */
    public int NumReleases {
      get { return unchecked((int)lockStats.st_nreleases); }
    }

#if BDB_4_3_29
    /* Number of requests that would have waited, but NOWAIT was set. */
    public int NumNoWaits {
      get { return unchecked((int)lockStats.st_nnowaits); }
    }
#endif

#if BDB_4_5_20
    /* Number of lock upgrades. */
    public int NumLockUpgrades {
      get { return unchecked((int)lockStats.st_nupgrade); }
    }

    /* Number of lock downgrades. */
    public int NumLockDowngrades {
      get { return unchecked((int)lockStats.st_ndowngrade); }
    }

    /* Lock conflicts w/ subsequent wait */
    public int NumLockWaits {
      get { return unchecked((int)lockStats.st_lock_wait); }
    }

    /* Lock conflicts w/o subsequent wait */
    public int NumLockNoWaits {
      get { return unchecked((int)lockStats.st_lock_nowait); }
    }
#endif

    /* Number of lock deadlocks. */
    public int NumDeadlocks {
      get { return unchecked((int)lockStats.st_ndeadlocks); }
    }

    /* Lock timeout. (typedef u_int32_t db_timeout_t;) */
    public int LockTimeout {
      get { return unchecked((int)lockStats.st_locktimeout); }
    }

    /* Number of lock timeouts. */
    public int NumLockTimeouts {
      get { return unchecked((int)lockStats.st_nlocktimeouts); }
    }

    /* Transaction timeout. (typedef u_int32_t db_timeout_t;) */
    public int TxnTimeout {
      get { return unchecked((int)lockStats.st_txntimeout); }
    }

    /* Number of transaction timeouts. */
    public int NumTxnTimeouts {
      get { return unchecked((int)lockStats.st_ntxntimeouts); }
    }

    /* Region lock granted after wait. */
    public int RegionWaits {
      get { return unchecked((int)lockStats.st_region_wait); }
    }

    /* Region lock granted without wait. */
    public int RegionNoWaits {
      get { return unchecked((int)lockStats.st_region_nowait); }
    }

    /* Region size. (typedef uintptr_t roff_t;) */
    public long RegionSize {
      get { return lockStats.st_regsize.ToInt64(); }
    }
  }
}