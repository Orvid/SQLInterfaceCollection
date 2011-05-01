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
  [Flags]
  public enum EnvFlags: int
  {
    AutoCommit = DbConst.DB_AUTO_COMMIT,
    CdbAllDb = DbConst.DB_CDB_ALLDB,
    DirectDb = DbConst.DB_DIRECT_DB,
    DirectLog = DbConst.DB_DIRECT_LOG,
#if BDB_4_5_20
    DSyncDb = DbConst.DB_DSYNC_DB,
#endif
    DSyncLog = DbConst.DB_DSYNC_LOG,
    LogAutoRemove = DbConst.DB_LOG_AUTOREMOVE,
    LogInMemory = DbConst.DB_LOG_INMEMORY,
    NoLocking = DbConst.DB_NOLOCKING,
#if BDB_4_5_20
    MultiVersion = DbConst.DB_MULTIVERSION,
#endif
    NoMMap = DbConst.DB_NOMMAP,
    NoPanic = DbConst.DB_NOPANIC,
    Overwrite = DbConst.DB_OVERWRITE,
    PanicEnvironment = DbConst.DB_PANIC_ENVIRONMENT,
    RegionInit = DbConst.DB_REGION_INIT,
    TimeNotGranted = DbConst.DB_TIME_NOTGRANTED,
    TxnNoSync = DbConst.DB_TXN_NOSYNC,
#if BDB_4_5_20
    TxnSnapshot = DbConst.DB_TXN_SNAPSHOT,
#endif
    TxnWriteNoSync = DbConst.DB_TXN_WRITE_NOSYNC,
    YieldCpu = DbConst.DB_YIELDCPU
  }

#if BDB_4_3_29
  /* Database Environment handle. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DB_ENV
  {
    #region Private Fields (to calculate offset to API function pointers)

    IntPtr db_errcall; /* Error message callback. */
    /* how to get/set a delegate with an IntPtr field
    public DbEnvErrorCall ErrCall {
      get { return (DbEnvErrorCall)Marshal.GetDelegateForFunctionPointer(db_errcall, typeof(DbEnvErrorCall)); }
      set { db_errcall = Marshal.GetFunctionPointerForDelegate(value); }
    }
    */
    FILE* db_errfile;      /* Error message file stream. */
    byte* db_errpfx;       /* Error message prefix. */
    FILE* db_msgfile;      /* Statistics message file stream. */
    IntPtr db_msgcall;     /* Statistics message callback. */
    /* Other Callbacks. */
    IntPtr db_feedback;
    IntPtr db_paniccall;
    /* App-specified alloc functions. */
    IntPtr db_malloc;
    IntPtr db_realloc;
    IntPtr db_free;

    UInt32 verbose;           /* Verbose output. */

    /* Documented, but no access method defined. */
    public IntPtr app_private;    /* Application-private handle. (void*) */

    IntPtr app_dispatch;      /* User-specified transaction recovery dispatch. */

    /* Locking. */
    byte* lk_conflicts;       /* Two dimensional conflict matrix. */
    int lk_modes;             /* Number of lock modes in table. */
    UInt32 lk_max;            /* Maximum number of locks. */
    UInt32 lk_max_lockers;    /* Maximum number of lockers. */
    UInt32 lk_max_objects;    /* Maximum number of locked objects. */
    UInt32 lk_detect;         /* Deadlock detect on all conflicts. */
    UInt32 lk_timeout;        /* Lock timeout period. (typedef UInt32  db_timeout_t;) */

    /* Logging. */
    UInt32 lg_bsize;          /* Buffer size. */
    UInt32 lg_size;           /* Log file size. */
    UInt32 lg_regionmax;      /* Region size. */

    /* Memory pool. */
    UInt32 mp_gbytes;         /* Cachesize: GB. */
    UInt32 mp_bytes;          /* Cachesize: Bytes. */
    uint mp_ncache;           /* Number of cache regions. */
    uint mp_mmapsize;         /* Maximum file size for mmap. */
    int mp_maxopenfd;         /* Maximum open file descriptors. */
    int mp_maxwrite;          /* Maximum buffers to write. */
    int mp_maxwrite_sleep;    /* Sleep after writing max buffers. */

    /* Replication */
    int rep_eid;              /* environment id. */

    IntPtr rep_send;          /* Send function. */

    /* Transactions. */
    UInt32 tx_max;            /* Maximum number of transactions. */
#if _USE_32BIT_TIME_T
    int tx_timestamp;         /* Recover to specific timestamp. */
#else
    long tx_timestamp;        /* Recover to specific timestamp. */
#endif
    UInt32 tx_timeout;        /* Timeout for transactions. */

    /*******************************************************
    * Private: owned by DB.
    *******************************************************/
    /* User files, paths. */
    byte* db_home;            /* Database home. */
    byte* db_log_dir;         /* Database log file directory. */
    byte* db_tmp_dir;         /* Database tmp file directory. */

    byte** db_data_dir;       /* Database data file directories. */
    int data_cnt;             /* Database data file slots. */
    int data_next;            /* Next Database data file slot. */

    int db_mode;              /* Default open permissions. */
    int dir_mode;             /* Intermediate directory perms. */
    UInt32 env_lid;           /* Locker ID in non-threaded handles. */
    UInt32 open_flags;        /* Flags passed to DB_ENV->open. */

    void* reginfo;            /* REGINFO structure reference. */
    DB_FH* lockfhp;           /* fcntl(2) locking file handle. */

    IntPtr* recover_dtab;     /* Dispatch table for recover funcs. */
    uint recover_dtab_size;   /* Slots in the dispatch table. */

    void* cl_handle;          /* RPC: remote client handle. */
    uint cl_id;               /* RPC: remote client env id. */

    int db_ref;               /* DB reference count. */

    int shm_key;              /* shmget(2) key. (long = int) */
    UInt32 tas_spins;         /* test-and-set spins. */

    /*
    * List of open DB handles for this DB_ENV, used for cursor
    * adjustment.  Must be protected for multi-threaded support.
    *
    * !!!
    * As this structure is allocated in per-process memory, the
    * mutex may need to be stored elsewhere on architectures unable
    * to support mutexes in heap memory, e.g. HP/UX 9.
    *
    * !!!
    * Explicit representation of structure in queue.h.
    * LIST_HEAD(dblist, __db);
    */
    DB_MUTEX* dblist_mutexp;  /* Mutex. */

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct DB_LIST
    {
      DB* lh_first;
    }
    DB_LIST dblist;

    /*
    * XA support.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_ENTRY(__db_env) links;
    * TAILQ_HEAD(xa_txn, __db_txn);
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct LINKS
    {
      DB_ENV* tqe_next;
      DB_ENV** tqe_prev;
    }
    LINKS links;

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct XA_TXN
    {           /* XA Active Transactions. */
      DB_TXN* tqh_first;
      DB_TXN** tqh_last;
    }
    XA_TXN xa_txn;
    
    int xa_rmid;               /* XA Resource Manager ID. */

    /* API-private structure. */
    // void* api1_internal;      /* C++, Perl API private */
    public IntPtr api_internal;  /* api1_internal renamed for use with dotNET */
    void* api2_internal;      /* Java API private */

    byte* passwd;             /* Cryptography support. */
    uint passwd_len;          /* (size_t = uint) */
    void* crypto_handle;      /* Primary handle. */
    DB_MUTEX* mt_mutexp;      /* Mersenne Twister mutex. */
    int mti;                  /* Mersenne Twister index. */
    uint* mt;                 /* Mersenne Twister state vector. (u_long = unsigned long = uint) */

    #endregion

    #region API Methods

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetUIntFcn(DB_ENV* dbenv, out UInt32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetUIntFcn(DB_ENV* dbenv, UInt32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetIntPtrFcn(DB_ENV* dbenv, IntPtr value);

    IntPtr close;
    // flags unused, must be 0
    public SetUIntFcn Close {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(close, typeof(SetUIntFcn)); }
    }

    IntPtr dbremove;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DbRemoveFcn(
      DB_ENV* dbenv, DB_TXN* txnid, byte* file, byte* database, UInt32 flags);
    public DbRemoveFcn DbRemove {
      get { return (DbRemoveFcn)Marshal.GetDelegateForFunctionPointer(dbremove, typeof(DbRemoveFcn)); }
    }

    IntPtr dbrename;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DbRenameFcn(
      DB_ENV* dbenv, DB_TXN* txnid, byte* file, byte* database, byte* newname, UInt32 flags);
    public DbRenameFcn DbRename {
      get { return (DbRenameFcn)Marshal.GetDelegateForFunctionPointer(dbrename, typeof(DbRenameFcn)); }
    }

    IntPtr err;
    // C style varargs can be translated using the __arglist keyword
    // C declaration: void (*err) __P((DB *, int, const char *, ...));
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrFcn(DB_ENV* dbenv, int error, byte* fmt/*, __arglist*/);
    public ErrFcn Err {
      get { return (ErrFcn)Marshal.GetDelegateForFunctionPointer(err, typeof(ErrFcn)); }
    }
    // public void Err(DB* db, int error, byte* fmt/*, __arglist*/) {
    //   ErrFcn(db, error, fmt/*, __arglist*/);
    // }

    IntPtr errx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrxFcn(DB_ENV* dbenv, byte* fmt/*, __arglist*/);
    public ErrxFcn Errx {
      get { return (ErrxFcn)Marshal.GetDelegateForFunctionPointer(errx, typeof(ErrxFcn)); }
    }
    // public void Errx(DB* db, byte* fmt/*, __arglist*/) {
    //   ErrxFcn(db, fmt/*, __arglist*/);
    // }

    IntPtr open;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal OpenFcn(DB_ENV* dbenv, byte* db_home, UInt32 flags, int mode);
    public OpenFcn Open {
      get { return (OpenFcn)Marshal.GetDelegateForFunctionPointer(open, typeof(OpenFcn)); }
    }

    IntPtr remove;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RemoveFcn(DB_ENV* dbenv, byte* home, UInt32 flags);
    public RemoveFcn Remove {
      get { return (RemoveFcn)Marshal.GetDelegateForFunctionPointer(remove, typeof(RemoveFcn)); }
    }

    IntPtr stat_print;
    public SetUIntFcn StatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(stat_print, typeof(SetUIntFcn)); }
    }

    /* House-keeping. */
    IntPtr fileid_reset;
    IntPtr is_bigendian;
    IntPtr lsn_reset;
    IntPtr prdbt;

    /* Setters/getters. */
    IntPtr set_alloc;

    IntPtr set_app_dispatch;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AppRecoverFcn(DB_ENV* dbenv, ref DBT log_rec, DB_LSN* lsn, DB_RECOPS op);
    public SetIntPtrFcn SetAppDispatch {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_app_dispatch, typeof(SetIntPtrFcn)); }
    }

    IntPtr get_data_dirs;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetDataDirsFcn(DB_ENV* dbenv, out byte** dirs);
    public GetDataDirsFcn GetDataDirs {
      get { return (GetDataDirsFcn)Marshal.GetDelegateForFunctionPointer(get_data_dirs, typeof(GetDataDirsFcn)); }
    }

    IntPtr set_data_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetDataDirFcn(DB_ENV* dbenv, byte* dir);
    public SetDataDirFcn SetDataDir {
      get { return (SetDataDirFcn)Marshal.GetDelegateForFunctionPointer(set_data_dir, typeof(SetDataDirFcn)); }
    }

    IntPtr get_encrypt_flags;
    public GetUIntFcn GetEncryptFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_encrypt_flags, typeof(GetUIntFcn)); }
    }

    IntPtr set_encrypt;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetEncryptFcn(DB_ENV* dbenv, byte* passwd, EncryptMode flags);
    public SetEncryptFcn SetEncrypt {
      get { return (SetEncryptFcn)Marshal.GetDelegateForFunctionPointer(set_encrypt, typeof(SetEncryptFcn)); }
    }

    IntPtr set_errcall;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrCallFcn(DB_ENV* dbenv, byte* errpfx, byte* msg);
    public SetIntPtrFcn SetErrCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_errcall, typeof(SetIntPtrFcn)); }
    }

    IntPtr get_errfile;
    IntPtr set_errfile;

    IntPtr get_errpfx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void GetErrPfxFcn(DB_ENV* dbenv, out byte* errpfx);
    public GetErrPfxFcn GetErrPfx {
      get { return (GetErrPfxFcn)Marshal.GetDelegateForFunctionPointer(get_errpfx, typeof(GetErrPfxFcn)); }
    }

    IntPtr set_errpfx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void SetErrPfxFcn(DB_ENV* dbenv, byte* errpfx);
    public SetErrPfxFcn SetErrPfx {
      get { return (SetErrPfxFcn)Marshal.GetDelegateForFunctionPointer(set_errpfx, typeof(SetErrPfxFcn)); }
    }

    IntPtr set_feedback;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void FeedbackFcn(DB_ENV* dbenv, int opcode, int percent);
    public SetIntPtrFcn SetFeedback {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_feedback, typeof(SetIntPtrFcn)); }
    }

    IntPtr get_flags;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFlagsFcn(DB_ENV* dbenv, out EnvFlags flags);
    public GetFlagsFcn GetFlags {
      get { return (GetFlagsFcn)Marshal.GetDelegateForFunctionPointer(get_flags, typeof(GetFlagsFcn)); }
    }

    IntPtr set_flags;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetFlagsFcn(DB_ENV* dbenv, EnvFlags flags, int onoff);
    public SetFlagsFcn SetFlags {
      get { return (SetFlagsFcn)Marshal.GetDelegateForFunctionPointer(set_flags, typeof(SetFlagsFcn)); }
    }

    IntPtr get_home;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetHomeFcn(DB_ENV* dbenv, out byte* home);
    public GetHomeFcn GetHome {
      get { return (GetHomeFcn)Marshal.GetDelegateForFunctionPointer(get_home, typeof(GetHomeFcn)); }
    }

    IntPtr set_intermediate_dir;

    IntPtr get_open_flags;
    public GetUIntFcn GetOpenFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_open_flags, typeof(GetUIntFcn)); }
    }

    IntPtr set_paniccall;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void PanicCallFcn(DB_ENV* dbenv, int errval);
    public SetIntPtrFcn SetPanicCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_paniccall, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_rpc_server;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetRpcServerFcn(
      DB_ENV* dbenv, void* client, byte* host, int cl_timeout, int sv_timeout, UInt32 flags);
    public SetRpcServerFcn SetRpcServer {
      get { return (SetRpcServerFcn)Marshal.GetDelegateForFunctionPointer(set_rpc_server, typeof(SetRpcServerFcn)); }
    }

    IntPtr get_shm_key;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetShmKeyFcn(DB_ENV* dbenv, out int shm_key);
    public GetShmKeyFcn GetShmKey {
      get { return (GetShmKeyFcn)Marshal.GetDelegateForFunctionPointer(get_shm_key, typeof(GetShmKeyFcn)); }
    }

    IntPtr set_shm_key;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetShmKeyFcn(DB_ENV* dbenv, int shm_key);
    public SetShmKeyFcn SetShmKey {
      get { return (SetShmKeyFcn)Marshal.GetDelegateForFunctionPointer(set_shm_key, typeof(SetShmKeyFcn)); }
    }

    IntPtr set_msgcall;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void MsgCallFcn(DB_ENV* dbenv, byte* msg);
    public SetIntPtrFcn SetMsgCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_msgcall, typeof(SetIntPtrFcn)); }
    }

    IntPtr get_msgfile;
    IntPtr set_msgfile;

    IntPtr get_tas_spins;
    public GetUIntFcn GetTasSpins {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_tas_spins, typeof(GetUIntFcn)); }
    }

    IntPtr set_tas_spins;
    public SetUIntFcn SetTasSpins {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_tas_spins, typeof(SetUIntFcn)); }
    }

    IntPtr get_tmp_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetTmpDirFcn(DB_ENV* dbenv, out byte* dir);
    public GetTmpDirFcn GetTmpDir {
      get { return (GetTmpDirFcn)Marshal.GetDelegateForFunctionPointer(get_tmp_dir, typeof(GetTmpDirFcn)); }
    }

    IntPtr set_tmp_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetTmpDirFcn(DB_ENV* dbenv, byte* dir);
    public SetTmpDirFcn SetTmpDir {
      get { return (SetTmpDirFcn)Marshal.GetDelegateForFunctionPointer(set_tmp_dir, typeof(SetTmpDirFcn)); }
    }

    IntPtr get_verbose;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetVerboseFcn(DB_ENV* dbenv, uint which, out int onoff);
    public GetVerboseFcn GetVerbose {
      get { return (GetVerboseFcn)Marshal.GetDelegateForFunctionPointer(get_verbose, typeof(GetVerboseFcn)); }
    }

    IntPtr set_verbose;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetVerboseFcn(DB_ENV* dbenv, uint which, int onoff);
    public SetVerboseFcn SetVerbose {
      get { return (SetVerboseFcn)Marshal.GetDelegateForFunctionPointer(set_verbose, typeof(SetVerboseFcn)); }
    }

    /* Log handle and methods. */
    void* lg_handle;

    IntPtr get_lg_bsize;
    public GetUIntFcn GetLogBufSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lg_bsize, typeof(GetUIntFcn)); }
    }
    
    IntPtr set_lg_bsize;
    public SetUIntFcn SetLogBufSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lg_bsize, typeof(SetUIntFcn)); }
    }

    IntPtr get_lg_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetLogDirFcn(DB_ENV* dbenv, out byte* dir);
    public GetLogDirFcn GetLogDir {
      get { return (GetLogDirFcn)Marshal.GetDelegateForFunctionPointer(get_lg_dir, typeof(GetLogDirFcn)); }
    }

    IntPtr set_lg_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetLogDirFcn(DB_ENV* dbenv, byte* dir);
    public SetLogDirFcn SetLogDir {
      get { return (SetLogDirFcn)Marshal.GetDelegateForFunctionPointer(set_lg_dir, typeof(SetLogDirFcn)); }
    }

    IntPtr get_lg_max;
    public GetUIntFcn GetMaxLogFileSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lg_max, typeof(GetUIntFcn)); }
    }

    IntPtr set_lg_max;
    public SetUIntFcn SetMaxLogFileSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lg_max, typeof(SetUIntFcn)); }
    }

    IntPtr get_lg_regionmax;
    public GetUIntFcn GetMaxLogRegionSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lg_regionmax, typeof(GetUIntFcn)); }
    }

    IntPtr set_lg_regionmax;
    public SetUIntFcn SetMaxLogRegionSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lg_regionmax, typeof(SetUIntFcn)); }
    }

    IntPtr log_archive;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogArchiveFcn(DB_ENV* dbenv, out byte** list, UInt32 flags);
    public LogArchiveFcn LogArchive {
      get { return (LogArchiveFcn)Marshal.GetDelegateForFunctionPointer(log_archive, typeof(LogArchiveFcn)); }
    }

    IntPtr log_cursor;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogCursorFcn(DB_ENV* dbenv, out DB_LOGC* logc, UInt32 flags);
    public LogCursorFcn LogCursor {
      get { return (LogCursorFcn)Marshal.GetDelegateForFunctionPointer(log_cursor, typeof(LogCursorFcn)); }
    }

    IntPtr log_file;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogFileFcn(DB_ENV* dbenv, ref DB_LSN lsn, byte* name, uint len);
    public LogFileFcn LogFile {
      get { return (LogFileFcn)Marshal.GetDelegateForFunctionPointer(log_file, typeof(LogFileFcn)); }
    }

    IntPtr log_flush;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogFlushFcn(DB_ENV* dbenv, DB_LSN* lsn);
    public LogFlushFcn LogFlush {
      get { return (LogFlushFcn)Marshal.GetDelegateForFunctionPointer(log_flush, typeof(LogFlushFcn)); }
    }

    IntPtr log_put;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogPutFcn(DB_ENV* dbenv, out DB_LSN lsn, ref DBT data, UInt32 flags);
    public LogPutFcn LogPut {
      get { return (LogPutFcn)Marshal.GetDelegateForFunctionPointer(log_put, typeof(LogPutFcn)); }
    }

    IntPtr log_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogStatFcn(DB_ENV* dbenv, out DB_LOG_STAT* stat, UInt32 flags);
    public LogStatFcn LogStat {
      get { return (LogStatFcn)Marshal.GetDelegateForFunctionPointer(log_stat, typeof(LogStatFcn)); }
    }

    IntPtr log_stat_print;
    public SetUIntFcn LogStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(log_stat_print, typeof(SetUIntFcn)); }
    }

    /* Lock handle and methods. */
    void* lk_handle;

    IntPtr get_lk_conflicts;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetLockConflictsFcn(DB_ENV* dbenv, ref byte* conflicts, ref int nmodes);
    public GetLockConflictsFcn GetLockConflicts {
      get { return (GetLockConflictsFcn)Marshal.GetDelegateForFunctionPointer(get_lk_conflicts, typeof(GetLockConflictsFcn)); }
    }

    IntPtr set_lk_conflicts;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetLockConflictsFcn(DB_ENV* dbenv, byte* conflicts, int nmodes);
    public SetLockConflictsFcn SetLockConflicts {
      get { return (SetLockConflictsFcn)Marshal.GetDelegateForFunctionPointer(set_lk_conflicts, typeof(SetLockConflictsFcn)); }
    }

    IntPtr get_lk_detect;
    public GetUIntFcn GetLockDetect {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lk_detect, typeof(GetUIntFcn)); }
    }

    IntPtr set_lk_detect;
    public SetUIntFcn SetLockDetect {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lk_detect, typeof(SetUIntFcn)); }
    }

    IntPtr set_lk_max;

    IntPtr get_lk_max_locks;
    public GetUIntFcn GetMaxLocks {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lk_max_locks, typeof(GetUIntFcn)); }
    }

    IntPtr set_lk_max_locks;
    public SetUIntFcn SetMaxLocks {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lk_max_locks, typeof(SetUIntFcn)); }
    }

    IntPtr get_lk_max_lockers;
    public GetUIntFcn GetMaxLockers {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lk_max_lockers, typeof(GetUIntFcn)); }
    }

    IntPtr set_lk_max_lockers;
    public SetUIntFcn SetMaxLockers {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lk_max_lockers, typeof(SetUIntFcn)); }
    }

    IntPtr get_lk_max_objects;
    public GetUIntFcn GetMaxObjects {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lk_max_objects, typeof(GetUIntFcn)); }
    }

    IntPtr set_lk_max_objects;
    public SetUIntFcn SetMaxObjects {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lk_max_objects, typeof(SetUIntFcn)); }
    }

    IntPtr lock_detect;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockDetectFcn(DB_ENV* dbenv, UInt32 flags, UInt32 atype, ref int aborted);
    public LockDetectFcn LockDetect {
      get { return (LockDetectFcn)Marshal.GetDelegateForFunctionPointer(lock_detect, typeof(LockDetectFcn)); }
    }

    IntPtr lock_get;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockGetFcn(DB_ENV* dbenv, UInt32 locker, UInt32 flags, ref DBT obj,
      DB_LOCKMODE lock_mode, ref DB_LOCK lck);
    public LockGetFcn LockGet {
      get { return (LockGetFcn)Marshal.GetDelegateForFunctionPointer(lock_get, typeof(LockGetFcn)); }
    }

    IntPtr lock_put;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockPutFcn(DB_ENV* dbenv, ref DB_LOCK lck);
    public LockPutFcn LockPut {
      get { return (LockPutFcn)Marshal.GetDelegateForFunctionPointer(lock_put, typeof(LockPutFcn)); }
    }

    IntPtr lock_id;
    public GetUIntFcn LockId {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(lock_id, typeof(GetUIntFcn)); }
    }

    IntPtr lock_id_free;
    public SetUIntFcn LockIdFree {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(lock_id_free, typeof(SetUIntFcn)); }
    }

    IntPtr lock_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockStatFcn(DB_ENV* dbenv, out DB_LOCK_STAT* stat, UInt32 flags);
    public LockStatFcn LockStat {
      get { return (LockStatFcn)Marshal.GetDelegateForFunctionPointer(lock_stat, typeof(LockStatFcn)); }
    }

    IntPtr lock_stat_print;
    public SetUIntFcn LockStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(lock_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr lock_vec;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockVectorFcn(DB_ENV* dbenv, UInt32 locker, UInt32 flags,
      DB_LOCKREQ* list, int nlist, out DB_LOCKREQ* elist);
    public LockVectorFcn LockVector {
      get { return (LockVectorFcn)Marshal.GetDelegateForFunctionPointer(lock_vec, typeof(LockVectorFcn)); }
    }

    /* Mpool handle and methods. */
    void* mp_handle;

    IntPtr get_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetCacheSizeFcn(DB_ENV* dbenv, out UInt32 gbytes, out UInt32 bytes, out int ncache);
    public GetCacheSizeFcn GetCacheSize {
      get { return (GetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(get_cachesize, typeof(GetCacheSizeFcn)); }
    }

    IntPtr set_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetCacheSizeFcn(DB_ENV* dbenv, UInt32 gbytes, UInt32 bytes, int ncache);
    public SetCacheSizeFcn SetCacheSize {
      get { return (SetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(set_cachesize, typeof(SetCacheSizeFcn)); }
    }

    IntPtr get_mp_mmapsize;
    public GetUIntFcn GetMpMMapSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_mp_mmapsize, typeof(GetUIntFcn)); }
    }

    IntPtr set_mp_mmapsize;
    public SetUIntFcn SetMpMMapSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_mp_mmapsize, typeof(SetUIntFcn)); }
    }

    IntPtr get_mp_max_openfd;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetMpMaxOpenFdFcn(DB_ENV* dbenv, out int maxOpenFd);
    public GetMpMaxOpenFdFcn GetMpMaxOpenFd {
      get { return (GetMpMaxOpenFdFcn)Marshal.GetDelegateForFunctionPointer(get_mp_max_openfd, typeof(GetMpMaxOpenFdFcn)); }
    }

    IntPtr set_mp_max_openfd;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetMpMaxOpenFdFcn(DB_ENV* dbenv, int maxOpenFd);
    public SetMpMaxOpenFdFcn SetMpMaxOpenFd {
      get { return (SetMpMaxOpenFdFcn)Marshal.GetDelegateForFunctionPointer(set_mp_max_openfd, typeof(SetMpMaxOpenFdFcn)); }
    }

    IntPtr get_mp_max_write;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetMpMaxWriteFcn(DB_ENV* dbenv, out int maxWrite, out int maxWriteSleep);
    public GetMpMaxWriteFcn GetMpMaxWrite {
      get { return (GetMpMaxWriteFcn)Marshal.GetDelegateForFunctionPointer(get_mp_max_write, typeof(GetMpMaxWriteFcn)); }
    }

    IntPtr set_mp_max_write;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetMpMaxWriteFcn(DB_ENV* dbenv, int maxWrite, int maxWriteSleep);
    public SetMpMaxWriteFcn SetMpMaxWrite {
      get { return (SetMpMaxWriteFcn)Marshal.GetDelegateForFunctionPointer(set_mp_max_write, typeof(SetMpMaxWriteFcn)); }
    }

    IntPtr memp_fcreate;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolFileCreateFcn(DB_ENV* dbenv, out DB_MPOOLFILE* dbmf, UInt32 flags);
    public MemPoolFileCreateFcn MemPoolFileCreate {
      get { return (MemPoolFileCreateFcn)Marshal.GetDelegateForFunctionPointer(memp_fcreate, typeof(MemPoolFileCreateFcn)); }
    }

    IntPtr memp_register;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PageInOutFcn(DB_ENV* dbenv, UInt32 pgno, void* pgaddr, ref DBT pgcookie);
    // pgin_fcn and pgout_fcn must be of type PageInOutFcn
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolRegisterFcn(DB_ENV* dbenv, int ftype, IntPtr pgin_fcn, IntPtr pgout_fcn);
    public MemPoolRegisterFcn MemPoolRegister {
      get { return (MemPoolRegisterFcn)Marshal.GetDelegateForFunctionPointer(memp_register, typeof(MemPoolRegisterFcn)); }
    }

    IntPtr memp_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolStatFcn(DB_ENV* dbenv, DB_MPOOL_STAT** gsp, DB_MPOOL_FSTAT*** fsp, UInt32 flags);
    public MemPoolStatFcn MemPoolStat {
      get { return (MemPoolStatFcn)Marshal.GetDelegateForFunctionPointer(memp_stat, typeof(MemPoolStatFcn)); }
    }

    IntPtr memp_stat_print;
    public SetUIntFcn MemPoolStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(memp_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr memp_sync;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolSyncFcn(DB_ENV* dbenv, DB_LSN* lsn);
    public MemPoolSyncFcn MemPoolSync {
      get { return (MemPoolSyncFcn)Marshal.GetDelegateForFunctionPointer(memp_sync, typeof(MemPoolSyncFcn)); }
    }

    IntPtr memp_trickle;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolTrickleFcn(DB_ENV* dbenv, int percent, out int nwrote);
    public MemPoolTrickleFcn MemPoolTrickle {
      get { return (MemPoolTrickleFcn)Marshal.GetDelegateForFunctionPointer(memp_trickle, typeof(MemPoolTrickleFcn)); }
    }

    /* Replication handle and methods. */
    void* rep_handle;

    IntPtr rep_elect;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepElectFcn(DB_ENV* dbenv, int nsites, int nvotes,
      int priority, UInt32 timeout, out int envid, UInt32 flags);
    public RepElectFcn RepElect {
      get { return (RepElectFcn)Marshal.GetDelegateForFunctionPointer(rep_elect, typeof(RepElectFcn)); }
    }

    IntPtr rep_flush;

    IntPtr rep_process_message;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepProcessMessageFcn(DB_ENV* dbenv, ref DBT control, ref DBT rec, ref int envid, out DB_LSN ret_lsnp);
    public RepProcessMessageFcn RepProcessMessage {
      get { return (RepProcessMessageFcn)Marshal.GetDelegateForFunctionPointer(rep_process_message, typeof(RepProcessMessageFcn)); }
    }

    IntPtr rep_start;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepStartFcn(DB_ENV* dbenv, ref DBT cdata, UInt32 flags);
    public RepStartFcn RepStart {
      get { return (RepStartFcn)Marshal.GetDelegateForFunctionPointer(rep_start, typeof(RepStartFcn)); }
    }

    IntPtr rep_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepStatFcn(DB_ENV* dbenv, out DB_REP_STAT* stat, UInt32 flags);
    public RepStatFcn RepStat {
      get { return (RepStatFcn)Marshal.GetDelegateForFunctionPointer(rep_stat, typeof(RepStatFcn)); }
    }

    IntPtr rep_stat_print;
    public SetUIntFcn RepStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(rep_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr get_rep_limit;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetRepLimitFcn(DB_ENV* dbenv, out UInt32 gbytes, out UInt32 bytes);
    public GetRepLimitFcn GetRepLimit {
      get { return (GetRepLimitFcn)Marshal.GetDelegateForFunctionPointer(get_rep_limit, typeof(GetRepLimitFcn)); }
    }

    IntPtr set_rep_limit;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetRepLimitFcn(DB_ENV* dbenv, UInt32 gbytes, UInt32 bytes);
    public SetRepLimitFcn SetRepLimit {
      get { return (SetRepLimitFcn)Marshal.GetDelegateForFunctionPointer(set_rep_limit, typeof(SetRepLimitFcn)); }
    }

    IntPtr set_rep_request;

    IntPtr set_rep_transport;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal
    RepSendFcn(DB_ENV* dbenv, ref DBT control, ref DBT rec, DB_LSN* lsnp, int envid, UInt32 flags);
    // send must be of type RepSendFcn
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetRepTransportFcn(DB_ENV* dbenv, int envid, IntPtr send);
    public SetRepTransportFcn SetRepTransport {
      get { return (SetRepTransportFcn)Marshal.GetDelegateForFunctionPointer(set_rep_transport, typeof(SetRepTransportFcn)); }
    }

    /* Txn handle and methods. */
    void* tx_handle;

    IntPtr get_tx_max;
    public GetUIntFcn GetTxMax {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_tx_max, typeof(GetUIntFcn)); }
    }

    IntPtr set_tx_max;
    public SetUIntFcn SetTxMax {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_tx_max, typeof(SetUIntFcn)); }
    }

    IntPtr get_tx_timestamp;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
#if _USE_32BIT_TIME_T
    public delegate DbRetVal GetTxTimestampFcn(DB_ENV* dbenv, out int txmax);
#else
    public delegate DbRetVal GetTxTimestampFcn(DB_ENV* dbenv, out long txmax);
#endif
    public GetTxTimestampFcn GetTxTimestamp {
      get { return (GetTxTimestampFcn)Marshal.GetDelegateForFunctionPointer(get_tx_timestamp, typeof(GetTxTimestampFcn)); }
    }

    IntPtr set_tx_timestamp;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
#if _USE_32BIT_TIME_T
    public delegate DbRetVal SetTxTimestampFcn(DB_ENV* dbenv, int txmax);
#else
    public delegate DbRetVal SetTxTimestampFcn(DB_ENV* dbenv, long txmax);
#endif
    public SetTxTimestampFcn SetTxTimestamp {
      get { return (SetTxTimestampFcn)Marshal.GetDelegateForFunctionPointer(set_tx_timestamp, typeof(SetTxTimestampFcn)); }
    }

    IntPtr txn_begin;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TxnBeginFcn(DB_ENV* dbenv, DB_TXN* parent, out DB_TXN* tid, UInt32 flags);
    public TxnBeginFcn TxnBegin {
      get { return (TxnBeginFcn)Marshal.GetDelegateForFunctionPointer(txn_begin, typeof(TxnBeginFcn)); }
    }

    IntPtr txn_checkpoint;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TxnCheckpointFcn(DB_ENV* dbenv, UInt32 kbyte, UInt32 min, UInt32 flags);
    public TxnCheckpointFcn TxnCheckpoint {
      get { return (TxnCheckpointFcn)Marshal.GetDelegateForFunctionPointer(txn_checkpoint, typeof(TxnCheckpointFcn)); }
    }

    IntPtr txn_recover;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TxnRecoverFcn(DB_ENV* dbenv, DB_PREPLIST* preplist, int count, out int retp, UInt32 flags);
    public TxnRecoverFcn TxnRecover {
      get { return (TxnRecoverFcn)Marshal.GetDelegateForFunctionPointer(txn_recover, typeof(TxnRecoverFcn)); }
    }

    IntPtr txn_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TxnStatFcn(DB_ENV* dbenv, out DB_TXN_STAT* stat, UInt32 flags);
    public TxnStatFcn TxnStat {
      get { return (TxnStatFcn)Marshal.GetDelegateForFunctionPointer(txn_stat, typeof(TxnStatFcn)); }
    }

    IntPtr txn_stat_print;
    public SetUIntFcn TxnStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(txn_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr get_timeout;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetTimeoutFcn(DB_ENV* dbenv, out UInt32 timeout, UInt32 flags);
    public GetTimeoutFcn GetTimeout {
      get { return (GetTimeoutFcn)Marshal.GetDelegateForFunctionPointer(get_timeout, typeof(GetTimeoutFcn)); }
    }

    IntPtr set_timeout;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetTimeoutFcn(DB_ENV* dbenv, UInt32 timeout, UInt32 flags);
    public SetTimeoutFcn SetTimeout {
      get { return (SetTimeoutFcn)Marshal.GetDelegateForFunctionPointer(set_timeout, typeof(SetTimeoutFcn)); }
    }

    #endregion

    int test_abort;     /* Abort value for testing. */
    int test_check;     /* Checkpoint value for testing. */
    int test_copy;      /* Copy value for testing. */

    UInt32 flags;
  }
#endif

#if BDB_4_5_20
  /* Database Environment handle. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DB_ENV
  {
    #region Private Fields (to calculate offset to API function pointers)

    IntPtr db_errcall;      /* Error message callback. */
    FILE* db_errfile;       /* Error message file stream. */
    byte* db_errpfx;        /* Error message prefix. */
    FILE* db_msgfile;       /* Statistics message file stream. */
    IntPtr db_msgcall;      /* Statistics message callback. */

    /* Other Callbacks. */
    IntPtr db_feedback;
    IntPtr db_paniccall;
    IntPtr db_event_func;

    /* App-specified alloc functions. */
    IntPtr db_malloc;
    IntPtr db_realloc;
    IntPtr db_free;

	  /* Application callback to copy data to/from a custom data source. */
    public const UInt32 DB_USERCOPY_GETDATA = 0x0001;
    public const UInt32 DB_USERCOPY_SETDATA = 0x0002;
    IntPtr dbt_usercopy;

    UInt32 verbose;           /* Verbose output. */

    /* Documented, but no access method defined. */
    public IntPtr app_private;    /* Application-private handle. (void*) */

    IntPtr app_dispatch;      /* User-specified transaction recovery dispatch. */

    /* Mutexes. */
	  UInt32 mutex_align;       /* Mutex alignment */
	  UInt32 mutex_cnt;         /* Number of mutexes to configure */
	  UInt32 mutex_inc;         /* Number of mutexes to add */
	  UInt32 mutex_tas_spins;   /* Test-and-set spin count */

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct MUTEX_IQ
    {
		  int alloc_id;           /* Allocation ID argument */
		  UInt32 flags;           /* Flags argument */
	  } 
    MUTEX_IQ* mutex_iq;			  /* Initial mutexes queue */
	  uint mutex_iq_next;       /* Count of initial mutexes */
	  uint mutex_iq_max;        /* Maximum initial mutexes */

    /* Locking. */
    byte* lk_conflicts;       /* Two dimensional conflict matrix. */
    int lk_modes;             /* Number of lock modes in table. */
    UInt32 lk_max;            /* Maximum number of locks. */
    UInt32 lk_max_lockers;    /* Maximum number of lockers. */
    UInt32 lk_max_objects;    /* Maximum number of locked objects. */
    UInt32 lk_detect;         /* Deadlock detect on all conflicts. */
    UInt32 lk_timeout;        /* Lock timeout period. (typedef u_int32_t db_timeout_t;) */

    /* Logging. */
    UInt32 lg_bsize;          /* Buffer size. */
    UInt32 lg_size;           /* Log file size. */
    UInt32 lg_regionmax;      /* Region size. */
    int lg_filemode;          /* Log file permission mode. */

    /* Memory pool. */
    UInt32 mp_gbytes;         /* Cachesize: GB. */
    UInt32 mp_bytes;          /* Cachesize: Bytes. */
    uint mp_ncache;           /* Number of cache regions. */
    uint mp_mmapsize;         /* Maximum file size for mmap. */
    int mp_maxopenfd;         /* Maximum open file descriptors. */
    int mp_maxwrite;          /* Maximum buffers to write. */
    int mp_maxwrite_sleep;    /* Sleep after writing max buffers. */

    /* Transactions. */
    UInt32 tx_max;            /* Maximum number of transactions. */
#if _USE_32BIT_TIME_T
    int tx_timestamp;         /* Recover to specific timestamp. */
#else
    long tx_timestamp;        /* Recover to specific timestamp. */
#endif
    UInt32 tx_timeout;        /* Timeout for transactions. */

    /* Thread tracking. */
    UInt32 thr_nbucket;       /* Number of hash buckets. */
    UInt32 thr_max;           /* Max before garbage collection. */
    void* thr_hashtab;        /* Hash table of DB_THREAD_INFO. */

    /*******************************************************
    * Private: owned by DB.
    *******************************************************/
    int pid_cache;            /* Cached process ID. (typedef int pid_t;) */

    /* User files, paths. */
    byte* db_home;            /* Database home. */
    byte* db_log_dir;         /* Database log file directory. */
    byte* db_tmp_dir;         /* Database tmp file directory. */

    byte** db_data_dir;       /* Database data file directories. */
    int data_cnt;             /* Database data file slots. */
    int data_next;            /* Next Database data file slot. */

    int db_mode;              /* Default open permissions. */
    int dir_mode;             /* Intermediate directory perms. */
    void* env_lref;           /* Locker in non-threaded handles. */
    UInt32 open_flags;        /* Flags passed to DB_ENV->open. */

    void* reginfo;            /* REGINFO structure reference. */
    DB_FH* lockfhp;           /* fcntl(2) locking file handle. */

	  DB_FH* registry;          /* DB_REGISTER file handle. */
	  UInt32 registry_off;      /* Offset of our slot.  We can't use
					                     * off_t because its size depends on
					                     * build settings. */

	  IntPtr thread_id;		      /* Return IDs. */
    IntPtr is_alive;          /* Return if IDs alive. */
		IntPtr thread_id_string;  /* Format IDs into a string. */

    IntPtr* recover_dtab;     /* Dispatch table for recover funcs. */
	  uint recover_dtab_size;   /* Slots in the dispatch table. */

    void* cl_handle;          /* RPC: remote client handle. */
    uint cl_id;               /* RPC: remote client env id. */

    int db_ref;               /* DB reference count. */

    int shm_key;              /* shmget(2) key. (long = int) */

    /*
    * List of open DB handles for this DB_ENV, used for cursor
    * adjustment.  Must be protected for multi-threaded support.
    *
    * !!!
    * As this structure is allocated in per-process memory, the
    * mutex may need to be stored elsewhere on architectures unable
    * to support mutexes in heap memory, e.g. HP/UX 9.
    *
    * !!!
    * Explicit representation of structure in queue.h.
    * TAILQ_HEAD(__dblist, __db);
    */
    UInt32 mtx_dblist;        /* Mutex. (typedef u_int32_t db_mutex_t;) */

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct DB_LIST
    {
		  DB* tqh_first;
		  DB** tqh_last;
    }
    DB_LIST dblist;

    /*
    * XA support.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_ENTRY(__db_env) links;
    * TAILQ_HEAD(xa_txn, __db_txn);
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct LINKS
    {
      DB_ENV* tqe_next;
      DB_ENV** tqe_prev;
    }
    LINKS links;

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct XA_TXN
    {           /* XA Active Transactions. */
      DB_TXN* tqh_first;
      DB_TXN** tqh_last;
    }
    XA_TXN xa_txn;
    
    int xa_rmid;                  /* XA Resource Manager ID. */

    byte* passwd;                 /* Cryptography support. */
    uint passwd_len;              /* (size_t = uint) */
    void* crypto_handle;          /* Primary handle. */
    UInt32 mtx_mt;                /* Mersenne Twister mutex. (typedef u_int32_t db_mutex_t;) */
    int mti;                      /* Mersenne Twister index. */
    uint* mt;                     /* Mersenne Twister state vector. (u_long = uint) */

    /* API-private structure. */
    // void* api1_internal;      /* C++, Perl API private */
    public IntPtr api_internal;   /* api1_internal renamed for use with dotNET */
    void* api2_internal;          /* Java API private */

    struct DB_LOCKTAB 
    {
      // dummy declaration 
    }
    DB_LOCKTAB* lk_handle;        /* Lock handle. */
    struct DB_LOG
    {
      // dummy declaration 
    }
    DB_LOG* lg_handle;            /* Log handle. */
    struct DB_MPOOL
    {
      // dummy declaration 
    }
    DB_MPOOL* mp_handle;          /* Mpool handle. */
    struct DB_MUTEXMGR
    {
      // dummy declaration 
    }
    DB_MUTEXMGR* mutex_handle;    /* Mutex handle. */
    struct DB_REP
    {
      // dummy declaration 
    }
    DB_REP* rep_handle;           /* Replication handle. */
    DB_TXNMGR* tx_handle;         /* Txn handle. */

    #endregion

    #region API Methods

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetUIntFcn(DB_ENV* dbenv, out UInt32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetUIntFcn(DB_ENV* dbenv, UInt32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetIntFcn(DB_ENV* dbenv, out Int32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetIntFcn(DB_ENV* dbenv, Int32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetIntPtrFcn(DB_ENV* dbenv, IntPtr value);

    IntPtr cdsgroup_begin;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CdsGroupBeginFcn(DB_ENV* dbenv, out DB_TXN* tid);
    public CdsGroupBeginFcn CdsGroupBegin {
      get { return (CdsGroupBeginFcn)Marshal.GetDelegateForFunctionPointer(cdsgroup_begin, typeof(CdsGroupBeginFcn)); }
    }

    IntPtr close;
    // flags unused, must be 0
    public SetUIntFcn Close {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(close, typeof(SetUIntFcn)); }
    }

    IntPtr dbremove;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DbRemoveFcn(
      DB_ENV* dbenv, DB_TXN* txnid, byte* file, byte* database, UInt32 flags);
    public DbRemoveFcn DbRemove {
      get { return (DbRemoveFcn)Marshal.GetDelegateForFunctionPointer(dbremove, typeof(DbRemoveFcn)); }
    }

    IntPtr dbrename;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DbRenameFcn(
      DB_ENV* dbenv, DB_TXN* txnid, byte* file, byte* database, byte* newname, UInt32 flags);
    public DbRenameFcn DbRename {
      get { return (DbRenameFcn)Marshal.GetDelegateForFunctionPointer(dbrename, typeof(DbRenameFcn)); }
    }

    IntPtr err;
    // C style varargs can be translated using the __arglist keyword
    // C declaration: void (*err) __P((DB *, int, const char *, ...));
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrFcn(DB_ENV* dbenv, int error, byte* fmt/*, __arglist*/);
    public ErrFcn Err {
      get { return (ErrFcn)Marshal.GetDelegateForFunctionPointer(err, typeof(ErrFcn)); }
    }
    // public void Err(DB* db, int error, byte* fmt/*, __arglist*/) {
    //   ErrFcn(db, error, fmt/*, __arglist*/);
    // }

    IntPtr errx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrxFcn(DB_ENV* dbenv, byte* fmt/*, __arglist*/);
    public ErrxFcn Errx {
      get { return (ErrxFcn)Marshal.GetDelegateForFunctionPointer(errx, typeof(ErrxFcn)); }
    }
    // public void Errx(DB* db, byte* fmt/*, __arglist*/) {
    //   ErrxFcn(db, fmt/*, __arglist*/);
    // }

    IntPtr failchk;
    public SetUIntFcn FailChk {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(failchk, typeof(SetUIntFcn)); }
    }

    IntPtr fileid_reset;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal FileIdResetFcn(DB_ENV* dbenv, byte* file, UInt32 flags);
    public FileIdResetFcn FileIdReset {
      get { return (FileIdResetFcn)Marshal.GetDelegateForFunctionPointer(fileid_reset, typeof(FileIdResetFcn)); }
    }

    IntPtr get_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetCacheSizeFcn(DB_ENV* dbenv, out UInt32 gbytes, out UInt32 bytes, out int ncache);
    public GetCacheSizeFcn GetCacheSize {
      get { return (GetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(get_cachesize, typeof(GetCacheSizeFcn)); }
    }

    IntPtr get_data_dirs;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetDataDirsFcn(DB_ENV* dbenv, out byte** dirs);
    public GetDataDirsFcn GetDataDirs {
      get { return (GetDataDirsFcn)Marshal.GetDelegateForFunctionPointer(get_data_dirs, typeof(GetDataDirsFcn)); }
    }

    IntPtr get_encrypt_flags;
    public GetUIntFcn GetEncryptFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_encrypt_flags, typeof(GetUIntFcn)); }
    }

    IntPtr get_errfile;

    IntPtr get_errpfx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void GetErrPfxFcn(DB_ENV* dbenv, out byte* errpfx);
    public GetErrPfxFcn GetErrPfx {
      get { return (GetErrPfxFcn)Marshal.GetDelegateForFunctionPointer(get_errpfx, typeof(GetErrPfxFcn)); }
    }

    IntPtr get_flags;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFlagsFcn(DB_ENV* dbenv, out EnvFlags flags);
    public GetFlagsFcn GetFlags {
      get { return (GetFlagsFcn)Marshal.GetDelegateForFunctionPointer(get_flags, typeof(GetFlagsFcn)); }
    }

    IntPtr get_home;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetHomeFcn(DB_ENV* dbenv, out byte* home);
    public GetHomeFcn GetHome {
      get { return (GetHomeFcn)Marshal.GetDelegateForFunctionPointer(get_home, typeof(GetHomeFcn)); }
    }

    IntPtr get_lg_bsize;
    public GetUIntFcn GetLogBufSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lg_bsize, typeof(GetUIntFcn)); }
    }

    IntPtr get_lg_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetLogDirFcn(DB_ENV* dbenv, out byte* dir);
    public GetLogDirFcn GetLogDir {
      get { return (GetLogDirFcn)Marshal.GetDelegateForFunctionPointer(get_lg_dir, typeof(GetLogDirFcn)); }
    }

    IntPtr get_lg_filemode;
    public GetIntFcn GetLogFileMode {
      get { return (GetIntFcn)Marshal.GetDelegateForFunctionPointer(get_lg_filemode, typeof(GetIntFcn)); }
    }

    IntPtr get_lg_max;
    public GetUIntFcn GetMaxLogFileSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lg_max, typeof(GetUIntFcn)); }
    }

    IntPtr get_lg_regionmax;
    public GetUIntFcn GetMaxLogRegionSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lg_regionmax, typeof(GetUIntFcn)); }
    }

    IntPtr get_lk_conflicts;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetLockConflictsFcn(DB_ENV* dbenv, ref byte* conflicts, ref int nmodes);
    public GetLockConflictsFcn GetLockConflicts {
      get { return (GetLockConflictsFcn)Marshal.GetDelegateForFunctionPointer(get_lk_conflicts, typeof(GetLockConflictsFcn)); }
    }

    IntPtr get_lk_detect;
    public GetUIntFcn GetLockDetect {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lk_detect, typeof(GetUIntFcn)); }
    }

    IntPtr get_lk_max_lockers;
    public GetUIntFcn GetMaxLockers {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lk_max_lockers, typeof(GetUIntFcn)); }
    }

    IntPtr get_lk_max_locks;
    public GetUIntFcn GetMaxLocks {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lk_max_locks, typeof(GetUIntFcn)); }
    }

    IntPtr get_lk_max_objects;
    public GetUIntFcn GetMaxObjects {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_lk_max_objects, typeof(GetUIntFcn)); }
    }

    IntPtr get_mp_max_openfd;
    public GetIntFcn GetMpMaxOpenFd {
      get { return (GetIntFcn)Marshal.GetDelegateForFunctionPointer(get_mp_max_openfd, typeof(GetIntFcn)); }
    }

    IntPtr get_mp_max_write;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetMpMaxWriteFcn(DB_ENV* dbenv, out int maxWrite, out int maxWriteSleep);
    public GetMpMaxWriteFcn GetMpMaxWrite {
      get { return (GetMpMaxWriteFcn)Marshal.GetDelegateForFunctionPointer(get_mp_max_write, typeof(GetMpMaxWriteFcn)); }
    }

    IntPtr get_mp_mmapsize;
    public GetUIntFcn GetMpMMapSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_mp_mmapsize, typeof(GetUIntFcn)); }
    }

    IntPtr get_msgfile;

    IntPtr get_open_flags;
    public GetUIntFcn GetOpenFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_open_flags, typeof(GetUIntFcn)); }
    }

    IntPtr get_shm_key;
    public GetIntFcn GetShmKey {
      get { return (GetIntFcn)Marshal.GetDelegateForFunctionPointer(get_shm_key, typeof(GetIntFcn)); }
    }

    IntPtr get_timeout;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetTimeoutFcn(DB_ENV* dbenv, out UInt32 timeout, UInt32 flags);
    public GetTimeoutFcn GetTimeout {
      get { return (GetTimeoutFcn)Marshal.GetDelegateForFunctionPointer(get_timeout, typeof(GetTimeoutFcn)); }
    }

    IntPtr get_tmp_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetTmpDirFcn(DB_ENV* dbenv, out byte* dir);
    public GetTmpDirFcn GetTmpDir {
      get { return (GetTmpDirFcn)Marshal.GetDelegateForFunctionPointer(get_tmp_dir, typeof(GetTmpDirFcn)); }
    }

    IntPtr get_tx_max;
    public GetUIntFcn GetTxMax {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_tx_max, typeof(GetUIntFcn)); }
    }

    IntPtr get_tx_timestamp;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
#if _USE_32BIT_TIME_T
    public delegate DbRetVal GetTxTimestampFcn(DB_ENV* dbenv, out int txmax);
#else
    public delegate DbRetVal GetTxTimestampFcn(DB_ENV* dbenv, out long txmax);
#endif
    public GetTxTimestampFcn GetTxTimestamp {
      get { return (GetTxTimestampFcn)Marshal.GetDelegateForFunctionPointer(get_tx_timestamp, typeof(GetTxTimestampFcn)); }
    }

    IntPtr get_verbose;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetVerboseFcn(DB_ENV* dbenv, uint which, out int onoff);
    public GetVerboseFcn GetVerbose {
      get { return (GetVerboseFcn)Marshal.GetDelegateForFunctionPointer(get_verbose, typeof(GetVerboseFcn)); }
    }

    IntPtr is_bigendian;

    IntPtr lock_detect;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockDetectFcn(DB_ENV* dbenv, UInt32 flags, UInt32 atype, ref int aborted);
    public LockDetectFcn LockDetect {
      get { return (LockDetectFcn)Marshal.GetDelegateForFunctionPointer(lock_detect, typeof(LockDetectFcn)); }
    }

    IntPtr lock_get;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockGetFcn(DB_ENV* dbenv, UInt32 locker, UInt32 flags, ref DBT obj,
      DB_LOCKMODE lock_mode, ref DB_LOCK lck);
    public LockGetFcn LockGet {
      get { return (LockGetFcn)Marshal.GetDelegateForFunctionPointer(lock_get, typeof(LockGetFcn)); }
    }

    IntPtr lock_id;
    public GetUIntFcn LockId {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(lock_id, typeof(GetUIntFcn)); }
    }

    IntPtr lock_id_free;
    public SetUIntFcn LockIdFree {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(lock_id_free, typeof(SetUIntFcn)); }
    }

    IntPtr lock_put;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockPutFcn(DB_ENV* dbenv, ref DB_LOCK lck);
    public LockPutFcn LockPut {
      get { return (LockPutFcn)Marshal.GetDelegateForFunctionPointer(lock_put, typeof(LockPutFcn)); }
    }

    IntPtr lock_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockStatFcn(DB_ENV* dbenv, out DB_LOCK_STAT* stat, UInt32 flags);
    public LockStatFcn LockStat {
      get { return (LockStatFcn)Marshal.GetDelegateForFunctionPointer(lock_stat, typeof(LockStatFcn)); }
    }

    IntPtr lock_stat_print;
    public SetUIntFcn LockStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(lock_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr lock_vec;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LockVectorFcn(DB_ENV* dbenv, UInt32 locker, UInt32 flags,
      DB_LOCKREQ* list, int nlist, out DB_LOCKREQ* elist);
    public LockVectorFcn LockVector {
      get { return (LockVectorFcn)Marshal.GetDelegateForFunctionPointer(lock_vec, typeof(LockVectorFcn)); }
    }

    IntPtr log_archive;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogArchiveFcn(DB_ENV* dbenv, out byte** list, UInt32 flags);
    public LogArchiveFcn LogArchive {
      get { return (LogArchiveFcn)Marshal.GetDelegateForFunctionPointer(log_archive, typeof(LogArchiveFcn)); }
    }

    IntPtr log_cursor;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogCursorFcn(DB_ENV* dbenv, out DB_LOGC* logc, UInt32 flags);
    public LogCursorFcn LogCursor {
      get { return (LogCursorFcn)Marshal.GetDelegateForFunctionPointer(log_cursor, typeof(LogCursorFcn)); }
    }

    IntPtr log_file;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogFileFcn(DB_ENV* dbenv, ref DB_LSN lsn, byte* name, uint len);
    public LogFileFcn LogFile {
      get { return (LogFileFcn)Marshal.GetDelegateForFunctionPointer(log_file, typeof(LogFileFcn)); }
    }

    IntPtr log_flush;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogFlushFcn(DB_ENV* dbenv, DB_LSN* lsn);
    public LogFlushFcn LogFlush {
      get { return (LogFlushFcn)Marshal.GetDelegateForFunctionPointer(log_flush, typeof(LogFlushFcn)); }
    }

    IntPtr log_printf;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogPrintFileFcn(DB_ENV* dbenv, DB_TXN* txnid, byte* fmt);
    public LogPrintFileFcn LogPrintFile {
      get { return (LogPrintFileFcn)Marshal.GetDelegateForFunctionPointer(log_printf, typeof(LogPrintFileFcn)); }
    }

    IntPtr log_put;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogPutFcn(DB_ENV* dbenv, out DB_LSN lsn, ref DBT data, UInt32 flags);
    public LogPutFcn LogPut {
      get { return (LogPutFcn)Marshal.GetDelegateForFunctionPointer(log_put, typeof(LogPutFcn)); }
    }

    IntPtr log_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LogStatFcn(DB_ENV* dbenv, out DB_LOG_STAT* stat, UInt32 flags);
    public LogStatFcn LogStat {
      get { return (LogStatFcn)Marshal.GetDelegateForFunctionPointer(log_stat, typeof(LogStatFcn)); }
    }

    IntPtr log_stat_print;
    public SetUIntFcn LogStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(log_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr lsn_reset;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal LsnResetFcn(DB_ENV* dbenv, byte* file, UInt32 flags);
    public LsnResetFcn LsnReset {
      get { return (LsnResetFcn)Marshal.GetDelegateForFunctionPointer(lsn_reset, typeof(LsnResetFcn)); }
    }

    IntPtr memp_fcreate;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolFileCreateFcn(DB_ENV* dbenv, out DB_MPOOLFILE* dbmf, UInt32 flags);
    public MemPoolFileCreateFcn MemPoolFileCreate {
      get { return (MemPoolFileCreateFcn)Marshal.GetDelegateForFunctionPointer(memp_fcreate, typeof(MemPoolFileCreateFcn)); }
    }

    IntPtr memp_register;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PageInOutFcn(DB_ENV* dbenv, UInt32 pgno, void* pgaddr, ref DBT pgcookie);
    // pgin_fcn and pgout_fcn must be of type PageInOutFcn
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolRegisterFcn(DB_ENV* dbenv, int ftype, IntPtr pgin_fcn, IntPtr pgout_fcn);
    public MemPoolRegisterFcn MemPoolRegister {
      get { return (MemPoolRegisterFcn)Marshal.GetDelegateForFunctionPointer(memp_register, typeof(MemPoolRegisterFcn)); }
    }

    IntPtr memp_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolStatFcn(DB_ENV* dbenv, DB_MPOOL_STAT** gsp, DB_MPOOL_FSTAT*** fsp, UInt32 flags);
    public MemPoolStatFcn MemPoolStat {
      get { return (MemPoolStatFcn)Marshal.GetDelegateForFunctionPointer(memp_stat, typeof(MemPoolStatFcn)); }
    }

    IntPtr memp_stat_print;
    public SetUIntFcn MemPoolStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(memp_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr memp_sync;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolSyncFcn(DB_ENV* dbenv, DB_LSN* lsn);
    public MemPoolSyncFcn MemPoolSync {
      get { return (MemPoolSyncFcn)Marshal.GetDelegateForFunctionPointer(memp_sync, typeof(MemPoolSyncFcn)); }
    }

    IntPtr memp_trickle;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MemPoolTrickleFcn(DB_ENV* dbenv, int percent, out int nwrote);
    public MemPoolTrickleFcn MemPoolTrickle {
      get { return (MemPoolTrickleFcn)Marshal.GetDelegateForFunctionPointer(memp_trickle, typeof(MemPoolTrickleFcn)); }
    }

    IntPtr mutex_alloc;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MutexAllocFcn(DB_ENV* dbenv, UInt32 flags, out UInt32 mutex);
    public MutexAllocFcn MutexAlloc {
      get { return (MutexAllocFcn)Marshal.GetDelegateForFunctionPointer(mutex_alloc, typeof(MutexAllocFcn)); }
    }

    IntPtr mutex_free;
    public SetUIntFcn MutexFree {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_free, typeof(SetUIntFcn)); }
    }

    IntPtr mutex_get_align;
    public GetUIntFcn MutexGetAlign {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_get_align, typeof(GetUIntFcn)); }
    }

    IntPtr mutex_get_increment;
    public GetUIntFcn MutexGetIncrement {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_get_increment, typeof(GetUIntFcn)); }
    }

    IntPtr mutex_get_max;
    public GetUIntFcn MutexGetMax {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_get_max, typeof(GetUIntFcn)); }
    }

    IntPtr mutex_get_tas_spins;
    public GetUIntFcn MutexGetTasSpins {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_get_tas_spins, typeof(GetUIntFcn)); }
    }

    IntPtr mutex_lock;
    public SetUIntFcn MutexLock {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_lock, typeof(SetUIntFcn)); }
    }

	  IntPtr mutex_set_align;
    public SetUIntFcn MutexSetAlign {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_set_align, typeof(SetUIntFcn)); }
    }

    IntPtr mutex_set_increment;
    public SetUIntFcn MutexSetIncrement {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_set_increment, typeof(SetUIntFcn)); }
    }

    IntPtr mutex_set_max;
    public SetUIntFcn MutexSetMax {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_set_max, typeof(SetUIntFcn)); }
    }

    IntPtr mutex_set_tas_spins;
    public SetUIntFcn MutexSetTasSpins {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_set_tas_spins, typeof(SetUIntFcn)); }
    }

    IntPtr mutex_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal MutexStatFcn(DB_ENV* dbenv, out DB_MUTEX_STAT* statp, UInt32 flags);
    public MutexStatFcn MutexStat {
      get { return (MutexStatFcn)Marshal.GetDelegateForFunctionPointer(mutex_stat, typeof(MutexStatFcn)); }
    }

    IntPtr mutex_stat_print;
    public SetUIntFcn MutexStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr mutex_unlock;
    public SetUIntFcn MutexUnlock {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(mutex_unlock, typeof(SetUIntFcn)); }
    }

    IntPtr open;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal OpenFcn(DB_ENV* dbenv, byte* db_home, UInt32 flags, int mode);
    public OpenFcn Open {
      get { return (OpenFcn)Marshal.GetDelegateForFunctionPointer(open, typeof(OpenFcn)); }
    }

    IntPtr remove;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RemoveFcn(DB_ENV* dbenv, byte* home, UInt32 flags);
    public RemoveFcn Remove {
      get { return (RemoveFcn)Marshal.GetDelegateForFunctionPointer(remove, typeof(RemoveFcn)); }
    }

    IntPtr rep_elect;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepElectFcn(DB_ENV* dbenv, int nsites, int nvotes, out int envid, UInt32 flags);
    public RepElectFcn RepElect {
      get { return (RepElectFcn)Marshal.GetDelegateForFunctionPointer(rep_elect, typeof(RepElectFcn)); }
    }

    IntPtr rep_flush;

    IntPtr rep_get_config;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepGetConfigFcn(DB_ENV* dbenv, UInt32 which, out int onoff);
    public RepGetConfigFcn RepGetConfig {
      get { return (RepGetConfigFcn)Marshal.GetDelegateForFunctionPointer(rep_get_config, typeof(RepGetConfigFcn)); }
    }

    IntPtr rep_get_limit;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepGetLimitFcn(DB_ENV* dbenv, out UInt32 gbytes, out UInt32 bytes);
    public RepGetLimitFcn RepGetLimit {
      get { return (RepGetLimitFcn)Marshal.GetDelegateForFunctionPointer(rep_get_limit, typeof(RepGetLimitFcn)); }
    }

    IntPtr rep_get_nsites;
    public GetIntFcn RepGetNSites {
      get { return (GetIntFcn)Marshal.GetDelegateForFunctionPointer(rep_get_nsites, typeof(GetIntFcn)); }
    }

    IntPtr rep_get_priority;
    public GetIntFcn RepGetPriority {
      get { return (GetIntFcn)Marshal.GetDelegateForFunctionPointer(rep_get_priority, typeof(GetIntFcn)); }
    }

    IntPtr rep_get_timeout;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepGetTimeoutFcn(DB_ENV* dbenv, UInt32 which, out UInt32 timeout);
    public RepGetTimeoutFcn RepGetTimeout {
      get { return (RepGetTimeoutFcn)Marshal.GetDelegateForFunctionPointer(rep_get_timeout, typeof(RepGetTimeoutFcn)); }
    }

    IntPtr rep_process_message;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepProcessMessageFcn(DB_ENV* dbenv, ref DBT control, ref DBT rec, ref int envid, out DB_LSN ret_lsnp);
    public RepProcessMessageFcn RepProcessMessage {
      get { return (RepProcessMessageFcn)Marshal.GetDelegateForFunctionPointer(rep_process_message, typeof(RepProcessMessageFcn)); }
    }

    IntPtr rep_set_config;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepSetConfigFcn(DB_ENV* dbenv, UInt32 which, int onoff);
    public RepSetConfigFcn RepSetConfig {
      get { return (RepSetConfigFcn)Marshal.GetDelegateForFunctionPointer(rep_set_config, typeof(RepSetConfigFcn)); }
    }

    IntPtr rep_set_limit;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepSetLimitFcn(DB_ENV* dbenv, UInt32 gbytes, UInt32 bytes);
    public RepSetLimitFcn RepSetLimit {
      get { return (RepSetLimitFcn)Marshal.GetDelegateForFunctionPointer(rep_set_limit, typeof(RepSetLimitFcn)); }
    }
    
    IntPtr rep_set_nsites;
    public SetIntFcn RepSetNSites {
      get { return (SetIntFcn)Marshal.GetDelegateForFunctionPointer(rep_set_nsites, typeof(SetIntFcn)); }
    }

    IntPtr rep_set_priority;
    public SetIntFcn RepSetPriority {
      get { return (SetIntFcn)Marshal.GetDelegateForFunctionPointer(rep_set_priority, typeof(SetIntFcn)); }
    }

    IntPtr rep_set_timeout;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepSetTimeoutFcn(DB_ENV* dbenv, UInt32 which, UInt32 timeout);
    public RepSetTimeoutFcn RepSetTimeout {
      get { return (RepSetTimeoutFcn)Marshal.GetDelegateForFunctionPointer(rep_set_timeout, typeof(RepSetTimeoutFcn)); }
    }

    IntPtr rep_set_transport;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal
    RepSendFcn(DB_ENV* dbenv, ref DBT control, ref DBT rec, DB_LSN* lsnp, int envid, UInt32 flags);
    // send must be of type RepSendFcn
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepSetTransportFcn(DB_ENV* dbenv, int envid, IntPtr send);
    public RepSetTransportFcn RepSetTransport {
      get { return (RepSetTransportFcn)Marshal.GetDelegateForFunctionPointer(rep_set_transport, typeof(RepSetTransportFcn)); }
    }

    IntPtr rep_start;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepStartFcn(DB_ENV* dbenv, ref DBT cdata, UInt32 flags);
    public RepStartFcn RepStart {
      get { return (RepStartFcn)Marshal.GetDelegateForFunctionPointer(rep_start, typeof(RepStartFcn)); }
    }

    IntPtr rep_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepStatFcn(DB_ENV* dbenv, out DB_REP_STAT* stat, UInt32 flags);
    public RepStatFcn RepStat {
      get { return (RepStatFcn)Marshal.GetDelegateForFunctionPointer(rep_stat, typeof(RepStatFcn)); }
    }

    IntPtr rep_stat_print;
    public SetUIntFcn RepStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(rep_stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr rep_sync;
    public SetUIntFcn RepSync {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(rep_sync, typeof(SetUIntFcn)); }
    }

    IntPtr repmgr_add_remote_site;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepMgrAddRemoteSiteFcn(DB_ENV* dbenv, byte* host, uint port, out int eid, UInt32 flags);
    public RepMgrAddRemoteSiteFcn RepMgrAddRemoteSite {
      get { return (RepMgrAddRemoteSiteFcn)Marshal.GetDelegateForFunctionPointer(repmgr_add_remote_site, typeof(RepMgrAddRemoteSiteFcn)); }
    }

    IntPtr repmgr_get_ack_policy;
    public GetIntFcn RepMgrGetAckPolicy {
      get { return (GetIntFcn)Marshal.GetDelegateForFunctionPointer(repmgr_get_ack_policy, typeof(GetIntFcn)); }
    }

    IntPtr repmgr_set_ack_policy;
    public SetIntFcn RepMgrSetAckPolicy {
      get { return (SetIntFcn)Marshal.GetDelegateForFunctionPointer(repmgr_set_ack_policy, typeof(SetIntFcn)); }
    }

    IntPtr repmgr_set_local_site;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepMgrSetLocalSiteFcn(DB_ENV* dbenv, byte* host, uint port, UInt32 flags);
    public RepMgrSetLocalSiteFcn RepMgrSetLocalSite {
      get { return (RepMgrSetLocalSiteFcn)Marshal.GetDelegateForFunctionPointer(repmgr_set_local_site, typeof(RepMgrSetLocalSiteFcn)); }
    }

    IntPtr repmgr_site_list;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepMgrSiteListFcn(DB_ENV* dbenv, out uint count, out DB_REPMGR_SITE* list);
    public RepMgrSiteListFcn RepMgrSiteList {
      get { return (RepMgrSiteListFcn)Marshal.GetDelegateForFunctionPointer(repmgr_site_list, typeof(RepMgrSiteListFcn)); }
    }

    IntPtr repmgr_start;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RepMgrStartFcn(DB_ENV* dbenv, int nthreads, UInt32 flags);
    public RepMgrStartFcn RepMgrStart {
      get { return (RepMgrStartFcn)Marshal.GetDelegateForFunctionPointer(repmgr_start, typeof(RepMgrStartFcn)); }
    }

    IntPtr set_alloc;

    IntPtr set_app_dispatch;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AppRecoverFcn(DB_ENV* dbenv, ref DBT log_rec, DB_LSN* lsn, DB_RECOPS op);
    public SetIntPtrFcn SetAppDispatch {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_app_dispatch, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetCacheSizeFcn(DB_ENV* dbenv, UInt32 gbytes, UInt32 bytes, int ncache);
    public SetCacheSizeFcn SetCacheSize {
      get { return (SetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(set_cachesize, typeof(SetCacheSizeFcn)); }
    }

    IntPtr set_data_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetDataDirFcn(DB_ENV* dbenv, byte* dir);
    public SetDataDirFcn SetDataDir {
      get { return (SetDataDirFcn)Marshal.GetDelegateForFunctionPointer(set_data_dir, typeof(SetDataDirFcn)); }
    }

    IntPtr set_encrypt;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetEncryptFcn(DB_ENV* dbenv, byte* passwd, EncryptMode flags);
    public SetEncryptFcn SetEncrypt {
      get { return (SetEncryptFcn)Marshal.GetDelegateForFunctionPointer(set_encrypt, typeof(SetEncryptFcn)); }
    }

    IntPtr set_errcall;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrCallFcn(DB_ENV* dbenv, byte* errpfx, byte* msg);
    public SetIntPtrFcn SetErrCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_errcall, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_errfile;

    IntPtr set_errpfx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void SetErrPfxFcn(DB_ENV* dbenv, byte* errpfx);
    public SetErrPfxFcn SetErrPfx {
      get { return (SetErrPfxFcn)Marshal.GetDelegateForFunctionPointer(set_errpfx, typeof(SetErrPfxFcn)); }
    }

    IntPtr set_event_notify;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void EventNotifyFcn(DB_ENV* dbenv, UInt32 evnt, void* event_info);
    public SetIntPtrFcn SetEventNotify {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_event_notify, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_feedback;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void FeedbackFcn(DB_ENV* dbenv, int opcode, int percent);
    public SetIntPtrFcn SetFeedback {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_feedback, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_flags;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetFlagsFcn(DB_ENV* dbenv, EnvFlags flags, int onoff);
    public SetFlagsFcn SetFlags {
      get { return (SetFlagsFcn)Marshal.GetDelegateForFunctionPointer(set_flags, typeof(SetFlagsFcn)); }
    }

    IntPtr set_intermediate_dir;

    IntPtr set_isalive;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate int IsAliveFcn(DB_ENV* dbenv, int pid, UInt32 tid, UInt32 flags);
    public SetIntPtrFcn SetIsAlive {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_isalive, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_lg_bsize;
    public SetUIntFcn SetLogBufSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lg_bsize, typeof(SetUIntFcn)); }
    }

    IntPtr set_lg_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetLogDirFcn(DB_ENV* dbenv, byte* dir);
    public SetLogDirFcn SetLogDir {
      get { return (SetLogDirFcn)Marshal.GetDelegateForFunctionPointer(set_lg_dir, typeof(SetLogDirFcn)); }
    }

    IntPtr set_lg_filemode;
    public SetIntFcn SetLogFileMode {
      get { return (SetIntFcn)Marshal.GetDelegateForFunctionPointer(set_lg_filemode, typeof(SetIntFcn)); }
    }

    IntPtr set_lg_max;
    public SetUIntFcn SetMaxLogFileSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lg_max, typeof(SetUIntFcn)); }
    }

    IntPtr set_lg_regionmax;
    public SetUIntFcn SetMaxLogRegionSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lg_regionmax, typeof(SetUIntFcn)); }
    }

    IntPtr set_lk_conflicts;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetLockConflictsFcn(DB_ENV* dbenv, byte* conflicts, int nmodes);
    public SetLockConflictsFcn SetLockConflicts {
      get { return (SetLockConflictsFcn)Marshal.GetDelegateForFunctionPointer(set_lk_conflicts, typeof(SetLockConflictsFcn)); }
    }

    IntPtr set_lk_detect;
    public SetUIntFcn SetLockDetect {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lk_detect, typeof(SetUIntFcn)); }
    }

    IntPtr set_lk_max_lockers;
    public SetUIntFcn SetMaxLockers {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lk_max_lockers, typeof(SetUIntFcn)); }
    }

    IntPtr set_lk_max_locks;
    public SetUIntFcn SetMaxLocks {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lk_max_locks, typeof(SetUIntFcn)); }
    }

    IntPtr set_lk_max_objects;
    public SetUIntFcn SetMaxObjects {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_lk_max_objects, typeof(SetUIntFcn)); }
    }

    IntPtr set_mp_max_openfd;
    public SetIntFcn SetMpMaxOpenFd {
      get { return (SetIntFcn)Marshal.GetDelegateForFunctionPointer(set_mp_max_openfd, typeof(SetIntFcn)); }
    }

    IntPtr set_mp_max_write;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetMpMaxWriteFcn(DB_ENV* dbenv, int maxWrite, int maxWriteSleep);
    public SetMpMaxWriteFcn SetMpMaxWrite {
      get { return (SetMpMaxWriteFcn)Marshal.GetDelegateForFunctionPointer(set_mp_max_write, typeof(SetMpMaxWriteFcn)); }
    }

    IntPtr set_mp_mmapsize;
    public SetUIntFcn SetMpMMapSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_mp_mmapsize, typeof(SetUIntFcn)); }
    }

    IntPtr set_msgcall;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void MsgCallFcn(DB_ENV* dbenv, byte* msg);
    public SetIntPtrFcn SetMsgCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_msgcall, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_msgfile;

    IntPtr set_paniccall;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void PanicCallFcn(DB_ENV* dbenv, int errval);
    public SetIntPtrFcn SetPanicCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_paniccall, typeof(SetIntPtrFcn)); }
    }

    // int  (*set_rep_request) __P((DB_ENV *, u_int32_t, u_int32_t));
    IntPtr set_rep_request;

    IntPtr set_rpc_server;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetRpcServerFcn(
      DB_ENV* dbenv, void* client, byte* host, int cl_timeout, int sv_timeout, UInt32 flags);
    public SetRpcServerFcn SetRpcServer {
      get { return (SetRpcServerFcn)Marshal.GetDelegateForFunctionPointer(set_rpc_server, typeof(SetRpcServerFcn)); }
    }

    IntPtr set_shm_key;
    public SetIntFcn SetShmKey {
      get { return (SetIntFcn)Marshal.GetDelegateForFunctionPointer(set_shm_key, typeof(SetIntFcn)); }
    }

    IntPtr set_thread_count;
    public SetUIntFcn SetThreadCount {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_thread_count, typeof(SetUIntFcn)); }
    }

    IntPtr set_thread_id;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ThreadIdFcn(DB_ENV* dbenv, out int pid, out UInt32 tid);
    public SetIntPtrFcn SetThreadId {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_thread_id, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_thread_id_string;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate byte* ThreadIdStringFcn(DB_ENV* dbenv, int pid, UInt32 tid, byte* buf);
    public SetIntPtrFcn SetThreadIdString {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_thread_id_string, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_timeout;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetTimeoutFcn(DB_ENV* dbenv, UInt32 timeout, UInt32 flags);
    public SetTimeoutFcn SetTimeout {
      get { return (SetTimeoutFcn)Marshal.GetDelegateForFunctionPointer(set_timeout, typeof(SetTimeoutFcn)); }
    }

    IntPtr set_tmp_dir;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetTmpDirFcn(DB_ENV* dbenv, byte* dir);
    public SetTmpDirFcn SetTmpDir {
      get { return (SetTmpDirFcn)Marshal.GetDelegateForFunctionPointer(set_tmp_dir, typeof(SetTmpDirFcn)); }
    }

    IntPtr set_tx_max;
    public SetUIntFcn SetTxMax {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_tx_max, typeof(SetUIntFcn)); }
    }

    IntPtr set_tx_timestamp;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
#if _USE_32BIT_TIME_T
    public delegate DbRetVal SetTxTimestampFcn(DB_ENV* dbenv, int txmax);
#else
    public delegate DbRetVal SetTxTimestampFcn(DB_ENV* dbenv, long txmax);
#endif
    public SetTxTimestampFcn SetTxTimestamp {
      get { return (SetTxTimestampFcn)Marshal.GetDelegateForFunctionPointer(set_tx_timestamp, typeof(SetTxTimestampFcn)); }
    }

    IntPtr set_verbose;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetVerboseFcn(DB_ENV* dbenv, uint which, int onoff);
    public SetVerboseFcn SetVerbose {
      get { return (SetVerboseFcn)Marshal.GetDelegateForFunctionPointer(set_verbose, typeof(SetVerboseFcn)); }
    }

    IntPtr stat_print;
    public SetUIntFcn StatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr txn_begin;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TxnBeginFcn(DB_ENV* dbenv, DB_TXN* parent, out DB_TXN* tid, UInt32 flags);
    public TxnBeginFcn TxnBegin {
      get { return (TxnBeginFcn)Marshal.GetDelegateForFunctionPointer(txn_begin, typeof(TxnBeginFcn)); }
    }

    IntPtr txn_checkpoint;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TxnCheckpointFcn(DB_ENV* dbenv, UInt32 kbyte, UInt32 min, UInt32 flags);
    public TxnCheckpointFcn TxnCheckpoint {
      get { return (TxnCheckpointFcn)Marshal.GetDelegateForFunctionPointer(txn_checkpoint, typeof(TxnCheckpointFcn)); }
    }

    IntPtr txn_recover;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TxnRecoverFcn(DB_ENV* dbenv, DB_PREPLIST* preplist, int count, out int retp, UInt32 flags);
    public TxnRecoverFcn TxnRecover {
      get { return (TxnRecoverFcn)Marshal.GetDelegateForFunctionPointer(txn_recover, typeof(TxnRecoverFcn)); }
    }

    IntPtr txn_stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TxnStatFcn(DB_ENV* dbenv, out DB_TXN_STAT* stat, UInt32 flags);
    public TxnStatFcn TxnStat {
      get { return (TxnStatFcn)Marshal.GetDelegateForFunctionPointer(txn_stat, typeof(TxnStatFcn)); }
    }

    IntPtr txn_stat_print;
    public SetUIntFcn TxnStatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(txn_stat_print, typeof(SetUIntFcn)); }
    }

    #endregion

    IntPtr prdbt;

    int test_abort;     /* Abort value for testing. */
    int test_check;     /* Checkpoint value for testing. */
    int test_copy;      /* Copy value for testing. */

    UInt32 flags;
  }
#endif

  //  not really usable in a .NET environment
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct FILE
  {
    // internals are platform dependent
  }

  /*
  * A DB_LSN has two parts, a fileid which identifies a specific file, and an
  * offset within that file.  The fileid is an unsigned 4-byte quantity that
  * uniquely identifies a file within the log directory -- currently a simple
  * counter inside the log.  The offset is also an unsigned 4-byte value.  The
  * log manager guarantees the offset is never more than 4 bytes by switching
  * to a new log file before the maximum length imposed by an unsigned 4-byte
  * offset is reached.
  */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public struct DB_LSN
  {
    public UInt32 file;       /* File ID. */
    public UInt32 offset;     /* File offset. */
  }

  public enum DB_RECOPS: int
  {
    TXN_ABORT = 0,            /* Public. */
    TXN_APPLY = 1,            /* Public. */
    TXN_BACKWARD_ALLOC = 2,   /* Internal. */
    TXN_BACKWARD_ROLL = 3,    /* Public. */
    TXN_FORWARD_ROLL = 4,     /* Public. */
    TXN_OPENFILES = 5,        /* Internal. */
    TXN_POPENFILES = 6,       /* Internal. */
    TXN_PRINT = 7             /* Public. */
  }

  /* Replication statistics. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_REP_STAT
  {
    /* !!!
     * Many replication statistics fields cannot be protected by a mutex
     * without an unacceptable performance penalty, since most message
     * processing is done without the need to hold a region-wide lock.
     * Fields whose comments end with a '+' may be updated without holding
     * the replication or log mutexes (as appropriate), and thus may be
     * off somewhat (or, on unreasonable architectures under unlucky
     * circumstances, garbaged).
     */
    public UInt32 st_status;              /* Current replication status. */
    public DB_LSN st_next_lsn;            /* Next LSN to use or expect. */
    public DB_LSN st_waiting_lsn;         /* LSN we're awaiting, if any. */
    public UInt32 st_next_pg;             /* Next pg we expect. (typedef  u_int32_t  db_pgno_t;) */
    public UInt32 st_waiting_pg;          /* pg we're awaiting, if any. (typedef  u_int32_t  db_pgno_t;) */

    public UInt32 st_dupmasters;          /* # of times a duplicate master condition was detected.+ */
    public int st_env_id;                 /* Current environment ID. */
    public int st_env_priority;           /* Current environment priority. */
#if BDB_4_5_20
    public UInt32 st_bulk_fills;          /* Bulk buffer fills. */
    public UInt32 st_bulk_overflows;      /* Bulk buffer overflows. */
    public UInt32 st_bulk_records;        /* Bulk records stored. */
    public UInt32 st_bulk_transfers;      /* Transfers of bulk buffers. */
    public UInt32 st_client_rerequests;   /* Number of forced rerequests. */
    public UInt32 st_client_svc_req;      /* Number of client service requests received by this client. */
    public UInt32 st_client_svc_miss;     /* Number of client service requests missing on this client. */
#endif
    public UInt32 st_gen;                 /* Current generation number. */
    public UInt32 st_egen;                /* Current election gen number. */
    public UInt32 st_log_duplicated;      /* Log records received multiply.+ */
    public UInt32 st_log_queued;          /* Log records currently queued.+ */
    public UInt32 st_log_queued_max;      /* Max. log records queued at once.+ */
    public UInt32 st_log_queued_total;    /* Total # of log recs. ever queued.+ */
    public UInt32 st_log_records;         /* Log records received and put.+ */
    public UInt32 st_log_requested;       /* Log recs. missed and requested.+ */
    public int st_master;                 /* Env. ID of the current master. */
    public UInt32 st_master_changes;      /* # of times we've switched masters. */
    public UInt32 st_msgs_badgen;         /* Messages with a bad generation #.+ */
    public UInt32 st_msgs_processed;      /* Messages received and processed.+ */
    public UInt32 st_msgs_recover;        /* Messages ignored because this site was a client in recovery.+ */
    public UInt32 st_msgs_send_failures;  /* # of failed message sends.+ */
    public UInt32 st_msgs_sent;           /* # of successful message sends.+ */
    public UInt32 st_newsites;            /* # of NEWSITE msgs. received.+ */
    public int st_nsites;                 /* Current number of sites we will assume during elections. */
    public UInt32 st_nthrottles;          /* # of times we were throttled. */
    public UInt32 st_outdated;            /* # of times we detected and returned an OUTDATED condition.+ */
    public UInt32 st_pg_duplicated;       /* Pages received multiply.+ */
    public UInt32 st_pg_records;          /* Pages received and stored.+ */
    public UInt32 st_pg_requested;        /* Pages missed and requested.+ */
    public UInt32 st_startup_complete;    /* Site completed client sync-up. */
    public UInt32 st_txns_applied;        /* # of transactions applied.+ */

    /* Elections generally. */
    public UInt32 st_elections;           /* # of elections held.+ */
    public UInt32 st_elections_won;       /* # of elections won by this site.+ */

    /* Statistics about an in-progress election. */
    public int st_election_cur_winner;    /* Current front-runner. */
    public UInt32 st_election_gen;        /* Election generation number. */
    public DB_LSN st_election_lsn;        /* Max. LSN of current winner. */
    public int st_election_nsites;        /* # of "registered voters". */
    public int st_election_nvotes;        /* # of "registered voters" needed. */
    public int st_election_priority;      /* Current election priority. */
    public int st_election_status;        /* Current election status. */
    public UInt32 st_election_tiebreaker; /* Election tiebreaker value. */
    public int st_election_votes;         /* Votes received in this round. */
#if BDB_4_5_20
    public UInt32 st_election_sec;        /* Last election time seconds. */
    public UInt32 st_election_usec;       /* Last election time useconds. */
#endif
  }

  /* Replication statistics. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_MUTEX_STAT
  {
    /* The following fields are maintained in the region's copy. */
    public UInt32 st_mutex_align;         /* Mutex alignment */
    public UInt32 st_mutex_tas_spins;     /* Mutex test-and-set spins */
    public UInt32 st_mutex_cnt;           /* Mutex count */
    public UInt32 st_mutex_free;          /* Available mutexes */
    public UInt32 st_mutex_inuse;         /* Mutexes in use */
    public UInt32 st_mutex_inuse_max;     /* Maximum mutexes ever in use */

    /* The following fields are filled-in from other places. */
    public UInt32 st_region_wait;         /* Region lock granted after wait. */
    public UInt32 st_region_nowait;       /* Region lock granted without wait. */
    public IntPtr st_regsize;             /* Region size. (typedef uintptr_t roff_t;) */
  }

#if BDB_4_3_29

  struct DB_MUTEX
  {
    // not visible externally - dummy declaration
  }

#endif

  /* Replication Manager site status. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public unsafe struct DB_REPMGR_SITE
  {
    public int eid;
    public byte* host;
    public uint port;
    public UInt32 status;
  }

  struct DB_FH
  {
    // not visible externally - dummy declaration
  }
}
