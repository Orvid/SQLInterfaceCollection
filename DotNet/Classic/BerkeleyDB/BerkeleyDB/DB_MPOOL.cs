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
  public enum CacheFileFlags: int
  {
    NoFile = DbConst.DB_MPOOL_NOFILE,
    Unlink = DbConst.DB_MPOOL_UNLINK
  }

  [Flags]
  public enum CacheFileOpenFlags: int
  {
    None = 0,
    Create = DbConst.DB_CREATE,
    Direct = DbConst.DB_DIRECT,
#if BDB_4_5_20
    MultiVersion = DbConst.DB_MULTIVERSION,
#endif
    NoMMap = DbConst.DB_NOMMAP,
    OddFileSize = DbConst.DB_ODDFILESIZE,
    ReadOnly = DbConst.DB_RDONLY
  }

  [Flags]
  public enum CachePageGetFlags: int
  {
    None = 0,
    Create = DbConst.DB_MPOOL_CREATE,
#if BDB_4_5_20
    Dirty = DbConst.DB_MPOOL_DIRTY,
    Edit = DbConst.DB_MPOOL_EDIT,
#endif
    Last = DbConst.DB_MPOOL_LAST,
    New = DbConst.DB_MPOOL_NEW
  }

  [Flags]
  public enum CachePagePutFlags: int
  {
    None = 0,
#if BDB_4_3_29
    Clean = DbConst.DB_MPOOL_CLEAN,
    Dirty = DbConst.DB_MPOOL_DIRTY,
#endif
    Discard = DbConst.DB_MPOOL_DISCARD,
  }

  /* Priority values for DB_MPOOLFILE->set_priority. */
  public enum CacheFilePriority: int
  {
    VeryLow = 1,    /* DB_CACHE_PRIORITY.VERY_LOW */
    Low = 2,        /* DB_CACHE_PRIORITY.LOW */
    Default = 3,    /* DB_CACHE_PRIORITY.DEFAULT */
    High = 4,       /* DB_CACHE_PRIORITY.HIGH */
    VeryHigh = 5    /* DB_CACHE_PRIORITY.VERY_HIGH */
  }

  /* Per-process DB_MPOOLFILE information. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DB_MPOOLFILE
  {
    #region Private Fields (to calculate offset to API function pointers)

    DB_FH* fhp;           /* Underlying file handle. */

    /*
     * !!!
     * The ref, pinref and q fields are protected by the region lock.
     */
    UInt32 _ref;          /* Reference count. */
    UInt32 pinref;        /* Pinned block reference count. */
    /*
     * !!!
     * Explicit representations of structures from queue.h.
     * TAILQ_ENTRY(__db_mpoolfile) q;
     */
    [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
    struct Q {
      DB_MPOOLFILE* tqe_next;
      DB_MPOOLFILE** tqe_prev;
    } 
    Q q;                  /* Linked list of DB_MPOOLFILE's. */

    /*
     * !!!
     * The rest of the fields (with the exception of the MP_FLUSH flag)
     * are not thread-protected, even when they may be modified at any
     * time by the application.  The reason is the DB_MPOOLFILE handle
     * is single-threaded from the viewpoint of the application, and so
     * the only fields needing to be thread-protected are those accessed
     * by checkpoint or sync threads when using DB_MPOOLFILE structures
     * to flush buffers from the cache.
     */
    public readonly DB_ENV* dbenv;    /* Overlying DB_ENV. */
    MPOOLFILE* mfp;                   /* Underlying MPOOLFILE. */

    UInt32 clear_len;                 /* Cleared length on created pages. */
    fixed byte fileid[DbConst.DB_FILE_ID_LEN];    /* Unique file ID. */
        
    int ftype;                        /* File type. */
    Int32 lsn_offset;                 /* LSN offset in page. */
    UInt32 gbytes, bytes;             /* Maximum file size. */
    DBT* pgcookie;                    /* Byte-string passed to pgin/pgout. */
#if BDB_4_3_29
    CacheFilePriority priority;       /* Cache priority. */
#endif
#if BDB_4_5_20
    Int32 priority;                   /* Cache priority. */
#endif

    void* addr;                       /* Address of mmap'd region. */
    uint len;                         /* Length of mmap'd region. */

    UInt32 config_flags;              /* Flags to DB_MPOOLFILE->set_flags. */

    #endregion

    #region API Methods

    IntPtr close;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CloseFcn(DB_MPOOLFILE* mpf, UInt32 flags);
    public CloseFcn Close {
      get { return (CloseFcn)Marshal.GetDelegateForFunctionPointer(close, typeof(CloseFcn)); }
    }
#endif

    IntPtr get;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
#if BDB_4_3_29
    public delegate DbRetVal GetFcn(DB_MPOOLFILE* mpf, ref UInt32 pageno, CachePageGetFlags flags, out void* page);
#endif
#if BDB_4_5_20
    public delegate DbRetVal GetFcn(DB_MPOOLFILE* mpf, ref UInt32 pageno, DB_TXN* txnid, CachePageGetFlags flags, out void* page);
#endif
    public GetFcn Get {
      get { return (GetFcn)Marshal.GetDelegateForFunctionPointer(get, typeof(GetFcn)); }
    }
#endif

    IntPtr open;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal OpenFcn(DB_MPOOLFILE* mpf, byte* file, CacheFileOpenFlags flags, int mode, UInt32 pagesize);
    public OpenFcn Open {
      get { return (OpenFcn)Marshal.GetDelegateForFunctionPointer(open, typeof(OpenFcn)); }
    }
#endif

    IntPtr put;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal PutFcn(DB_MPOOLFILE* mpf, void* page, CachePagePutFlags flags);
    public PutFcn Put {
      get { return (PutFcn)Marshal.GetDelegateForFunctionPointer(put, typeof(PutFcn)); }
    }
#endif

    IntPtr set;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetFcn(DB_MPOOLFILE* mpf, void* page, CachePagePutFlags flags);
    public SetFcn Set {
      get { return (SetFcn)Marshal.GetDelegateForFunctionPointer(set, typeof(SetFcn)); }
    }
#endif

    IntPtr get_clear_len;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetClearLenFcn(DB_MPOOLFILE* mpf, out UInt32 len);
    public GetClearLenFcn GetClearLen {
      get { return (GetClearLenFcn)Marshal.GetDelegateForFunctionPointer(get_clear_len, typeof(GetClearLenFcn)); }
    }
#endif

    IntPtr set_clear_len;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetClearLenFcn(DB_MPOOLFILE* mpf, UInt32 len);
    public SetClearLenFcn SetClearLen {
      get { return (SetClearLenFcn)Marshal.GetDelegateForFunctionPointer(set_clear_len, typeof(SetClearLenFcn)); }
    }
#endif

#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal FileIdFcn(DB_MPOOLFILE* mpf, byte* fileid);
#endif
    
    IntPtr get_fileid;
#if BDB_FULL_MPOOL_API
    public FileIdFcn GetFileId {
      get { return (FileIdFcn)Marshal.GetDelegateForFunctionPointer(get_fileid, typeof(FileIdFcn)); }
    }
#endif

    IntPtr set_fileid;
#if BDB_FULL_MPOOL_API
    public FileIdFcn SetFileId {
      get { return (FileIdFcn)Marshal.GetDelegateForFunctionPointer(set_fileid, typeof(FileIdFcn)); }
    }
#endif

    IntPtr get_flags;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFlagsFcn(DB_MPOOLFILE* mpf, out CacheFileFlags flags);
    public GetFlagsFcn GetFlags {
      get { return (GetFlagsFcn)Marshal.GetDelegateForFunctionPointer(get_flags, typeof(GetFlagsFcn)); }
    }

    IntPtr set_flags;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetFlagsFcn(DB_MPOOLFILE* mpf, CacheFileFlags flags, int onoff);
    public SetFlagsFcn SetFlags {
      get { return (SetFlagsFcn)Marshal.GetDelegateForFunctionPointer(set_flags, typeof(SetFlagsFcn)); }
    }

    IntPtr get_ftype;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFileTypeFcn(DB_MPOOLFILE* mpf, out int ftype);
    public GetFileTypeFcn GetFileType {
      get { return (GetFileTypeFcn)Marshal.GetDelegateForFunctionPointer(get_ftype, typeof(GetFileTypeFcn)); }
    }
#endif

    IntPtr set_ftype;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetFileTypeFcn(DB_MPOOLFILE* mpf, int ftype);
    public SetFileTypeFcn SetFileType {
      get { return (SetFileTypeFcn)Marshal.GetDelegateForFunctionPointer(set_ftype, typeof(SetFileTypeFcn)); }
    }
#endif

    IntPtr get_lsn_offset;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetLsnOffsetFcn(DB_MPOOLFILE* mpf, out Int32 lsn_offset);
    public GetLsnOffsetFcn GetLsnOffset {
      get { return (GetLsnOffsetFcn)Marshal.GetDelegateForFunctionPointer(get_lsn_offset, typeof(GetLsnOffsetFcn)); }
    }
#endif

    IntPtr set_lsn_offset;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetLsnOffsetFcn(DB_MPOOLFILE* mpf, Int32 lsn_offset);
    public SetLsnOffsetFcn SetLsnOffset {
      get { return (SetLsnOffsetFcn)Marshal.GetDelegateForFunctionPointer(set_lsn_offset, typeof(SetLsnOffsetFcn)); }
    }
#endif

    IntPtr get_maxsize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetMaxSizeFcn(DB_MPOOLFILE* mpf, out UInt32 gbytes, out UInt32 bytes);
    public GetMaxSizeFcn GetMaxSize {
      get { return (GetMaxSizeFcn)Marshal.GetDelegateForFunctionPointer(get_maxsize, typeof(GetMaxSizeFcn)); }
    }

    IntPtr set_maxsize;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetMaxSizeFcn(DB_MPOOLFILE* mpf, UInt32 gbytes, UInt32 bytes);
    public SetMaxSizeFcn SetMaxSize {
      get { return (SetMaxSizeFcn)Marshal.GetDelegateForFunctionPointer(set_maxsize, typeof(SetMaxSizeFcn)); }
    }

    IntPtr get_pgcookie;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetPageCookieFcn(DB_MPOOLFILE* mpf, out DBT pgcookie);
    public GetPageCookieFcn GetPageCookie {
      get { return (GetPageCookieFcn)Marshal.GetDelegateForFunctionPointer(get_pgcookie, typeof(GetPageCookieFcn)); }
    }
#endif

    IntPtr set_pgcookie;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetPageCookieFcn(DB_MPOOLFILE* mpf, ref DBT pgcookie);
    public SetPageCookieFcn SetPageCookie {
      get { return (SetPageCookieFcn)Marshal.GetDelegateForFunctionPointer(set_pgcookie, typeof(SetPageCookieFcn)); }
    }
#endif

    IntPtr get_priority;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetPriorityFcn(DB_MPOOLFILE* mpf, out CacheFilePriority priority);
    public GetPriorityFcn GetPriority {
      get { return (GetPriorityFcn)Marshal.GetDelegateForFunctionPointer(get_priority, typeof(GetPriorityFcn)); }
    }

    IntPtr set_priority;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SetPriorityFcn(DB_MPOOLFILE* mpf, CacheFilePriority priority);
    public SetPriorityFcn SetPriority {
      get { return (SetPriorityFcn)Marshal.GetDelegateForFunctionPointer(set_priority, typeof(SetPriorityFcn)); }
    }

    IntPtr sync;
#if BDB_FULL_MPOOL_API
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal SyncFcn(DB_MPOOLFILE* mpf);
    public SyncFcn Sync {
      get { return (SyncFcn)Marshal.GetDelegateForFunctionPointer(sync, typeof(SyncFcn)); }
    }
#endif

    #endregion

    /*
     * MP_FILEID_SET, MP_OPEN_CALLED and MP_READONLY do not need to be
     * thread protected because they are initialized before the file is
     * linked onto the per-process lists, and never modified.
     *
     * MP_FLUSH is thread protected because it is potentially read/set by
     * multiple threads of control.
     */
    public const UInt32 MP_FILEID_SET = 0x001;    /* Application supplied a file ID. */
    public const UInt32 MP_FLUSH = 0x002;         /* Was opened to flush a buffer. */
#if BDB_4_3_29
    public const UInt32 MP_OPEN_CALLED = 0x004;   /* File opened. */
    public const UInt32 MP_READONLY = 0x008;      /* File is readonly. */
#endif
#if BDB_4_5_20
    public const UInt32 MP_MULTIVERSION = 0x004;  /* Opened for multiversion access. */
    public const UInt32 MP_OPEN_CALLED = 0x008;   /* File opened. */
    public const UInt32 MP_READONLY = 0x010;      /* File is readonly. */
#endif
    UInt32 flags;
  }

  struct MPOOLFILE
  {
    // not visible externally
  }
  
  /* Mpool statistics structure. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_MPOOL_STAT
  {
    public UInt32 st_gbytes;              /* Total cache size: GB. */
    public UInt32 st_bytes;               /* Total cache size: B. */
    public UInt32 st_ncache;              /* Number of caches. */
    public IntPtr st_regsize;             /* Region size. (typedef uintptr_t roff_t;) */
    public uint st_mmapsize;              /* Maximum file size for mmap. */
    public int st_maxopenfd;              /* Maximum number of open fd's. */
    public int st_maxwrite;               /* Maximum buffers to write. */
    public int st_maxwrite_sleep;         /* Sleep after writing max buffers. */
    public UInt32 st_map;                 /* Pages from mapped files. */
    public UInt32 st_cache_hit;           /* Pages found in the cache. */
    public UInt32 st_cache_miss;          /* Pages not found in the cache. */
    public UInt32 st_page_create;         /* Pages created in the cache. */
    public UInt32 st_page_in;             /* Pages read in. */
    public UInt32 st_page_out;            /* Pages written out. */
    public UInt32 st_ro_evict;            /* Clean pages forced from the cache. */
    public UInt32 st_rw_evict;            /* Dirty pages forced from the cache. */
    public UInt32 st_page_trickle;        /* Pages written by memp_trickle. */
    public UInt32 st_pages;               /* Total number of pages. */
    public UInt32 st_page_clean;          /* Clean pages. */
    public UInt32 st_page_dirty;          /* Dirty pages. */
    public UInt32 st_hash_buckets;        /* Number of hash buckets. */
    public UInt32 st_hash_searches;       /* Total hash chain searches. */
    public UInt32 st_hash_longest;        /* Longest hash chain searched. */
    public UInt32 st_hash_examined;       /* Total hash entries searched. */
    public UInt32 st_hash_nowait;         /* Hash lock granted with nowait. */
    public UInt32 st_hash_wait;           /* Hash lock granted after wait. */
#if BDB_4_5_20
    public UInt32 st_hash_max_nowait;     /* Max hash lock granted with nowait. */
#endif
    public UInt32 st_hash_max_wait;       /* Max hash lock granted after wait. */
    public UInt32 st_region_nowait;       /* Region lock granted with nowait. */
    public UInt32 st_region_wait;         /* Region lock granted after wait. */
#if BDB_4_5_20
    public UInt32 st_mvcc_frozen;         /* Buffers frozen. */
    public UInt32 st_mvcc_thawed;         /* Buffers thawed. */
    public UInt32 st_mvcc_freed;          /* Frozen buffers freed. */
#endif
    public UInt32 st_alloc;               /* Number of page allocations. */
    public UInt32 st_alloc_buckets;       /* Buckets checked during allocation. */
    public UInt32 st_alloc_max_buckets;   /* Max checked during allocation. */
    public UInt32 st_alloc_pages;         /* Pages checked during allocation. */
    public UInt32 st_alloc_max_pages;     /* Max checked during allocation. */
#if BDB_4_5_20
    public UInt32 st_io_wait;             /* Thread waited on buffer I/O. */
#endif
  }

  /* Mpool file statistics structure. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public unsafe struct DB_MPOOL_FSTAT
  {
    public byte* file_name;               /* File name. */
    public UInt32 st_pagesize;            /* Page size. */
    public UInt32 st_map;                 /* Pages from mapped files. */
    public UInt32 st_cache_hit;           /* Pages found in the cache. */
    public UInt32 st_cache_miss;          /* Pages not found in the cache. */
    public UInt32 st_page_create;         /* Pages created in the cache. */
    public UInt32 st_page_in;             /* Pages read in. */
    public UInt32 st_page_out;            /* Pages written out. */
  }
}
