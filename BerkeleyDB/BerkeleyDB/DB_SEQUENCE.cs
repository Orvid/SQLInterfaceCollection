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
  public enum SeqFlags: int
  {
    Dec = DbConst.DB_SEQ_DEC,
    Inc = DbConst.DB_SEQ_INC,
    Wrap = DbConst.DB_SEQ_WRAP
  }

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DB_SEQUENCE
  {
    #region Private Fields (to calculate offset to API function pointers)

    // made public because of bug in get_db implementation
    public readonly DB* seq_dbp;               /* DB handle for this sequence. */
#if BDB_4_3_29
    DB_MUTEX* seq_mutexp;      /* Mutex if sequence is threaded. */
#endif
#if BDB_4_5_20
    UInt32 mtx_seq;             /* Mutex if sequence is threaded. (typedef u_int32_t	db_mutex_t;) */
#endif
    DB_SEQ_RECORD* seq_rp;     /* Pointer to current data. */
    DB_SEQ_RECORD seq_record;  /* Data from DB_SEQUENCE. */
    Int32 seq_cache_size;      /* Number of values cached. */
    Int64 seq_last_value;      /* Last value cached. (typedef int64_t db_seq_t;) */
    DBT seq_key;               /* DBT pointing to sequence key. */
    DBT seq_data;              /* DBT pointing to seq_record. */

    /* API-private structure: used by C++ and Java  - and dotNET. */
    public IntPtr api_internal;   // void* api_internal;

    #endregion

    #region API Methods

    IntPtr close;
    // flags unused, must be 0
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CloseFcn(DB_SEQUENCE* seq, UInt32 flags);
    public CloseFcn Close {
      get { return (CloseFcn)Marshal.GetDelegateForFunctionPointer(close, typeof(CloseFcn)); }
    }

    IntPtr get;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal 
    GetFcn(DB_SEQUENCE* seq, DB_TXN* txnid, Int32 delta, out Int64 ret, UInt32 flags);
    public GetFcn Get {
      get { return (GetFcn)Marshal.GetDelegateForFunctionPointer(get, typeof(GetFcn)); }
    }

    IntPtr get_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetCacheSizeFcn(DB_SEQUENCE* seq, out Int32 size);
    public GetCacheSizeFcn GetCacheSize {
      get { return (GetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(get_cachesize, typeof(GetCacheSizeFcn)); }
    }

    IntPtr get_db;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetDbFcn(DB_SEQUENCE* seq, out DB* db);
    public GetDbFcn GetDb {
      get { return (GetDbFcn)Marshal.GetDelegateForFunctionPointer(get_db, typeof(GetDbFcn)); }
    }

    IntPtr get_flags;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFlagsFcn(DB_SEQUENCE* seq, out SeqFlags flags);
    public GetFlagsFcn GetFlags {
      get { return (GetFlagsFcn)Marshal.GetDelegateForFunctionPointer(get_flags, typeof(GetFlagsFcn)); }
    }

    IntPtr get_key;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetKeyFcn(DB_SEQUENCE* seq, out DBT key);
    public GetKeyFcn GetKey {
      get { return (GetKeyFcn)Marshal.GetDelegateForFunctionPointer(get_key, typeof(GetKeyFcn)); }
    }

    IntPtr get_range;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetRangeFcn(DB_SEQUENCE* seq, out Int64 min, out Int64 max);
    public GetRangeFcn GetRange {
      get { return (GetRangeFcn)Marshal.GetDelegateForFunctionPointer(get_range, typeof(GetRangeFcn)); }
    }

    IntPtr initial_value;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal InitialValueFcn(DB_SEQUENCE* seq, Int64 value);
    public InitialValueFcn InitialValue {
      get { return (InitialValueFcn)Marshal.GetDelegateForFunctionPointer(initial_value, typeof(InitialValueFcn)); }
    }

    IntPtr open;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal OpenFcn(DB_SEQUENCE* seq, DB_TXN* txnid, ref DBT key, UInt32 flags);
    public OpenFcn Open {
      get { return (OpenFcn)Marshal.GetDelegateForFunctionPointer(open, typeof(OpenFcn)); }
    }

    IntPtr remove;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal RemoveFcn(DB_SEQUENCE* seq, DB_TXN* txnid, UInt32 flags);
    public RemoveFcn Remove {
      get { return (RemoveFcn)Marshal.GetDelegateForFunctionPointer(remove, typeof(RemoveFcn)); }
    }

    IntPtr set_cachesize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetCacheSizeFcn(DB_SEQUENCE* seq, Int32 size);
    public SetCacheSizeFcn SetCacheSize {
      get { return (SetCacheSizeFcn)Marshal.GetDelegateForFunctionPointer(set_cachesize, typeof(SetCacheSizeFcn)); }
    }

    IntPtr set_flags;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetFlagsFcn(DB_SEQUENCE* seq, SeqFlags flags);
    public SetFlagsFcn SetFlags {
      get { return (SetFlagsFcn)Marshal.GetDelegateForFunctionPointer(set_flags, typeof(SetFlagsFcn)); }
    }

    IntPtr set_range;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetRangeFcn(DB_SEQUENCE* seq, Int64 min, Int64 max);
    public SetRangeFcn SetRange {
      get { return (SetRangeFcn)Marshal.GetDelegateForFunctionPointer(set_range, typeof(SetRangeFcn)); }
    }

    IntPtr stat;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal StatFcn(DB_SEQUENCE* seq, out DB_SEQUENCE_STAT* sp, UInt32 flags);
    public StatFcn Stat {
      get { return (StatFcn)Marshal.GetDelegateForFunctionPointer(stat, typeof(StatFcn)); }
    }

    IntPtr stat_print;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal StatPrintFcn(DB_SEQUENCE* seq, UInt32 flags);
    public StatPrintFcn StatPrint {
      get { return (StatPrintFcn)Marshal.GetDelegateForFunctionPointer(stat_print, typeof(StatPrintFcn)); }
    }

    #endregion
  }

  /*
  * The storage record for a sequence.
  */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  struct DB_SEQ_RECORD
  {
    UInt32 seq_version;       /* Version size/number. */
    UInt32 flags;             /* Flags. (SeqFlags) */
    Int64 seq_value;          /* Current value. (typedef int64_t db_seq_t;) */
    Int64 seq_max;            /* Max permitted. */
    Int64 seq_min;            /* Min permitted. */
  }

  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_SEQUENCE_STAT
  {
    public UInt32 st_wait;       /* Sequence lock granted without wait. */
    public UInt32 st_nowait;     /* Sequence lock granted after wait. */
    public Int64 st_current;     /* Current value in db. (typedef int64_t db_seq_t;) */
    public Int64 st_value;       /* Current cached value. */
    public Int64 st_last_value;  /* Last cached value. */
    public Int64 st_min;         /* Minimum value. */
    public Int64 st_max;         /* Maximum value. */
    public Int32 st_cache_size;  /* Cache size. */
    public UInt32 st_flags;      /* Flag value. */
  }
}
