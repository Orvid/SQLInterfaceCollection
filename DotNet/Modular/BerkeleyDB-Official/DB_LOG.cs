/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace BerkeleyDb
{
  /*
   * Log cursor.
   */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize), CLSCompliant(false)]
  public unsafe struct DB_LOGC
  {
    #region Private Fields (to calculate offset to API function pointers)

    public readonly DB_ENV* dbenv;    /* Enclosing dbenv. */

    DB_FH* c_fhp;       /* File handle. */
    DB_LSN c_lsn;       /* Cursor: LSN */
    UInt32 c_len;       /* Cursor: record length */
    UInt32 c_prev;      /* Cursor: previous record's offset */

    DBT c_dbt;          /* Return DBT. */
#if BDB_4_5_20
    DB_LSN p_lsn;       /* Persist LSN. */
	  UInt32 p_version;   /* Persist version. */
#endif

#if BDB_4_3_29
    public const UInt32 DB_LOGC_BUF_SIZE = (32 * 1024);
#endif
    byte* bp;           /* Allocated read buffer. */
    UInt32 bp_size;     /* Read buffer length in bytes. */
    UInt32 bp_rlen;     /* Read buffer valid data length. */
    DB_LSN bp_lsn;      /* Read buffer first byte LSN. */

    UInt32 bp_maxrec;   /* Max record length in the log file. */

    #endregion

    #region API Methods

    IntPtr close;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal CloseFcn(DB_LOGC* logc, UInt32 flags);
    public CloseFcn Close {
      get { return (CloseFcn)Marshal.GetDelegateForFunctionPointer(close, typeof(CloseFcn)); }
    }

    IntPtr get;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal GetFcn(DB_LOGC* logc, ref DB_LSN lsn, ref DBT data, UInt32 flags);
    public GetFcn Get {
      get { return (GetFcn)Marshal.GetDelegateForFunctionPointer(get, typeof(GetFcn)); }
    }

    IntPtr version;
    [UnmanagedFunctionPointer(Compile.CallConv), SuppressUnmanagedCodeSecurity]
    public delegate DbRetVal VersionFcn(DB_LOGC* logc, out UInt32 version, UInt32 flags);
    public VersionFcn Version {
      get { return (VersionFcn)Marshal.GetDelegateForFunctionPointer(version, typeof(VersionFcn)); }
    }

    #endregion

    public const UInt32 DB_LOG_DISK = 0x01;       /* Log record came from disk. */
    public const UInt32 DB_LOG_LOCKED = 0x02;     /* Log region already locked */
    public const UInt32 DB_LOG_SILENT_ERR = 0x04; /* Turn-off error messages. */
    UInt32 flags;
  }

  /* Log statistics structure. */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public struct DB_LOG_STAT
  {
    public UInt32 st_magic;               /* Log file magic number. */
    public UInt32 st_version;             /* Log file version number. */
    public int st_mode;                   /* Log file permissions mode. */
    public UInt32 st_lg_bsize;            /* Log buffer size. */
    public UInt32 st_lg_size;             /* Log file size. */
#if BDB_4_5_20
    public UInt32 st_record;              /* Records entered into the log. */
#endif
    public UInt32 st_w_bytes;             /* Bytes to log. */
    public UInt32 st_w_mbytes;            /* Megabytes to log. */
    public UInt32 st_wc_bytes;            /* Bytes to log since checkpoint. */
    public UInt32 st_wc_mbytes;           /* Megabytes to log since checkpoint. */
    public UInt32 st_wcount;              /* Total I/O writes to the log. */
    public UInt32 st_wcount_fill;         /* Overflow writes to the log. */
#if BDB_4_5_20
    public UInt32 st_rcount;              /* Total I/O reads from the log. */
#endif
    public UInt32 st_scount;              /* Total syncs to the log. */
    public UInt32 st_region_wait;         /* Region lock granted after wait. */
    public UInt32 st_region_nowait;       /* Region lock granted without wait. */
    public UInt32 st_cur_file;            /* Current log file number. */
    public UInt32 st_cur_offset;          /* Current log file offset. */
    public UInt32 st_disk_file;           /* Known on disk log file number. */
    public UInt32 st_disk_offset;         /* Known on disk log file offset. */
    public IntPtr st_regsize;             /* Region size. (typedef uintptr_t roff_t;) */
    public UInt32 st_maxcommitperflush;   /* Max number of commits in a flush. */
    public UInt32 st_mincommitperflush;   /* Min number of commits in a flush. */
  }
}