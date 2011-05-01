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
  public unsafe struct DB_TXN
  {
    #region Private Fields (to calculate offset to API function pointers)

    DB_TXNMGR* mgrp;        /* Pointer to transaction manager. */
    DB_TXN* parent;         /* Pointer to transaction's parent. */
    DB_LSN last_lsn;        /* Lsn of last log write. */
    UInt32 txnid;           /* Unique transaction id. */
    UInt32 tid;             /* Thread id for use in MT XA. */
    IntPtr off;             /* Detail structure within region. (typedef uintptr_t roff_t;) */
    UInt32 lock_timeout;    /* Timeout for locks for this txn. (typedef u_int32_t db_timeout_t;) */
    UInt32 expire;          /* Time this txn expires. */
    void* txn_list;         /* Undo information for parent. */

    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_ENTRY(__db_txn) links;
    * TAILQ_ENTRY(__db_txn) xalinks;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct LINKS
    {
      DB_TXN* tqe_next;
      DB_TXN** tqe_prev;
    } 
    LINKS links;            /* Links transactions off manager. */

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct XA_LINKS
    {
      DB_TXN* tqe_next;
      DB_TXN** tqe_prev;
    } 
    XA_LINKS xalinks;       /* Links active XA transactions. */

    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_HEAD(__events, __txn_event) events;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct EVENTS
    {
      TXN_EVENT* tqh_first;
      TXN_EVENT** tqh_last;
    } 
    EVENTS events;

    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * STAILQ_HEAD(__logrec, __txn_logrec) logs;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct LOGS
    {
      TXN_LOGREC* stqh_first;
      TXN_LOGREC** stqh_last;
    } 
    LOGS logs;              /* Links deferred events. */

    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_HEAD(__kids, __db_txn) kids;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct KIDS
    {
      DB_TXN* tqh_first;
      DB_TXN** tqh_last;
    } 
    KIDS kids;

    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_ENTRY(__db_txn) klinks;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct KLINKS
    {
      DB_TXN* tqe_next;
      DB_TXN** tqe_prev;
    } 
    KLINKS klinks;

    /* C++ and dotNET API private. */
    public IntPtr api_internal;   // void* api_internal;

    void* xml_internal;     /* XML API private. */

    UInt32  cursors;  /* Number of cursors open for txn */

    #endregion

    #region API Methods

    IntPtr abort;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AbortFcn(DB_TXN* tid);
    public AbortFcn Abort {
      get { return (AbortFcn)Marshal.GetDelegateForFunctionPointer(abort, typeof(AbortFcn)); }
    }

    IntPtr commit;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CommitFcn(DB_TXN* tid, UInt32 flags);
    public CommitFcn Commit {
      get { return (CommitFcn)Marshal.GetDelegateForFunctionPointer(commit, typeof(CommitFcn)); }
    }

    // flags must be 0
    IntPtr discard;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DiscardFcn(DB_TXN* tid, UInt32 flags);
    public DiscardFcn Discard {
      get { return (DiscardFcn)Marshal.GetDelegateForFunctionPointer(discard, typeof(DiscardFcn)); }
    }

    IntPtr id;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate UInt32 IdFcn(DB_TXN* tid);
    public IdFcn Id {
      get { return (IdFcn)Marshal.GetDelegateForFunctionPointer(id, typeof(IdFcn)); }
    }

    // gid must be a byte array of fixed size Const.DB_XIDDATASIZE
    IntPtr prepare;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PrepareFcn(DB_TXN* tid, byte* gid);
    public PrepareFcn Prepare {
      get { return (PrepareFcn)Marshal.GetDelegateForFunctionPointer(prepare, typeof(PrepareFcn)); }
    }

    IntPtr set_begin_lsnp;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetBeginLsnpFcn(DB_TXN* tid, out DB_LSN* lsn);
    public SetBeginLsnpFcn SetBeginLsnp {
      get { return (SetBeginLsnpFcn)Marshal.GetDelegateForFunctionPointer(set_begin_lsnp, typeof(SetBeginLsnpFcn)); }
    }

    IntPtr set_timeout;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetTimeoutFcn(DB_TXN* tid, UInt32 timeout, UInt32 flags);
    public SetTimeoutFcn SetTimeout {
      get { return (SetTimeoutFcn)Marshal.GetDelegateForFunctionPointer(set_timeout, typeof(SetTimeoutFcn)); }
    }

    #endregion

    public const UInt32 TXN_CHILDCOMMIT = 0x001;   /* Transaction that has committed. */
    public const UInt32 TXN_COMPENSATE = 0x002;    /* Compensating transaction. */
    public const UInt32 TXN_DEADLOCK = 0x004;      /* Transaction has deadlocked. */
    public const UInt32 TXN_DEGREE_2 = 0x008;      /* Has degree 2 isolation. */
    public const UInt32 TXN_DIRTY_READ = 0x010;    /* Transaction does dirty reads. */
    public const UInt32 TXN_LOCKTIMEOUT = 0x020;   /* Transaction has a lock timeout. */
    public const UInt32 TXN_MALLOC = 0x040;        /* Structure allocated by TXN system. */
    public const UInt32 TXN_NOSYNC = 0x080;        /* Do not sync on prepare and commit. */
    public const UInt32 TXN_NOWAIT = 0x100;        /* Do not wait on locks. */
    public const UInt32 TXN_RESTORED = 0x200;      /* Transaction has been restored. */
    public const UInt32 TXN_SYNC = 0x400;          /* Sync on prepare and commit. */
    UInt32 flags;
  }
#endif

#if BDB_4_5_20
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DB_TXN
  {
  #region Private Fields (to calculate offset to API function pointers)

    DB_TXNMGR* mgrp;        /* Pointer to transaction manager. */
    DB_TXN* parent;         /* Pointer to transaction's parent. */
    UInt32 txnid;           /* Unique transaction id. */
    byte* name;             /* Transaction name */
    UInt32 tid;             /* Thread id for use in MT XA. (typedef u_int32_t db_threadid_t;) */
    void* td;               /* Detail structure within region. */
    UInt32 lock_timeout;    /* Timeout for locks for this txn. (typedef u_int32_t db_timeout_t;) */
    UInt32 expire;          /* Time this txn expires. */
    void* txn_list;         /* Undo information for parent. */

    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_ENTRY(__db_txn) links;
    * TAILQ_ENTRY(__db_txn) xalinks;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct LINKS
    {
      DB_TXN* tqe_next;
      DB_TXN** tqe_prev;
    } 
    LINKS links;            /* Links transactions off manager. */

    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct XA_LINKS
    {
      DB_TXN* tqe_next;
      DB_TXN** tqe_prev;
    } 
    XA_LINKS xalinks;       /* Links active XA transactions. */

    /*
	  * !!!
	  * Explicit representations of structures from queue.h.
	  * TAILQ_HEAD(__kids, __db_txn) kids;
	  */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct KIDS
    {
      DB_TXN* tqh_first;
      DB_TXN** tqh_last;
    }
    KIDS kids;
    
    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_HEAD(__events, __txn_event) events;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct EVENTS
    {
      TXN_EVENT* tqh_first;
      TXN_EVENT** tqh_last;
    } 
    EVENTS events;

    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * STAILQ_HEAD(__logrec, __txn_logrec) logs;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct LOGS
    {
      TXN_LOGREC* stqh_first;
      TXN_LOGREC** stqh_last;
    } 
    LOGS logs;              /* Links deferred events. */

    /*
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_ENTRY(__db_txn) klinks;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct KLINKS
    {
      DB_TXN* tqe_next;
      DB_TXN** tqe_prev;
    } 
    KLINKS klinks;

    /* C++ and dotNET API private. */
    public IntPtr api_internal;   // void* api_internal;

    void* xml_internal;     /* XML API private. */

    UInt32  cursors;  /* Number of cursors open for txn */

    #endregion

  #region API Methods

    IntPtr abort;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal AbortFcn(DB_TXN* tid);
    public AbortFcn Abort {
      get { return (AbortFcn)Marshal.GetDelegateForFunctionPointer(abort, typeof(AbortFcn)); }
    }

    IntPtr commit;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CommitFcn(DB_TXN* tid, UInt32 flags);
    public CommitFcn Commit {
      get { return (CommitFcn)Marshal.GetDelegateForFunctionPointer(commit, typeof(CommitFcn)); }
    }

    // flags must be 0
    IntPtr discard;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DiscardFcn(DB_TXN* tid, UInt32 flags);
    public DiscardFcn Discard {
      get { return (DiscardFcn)Marshal.GetDelegateForFunctionPointer(discard, typeof(DiscardFcn)); }
    }

    IntPtr get_name;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetNameFcn(DB_TXN* tid, out byte* name);
    public GetNameFcn GetName {
      get { return (GetNameFcn)Marshal.GetDelegateForFunctionPointer(get_name, typeof(GetNameFcn)); }
    }

    IntPtr id;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate UInt32 IdFcn(DB_TXN* tid);
    public IdFcn Id {
      get { return (IdFcn)Marshal.GetDelegateForFunctionPointer(id, typeof(IdFcn)); }
    }

    // gid must be a byte array of fixed size Const.DB_XIDDATASIZE
    IntPtr prepare;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PrepareFcn(DB_TXN* tid, byte* gid);
    public PrepareFcn Prepare {
      get { return (PrepareFcn)Marshal.GetDelegateForFunctionPointer(prepare, typeof(PrepareFcn)); }
    }

    IntPtr set_name;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetNameFcn(DB_TXN* tid, byte* name);
    public SetNameFcn SetName {
      get { return (SetNameFcn)Marshal.GetDelegateForFunctionPointer(set_name, typeof(SetNameFcn)); }
    }

    IntPtr set_timeout;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetTimeoutFcn(DB_TXN* tid, UInt32 timeout, UInt32 flags);
    public SetTimeoutFcn SetTimeout {
      get { return (SetTimeoutFcn)Marshal.GetDelegateForFunctionPointer(set_timeout, typeof(SetTimeoutFcn)); }
    }

    #endregion

    IntPtr set_txn_lsnp;

    public const UInt32 TXN_CHILDCOMMIT = 0x0001;       /* Txn that has committed. */
    public const UInt32 TXN_CDSGROUP = 0x0002;          /* CDS group handle. */
    public const UInt32 TXN_COMPENSATE = 0x0004;        /* Compensating transaction. */
    public const UInt32 TXN_DEADLOCK = 0x0008;          /* Txn has deadlocked. */
    public const UInt32 TXN_LOCKTIMEOUT = 0x0010;       /* Txn has a lock timeout. */
    public const UInt32 TXN_MALLOC = 0x0020;            /* Structure allocated by TXN system. */
    public const UInt32 TXN_NOSYNC = 0x0040;            /* Do not sync on prepare and commit. */
    public const UInt32 TXN_NOWAIT = 0x0080;            /* Do not wait on locks. */
    public const UInt32 TXN_PRIVATE = 0x0100;           /* Txn owned by cursor. */
    public const UInt32 TXN_READ_COMMITTED = 0x0200;    /* Txn has degree 2 isolation. */
    public const UInt32 TXN_READ_UNCOMMITTED = 0x0400;  /* Txn has degree 1 isolation. */
    public const UInt32 TXN_RESTORED = 0x0800;          /* Txn has been restored. */
    public const UInt32 TXN_SNAPSHOT = 0x1000;          /* Snapshot isolation. */
    public const UInt32 TXN_SYNC = 0x2000;              /* Write and sync on prepare/commit. */
    public const UInt32 TXN_WRITE_NOSYNC = 0x4000;      /* Write only on prepare/commit. */
    UInt32 flags;
  }
#endif

  struct DB_TXNMGR
  {
    // translate if necessary (used in API call)
  }

  struct TXN_EVENT
  {
    // translate if necessary (used in API call)
  }

  struct TXN_LOGREC
  {
    // translate if necessary (used in API call)
  }

  /* Transaction statistics structures. */

#if BDB_4_3_29

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  unsafe struct DB_TXN_ACTIVE
  {
    public UInt32 txnid;             /* Transaction ID */
    public UInt32 parentid;          /* Transaction ID of parent */
    public DB_LSN lsn;               /* LSN when transaction began */
    public UInt32 xa_status;         /* XA status */
    public fixed byte xid[DbConst.DB_XIDDATASIZE]; /* XA global transaction ID */
  }

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  unsafe struct DB_TXN_STAT
  {
    public DB_LSN st_last_ckp;           /* lsn of the last checkpoint */
#if _USE_32BIT_TIME_T
    public int st_time_ckp;              /* time of last checkpoint */
#else
    public long st_time_ckp;             /* time of last checkpoint */
#endif
    public UInt32 st_last_txnid;         /* last transaction id given out */
    public UInt32 st_maxtxns;            /* maximum txns possible */
    public UInt32 st_naborts;            /* number of aborted transactions */
    public UInt32 st_nbegins;            /* number of begun transactions */
    public UInt32 st_ncommits;           /* number of committed transactions */
    public UInt32 st_nactive;            /* number of active transactions */
    public UInt32 st_nrestores;          /* number of restored transactions after recovery. */
    public UInt32 st_maxnactive;         /* maximum active transactions */
    public DB_TXN_ACTIVE* st_txnarray;   /* array of active transactions */
    public UInt32 st_region_wait;        /* Region lock granted after wait. */
    public UInt32 st_region_nowait;      /* Region lock granted without wait. */
    // must be integer of same size as pointer type - therefore use IntPtr
    public IntPtr st_regsize;            /* Region size. */
  }

#endif

#if BDB_4_5_20

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public unsafe struct DB_TXN_ACTIVE
  {
    public UInt32 txnid;              /* Transaction ID */
    public UInt32 parentid;           /* Transaction ID of parent */
    public int pid;                   /* Process owning txn ID (typedef int pid_t;) */
    public UInt32 tid;		            /* Thread owning txn ID (typedef u_int32_t db_threadid_t;) */
    public DB_LSN lsn;                /* LSN when transaction began */
    public DB_LSN read_lsn;           /* Read LSN for MVCC */
    public UInt32 mvcc_ref;           /* MVCC reference count */
    public UInt32 status;             /* Status of the transaction */
    public UInt32 xa_status;          /* XA status */
    public fixed byte xid[DbConst.DB_XIDDATASIZE];  /* XA global transaction ID */
    public fixed byte name[51];       /* 50 bytes of name, nul termination */
  }

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public unsafe struct DB_TXN_STAT
  {
    public DB_LSN st_last_ckp;            /* lsn of the last checkpoint */
#if _USE_32BIT_TIME_T
    public int st_time_ckp;               /* time of last checkpoint */
#else
    public long st_time_ckp;              /* time of last checkpoint */
#endif
    public UInt32 st_last_txnid;          /* last transaction id given out */
    public UInt32 st_maxtxns;             /* maximum txns possible */
    public UInt32 st_naborts;             /* number of aborted transactions */
    public UInt32 st_nbegins;             /* number of begun transactions */
    public UInt32 st_ncommits;            /* number of committed transactions */
    public UInt32 st_nactive;             /* number of active transactions */
    public UInt32 st_nsnapshot;           /* number of snapshot transactions */
    public UInt32 st_nrestores;           /* number of restored transactions after recovery. */
    public UInt32 st_maxnactive;          /* maximum active transactions */
    public UInt32 st_maxnsnapshot;        /* maximum snapshot transactions */
    public DB_TXN_ACTIVE* st_txnarray;    /* array of active transactions */
    public UInt32 st_region_wait;         /* Region lock granted after wait. */
    public UInt32 st_region_nowait;       /* Region lock granted without wait. */
    // must be integer of same size as pointer type - therefore use IntPtr
    public IntPtr st_regsize;            /* Region size. (typedef uintptr_t roff_t;) */
  }

#endif

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public unsafe struct DB_PREPLIST
  {
    public TxnUnion txn;
    public fixed byte gid[DbConst.DB_XIDDATASIZE];
  }

  // stores either DB_TXN* or IntPtr (GCHandle), as they have the same size
  [StructLayout(LayoutKind.Explicit)]
  public unsafe struct TxnUnion
  {
    [FieldOffset(0)]
    public DB_TXN* txp;

    [FieldOffset(0)]
    public IntPtr txnHandle;
  }
}
