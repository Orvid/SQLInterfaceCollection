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
  internal static class Compile
  {
    public const int PackSize  = 
    #if BDB_PACK
      1;
    #elif BDB_PACK2
      2;
    #elif BDB_PACK4
      4;
    #elif BDB_PACK8
      8;
    #elif BDB_PACK16
      16;
    #else
      0;
    #endif

    public const CallingConvention CallConv  = 
    #if BDB_STDCALL
      CallingConvention.StdCall;
    #elif BDB_CDECL
      CallingConvention.Cdecl;
    #elif BDB_WINAPI
      CallingConvention.WinApi;
    #else
      CallingConvention.Cdecl;
    #endif
  }

  /// <summary>Exception class representing errors returned from Berkeley DB API calls,
  /// or inappropriate use of the .NET bindings.</summary>
  public class BdbException: ApplicationException
  {
    DbRetVal error;

    public BdbException() { }

    public BdbException(string message) : base(message) { }

    public BdbException(string message, Exception e) : base(message, e) { }

    public BdbException(DbRetVal error) {
      this.error = error;
    }

    public BdbException(DbRetVal error, string message) : base(message) {
      this.error = error;
    }

    public DbRetVal Error {
      get { return error; }
    }
  }

  [UnmanagedFunctionPointer(Compile.CallConv), CLSCompliant(false), SuppressUnmanagedCodeSecurity]
  public unsafe delegate void* MallocFcn(uint size);

  [UnmanagedFunctionPointer(Compile.CallConv), CLSCompliant(false), SuppressUnmanagedCodeSecurity]
  public unsafe delegate void* ReallocFcn(void* ptr, uint size);

  [UnmanagedFunctionPointer(Compile.CallConv), CLSCompliant(false), SuppressUnmanagedCodeSecurity]
  public unsafe delegate void FreeFcn(void* ptr);

  [UnmanagedFunctionPointer(Compile.CallConv), CLSCompliant(false), SuppressUnmanagedCodeSecurity]
  public unsafe delegate DbRetVal VerifyCallback(IntPtr handle, byte* arg);

  /// <summary>General Berkeley DB API return code.</summary>
  /// <remarks>Also includes framework specific custom codes such as those returned from a call-back.</remarks>
  public enum DbRetVal: int
  {
    /* Error codes for .NET wrapper. 
     * Keep in sync with error strings defined in Util.dotNetStr.
     */
    KEYGEN_FAILED = -40999,         /* Key generator callback failed. */
    APPEND_RECNO_FAILED = -40998,   /* Append record number callback failed. */
    DUPCOMP_FAILED = -40997,        /* Duplicate comparison callback failed. */
    BTCOMP_FAILED = -40996,         /* BTree key comparison callback failed. */
    BTPREFIX_FAILED = -40995,       /* BTree prefix comparison callback failed. */
    HHASH_FAILED = -40994,          /* Hash function callback failed. */
    FEEDBACK_FAILED = -40993,       /* Feedback callback failed. */
    PANICCALL_FAILED = -40992,      /* Panic callback failed. */
    APP_RECOVER_FAILED = -40991,    /* Application recovery callback failed. */
    VERIFY_FAILED = -40990,         /* Verify callback failed. */
    REPSEND_FAILED = -40899,        /* Replication callback failed. */
    PAGE_IN_FAILED = -40898,        /* Cache page-in callback failed. */
    PAGE_OUT_FAILED = -40897,       /* Cache page-out callback failed. */
#if BDB_4_5_20
    EVENT_NOTIFY_FAILED = -40896,   /* Event notification failed. */
    ISALIVE_FAILED = -40895,        /* IsAlive callback failed. */
    THREADID_FAILED = -40894,       /* ThreadId callback failed. */
    THREADID_STRING_FAILED = -40983,/* ThreadIdString callback failed. */
#endif

    /* DB (public) error return codes. Range reserved: -30,800 to -30,999 */
    BUFFER_SMALL = -30999,          /* User memory too small for return. */
    DONOTINDEX = -30998,            /* "Null" return from 2ndary callbk. */
    KEYEMPTY = -30997,              /* Key/data deleted or never created. */
    KEYEXIST = -30996,              /* The key/data pair already exists. */
    LOCK_DEADLOCK = -30995,         /* Deadlock. */
    LOCK_NOTGRANTED = -30994,       /* Lock unavailable. */
    LOG_BUFFER_FULL = -30993,       /* In-memory log buffer full. */
    NOSERVER = -30992,              /* Server panic return. */
    NOSERVER_HOME = -30991,         /* Bad home sent to server. */
    NOSERVER_ID = -30990,           /* Bad ID sent to server. */
    NOTFOUND = -30989,              /* Key/data pair not found (EOF). */
    OLD_VERSION = -30988,           /* Out-of-date version. */
    PAGE_NOTFOUND = -30987,         /* Requested page not found. */
    REP_DUPMASTER = -30986,         /* There are two masters. */
    REP_HANDLE_DEAD = -30985,       /* Rolled back a commit. */
    REP_HOLDELECTION = -30984,      /* Time to hold an election. */
#if BDB_4_3_29
    REP_ISPERM = -30983,            /* Cached not written perm written.*/
    REP_NEWMASTER = -30982,         /* We have learned of a new master. */
    REP_NEWSITE = -30981,           /* New site entered system. */
    REP_NOTPERM = -30980,           /* Permanent log record not written. */
    REP_STARTUPDONE = -30979,       /* Client startup complete. */
    REP_UNAVAIL = -30978,           /* Site cannot currently be reached. */
    RUNRECOVERY = -30977,           /* Panic return. */
    SECONDARY_BAD = -30976,         /* Secondary index corrupt. */
    VERIFY_BAD = -30975,            /* Verify failed; bad format. */
    VERSION_MISMATCH = -30974,      /* Environment version mismatch. */
    /* DB (private) error return codes. */
    ALREADY_ABORTED = -30899,
    DELETED = -30898,               /* Recovery file marked deleted. */
    LOCK_NOTEXIST = -30897,         /* Object to lock is gone. */
    NEEDSPLIT = -30896,             /* Page needs to be split. */
    REP_EGENCHG = -30895,           /* Egen changed while in election. */
    REP_LOGREADY = -30894,          /* Rep log ready for recovery. */
    REP_PAGEDONE = -30893,          /* This page was already done. */
    SURPRISE_KID = -30892,          /* Child commit where parent didn't know it was a parent. */
    SWAPBYTES = -30891,             /* Database needs byte swapping. */
    TIMEOUT = -30890,               /* Timed out waiting for election. */
    TXN_CKP = -30889,               /* Encountered ckp record in log. */
    VERIFY_FATAL = -30888,          /* DB->verify cannot proceed. */
#endif
#if BDB_4_5_20
    REP_IGNORE = -30983,            /* This msg should be ignored.*/
    REP_ISPERM = -30982,            /* Cached not written perm written.*/
    REP_JOIN_FAILURE = -30981,      /* Unable to join replication group. */
    REP_LOCKOUT = -30980,           /* API/Replication lockout now. */
    REP_NEWMASTER = -30979,         /* We have learned of a new master. */
    REP_NEWSITE = -30978,           /* New site entered system. */
    REP_NOTPERM = -30977,           /* Permanent log record not written. */
    REP_UNAVAIL = -30976,           /* Site cannot currently be reached. */
    RUNRECOVERY = -30975,           /* Panic return. */
    SECONDARY_BAD = -30974,         /* Secondary index corrupt. */
    VERIFY_BAD = -30973,            /* Verify failed; bad format. */
    VERSION_MISMATCH = -30972,      /* Environment version mismatch. */
    /* DB (private) error return codes. */
    ALREADY_ABORTED = -30899,
    DELETED = -30898,               /* Recovery file marked deleted. */
    NEEDSPLIT = -30897,             /* Page needs to be split. */
    REP_BULKOVF = -30896,           /* Rep bulk buffer overflow. */
    REP_EGENCHG = -30895,           /* Egen changed while in election. */
    REP_LOGREADY = -30894,          /* Rep log ready for recovery. */
    REP_PAGEDONE = -30893,          /* This page was already done. */
    SURPRISE_KID = -30892,          /* Child commit where parent didn't know it was a parent. */
    SWAPBYTES = -30891,             /* Database needs byte swapping. */
    TIMEOUT = -30890,               /* Timed out waiting for election. */
    TXN_CKP = -30889,               /* Encountered ckp record in log. */
    VERIFY_FATAL = -30888,          /* DB->verify cannot proceed. */
#endif

    /* No error. */
    SUCCESS = 0,

    /* Error Codes defined in C runtime (errno.h) */
    EPERM = 1,
    ENOENT = 2,
    ESRCH = 3,
    EINTR = 4,
    EIO = 5,
    ENXIO = 6,
    E2BIG = 7,
    ENOEXEC = 8,
    EBADF = 9,
    ECHILD = 10,
    EAGAIN = 11,
    ENOMEM = 12,
    EACCES = 13,
    EFAULT = 14,
    EBUSY = 16,
    EEXIST = 17,
    EXDEV = 18,
    ENODEV = 19,
    ENOTDIR = 20,
    EISDIR = 21,
    ENFILE = 23,
    EMFILE = 24,
    ENOTTY = 25,
    EFBIG = 27,
    ENOSPC = 28,
    ESPIPE = 29,
    EROFS = 30,
    EMLINK = 31,
    EPIPE = 32,
    EDOM = 33,
    EDEADLK = 36,
    ENAMETOOLONG = 38,
    ENOLCK = 39,
    ENOSYS = 40,
    ENOTEMPTY = 41,
    /* Error codes used in the Secure CRT functions */
    EINVAL = 22,
    ERANGE = 34,
    EILSEQ = 42,
    STRUNCATE = 80
  }

  /* Flags private to db_create. */
  public enum DbCreateFlags: int
  {
    None = 0,
#if BDB_4_3_29
    RepCreate = DbConst.DB_REP_CREATE,
#endif
    XACreate = DbConst.DB_XA_CREATE
  }

  /* Flags private to db_env_create. */
  public enum EnvCreateFlags: int
  {
    None = 0,
    RpcClient = DbConst.DB_RPCCLIENT
  }

  /// <summary>Interface to the Berkeley DB library.</summary>
  [CLSCompliant(false), SuppressUnmanagedCodeSecurity]
  public unsafe static class LibDb
  {
#if BDB_4_3_29
    public const string libDb  =  "libdb43.dll";
#endif
#if BDB_4_5_20
    public const string libDb = "libdb45.dll";
#endif

    [DllImport(libDb, CallingConvention = Compile.CallConv)]
    public static extern DbRetVal
    db_create(out DB* dbp, DB_ENV* dbenv, DbCreateFlags flags);

    [DllImport(libDb, CallingConvention = Compile.CallConv)]
    public static extern DbRetVal
    db_env_create(out DB_ENV* dbenvp, EnvCreateFlags flags);

    // flags unused, must be 0
    [DllImport(libDb, CallingConvention = Compile.CallConv)]
    public static extern DbRetVal
    db_sequence_create(out DB_SEQUENCE* seq, DB* db, UInt32 flags);

    [DllImport(libDb, EntryPoint = "db_strerror", CallingConvention = Compile.CallConv)]
    static extern IntPtr _db_strerror(int error);

    public static string db_strerror(DbRetVal error) {
      IntPtr errStr = _db_strerror((int)error);
      return Marshal.PtrToStringAnsi(errStr);
    }

    [DllImport(libDb, EntryPoint = "db_version", CallingConvention = Compile.CallConv)]
    static extern IntPtr _db_version(ref int major, ref int minor, ref int patch);

    public static string db_version(ref int major, ref int minor, ref int patch) {
      IntPtr verStr = _db_version(ref major, ref minor, ref patch);
      return Marshal.PtrToStringAnsi(verStr);
    }

    // it seems Dll data members cannot be imported using the DllImportAttribute
    // [DllImport(libDb)]
    // public static extern XA_SWITCH* db_xa_switch;

    [DllImport("Kernel32", SetLastError = true)]
    static extern uint GetModuleHandle(string name);

    [DllImport("Kernel32", SetLastError = true)]
    static extern IntPtr GetProcAddress(uint module, string name);

    // this works on Windows only!
    public static unsafe XA_SWITCH* db_xa_switch {
      get {
        uint module = GetModuleHandle(libDb);
        if (module == 0) {
          // maybe not yet loaded, call any function to load Dll
          void* dummyPtr;
          os_umalloc(null, 0, out dummyPtr);
          // now try again
          module = GetModuleHandle(libDb);
        }
        if (module == 0)
          throw new BdbException("Cannot load '" + libDb + "'.");
        XA_SWITCH* xaSwitchPtr = (XA_SWITCH*)GetProcAddress(module, "db_xa_switch");
        if (xaSwitchPtr == null)
          throw new BdbException("Cannot get address of 'db_xa_switch'.");
        return xaSwitchPtr;
      }
    }

    [DllImport(libDb, CallingConvention = Compile.CallConv)]
    public static extern int
    log_compare(DB_LSN* lsn0, DB_LSN* lsn1);

    [DllImport(libDb, EntryPoint = "__os_umalloc", CallingConvention = Compile.CallConv)]
    public static extern DbRetVal
    os_umalloc(DB_ENV* dbenv, uint size, out void* ptr);

    /* __os_urealloc not yet exported
    [DllImport(libDb, EntryPoint = "__os_urealloc", CallingConvention = Compile.CallConv)]
    public static extern DbRetVal
    os_urealloc(DB_ENV* dbenv, uint size, ref void* ptr);
    */

    [DllImport(libDb, EntryPoint = "__os_ufree", CallingConvention = Compile.CallConv)]
    public static extern void
    os_ufree(DB_ENV* dbenv, void* ptr);

    // callback must be of type VerifyCallback
    [DllImport(libDb, EntryPoint = "__db_verify_internal", CallingConvention = Compile.CallConv)]
    public static extern DbRetVal
    db_verify_internal(DB* db, byte* file, byte* database, IntPtr handle, IntPtr callback, UInt32 flags);
  }
}
