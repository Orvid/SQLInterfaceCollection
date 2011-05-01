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
#if BDB_4_3_29

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DB
  {
    #region Private Fields (to calculate offset to API function pointers)

    /*******************************************************
    * Public: owned by the application.
    *******************************************************/
    UInt32 pgsize;              /* Database logical page size. */
    IntPtr db_append_recno;     /* Callback. */
    IntPtr db_feedback;         /* Callbacks */
    IntPtr dup_compare;         /* Callback. */
    /* Documented, but no access method defined. */
    public IntPtr app_private;  /* Application-private handle. (void*) */
    /*******************************************************
    * Private: owned by DB.
    *******************************************************/
    DB_ENV* dbenv;              /* Backing environment. */
    DbType type;                /* DB access method type. */

    /* Documented, but no access method defined. */
    public readonly DB_MPOOLFILE* mpf;  /* Backing buffer pool. */

    DB_MUTEX* mutexp;           /* Synchronization for free threading */

    byte* fname, dname;         /* File/database passed to DB->open. */
    UInt32 open_flags;          /* Flags passed to DB->open. */
    fixed byte fileid[DbConst.DB_FILE_ID_LEN]; /* File's unique ID for locking. */
    UInt32 adj_fileid;          /* File's unique ID for curs. adj. */

    struct FNAME
    {
      // dummy declaration
    }
    FNAME* log_filename;    /* File's naming info for logging. */

    UInt32 meta_pgno;       /* Meta page number (typedef u_int32_t db_pgno_t;) */
    UInt32 lid;             /* Locker id for handle locking. */
    UInt32 cur_lid;         /* Current handle lock holder. */
    UInt32 associate_lid;   /* Locker id for DB->associate call. */
    DB_LOCK handle_lock;    /* Lock held on this handle. */

    uint   cl_id;           /* RPC: remote client id. */

    /* what is the size of time_t */
#if _USE_32BIT_TIME_T
    int timestamp;          /* Handle timestamp for replication. */
#else
    long timestamp;         /* Handle timestamp for replication. */
#endif

    /*
    * Returned data memory for DB->get() and friends.
    */
    DBT my_rskey;           /* Secondary key. */
    DBT my_rkey;            /* [Primary] key. */
    DBT my_rdata;           /* Data. */

    /*
    * !!!
    * Some applications use DB but implement their own locking outside of
    * DB.  If they're using fcntl(2) locking on the underlying database
    * file, and we open and close a file descriptor for that file, we will
    * discard their locks.  The DB_FCNTL_LOCKING flag to DB->open is an
    * undocumented interface to support this usage which leaves any file
    * descriptors we open until DB->close.  This will only work with the
    * DB->open interface and simple caches, e.g., creating a transaction
    * thread may open/close file descriptors this flag doesn't protect.
    * Locking with fcntl(2) on a file that you don't own is a very, very
    * unsafe thing to do.  'Nuff said.
    */
    DB_FH* saved_open_fhp;   /* Saved file handle. */

    /*
    * Linked list of DBP's, linked from the DB_ENV, used to keep track
    * of all open db handles for cursor adjustment.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * LIST_ENTRY(__db) dblistlinks;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct DB_LIST_LINKS {
      DB* le_next;
      DB** le_prev;
    }
    DB_LIST_LINKS dblistlinks;

    /*
    * Cursor queues.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_HEAD(__cq_fq, __dbc) free_queue;
    * TAILQ_HEAD(__cq_aq, __dbc) active_queue;
    * TAILQ_HEAD(__cq_jq, __dbc) join_queue;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct CQ_FQ {
      DBC* tqh_first;
      DBC** tqh_last;
    }
    CQ_FQ free_queue;

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct CQ_AQ
    {
      DBC* tqh_first;
      DBC** tqh_last;
    } 
    CQ_AQ active_queue;

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct CQ_JQ
    {
      DBC* tqh_first;
      DBC** tqh_last;
    } 
    CQ_JQ join_queue;

    /*
    * Secondary index support.
    *
    * Linked list of secondary indices -- set in the primary.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * LIST_HEAD(s_secondaries, __db);
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct S_SECONDARIES
    {
      DB* lh_first;
    } 
    S_SECONDARIES s_secondaries;

    /*
    * List entries for secondaries, and reference count of how
    * many threads are updating this secondary (see __db_c_put).
    *
    * !!!
    * Note that these are synchronized by the primary's mutex, but
    * filled in in the secondaries.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * LIST_ENTRY(__db) s_links;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct S_LINKS
    {
      DB* le_next;
      DB** le_prev;
    } 
    S_LINKS s_links;

    UInt32 s_refcnt;

    /* Secondary callback and free functions -- set in the secondary. */
    IntPtr s_callback;

    /* Reference to primary -- set in the secondary. */
    DB* s_primary;

    /* API-private structure: used by DB 1.85, C++, Java, Perl and Tcl - and dotNET */
    public IntPtr api_internal; // void* api_internal;

    /* Subsystem-private structure. */
    void* bt_internal;    /* Btree/Recno access method. */
    void* h_internal;     /* Hash access method. */
    void* q_internal;     /* Queue access method. */
    void* xa_internal;    /* XA. */

    #endregion

  #region API Methods

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetUIntFcn(DB* db, out UInt32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetUIntFcn(DB* db, UInt32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetIntPtrFcn(DB* db, IntPtr value);

    IntPtr associate;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal KeyGeneratorFcn(DB* secondary, ref DBT key, ref DBT data, ref DBT result);
    // callback must be based on a delegate of type KeyGeneratorFcn
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AssociateFcn(DB* primary, DB_TXN* txnid, DB* secondary, IntPtr callback, UInt32 flags);
    public AssociateFcn Associate {
      get { return (AssociateFcn)Marshal.GetDelegateForFunctionPointer(associate, typeof(AssociateFcn)); }
    }

    IntPtr close;
    public SetUIntFcn Close {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(close, typeof(SetUIntFcn)); }
    }

    IntPtr cursor;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CursorFcn(DB* db, DB_TXN* txnid, out DBC* cursor, UInt32 flags);
    public CursorFcn Cursor {
      get { return (CursorFcn)Marshal.GetDelegateForFunctionPointer(cursor, typeof(CursorFcn)); }
    }

    IntPtr del;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DelFcn(DB* db, DB_TXN* txnid, ref DBT key, UInt32 flags);
    public DelFcn Del {
      get { return (DelFcn)Marshal.GetDelegateForFunctionPointer(del, typeof(DelFcn)); }
    }

    // not documented, do we need to expose publicly?
    IntPtr dump;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DumpCallbackFcn(void* handle, void* buf);
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DumpFcn(DB* db, byte* subname, IntPtr callback, void* handle, int pflag, int keyflag);
    public DumpFcn Dump {
      get { return (DumpFcn)Marshal.GetDelegateForFunctionPointer(dump, typeof(DumpFcn)); }
    }
    // public DbRetVal Dump(DB* db, byte* subname, DbDumpCallbackFcn callback, void* handle, int pflag, int keyflag) {
    //   IntPtr cb = Marshal.GetFunctionPointerForDelegate(callback);
    //   return DumpFcn(db, subname, cb, handle, pflag, keyflag);
    // }

    IntPtr err;
    // C style varargs can be translated using the __arglist keyword
    // C declaration: void (*err) __P((DB *, int, const char *, ...));
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrFcn(DB* db, int error, byte* fmt/*, __arglist*/);
    public ErrFcn Err {
      get { return (ErrFcn)Marshal.GetDelegateForFunctionPointer(err, typeof(ErrFcn)); }
    }

    IntPtr errx;
    // see ErrFcn
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrxFcn(DB* db, byte* fmt/*, __arglist*/);
    public ErrxFcn Errx {
      get { return (ErrxFcn)Marshal.GetDelegateForFunctionPointer(errx, typeof(ErrxFcn)); }
    }

    IntPtr fd;
    /* It may not make sense to expose this API in the .NET environemnt
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal FdFcn(DB* db, out int fd);
    public FdFcn Fd {
      get { return (FdFcn)Marshal.GetDelegateForFunctionPointer(fd, typeof(FdFcn)); }
    }
    */

    IntPtr get;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFcn(DB* db, DB_TXN* txnid, ref DBT key, ref DBT data, UInt32 flags);
    public GetFcn Get {
      get { return (GetFcn)Marshal.GetDelegateForFunctionPointer(get, typeof(GetFcn)); }
    }

    IntPtr pget;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PGetFcn(DB* db, DB_TXN* txnid, ref DBT key, ref DBT pkey, ref DBT data, UInt32 flags);
    public PGetFcn PGet {
      get { return (PGetFcn)Marshal.GetDelegateForFunctionPointer(pget, typeof(PGetFcn)); }
    }

    IntPtr get_byteswapped;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetByteSwappedFcn(DB* db, out int isswapped);
    public GetByteSwappedFcn GetByteSwapped {
      get { return (GetByteSwappedFcn)Marshal.GetDelegateForFunctionPointer(get_byteswapped, typeof(GetByteSwappedFcn)); }
    }

    IntPtr get_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetCacheSizeFcn(DB* db, out UInt32 gbytes, out UInt32 bytes, out int ncache);
    public GetCacheSizeFcn GetCacheSize {
      get { return (GetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(get_cachesize, typeof(GetCacheSizeFcn)); }
    }

    IntPtr get_dbname;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetDbNameFcn(DB* db, out byte* filename, out byte* dbname);
    public GetDbNameFcn GetDbName {
      get { return (GetDbNameFcn)Marshal.GetDelegateForFunctionPointer(get_dbname, typeof(GetDbNameFcn)); }
    }

    IntPtr get_encrypt_flags;
    public GetUIntFcn GetEncryptFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_encrypt_flags, typeof(GetUIntFcn)); }
    }

    IntPtr get_env;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DB_ENV* GetEnvFcn(DB* db);
    public GetEnvFcn GetEnv {
      get { return (GetEnvFcn)Marshal.GetDelegateForFunctionPointer(get_env, typeof(GetEnvFcn)); }
    }

    IntPtr get_errfile;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void GetErrFileFcn(DB* db, out FILE* errfile);
    public GetErrFileFcn GetErrFile {
      get { return (GetErrFileFcn)Marshal.GetDelegateForFunctionPointer(get_errfile, typeof(GetErrFileFcn)); }
    }

    IntPtr get_errpfx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void GetErrPfxFcn(DB* db, out byte* errpfx);
    public GetErrPfxFcn GetErrPfx {
      get { return (GetErrPfxFcn)Marshal.GetDelegateForFunctionPointer(get_errpfx, typeof(GetErrPfxFcn)); }
    }

    IntPtr get_flags;
    public GetUIntFcn GetFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_flags, typeof(GetUIntFcn)); }
    }

    IntPtr get_lorder;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetLOrderFcn(DB* db, out int lorder);
    public GetLOrderFcn GetLOrder {
      get { return (GetLOrderFcn)Marshal.GetDelegateForFunctionPointer(get_lorder, typeof(GetLOrderFcn)); }
    }

    IntPtr get_open_flags;
    public GetUIntFcn GetOpenFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_open_flags, typeof(GetUIntFcn)); }
    }

    IntPtr get_pagesize;
    public GetUIntFcn GetPageSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_pagesize, typeof(GetUIntFcn)); }
    }
    
    IntPtr get_transactional;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate int GetTransactionalFcn(DB* db);
    public GetTransactionalFcn GetTransactional {
      get { return (GetTransactionalFcn)Marshal.GetDelegateForFunctionPointer(get_transactional, typeof(GetTransactionalFcn)); }
    }

    IntPtr get_type;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetDbTypeFcn(DB* db, out DbType type);
    public GetDbTypeFcn GetDbType {
      get { return (GetDbTypeFcn)Marshal.GetDelegateForFunctionPointer(get_type, typeof(GetDbTypeFcn)); }
    }

    IntPtr join;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal JoinFcn(DB* primary, DBC** curslist, out DBC* dbc, UInt32 flags);
    public JoinFcn Join {
      get { return (JoinFcn)Marshal.GetDelegateForFunctionPointer(join, typeof(JoinFcn)); }
    }

    IntPtr key_range;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal KeyRangeFcn(DB* db, DB_TXN* txnid, ref DBT key, out DB_KEY_RANGE key_range, UInt32 flags);
    public KeyRangeFcn KeyRange {
      get { return (KeyRangeFcn)Marshal.GetDelegateForFunctionPointer(key_range, typeof(KeyRangeFcn)); }
    }

    IntPtr open;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal
    OpenFcn(DB* db, DB_TXN* txnid, byte* file, byte* database, DbType type, UInt32 flags, int mode);
    public OpenFcn Open {
      get { return (OpenFcn)Marshal.GetDelegateForFunctionPointer(open, typeof(OpenFcn)); }
    }

    IntPtr put;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PutFcn(DB* db, DB_TXN* txnid, ref DBT key, ref DBT data, UInt32 flags);
    public PutFcn Put {
      get { return (PutFcn)Marshal.GetDelegateForFunctionPointer(put, typeof(PutFcn)); }
    }

    IntPtr remove;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RemoveFcn(DB* db, byte* file, byte* database, UInt32 flags);
    public RemoveFcn Remove {
      get { return (RemoveFcn)Marshal.GetDelegateForFunctionPointer(remove, typeof(RemoveFcn)); }
    }

    IntPtr rename;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RenameFcn(DB* db, byte* file, byte* database, byte* newname, UInt32 flags);
    public RenameFcn Rename {
      get { return (RenameFcn)Marshal.GetDelegateForFunctionPointer(rename, typeof(RenameFcn)); }
    }

    IntPtr truncate;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TruncateFcn(DB* db, DB_TXN* txnid, out UInt32 count, UInt32 flags);
    public TruncateFcn Truncate {
      get { return (TruncateFcn)Marshal.GetDelegateForFunctionPointer(truncate, typeof(TruncateFcn)); }
    }

    IntPtr set_append_recno;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AppendRecnoFcn(DB* db, ref DBT data, UInt32 recno);
    public SetIntPtrFcn SetAppendRecno {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_append_recno, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_alloc;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetAllocFcn(DB* db, IntPtr app_malloc, IntPtr app_realloc, IntPtr app_free);
    public SetAllocFcn SetAlloc {
      get { return (SetAllocFcn)Marshal.GetDelegateForFunctionPointer(set_alloc, typeof(SetAllocFcn)); }
    }
  
    IntPtr set_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetCacheSizeFcn(DB* db, UInt32 gbytes, UInt32 bytes, int ncache);
    public SetCacheSizeFcn SetCacheSize {
      get { return (SetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(set_cachesize, typeof(SetCacheSizeFcn)); }
    }
  
    IntPtr set_dup_compare;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate int DupCompareFcn(DB* db, ref DBT appData, ref DBT dbData);
    public SetIntPtrFcn SetDupCompare {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_dup_compare, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_encrypt;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetEncryptFcn(DB* db, byte* passwd, UInt32 flags);
    public SetEncryptFcn SetEncrypt {
      get { return (SetEncryptFcn)Marshal.GetDelegateForFunctionPointer(set_encrypt, typeof(SetEncryptFcn)); }
    }

    // likely not used, as it fits better with DB_ENV (and its equivalent)
    IntPtr set_errcall;
    // the argument to SetErrCall must be of type DB_ENV.ErrCallFcn
    public SetIntPtrFcn SetErrCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_errcall, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_errfile;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void SetErrFileFcn(DB* db, FILE* errfile);
    public SetErrFileFcn SetErrFile {
      get { return (SetErrFileFcn)Marshal.GetDelegateForFunctionPointer(set_errfile, typeof(SetErrFileFcn)); }
    }

    // likely not used, as it fits better with DB_ENV (and its equivalent)
    IntPtr set_errpfx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void SetErrPfxFcn(DB* db, byte* errpfx);
    public SetErrPfxFcn SetErrPfx {
      get { return (SetErrPfxFcn)Marshal.GetDelegateForFunctionPointer(set_errpfx, typeof(SetErrPfxFcn)); }
    }

    IntPtr set_feedback;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void FeedbackFcn(DB* db, int opcode, int percent);
    public SetIntPtrFcn SetFeedback {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_feedback, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_flags;
    public SetUIntFcn SetFlags {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_flags, typeof(SetUIntFcn)); }
    }

    IntPtr set_lorder;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetLOrderFcn(DB* db, int lorder);
    public SetLOrderFcn SetLOrder {
      get { return (SetLOrderFcn)Marshal.GetDelegateForFunctionPointer(set_lorder, typeof(SetLOrderFcn)); }
    }

    // likely not used, as it fits better with DB_ENV (and its equivalent)
    IntPtr set_msgcall;
    public SetIntPtrFcn SetMsgCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_msgcall, typeof(SetIntPtrFcn)); }
    }

    IntPtr get_msgfile;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void GetMsgFileFcn(DB* db, out FILE* msgfile);
    public GetMsgFileFcn GetMsgFile {
      get { return (GetMsgFileFcn)Marshal.GetDelegateForFunctionPointer(get_msgfile, typeof(GetMsgFileFcn)); }
    }

    IntPtr set_msgfile;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void SetMsgFileFcn(DB* db, FILE* msgfile);
    public SetMsgFileFcn SetMsgFile {
      get { return (SetMsgFileFcn)Marshal.GetDelegateForFunctionPointer(set_msgfile, typeof(SetMsgFileFcn)); }
    }

    IntPtr set_pagesize;
    public SetUIntFcn SetPageSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_pagesize, typeof(SetUIntFcn)); }
    }

    IntPtr set_paniccall;
    // the argument to SetPanicCall must be of type DB_ENV.PanicCallFcn
    public SetIntPtrFcn SetPanicCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_paniccall, typeof(SetIntPtrFcn)); }
    }

    IntPtr stat;  // returns different struct types for different db types
    //[UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    //public delegate DbRetVal StatFcn<T>(DB* db, DB_TXN* txnid, out T* sp, UInt32 flags)
    //  where T: struct;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal BtreeStatFcn(DB* db, DB_TXN* txnid, out DB_BTREE_STAT* sp, UInt32 flags);
    public BtreeStatFcn BTreeStat {
      get { return (BtreeStatFcn)Marshal.GetDelegateForFunctionPointer(stat, typeof(BtreeStatFcn)); }
    }
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal HashStatFcn(DB* db, DB_TXN* txnid, out DB_HASH_STAT* sp, UInt32 flags);
    public HashStatFcn HashStat {
      get { return (HashStatFcn)Marshal.GetDelegateForFunctionPointer(stat, typeof(HashStatFcn)); }
    }
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal QueueStatFcn(DB* db, DB_TXN* txnid, out DB_QUEUE_STAT* sp, UInt32 flags);
    public QueueStatFcn QueueStat {
      get { return (QueueStatFcn)Marshal.GetDelegateForFunctionPointer(stat, typeof(QueueStatFcn)); }
    }

    IntPtr stat_print;  // seems to hang when called while a transaction is open
    public SetUIntFcn StatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr sync;
    // flags currently unused, must be 0
    public SetUIntFcn Sync {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(sync, typeof(SetUIntFcn)); }
    }

    IntPtr upgrade;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal UpgradeFcn(DB* db, byte* file, UInt32 flags);
    public UpgradeFcn Upgrade {
      get { return (UpgradeFcn)Marshal.GetDelegateForFunctionPointer(upgrade, typeof(UpgradeFcn)); }
    }
    
    IntPtr verify;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal VerifyFcn(DB* db, byte* file, byte* database, FILE* outfile, UInt32 flags);
    public VerifyFcn Verify {
      get { return (VerifyFcn)Marshal.GetDelegateForFunctionPointer(verify, typeof(VerifyFcn)); }
    }

    IntPtr get_bt_minkey;
    public GetUIntFcn GetBtMinKey {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_bt_minkey, typeof(GetUIntFcn)); }
    }

    IntPtr set_bt_compare;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate int BtCompareFcn(DB* db, ref DBT dbt1, ref DBT dbt2);
    public SetIntPtrFcn SetBtCompare {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_bt_compare, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_bt_maxkey;
    public SetUIntFcn SetBtMaxKey {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_bt_maxkey, typeof(SetUIntFcn)); }
    }

    IntPtr set_bt_minkey;
    public SetUIntFcn SetBtMinKey {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_bt_minkey, typeof(SetUIntFcn)); }
    }

    IntPtr set_bt_prefix;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate uint /* should map to size_t, so maybe IntPtr? */
    BtPrefixFcn(DB* db, ref DBT dbt1, ref DBT dbt2);
    public SetIntPtrFcn SetBtPrefix {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_bt_prefix, typeof(SetIntPtrFcn)); }
    }

    IntPtr get_h_ffactor;
    public GetUIntFcn GetHFFactor {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_h_ffactor, typeof(GetUIntFcn)); }
    }

    IntPtr get_h_nelem;
    public GetUIntFcn GetHNelem {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_h_nelem, typeof(GetUIntFcn)); }
    }

    IntPtr set_h_ffactor;
    public SetUIntFcn SetHFFactor {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_h_ffactor, typeof(SetUIntFcn)); }
    }

    IntPtr set_h_hash;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate UInt32 HHashFcn(DB* db, void* bytes, UInt32 length);
    public SetIntPtrFcn SetHHash {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_h_hash, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_h_nelem;
    public SetUIntFcn SetHNelem {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_h_nelem, typeof(SetUIntFcn)); }
    }

    IntPtr get_re_delim;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetReDelimFcn(DB* db, out int delim);
    public GetReDelimFcn GetReDelim {
      get { return (GetReDelimFcn)Marshal.GetDelegateForFunctionPointer(get_re_delim, typeof(GetReDelimFcn)); }
    }

    IntPtr get_re_len;
    public GetUIntFcn GetReLen {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_re_len, typeof(GetUIntFcn)); }
    }

    IntPtr get_re_pad;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetRePadFcn(DB* db, out int re_pad);
    public GetRePadFcn GetRePad {
      get { return (GetRePadFcn)Marshal.GetDelegateForFunctionPointer(get_re_pad, typeof(GetRePadFcn)); }
    }

    IntPtr get_re_source;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetReSourceFcn(DB* db, out byte* source);
    public GetReSourceFcn GetReSource {
      get { return (GetReSourceFcn)Marshal.GetDelegateForFunctionPointer(get_re_source, typeof(GetReSourceFcn)); }
    }

    IntPtr set_re_delim;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetReDelimFcn(DB* db, int delim);
    public SetReDelimFcn SetReDelim {
      get { return (SetReDelimFcn)Marshal.GetDelegateForFunctionPointer(set_re_delim, typeof(SetReDelimFcn)); }
    }

    IntPtr set_re_len;
    public SetUIntFcn SetReLen {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_re_len, typeof(SetUIntFcn)); }
    }

    IntPtr set_re_pad;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetRePadFcn(DB* db, int re_pad);
    public SetRePadFcn SetRePad {
      get { return (SetRePadFcn)Marshal.GetDelegateForFunctionPointer(set_re_pad, typeof(SetRePadFcn)); }
    }

    IntPtr set_re_source;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetReSourceFcn(DB* db, byte* source);
    public SetReSourceFcn SetReSource {
      get { return (SetReSourceFcn)Marshal.GetDelegateForFunctionPointer(set_re_source, typeof(SetReSourceFcn)); }
    }

    IntPtr get_q_extentsize;
    public GetUIntFcn GetQExtentSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_q_extentsize, typeof(GetUIntFcn)); }
    }

    IntPtr set_q_extentsize;
    public SetUIntFcn SetQExtentSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_q_extentsize, typeof(SetUIntFcn)); }
    }

    IntPtr db_am_remove;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AMRemoveFcn(DB* db, DB_TXN* txnid, byte* file, byte* database);
    public AMRemoveFcn AMRemove {
      get { return (AMRemoveFcn)Marshal.GetDelegateForFunctionPointer(db_am_remove, typeof(AMRemoveFcn)); }
    }

    IntPtr db_am_rename;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AMRenameFcn(DB* db, DB_TXN* txnid, byte* file, byte* database, byte* newname);
    public AMRenameFcn AMRename {
      get { return (AMRenameFcn)Marshal.GetDelegateForFunctionPointer(db_am_rename, typeof(AMRenameFcn)); }
    }

    /*
    * Never called; these are a place to save function pointers
    * so that we can undo an associate.
    */
    IntPtr stored_get;
    IntPtr stored_close;

  #endregion

    UInt32 am_ok;           /* Legal AM choices. */
    UInt32 orig_flags;      /* Flags at  open, for refresh. */
    UInt32 flags;

    // It seems there is no other way to detect if a database is associated.
    public bool IsSecondary {
      get { return (flags & DbConst.DB_AM_SECONDARY) != 0; }
    }
  }

#endif

#if BDB_4_5_20

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DB
  {
    #region Private Fields (to calculate offset to API function pointers)

    /*******************************************************
    * Public: owned by the application.
    *******************************************************/
    UInt32 pgsize;              /* Database logical page size. */
    IntPtr db_append_recno;     /* Callback. */
    IntPtr db_feedback;         /* Callbacks */
    IntPtr dup_compare;         /* Callback. */
    /* Documented, but no access method defined. */
    public IntPtr app_private;  /* Application-private handle. (void*) */
    /*******************************************************
    * Private: owned by DB.
    *******************************************************/
    DB_ENV* dbenv;          /* Backing environment. */
    DbType type;            /* DB access method type. */

    /* Documented, but no access method defined. */
#if BDB_4_3_29
    public readonly 
#endif
    DB_MPOOLFILE* mpf;  /* Backing buffer pool. */

    UInt32 mutex;           /* Synchronization for free threading (typedef u_int32_t db_mutex_t;) */

    byte* fname, dname;     /* File/database passed to DB->open. */
    UInt32 open_flags;      /* Flags passed to DB->open. */
    fixed byte fileid[DbConst.DB_FILE_ID_LEN]; /* File's unique ID for locking. */
    UInt32 adj_fileid;      /* File's unique ID for curs. adj. */

    struct FNAME
    {
      // dummy declaration
    }
    FNAME* log_filename;    /* File's naming info for logging. */

    UInt32 meta_pgno;       /* Meta page number (typedef u_int32_t db_pgno_t;) */
    UInt32 lid;             /* Locker id for handle locking. */
    UInt32 cur_lid;         /* Current handle lock holder. */
    UInt32 associate_lid;   /* Locker id for DB->associate call. */
    DB_LOCK handle_lock;    /* Lock held on this handle. */

    uint   cl_id;           /* RPC: remote client id. */

    /* what is the size of time_t */
  #if _USE_32BIT_TIME_T
    int timestamp;          /* Handle timestamp for replication. */
  #else
    long timestamp;         /* Handle timestamp for replication. */
  #endif

    UInt32 fid_gen;         /* Rep generation number for fids. */

    /*
    * Returned data memory for DB->get() and friends.
    */
    DBT my_rskey;           /* Secondary key. */
    DBT my_rkey;            /* [Primary] key. */
    DBT my_rdata;           /* Data. */

    /*
    * !!!
    * Some applications use DB but implement their own locking outside of
    * DB.  If they're using fcntl(2) locking on the underlying database
    * file, and we open and close a file descriptor for that file, we will
    * discard their locks.  The DB_FCNTL_LOCKING flag to DB->open is an
    * undocumented interface to support this usage which leaves any file
    * descriptors we open until DB->close.  This will only work with the
    * DB->open interface and simple caches, e.g., creating a transaction
    * thread may open/close file descriptors this flag doesn't protect.
    * Locking with fcntl(2) on a file that you don't own is a very, very
    * unsafe thing to do.  'Nuff said.
    */
    DB_FH* saved_open_fhp;   /* Saved file handle. */

    /*
    * Linked list of DBP's, linked from the DB_ENV, used to keep track
    * of all open db handles for cursor adjustment.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_ENTRY(__db) dblistlinks;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct DB_LIST_LINKS {
      DB* tqe_next;
      DB** tqe_prev;
    }
    DB_LIST_LINKS dblistlinks;

    /*
    * Cursor queues.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_HEAD(__cq_fq, __dbc) free_queue;
    * TAILQ_HEAD(__cq_aq, __dbc) active_queue;
    * TAILQ_HEAD(__cq_jq, __dbc) join_queue;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct CQ_FQ {
      DBC* tqh_first;
      DBC** tqh_last;
    }
    CQ_FQ free_queue;

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct CQ_AQ
    {
      DBC* tqh_first;
      DBC** tqh_last;
    } 
    CQ_AQ active_queue;

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct CQ_JQ
    {
      DBC* tqh_first;
      DBC** tqh_last;
    } 
    CQ_JQ join_queue;

    /*
    * Secondary index support.
    *
    * Linked list of secondary indices -- set in the primary.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * LIST_HEAD(s_secondaries, __db);
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct S_SECONDARIES
    {
      DB* lh_first;
    } 
    S_SECONDARIES s_secondaries;

    /*
    * List entries for secondaries, and reference count of how
    * many threads are updating this secondary (see __db_c_put).
    *
    * !!!
    * Note that these are synchronized by the primary's mutex, but
    * filled in in the secondaries.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * LIST_ENTRY(__db) s_links;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct S_LINKS
    {
      DB* le_next;
      DB** le_prev;
    } 
    S_LINKS s_links;

    UInt32 s_refcnt;

    /* Secondary callback and free functions -- set in the secondary. */
    IntPtr s_callback;

    /* Reference to primary -- set in the secondary. */
    DB* s_primary;

    public const UInt32 DB_ASSOC_IMMUTABLE_KEY = 0x00000001;  /* Secondary key is immutable. */

    /* Flags passed to associate -- set in the secondary. */
	  UInt32 s_assoc_flags;

    /* API-private structure: used by DB 1.85, C++, Java, Perl and Tcl - and dotNET */
    public IntPtr api_internal; // void* api_internal;

    /* Subsystem-private structure. */
    void* bt_internal;    /* Btree/Recno access method. */
    void* h_internal;     /* Hash access method. */
    void* q_internal;     /* Queue access method. */
    void* xa_internal;    /* XA. */

    #endregion

    #region API Methods

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetUIntFcn(DB* db, out UInt32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetUIntFcn(DB* db, UInt32 value);

    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetIntPtrFcn(DB* db, IntPtr value);

    IntPtr associate;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal KeyGeneratorFcn(DB* secondary, ref DBT key, ref DBT data, ref DBT result);
    // callback must be based on a delegate of type KeyGeneratorFcn
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AssociateFcn(DB* primary, DB_TXN* txnid, DB* secondary, IntPtr callback, UInt32 flags);
    public AssociateFcn Associate {
      get { return (AssociateFcn)Marshal.GetDelegateForFunctionPointer(associate, typeof(AssociateFcn)); }
    }

    IntPtr close;
    public SetUIntFcn Close {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(close, typeof(SetUIntFcn)); }
    }

    IntPtr compact;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CompactFcn(DB* db, DB_TXN *txnid, DBT* start, DBT* stop, 
      DB_COMPACT* c_data, UInt32 flags, out DBT end);
    public CompactFcn Compact {
      get { return (CompactFcn)Marshal.GetDelegateForFunctionPointer(compact, typeof(CompactFcn)); }
    }

    IntPtr cursor;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CursorFcn(DB* db, DB_TXN* txnid, out DBC* cursor, UInt32 flags);
    public CursorFcn Cursor {
      get { return (CursorFcn)Marshal.GetDelegateForFunctionPointer(cursor, typeof(CursorFcn)); }
    }

    IntPtr del;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DelFcn(DB* db, DB_TXN* txnid, ref DBT key, UInt32 flags);
    public DelFcn Del {
      get { return (DelFcn)Marshal.GetDelegateForFunctionPointer(del, typeof(DelFcn)); }
    }

    IntPtr err;
    // C style varargs can be translated using the __arglist keyword
    // C declaration: void (*err) __P((DB *, int, const char *, ...));
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrFcn(DB* db, int error, byte* fmt/*, __arglist*/);
    public ErrFcn Err {
      get { return (ErrFcn)Marshal.GetDelegateForFunctionPointer(err, typeof(ErrFcn)); }
    }

    IntPtr errx;
    // see ErrFcn
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void ErrxFcn(DB* db, byte* fmt/*, __arglist*/);
    public ErrxFcn Errx {
      get { return (ErrxFcn)Marshal.GetDelegateForFunctionPointer(errx, typeof(ErrxFcn)); }
    }

    IntPtr fd;
    /* It may not make sense to expose this API in the .NET environemnt
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal FdFcn(DB* db, out int fd);
    public FdFcn Fd {
      get { return (FdFcn)Marshal.GetDelegateForFunctionPointer(fd, typeof(FdFcn)); }
    }
    */

    IntPtr get;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFcn(DB* db, DB_TXN* txnid, ref DBT key, ref DBT data, UInt32 flags);
    public GetFcn Get {
      get { return (GetFcn)Marshal.GetDelegateForFunctionPointer(get, typeof(GetFcn)); }
    }

    IntPtr get_bt_minkey;
    public GetUIntFcn GetBtMinKey {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_bt_minkey, typeof(GetUIntFcn)); }
    }

    IntPtr get_byteswapped;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetByteSwappedFcn(DB* db, out int isswapped);
    public GetByteSwappedFcn GetByteSwapped {
      get { return (GetByteSwappedFcn)Marshal.GetDelegateForFunctionPointer(get_byteswapped, typeof(GetByteSwappedFcn)); }
    }

    IntPtr get_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetCacheSizeFcn(DB* db, out UInt32 gbytes, out UInt32 bytes, out int ncache);
    public GetCacheSizeFcn GetCacheSize {
      get { return (GetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(get_cachesize, typeof(GetCacheSizeFcn)); }
    }

    IntPtr get_dbname;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetDbNameFcn(DB* db, out byte* filename, out byte* dbname);
    public GetDbNameFcn GetDbName {
      get { return (GetDbNameFcn)Marshal.GetDelegateForFunctionPointer(get_dbname, typeof(GetDbNameFcn)); }
    }

    IntPtr get_encrypt_flags;
    public GetUIntFcn GetEncryptFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_encrypt_flags, typeof(GetUIntFcn)); }
    }

    IntPtr get_env;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DB_ENV* GetEnvFcn(DB* db);
    public GetEnvFcn GetEnv {
      get { return (GetEnvFcn)Marshal.GetDelegateForFunctionPointer(get_env, typeof(GetEnvFcn)); }
    }

    IntPtr get_errfile;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void GetErrFileFcn(DB* db, out FILE* errfile);
    public GetErrFileFcn GetErrFile {
      get { return (GetErrFileFcn)Marshal.GetDelegateForFunctionPointer(get_errfile, typeof(GetErrFileFcn)); }
    }

    IntPtr get_errpfx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void GetErrPfxFcn(DB* db, out byte* errpfx);
    public GetErrPfxFcn GetErrPfx {
      get { return (GetErrPfxFcn)Marshal.GetDelegateForFunctionPointer(get_errpfx, typeof(GetErrPfxFcn)); }
    }

    IntPtr get_flags;
    public GetUIntFcn GetFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_flags, typeof(GetUIntFcn)); }
    }

    IntPtr get_h_ffactor;
    public GetUIntFcn GetHFFactor {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_h_ffactor, typeof(GetUIntFcn)); }
    }

    IntPtr get_h_nelem;
    public GetUIntFcn GetHNelem {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_h_nelem, typeof(GetUIntFcn)); }
    }

    IntPtr get_lorder;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetLOrderFcn(DB* db, out int lorder);
    public GetLOrderFcn GetLOrder {
      get { return (GetLOrderFcn)Marshal.GetDelegateForFunctionPointer(get_lorder, typeof(GetLOrderFcn)); }
    }

    IntPtr get_mpf;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DB_MPOOLFILE* GetMpfFcn(DB* db);
    public GetMpfFcn GetMpf {
      get { return (GetMpfFcn)Marshal.GetDelegateForFunctionPointer(get_mpf, typeof(GetMpfFcn)); }
    }

    IntPtr get_msgfile;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void GetMsgFileFcn(DB* db, out FILE* msgfile);
    public GetMsgFileFcn GetMsgFile {
      get { return (GetMsgFileFcn)Marshal.GetDelegateForFunctionPointer(get_msgfile, typeof(GetMsgFileFcn)); }
    }

    IntPtr get_open_flags;
    public GetUIntFcn GetOpenFlags {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_open_flags, typeof(GetUIntFcn)); }
    }

    IntPtr get_pagesize;
    public GetUIntFcn GetPageSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_pagesize, typeof(GetUIntFcn)); }
    }

    IntPtr get_q_extentsize;
    public GetUIntFcn GetQExtentSize {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_q_extentsize, typeof(GetUIntFcn)); }
    }

    IntPtr get_re_delim;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetReDelimFcn(DB* db, out int delim);
    public GetReDelimFcn GetReDelim {
      get { return (GetReDelimFcn)Marshal.GetDelegateForFunctionPointer(get_re_delim, typeof(GetReDelimFcn)); }
    }

    IntPtr get_re_len;
    public GetUIntFcn GetReLen {
      get { return (GetUIntFcn)Marshal.GetDelegateForFunctionPointer(get_re_len, typeof(GetUIntFcn)); }
    }

    IntPtr get_re_pad;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetRePadFcn(DB* db, out int re_pad);
    public GetRePadFcn GetRePad {
      get { return (GetRePadFcn)Marshal.GetDelegateForFunctionPointer(get_re_pad, typeof(GetRePadFcn)); }
    }

    IntPtr get_re_source;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetReSourceFcn(DB* db, out byte* source);
    public GetReSourceFcn GetReSource {
      get { return (GetReSourceFcn)Marshal.GetDelegateForFunctionPointer(get_re_source, typeof(GetReSourceFcn)); }
    }
    
    IntPtr get_transactional;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate int GetTransactionalFcn(DB* db);
    public GetTransactionalFcn GetTransactional {
      get { return (GetTransactionalFcn)Marshal.GetDelegateForFunctionPointer(get_transactional, typeof(GetTransactionalFcn)); }
    }

    IntPtr get_type;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetDbTypeFcn(DB* db, out DbType type);
    public GetDbTypeFcn GetDbType {
      get { return (GetDbTypeFcn)Marshal.GetDelegateForFunctionPointer(get_type, typeof(GetDbTypeFcn)); }
    }

    IntPtr join;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal JoinFcn(DB* primary, DBC** curslist, out DBC* dbc, UInt32 flags);
    public JoinFcn Join {
      get { return (JoinFcn)Marshal.GetDelegateForFunctionPointer(join, typeof(JoinFcn)); }
    }

    IntPtr key_range;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal KeyRangeFcn(DB* db, DB_TXN* txnid, ref DBT key, out DB_KEY_RANGE key_range, UInt32 flags);
    public KeyRangeFcn KeyRange {
      get { return (KeyRangeFcn)Marshal.GetDelegateForFunctionPointer(key_range, typeof(KeyRangeFcn)); }
    }

    IntPtr open;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal
    OpenFcn(DB* db, DB_TXN* txnid, byte* file, byte* database, DbType type, UInt32 flags, int mode);
    public OpenFcn Open {
      get { return (OpenFcn)Marshal.GetDelegateForFunctionPointer(open, typeof(OpenFcn)); }
    }

    IntPtr pget;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PGetFcn(DB* db, DB_TXN* txnid, ref DBT key, ref DBT pkey, ref DBT data, UInt32 flags);
    public PGetFcn PGet {
      get { return (PGetFcn)Marshal.GetDelegateForFunctionPointer(pget, typeof(PGetFcn)); }
    }

    IntPtr put;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PutFcn(DB* db, DB_TXN* txnid, ref DBT key, ref DBT data, UInt32 flags);
    public PutFcn Put {
      get { return (PutFcn)Marshal.GetDelegateForFunctionPointer(put, typeof(PutFcn)); }
    }

    IntPtr remove;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RemoveFcn(DB* db, byte* file, byte* database, UInt32 flags);
    public RemoveFcn Remove {
      get { return (RemoveFcn)Marshal.GetDelegateForFunctionPointer(remove, typeof(RemoveFcn)); }
    }

    IntPtr rename;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RenameFcn(DB* db, byte* file, byte* database, byte* newname, UInt32 flags);
    public RenameFcn Rename {
      get { return (RenameFcn)Marshal.GetDelegateForFunctionPointer(rename, typeof(RenameFcn)); }
    }

    IntPtr set_alloc;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetAllocFcn(DB* db, IntPtr app_malloc, IntPtr app_realloc, IntPtr app_free);
    public SetAllocFcn SetAlloc {
      get { return (SetAllocFcn)Marshal.GetDelegateForFunctionPointer(set_alloc, typeof(SetAllocFcn)); }
    }

    IntPtr set_append_recno;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AppendRecnoFcn(DB* db, ref DBT data, UInt32 recno);
    public SetIntPtrFcn SetAppendRecno {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_append_recno, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_bt_compare;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate int BtCompareFcn(DB* db, ref DBT dbt1, ref DBT dbt2);
    public SetIntPtrFcn SetBtCompare {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_bt_compare, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_bt_minkey;
    public SetUIntFcn SetBtMinKey {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_bt_minkey, typeof(SetUIntFcn)); }
    }

    IntPtr set_bt_prefix;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate uint /* should map to size_t, so maybe IntPtr? */
    BtPrefixFcn(DB* db, ref DBT dbt1, ref DBT dbt2);
    public SetIntPtrFcn SetBtPrefix {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_bt_prefix, typeof(SetIntPtrFcn)); }
    }
  
    IntPtr set_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetCacheSizeFcn(DB* db, UInt32 gbytes, UInt32 bytes, int ncache);
    public SetCacheSizeFcn SetCacheSize {
      get { return (SetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(set_cachesize, typeof(SetCacheSizeFcn)); }
    }
  
    IntPtr set_dup_compare;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate int DupCompareFcn(DB* db, ref DBT appData, ref DBT dbData);
    public SetIntPtrFcn SetDupCompare {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_dup_compare, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_encrypt;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetEncryptFcn(DB* db, byte* passwd, UInt32 flags);
    public SetEncryptFcn SetEncrypt {
      get { return (SetEncryptFcn)Marshal.GetDelegateForFunctionPointer(set_encrypt, typeof(SetEncryptFcn)); }
    }

    // likely not used, as it fits better with DB_ENV (and its equivalent)
    IntPtr set_errcall;
    // the argument to SetErrCall must be of type DB_ENV.ErrCallFcn
    public SetIntPtrFcn SetErrCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_errcall, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_errfile;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void SetErrFileFcn(DB* db, FILE* errfile);
    public SetErrFileFcn SetErrFile {
      get { return (SetErrFileFcn)Marshal.GetDelegateForFunctionPointer(set_errfile, typeof(SetErrFileFcn)); }
    }

    // likely not used, as it fits better with DB_ENV (and its equivalent)
    IntPtr set_errpfx;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void SetErrPfxFcn(DB* db, byte* errpfx);
    public SetErrPfxFcn SetErrPfx {
      get { return (SetErrPfxFcn)Marshal.GetDelegateForFunctionPointer(set_errpfx, typeof(SetErrPfxFcn)); }
    }

    IntPtr set_feedback;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void FeedbackFcn(DB* db, int opcode, int percent);
    public SetIntPtrFcn SetFeedback {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_feedback, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_flags;
    public SetUIntFcn SetFlags {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_flags, typeof(SetUIntFcn)); }
    }

    IntPtr set_h_ffactor;
    public SetUIntFcn SetHFFactor {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_h_ffactor, typeof(SetUIntFcn)); }
    }

    IntPtr set_h_hash;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate UInt32 HHashFcn(DB* db, void* bytes, UInt32 length);
    public SetIntPtrFcn SetHHash {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_h_hash, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_h_nelem;
    public SetUIntFcn SetHNelem {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_h_nelem, typeof(SetUIntFcn)); }
    }

    IntPtr set_lorder;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetLOrderFcn(DB* db, int lorder);
    public SetLOrderFcn SetLOrder {
      get { return (SetLOrderFcn)Marshal.GetDelegateForFunctionPointer(set_lorder, typeof(SetLOrderFcn)); }
    }

    // likely not used, as it fits better with DB_ENV (and its equivalent)
    IntPtr set_msgcall;
    public SetIntPtrFcn SetMsgCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_msgcall, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_msgfile;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate void SetMsgFileFcn(DB* db, FILE* msgfile);
    public SetMsgFileFcn SetMsgFile {
      get { return (SetMsgFileFcn)Marshal.GetDelegateForFunctionPointer(set_msgfile, typeof(SetMsgFileFcn)); }
    }

    IntPtr set_pagesize;
    public SetUIntFcn SetPageSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_pagesize, typeof(SetUIntFcn)); }
    }

    IntPtr set_paniccall;
    // the argument to SetPanicCall must be of type DB_ENV.PanicCallFcn
    public SetIntPtrFcn SetPanicCall {
      get { return (SetIntPtrFcn)Marshal.GetDelegateForFunctionPointer(set_paniccall, typeof(SetIntPtrFcn)); }
    }

    IntPtr set_q_extentsize;
    public SetUIntFcn SetQExtentSize {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_q_extentsize, typeof(SetUIntFcn)); }
    }

    IntPtr set_re_delim;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetReDelimFcn(DB* db, int delim);
    public SetReDelimFcn SetReDelim {
      get { return (SetReDelimFcn)Marshal.GetDelegateForFunctionPointer(set_re_delim, typeof(SetReDelimFcn)); }
    }

    IntPtr set_re_len;
    public SetUIntFcn SetReLen {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(set_re_len, typeof(SetUIntFcn)); }
    }

    IntPtr set_re_pad;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetRePadFcn(DB* db, int re_pad);
    public SetRePadFcn SetRePad {
      get { return (SetRePadFcn)Marshal.GetDelegateForFunctionPointer(set_re_pad, typeof(SetRePadFcn)); }
    }

    IntPtr set_re_source;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetReSourceFcn(DB* db, byte* source);
    public SetReSourceFcn SetReSource {
      get { return (SetReSourceFcn)Marshal.GetDelegateForFunctionPointer(set_re_source, typeof(SetReSourceFcn)); }
    }

    IntPtr stat;  // returns different struct types for different db types
    //[UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    //public delegate DbRetVal StatFcn<T>(DB* db, DB_TXN* txnid, out T* sp, UInt32 flags)
    //  where T: struct;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal BtreeStatFcn(DB* db, DB_TXN* txnid, out DB_BTREE_STAT* sp, UInt32 flags);
    public BtreeStatFcn BTreeStat {
      get { return (BtreeStatFcn)Marshal.GetDelegateForFunctionPointer(stat, typeof(BtreeStatFcn)); }
    }
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal HashStatFcn(DB* db, DB_TXN* txnid, out DB_HASH_STAT* sp, UInt32 flags);
    public HashStatFcn HashStat {
      get { return (HashStatFcn)Marshal.GetDelegateForFunctionPointer(stat, typeof(HashStatFcn)); }
    }
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal QueueStatFcn(DB* db, DB_TXN* txnid, out DB_QUEUE_STAT* sp, UInt32 flags);
    public QueueStatFcn QueueStat {
      get { return (QueueStatFcn)Marshal.GetDelegateForFunctionPointer(stat, typeof(QueueStatFcn)); }
    }

    IntPtr stat_print;  // seems to hang when called while a transaction is open
    public SetUIntFcn StatPrint {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(stat_print, typeof(SetUIntFcn)); }
    }

    IntPtr sync;
    // flags currently unused, must be 0
    public SetUIntFcn Sync {
      get { return (SetUIntFcn)Marshal.GetDelegateForFunctionPointer(sync, typeof(SetUIntFcn)); }
    }

    IntPtr truncate;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal TruncateFcn(DB* db, DB_TXN* txnid, out UInt32 count, UInt32 flags);
    public TruncateFcn Truncate {
      get { return (TruncateFcn)Marshal.GetDelegateForFunctionPointer(truncate, typeof(TruncateFcn)); }
    }

    IntPtr upgrade;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal UpgradeFcn(DB* db, byte* file, UInt32 flags);
    public UpgradeFcn Upgrade {
      get { return (UpgradeFcn)Marshal.GetDelegateForFunctionPointer(upgrade, typeof(UpgradeFcn)); }
    }
    
    IntPtr verify;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal VerifyFcn(DB* db, byte* file, byte* database, FILE* outfile, UInt32 flags);
    public VerifyFcn Verify {
      get { return (VerifyFcn)Marshal.GetDelegateForFunctionPointer(verify, typeof(VerifyFcn)); }
    }

    #endregion

    /* Private API */
    IntPtr dump;
    IntPtr db_am_remove;
    IntPtr db_am_rename;

    /* Never called - saving function pointers to undo an associate. */
    IntPtr stored_get;
    IntPtr stored_close;

    UInt32 am_ok;           /* Legal AM choices. */
    int preserve_fid;       /* Do not free fileid on close. */
    UInt32 orig_flags;      /* Flags at  open, for refresh. */
    UInt32 flags;

    // It seems there is no other way to detect if a database is associated.
    public bool IsSecondary {
      get { return (flags & DbConst.DB_AM_SECONDARY) != 0; }
    }
  }

#endif

  /* Key range statistics structure */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_KEY_RANGE
  {
	  public double less;
	  public double equal;
	  public double greater;
  }

#if BDB_4_5_20

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_COMPACT
  {
    /* Input Parameters. */
    public UInt32 compact_fillpercent;     /* Desired fillfactor: 1-100 */
    public UInt32 compact_timeout;         /* Lock timeout. (typedef u_int32_t db_timeout_t;) */
    public UInt32 compact_pages;           /* Max pages to process. */
    /* Output Stats. */
    public UInt32 compact_pages_free;      /* Number of pages freed. */
    public UInt32 compact_pages_examine;   /* Number of pages examine. */
    public UInt32 compact_levels;          /* Number of levels removed. */
    public UInt32 compact_deadlock;        /* Number of deadlocks. */
    public UInt32 compact_pages_truncated; /* Pages truncated to OS. (typedef u_int32_t db_pgno_t;) */
    /* Internal. */
    private UInt32 compact_truncate;       /* Page number for truncation. (typedef u_int32_t db_pgno_t;) */
  }

#endif

  /* Btree/Recno statistics structure. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_BTREE_STAT
  {
    public UInt32 bt_magic;         /* Magic number. */
    public UInt32 bt_version;       /* Version number. */
    public UInt32 bt_metaflags;     /* Metadata flags. */
    public UInt32 bt_nkeys;         /* Number of unique keys. */
    public UInt32 bt_ndata;         /* Number of data items. */
    public UInt32 bt_pagesize;      /* Page size. */
#if BDB_4_3_29
    public UInt32 bt_maxkey;        /* Maxkey value. */
#endif
    public UInt32 bt_minkey;        /* Minkey value. */
    public UInt32 bt_re_len;        /* Fixed-length record length. */
    public UInt32 bt_re_pad;        /* Fixed-length record pad. */
    public UInt32 bt_levels;        /* Tree levels. */
    public UInt32 bt_int_pg;        /* Internal pages. */
    public UInt32 bt_leaf_pg;       /* Leaf pages. */
    public UInt32 bt_dup_pg;        /* Duplicate pages. */
    public UInt32 bt_over_pg;       /* Overflow pages. */
    public UInt32 bt_empty_pg;      /* Empty pages. */
    public UInt32 bt_free;          /* Pages on the free list. */
    public UInt32 bt_int_pgfree;    /* Bytes free in internal pages. */
    public UInt32 bt_leaf_pgfree;   /* Bytes free in leaf pages. */
    public UInt32 bt_dup_pgfree;    /* Bytes free in duplicate pages. */
    public UInt32 bt_over_pgfree;   /* Bytes free in overflow pages. */
  }

  /* Hash statistics structure. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_HASH_STAT
  {
    public UInt32 hash_magic;      /* Magic number. */
    public UInt32 hash_version;    /* Version number. */
    public UInt32 hash_metaflags;  /* Metadata flags. */
    public UInt32 hash_nkeys;      /* Number of unique keys. */
    public UInt32 hash_ndata;      /* Number of data items. */
    public UInt32 hash_pagesize;   /* Page size. */
    public UInt32 hash_ffactor;    /* Fill factor specified at create. */
    public UInt32 hash_buckets;    /* Number of hash buckets. */
    public UInt32 hash_free;       /* Pages on the free list. */
    public UInt32 hash_bfree;      /* Bytes free on bucket pages. */
    public UInt32 hash_bigpages;   /* Number of big key/data pages. */
    public UInt32 hash_big_bfree;  /* Bytes free on big item pages. */
    public UInt32 hash_overflows;  /* Number of overflow pages. */
    public UInt32 hash_ovfl_free;  /* Bytes free on ovfl pages. */
    public UInt32 hash_dup;        /* Number of dup pages. */
    public UInt32 hash_dup_free;   /* Bytes free on duplicate pages. */
  }

  /* Queue statistics structure. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_QUEUE_STAT
  {
    public UInt32 qs_magic;        /* Magic number. */
    public UInt32 qs_version;      /* Version number. */
    public UInt32 qs_metaflags;    /* Metadata flags. */
    public UInt32 qs_nkeys;        /* Number of unique keys. */
    public UInt32 qs_ndata;        /* Number of data items. */
    public UInt32 qs_pagesize;     /* Page size. */
    public UInt32 qs_extentsize;   /* Pages per extent. */
    public UInt32 qs_pages;        /* Data pages. */
    public UInt32 qs_re_len;       /* Fixed-length record length. */
    public UInt32 qs_re_pad;       /* Fixed-length record pad. */
    public UInt32 qs_pgfree;       /* Bytes free in data pages. */
    public UInt32 qs_first_recno;  /* First not deleted record. */
    public UInt32 qs_cur_recno;    /* Next available record number. */
  }
}
