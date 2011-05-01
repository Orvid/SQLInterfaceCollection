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
  /*******************************************************
  * Access method cursors.
  *******************************************************/
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DBC
  {
    #region Private Fields (to calculate offset to API function pointers)

    public readonly DB* dbp;          /* Related DB access method. */
    public readonly DB_TXN* txn;      /* Associated transaction. */

    /*
    * Active/free cursor queues.
    *
    * !!!
    * Explicit representations of structures from queue.h.
    * TAILQ_ENTRY(__dbc) links;
    */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    public struct LINKS
    {
      DBC* tqe_next;
      DBC** tqe_prev;
    }
    LINKS links;

    /*
    * The DBT *'s below are used by the cursor routines to return
    * data to the user when DBT flags indicate that DB should manage
    * the returned memory.  They point at a DBT containing the buffer
    * and length that will be used, and "belonging" to the handle that
    * should "own" this memory.  This may be a "my_*" field of this
    * cursor--the default--or it may be the corresponding field of
    * another cursor, a DB handle, a join cursor, etc.  In general, it
    * will be whatever handle the user originally used for the current
    * DB interface call.
    */
    DBT* rskey;           /* Returned secondary key. */
    DBT* rkey;            /* Returned [primary] key. */
    DBT* rdata;           /* Returned data. */

    DBT my_rskey;         /* Space for returned secondary key. */
    DBT my_rkey;          /* Space for returned [primary] key. */
    DBT my_rdata;         /* Space for returned data. */

#if BDB_4_3_29
    UInt32 lid;           /* Default process' locker id. */
#endif
#if BDB_4_5_20
    void* lref;           /* Reference to default locker. */
#endif
    UInt32 locker;        /* Locker for this operation. */
    DBT lock_dbt;         /* DBT referencing lock. */
    DB_LOCK_ILOCK @lock;   /* Object to be locked. */
    DB_LOCK mylock;       /* CDB lock held on this cursor. */

    uint cl_id;           /* Remote client id. */

    DbType dbtype;        /* Cursor type. */

    struct DBC_INTERNAL
    {
      // dummy declaration, to have a type for the pointer
    }  
    DBC_INTERNAL* @internal;    /* Access method private. */

    #endregion

    #region API Methods

    IntPtr c_close;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CloseFcn(DBC* DBcursor);
    public CloseFcn Close {
      get { return (CloseFcn)Marshal.GetDelegateForFunctionPointer(c_close, typeof(CloseFcn)); }
    }

    IntPtr c_count;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CountFcn(DBC* DBcursor, out UInt32 count, UInt32 flags);
    public CountFcn Count {
      get { return (CountFcn)Marshal.GetDelegateForFunctionPointer(c_count, typeof(CountFcn)); }
    }

    IntPtr c_del;
    // flags currently unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DelFcn(DBC* DBcursor, UInt32 flags);
    public DelFcn Del {
      get { return (DelFcn)Marshal.GetDelegateForFunctionPointer(c_del, typeof(DelFcn)); }
    }

    IntPtr c_dup;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal DupFcn(DBC* DBcursor, out DBC* cursor, UInt32 flags);
    public DupFcn Dup {
      get { return (DupFcn)Marshal.GetDelegateForFunctionPointer(c_dup, typeof(DupFcn)); }
    }
  
    IntPtr c_get;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFcn(DBC* DBcursor, ref DBT key, ref DBT data, UInt32 flags);
    public GetFcn Get {
      get { return (GetFcn)Marshal.GetDelegateForFunctionPointer(c_get, typeof(GetFcn)); }
    }

    IntPtr c_pget;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PGetFcn(DBC* DBcursor, ref DBT key, ref DBT pkey, ref DBT data, UInt32 flags);
    public PGetFcn PGet {
      get { return (PGetFcn)Marshal.GetDelegateForFunctionPointer(c_pget, typeof(PGetFcn)); }
    }
    
    IntPtr c_put;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PutFcn(DBC* DBcursor, ref DBT key, ref DBT data, UInt32 flags);
    public PutFcn Put {
      get { return (PutFcn)Marshal.GetDelegateForFunctionPointer(c_put, typeof(PutFcn)); }
    }

    /* Methods: private. */
    IntPtr c_am_bulk;
    IntPtr c_am_close;
    IntPtr c_am_del;
    IntPtr c_am_destroy;
    IntPtr c_am_get;
    IntPtr c_am_put;
    IntPtr c_am_writelock;

    #endregion

    UInt32 flags;
  }

  /*
  * DB_LOCK_ILOCK --
  *	Internal DB access method lock.
  */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  unsafe struct DB_LOCK_ILOCK
  {
    UInt32 pgno;			/* Page being locked. (typedef  u_int32_t  db_pgno_t;) */
    fixed byte fileid[DbConst.DB_FILE_ID_LEN];  /* File id. */

    public const UInt32 DB_HANDLE_LOCK = 1;
    public const UInt32 DB_RECORD_LOCK = 2;
    public const UInt32 DB_PAGE_LOCK = 3;
    UInt32 type;			                      /* Type of lock. */
  }
}
