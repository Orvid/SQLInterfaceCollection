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
  /*
   *  The structure is allocated by the caller and filled in during a
   *  lock_get request (or a lock_vec/DB_LOCK_GET).
   */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_LOCK
  {
    IntPtr off;         /* Offset of the lock in the region (typedef uintptr_t roff_t;) */
    UInt32 ndx;         /* Index of the object referenced by this lock; used for locking. */
    UInt32 gen;         /* Generation number of this lock. */
    DB_LOCKMODE mode;   /* mode of this lock. */
  }

  /*
   * Simple R/W lock modes and for multi-granularity intention locking.
   *
   * !!!
   * These values are NOT random, as they are used as an index into the lock
   * conflicts arrays, i.e., DB_LOCK_IWRITE must be = 3, and DB_LOCK_IREAD
   * must be = 4.
   */
  public enum DB_LOCKMODE: int
  {
    NG = 0,               /* Not granted. */
    READ = 1,             /* Shared/read. */
    WRITE = 2,            /* Exclusive/write. */
    WAIT = 3,             /* Wait for event */
    IWRITE = 4,           /* Intent exclusive/write. */
    IREAD = 5,            /* Intent to share/read. */
    IWR = 6,              /* Intent to read and write. */
#if BDB_4_3_29
    DIRTY = 7,            /* Dirty Read. */
#endif
#if BDB_4_5_20
    READ_UNCOMMITTED = 7, /* Degree 1 isolation. */
#endif
    WWRITE = 8            /* Was Written. */
  }

  /*
  * Request types.
  */
  public enum DB_LOCKOP : int
  {
    DUMP = 0,             /* Display held locks. */
    GET = 1,              /* Get the lock. */
    GET_TIMEOUT = 2,      /* Get lock with a timeout. */
    INHERIT = 3,          /* Pass locks to parent. */
    PUT = 4,              /* Release the lock. */
    PUT_ALL = 5,          /* Release locker's locks. */
    PUT_OBJ = 6,          /* Release locker's locks on obj. */
    PUT_READ = 7,         /* Release locker's read locks. */
    TIMEOUT = 8,          /* Force a txn to timeout. */
    TRADE = 9,            /* Trade locker ids on a lock. */
    UPGRADE_WRITE = 10    /* Upgrade writes for dirty reads. */
  }

  /* Lock request structure. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public unsafe struct DB_LOCKREQ
  {
    public DB_LOCKOP op;          /* Operation. */
    public DB_LOCKMODE mode;      /* Requested mode. */
    public UInt32 timeout;        /* Time to expire lock. (typedef u_int32_t db_timeout_t;) */
    public IntPtr obj;            /* Object being locked. (DBT*) */
    public DB_LOCK dblock;        /* Lock returned. */

    public DB_LOCKREQ(DB_LOCKOP op, DB_LOCKMODE mode, UInt32 timeout, IntPtr obj, DB_LOCK dblock) {
      this.op = op;
      this.mode = mode;
      this.timeout = timeout;
      this.obj = obj;
      this.dblock = dblock;
    }
  }

  /* Lock statistics structure. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_LOCK_STAT
  {
    public UInt32 st_id;              /* Last allocated locker ID. */
    public UInt32 st_cur_maxid;       /* Current maximum unused ID. */
    public UInt32 st_maxlocks;        /* Maximum number of locks in table. */
    public UInt32 st_maxlockers;      /* Maximum num of lockers in table. */
    public UInt32 st_maxobjects;      /* Maximum num of objects in table. */
    public int st_nmodes;             /* Number of lock modes. */
    public UInt32 st_nlocks;          /* Current number of locks. */
    public UInt32 st_maxnlocks;       /* Maximum number of locks so far. */
    public UInt32 st_nlockers;        /* Current number of lockers. */
    public UInt32 st_maxnlockers;     /* Maximum number of lockers so far. */
    public UInt32 st_nobjects;        /* Current number of objects. */
    public UInt32 st_maxnobjects;     /* Maximum number of objects so far. */
#if BDB_4_3_29
    public UInt32 st_nconflicts;      /* Number of lock conflicts. */
#endif
    public UInt32 st_nrequests;       /* Number of lock gets. */
    public UInt32 st_nreleases;       /* Number of lock puts. */
#if BDB_4_3_29
    public UInt32 st_nnowaits;        /* Number of requests that would have waited, but NOWAIT was set. */
#endif
#if BDB_4_5_20
    public UInt32 st_nupgrade;        /* Number of lock upgrades. */
    public UInt32 st_ndowngrade;      /* Number of lock downgrades. */
    public UInt32 st_lock_wait;       /* Lock conflicts w/ subsequent wait */
    public UInt32 st_lock_nowait;     /* Lock conflicts w/o subsequent wait */
#endif
    public UInt32 st_ndeadlocks;      /* Number of lock deadlocks. */
    public UInt32 st_locktimeout;     /* Lock timeout. (typedef u_int32_t db_timeout_t;) */
    public UInt32 st_nlocktimeouts;   /* Number of lock timeouts. */
    public UInt32 st_txntimeout;      /* Transaction timeout. (typedef u_int32_t db_timeout_t;) */
    public UInt32 st_ntxntimeouts;    /* Number of transaction timeouts. */
    public UInt32 st_region_wait;     /* Region lock granted after wait. */
    public UInt32 st_region_nowait;   /* Region lock granted without wait. */
    public IntPtr st_regsize;         /* Region size. (typedef uintptr_t roff_t;) */
  };
}
