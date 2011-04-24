/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BerkeleyDb
{
#if BDB_4_3_29

  /// <summary>Constants translated from Berkeley DB header file db.h.</summary>
  public static class DbConst
  {
    public const int DB_FILE_ID_LEN = 20;           /* Unique file ID length. */
    public const int DB_LOGFILEID_INVALID = -1;
    /* This has to change when the max opcode hits 255. */
    public const int DB_OPFLAGS_MASK = 0x000000ff;  /* Mask for operations flags. */
    public const int DB_XIDDATASIZE = 128;          /* Size of XA global transaction ID. */

    #region Common Flags
    /*
    *  Interfaces which use any of these common flags should never have
    *  interface specific flags in this range.
    */
    public const int DB_CREATE = 0x0000001;               /* Create file as necessary. */
    public const int DB_CXX_NO_EXCEPTIONS = 0x0000002;    /* C++: return error values. */
    public const int DB_FORCE = 0x0000004;                /* Force (anything). */
    public const int DB_NOMMAP = 0x0000008;               /* Don't mmap underlying file. */
    public const int DB_RDONLY = 0x0000010;               /* Read-only (O_RDONLY). */
    public const int DB_RECOVER = 0x0000020;              /* Run normal recovery. */
    public const int DB_THREAD = 0x0000040;               /* Applications are threaded. */
    public const int DB_TRUNCATE = 0x0000080;             /* Discard existing DB (O_TRUNC). */
    public const int DB_TXN_NOSYNC = 0x0000100;           /* Do not sync log on commit. */
    public const int DB_TXN_NOT_DURABLE = 0x0000200;      /* Do not log changes. */
    public const int DB_USE_ENVIRON = 0x0000400;          /* Use the environment. */
    public const int DB_USE_ENVIRON_ROOT = 0x0000800;     /* Use the environment if root. */
    /*
    * Common flags --
    *  Interfaces which use any of these common flags should never have
    *  interface specific flags in this range.
    *
    * DB_AUTO_COMMIT:
    *  DB_ENV->set_flags; DB->associate; DB->del; DB->put; DB->open;
    *  DB->remove; DB->rename; DB->truncate
    * DB_DEGREE_2:
    *  DB->cursor; DB->get; DB->join; DBcursor->c_get; DB_ENV->txn_begin
    * DB_DIRTY_READ:
    *  DB->cursor; DB->get; DB->join; DB->open; DBcursor->c_get;
    *  DB_ENV->txn_begin
    * DB_NOAUTO_COMMIT
    *  DB->associate; DB->del; DB->put; DB->open;
    *  DB->remove; DB->rename; DB->truncate
    *
    * !!!
    * The DB_DIRTY_READ and DB_DEGREE_2 bit masks can't be changed without
    * also changing the masks for the flags that can be OR'd into DB
    * access method and cursor operation values.
    */
    public const int DB_AUTO_COMMIT = 0x01000000;       /* Implied transaction. */
    public const int DB_DEGREE_2 = 0x02000000;          /* Degree 2. */
    public const int DB_DIRTY_READ = 0x04000000;        /* Dirty Read. */
    public const int DB_NO_AUTO_COMMIT = 0x08000000;    /* Override env-wide AUTOCOMMIT. */
    #endregion

    #region Db Flags

    public const int DB_CHKSUM = 0x0000001;       /* Do checksumming */
    public const int DB_DUP = 0x0000002;          /* Btree, Hash: duplicate keys. */
    public const int DB_DUPSORT = 0x0000004;      /* Btree, Hash: duplicate keys. */
    public const int DB_ENCRYPT = 0x0000008;      /* Btree, Hash: duplicate keys. */
    public const int DB_INORDER = 0x0000010;      /* Queue: strict ordering on consume. */
    public const int DB_RECNUM = 0x0000020;       /* Btree: record numbers. */
    public const int DB_RENUMBER = 0x0000040;     /* Recno: renumber on insert/delete. */
    public const int DB_REVSPLITOFF = 0x0000080;  /* Btree: turn off reverse splits. */
    public const int DB_SNAPSHOT = 0x0000100;     /* Recno: snapshot the input. */

    #endregion

    #region DB Access Methods and Cursor Operation

    /* Each value is an operation code to which additional bit flags are added. */
    public const int DB_AFTER = 1;              /* c_put() */
    public const int DB_APPEND = 2;             /* put() */
    public const int DB_BEFORE = 3;             /* c_put() */
    public const int DB_CACHED_COUNTS = 4;      /* stat() */
    public const int DB_CONSUME = 5;            /* get() */
    public const int DB_CONSUME_WAIT = 6;       /* get() */
    public const int DB_CURRENT = 7;            /* c_get(); c_put(); DB_LOGC->get() */
    public const int DB_FAST_STAT = 8;          /* stat() */
    public const int DB_FIRST = 9;              /* c_get(); DB_LOGC->get() */
    public const int DB_GET_BOTH = 10;          /* get(); c_get() */
    public const int DB_GET_BOTHC = 11;         /* c_get() (internal) */
    public const int DB_GET_BOTH_RANGE = 12;    /* get(); c_get() */
    public const int DB_GET_RECNO = 13;         /* c_get() */
    public const int DB_JOIN_ITEM = 14;         /* c_get(); do not do primary lookup */
    public const int DB_KEYFIRST = 15;          /* c_put() */
    public const int DB_KEYLAST = 16;           /* c_put() */
    public const int DB_LAST = 17;              /* c_get(); DB_LOGC->get() */
    public const int DB_NEXT = 18;              /* c_get(); DB_LOGC->get() */
    public const int DB_NEXT_DUP = 19;          /* c_get() */
    public const int DB_NEXT_NODUP = 20;        /* c_get() */
    public const int DB_NODUPDATA = 21;         /* put(); c_put() */
    public const int DB_NOOVERWRITE = 22;       /* put() */
    public const int DB_NOSYNC = 23;            /* close() */
    public const int DB_POSITION = 24;          /* c_dup() */
    public const int DB_PREV = 25;              /* c_get(); DB_LOGC->get() */
    public const int DB_PREV_NODUP = 26;        /* c_get(); DB_LOGC->get() */
    public const int DB_RECORDCOUNT = 27;       /* stat() */
    public const int DB_SET = 28;               /* c_get(); DB_LOGC->get() */
    public const int DB_SET_LOCK_TIMEOUT = 29;  /* set_timout() */
    public const int DB_SET_RANGE = 30;         /* c_get() */
    public const int DB_SET_RECNO = 31;         /* get(); c_get() */
    public const int DB_SET_TXN_NOW = 32;       /* set_timout() (internal) */
    public const int DB_SET_TXN_TIMEOUT = 33;   /* set_timout() */
    public const int DB_UPDATE_SECONDARY = 34;  /* c_get(); c_del() (internal) */
    public const int DB_WRITECURSOR = 35;       /* cursor() */
    public const int DB_WRITELOCK = 36;         /* cursor() (internal) */
    /* Masks for flags that can be OR'd into DB access method and cursor operation values. */
    public const int DB_MULTIPLE = 0x08000000;      /* Return multiple data values. */
    public const int DB_MULTIPLE_KEY = 0x10000000;  /* Return multiple data/key pairs. */
    public const int DB_RMW = 0x20000000;           /* Acquire write flag immediately. */
    #endregion

    #region Misc Db Flags

    public const int DB_EXCL = 0x0001000;           /* Exclusive open (O_EXCL). */
    public const int DB_JOIN_NOSORT = 0x0000001;    /* Don't try to optimize join. */
    public const int DB_ENCRYPT_AES = 0x0000001;    /* AES, assumes SHA1 checksum */
    public const int DB_AM_SECONDARY = 0x04000000;  /* Database is a secondary index. */

    public const int DB_REP_CREATE = 0x0000001;     /* Open of an internal rep database. */
    public const int DB_XA_CREATE = 0x0000002;      /* Open in an XA environment. */

    #endregion

    #region Transaction Flags

    public const int DB_TXN_NOWAIT = 0x0001000;     /* Do not wait for locks in this TXN. */
    public const int DB_TXN_SYNC = 0x0002000;       /* Always sync log on commit. */

    #endregion

    #region Environment Flags

    public const int DB_CDB_ALLDB = 0x00001000;         /* Set CDB locking per environment. */
    public const int DB_DIRECT_DB = 0x00002000;         /* Don't buffer databases in the OS. */
    public const int DB_DIRECT_LOG = 0x00004000;        /* Don't buffer log files in the OS. */
    public const int DB_DSYNC_LOG = 0x00008000;         /* Set O_DSYNC on the log. */
    public const int DB_LOG_AUTOREMOVE = 0x00010000;    /* Automatically remove log files. */
    public const int DB_LOG_INMEMORY = 0x00020000;      /* Store logs in buffers in memory. */
    public const int DB_NOLOCKING = 0x00040000;         /* Set locking/mutex behavior. */
    public const int DB_NOPANIC = 0x00080000;           /* Set panic state per DB_ENV. */
    public const int DB_OVERWRITE = 0x00100000;         /* Overwrite unlinked region files. */
    public const int DB_PANIC_ENVIRONMENT = 0x00200000; /* Set panic state per environment. */
    public const int DB_REGION_INIT = 0x00400000;       /* Page-fault regions on open. */
    public const int DB_TIME_NOTGRANTED = 0x00800000;   /* Return NOTGRANTED on timeout. */
    public const int DB_TXN_WRITE_NOSYNC = 0x10000000;  /* Write, don't sync, on txn commit. */
    public const int DB_YIELDCPU = 0x20000000;          /* Yield the CPU (a lot). */

    public const int DB_RPCCLIENT = 0x0000001;          /* An RPC client environment. */

    /* Flags private to DB_ENV->open.
    *	   Shared flags up to 0x0000800 */
    public const int DB_INIT_CDB = 0x0001000;           /* Concurrent Access Methods. */
    public const int DB_INIT_LOCK = 0x0002000;          /* Initialize locking. */
    public const int DB_INIT_LOG = 0x0004000;           /* Initialize logging. */
    public const int DB_INIT_MPOOL = 0x0008000;         /* Initialize mpool. */
    public const int DB_INIT_REP = 0x0010000;           /* Initialize replication. */
    public const int DB_INIT_TXN = 0x0020000;           /* Initialize transactions. */
    public const int DB_JOINENV = 0x0040000;            /* Initialize all subsystems present. */
    public const int DB_LOCKDOWN = 0x0080000;           /* Lock memory into physical core. */
    public const int DB_PRIVATE = 0x0100000;            /* DB_ENV is process local. */
    public const int DB_RECOVER_FATAL = 0x0200000;      /* Run catastrophic recovery. */
    public const int DB_SYSTEM_MEM = 0x0400000;         /* Use system-backed memory. */

    /* Currently, the verbose list is a bit field with room for 32
    * entries.  There's no reason that it needs to be limited, if
    * there are ever more than 32 entries, convert to a bit array. */
    public const int DB_VERB_DEADLOCK = 0x0001;         /* Deadlock detection information. */
    public const int DB_VERB_RECOVERY = 0x0002;         /* Recovery information. */
    public const int DB_VERB_REPLICATION = 0x0004;      /* Replication information. */
    public const int DB_VERB_WAITSFOR = 0x008;         /* Dump waits-for table. */

    #endregion

  #region Logging Flags

    /* Flag values for DB_ENV->log_archive(). */
    public const int DB_ARCH_ABS = 0x001;         /* Absolute pathnames. */
    public const int DB_ARCH_DATA = 0x002;        /* Data files. */
    public const int DB_ARCH_LOG = 0x004;         /* Log files. */
    public const int DB_ARCH_REMOVE = 0x008;      /* Remove log files. */
    /* Flag values for DB_ENV->log_put(). */
    public const int DB_FLUSH = 0x001;            /* Flush data to disk (public). */

  #endregion

  #region Locking Flags

    /*
     * Deadlock detector modes; used in the DB_ENV structure to configure the
     * locking subsystem.
     */
    public const int DB_LOCK_NORUN = 0;
    public const int DB_LOCK_DEFAULT = 1;         /* Default policy. */
    public const int DB_LOCK_EXPIRE = 2;          /* Only expire locks, no detection. */
    public const int DB_LOCK_MAXLOCKS = 3;        /* Select locker with max locks. */
    public const int DB_LOCK_MAXWRITE = 4;        /* Select locker with max writelocks. */
    public const int DB_LOCK_MINLOCKS = 5;        /* Select locker with min locks. */
    public const int DB_LOCK_MINWRITE = 6;        /* Select locker with min writelocks. */
    public const int DB_LOCK_OLDEST = 7;          /* Select oldest locker. */
    public const int DB_LOCK_RANDOM = 8;          /* Select random locker. */
    public const int DB_LOCK_YOUNGEST = 9;        /* Select youngest locker. */

    /* Flag values for lock_vec(), lock_get(). */
    public const int DB_LOCK_ABORT = 0x001;       /* Internal: Lock during abort. */
    public const int DB_LOCK_NOWAIT = 0x002;      /* Don't wait on unavailable lock. */
    public const int DB_LOCK_RECORD = 0x004;      /* Internal: record lock. */
    public const int DB_LOCK_REMOVE = 0x008;      /* Internal: flag object removed. */
    public const int DB_LOCK_SET_TIMEOUT = 0x010; /* Internal: set lock timeout. */
    public const int DB_LOCK_SWITCH = 0x020;      /* Internal: switch existing lock. */
    public const int DB_LOCK_UPGRADE = 0x040;     /* Internal: upgrade existing lock. */

  #endregion

  #region Memory Pool Flags

    /* Flags values for DB_MPOOLFILE->set_flags. */
    public const int DB_MPOOL_NOFILE = 0x001;     /* Never open a backing file. */
    public const int DB_MPOOL_UNLINK = 0x002;     /* Unlink the file on last close. */

    /*
     * Flags private to DB_MPOOLFILE->open.
     *	   Shared flags up to 0x0000800 */
    public const int DB_DIRECT = 0x0001000;       /* Don't buffer the file in the OS. */
    public const int DB_ODDFILESIZE = 0x0008000;  /* Truncate file to N * pgsize. */

    /* Flag values for DB_MPOOLFILE->get. */
    public const int DB_MPOOL_CREATE = 0x001;     /* Create a page. */
    public const int DB_MPOOL_LAST = 0x002;       /* Return the last page. */
    public const int DB_MPOOL_NEW = 0x004;        /* Create a new page. */

    /* Flag values for DB_MPOOLFILE->put, DB_MPOOLFILE->set. */
    public const int DB_MPOOL_CLEAN = 0x001;      /* Page is not modified. */
    public const int DB_MPOOL_DIRTY = 0x002;      /* Page is modified. */
    public const int DB_MPOOL_DISCARD = 0x004;    /* Don't cache the page. */
    public const int DB_MPOOL_FREE = 0x008;       /* Free page if present. */

  #endregion

  #region Sequence Flags

    public const int DB_SEQ_DEC = 0x00000001;     /* Decrement sequence. */
    public const int DB_SEQ_INC = 0x00000002;     /* Increment sequence. */
    public const int DB_SEQ_WRAP = 0x00000008;    /* Wrap sequence at min/max. */

  #endregion

  #region Stats Flags

    public const int DB_STAT_ALL = 0x0000001;           /* Print: Everything. */
    public const int DB_STAT_CLEAR = 0x0000002;         /* Clear stat after returning values. */
    public const int DB_STAT_LOCK_CONF = 0x0000004;     /* Print: Lock conflict matrix. */
    public const int DB_STAT_LOCK_LOCKERS = 0x0000008;  /* Print: Lockers. */
    public const int DB_STAT_LOCK_OBJECTS = 0x0000010;  /* Print: Lock objects. */
    public const int DB_STAT_LOCK_PARAMS = 0x0000020;   /* Print: Lock parameters. */
    public const int DB_STAT_MEMP_HASH = 0x0000040;   /* Print: Mpool hash buckets. */
    public const int DB_STAT_SUBSYSTEM = 0x0000080;   /* Print: Subsystems too. */

  #endregion

  #region Verify Flags

    public const int DB_AGGRESSIVE = 0x0000001;       /* Salvage whatever could be data.*/
    public const int DB_NOORDERCHK = 0x0000002;       /* Skip sort order/hashing check. */
    public const int DB_ORDERCHKONLY = 0x0000004;     /* Only perform the order check. */
    public const int DB_PR_PAGE = 0x0000008;          /* Show page contents (-da). */
    public const int DB_PR_RECOVERYTEST = 0x0000010;  /* Recovery test (-dr). */
    public const int DB_PRINTABLE = 0x0000020;        /* Use printable format for salvage. */
    public const int DB_SALVAGE = 0x0000040;          /* Salvage what looks like data. */
    public const int DB_UNREF = 0x0000080;            /* Report unreferenced pages. */

  #endregion

  #region Replication FLags

    /* Flags private to DB->set_rep_transport's send callback. */
    public const int DB_REP_NOBUFFER = 0x0000001;     /* Do not buffer this message. */
    public const int DB_REP_PERMANENT = 0x0000002;    /* Important--app. may want to flush. */

    /* rep_start flags values */
    public const int DB_REP_CLIENT = 0x0000001;
    public const int DB_REP_MASTER = 0x0000002;

  #endregion
  }

#endif

#if BDB_4_5_20

  /// <summary>Constants translated from Berkeley DB header file db.h.</summary>
  public static class DbConst
  {
    public const int DB_FILE_ID_LEN = 20;           /* Unique file ID length. */
    public const int DB_LOGFILEID_INVALID = -1;
    /* This has to change when the max opcode hits 255. */
    public const int DB_OPFLAGS_MASK = 0x000000ff;  /* Mask for operations flags. */
    public const int DB_XIDDATASIZE = 128;          /* Size of XA global transaction ID. */
    /* This is the length of the buffer passed to DB_ENV->thread_id_string() */
    public const int DB_THREADID_STRLEN = 128;


    #region Common Flags

    /* Interfaces which use any of these common flags should never have
    *  interface specific flags in this range. */
    public const int DB_CREATE = 0x0000001;               /* Create file as necessary. */
    //public const int DB_DURABLE_UNKNOWN = 0x0000002;      /* Durability on open (internal). */
    public const int DB_FORCE = 0x0000004;                /* Force (anything). */
    public const int DB_MULTIVERSION = 0x0000008;         /* Multiversion concurrency control. */
    public const int DB_NOMMAP = 0x0000010;               /* Don't mmap underlying file. */
    public const int DB_RDONLY = 0x0000020;               /* Read-only (O_RDONLY). */
    public const int DB_RECOVER = 0x0000040;              /* Run normal recovery. */
    public const int DB_THREAD = 0x0000080;               /* Applications are threaded. */
    public const int DB_TRUNCATE = 0x0000100;             /* Discard existing DB (O_TRUNC). */
    public const int DB_TXN_NOSYNC = 0x0000200;           /* Do not sync log on commit. */
    public const int DB_TXN_NOT_DURABLE = 0x0000400;      /* Do not log changes. */
    public const int DB_TXN_WRITE_NOSYNC = 0x0000800;     /* Write the log but don't sync. */
    public const int DB_USE_ENVIRON = 0x0001000;          /* Use the environment. */
    public const int DB_USE_ENVIRON_ROOT = 0x0001000;     /* Use the environment if root. */
    /*
    * Common flags --
    *	Interfaces which use any of these common flags should never have
    *	interface specific flags in this range.
    *
    * DB_AUTO_COMMIT:
    *	DB_ENV->set_flags, DB->open
    *      (Note: until the 4.3 release, legal to DB->associate, DB->del,
    *	DB->put, DB->remove, DB->rename and DB->truncate, and others.)
    * DB_READ_COMMITTED:
    *	DB->cursor, DB->get, DB->join, DBcursor->c_get, DB_ENV->txn_begin
    * DB_READ_UNCOMMITTED:
    *	DB->cursor, DB->get, DB->join, DB->open, DBcursor->c_get,
    *	DB_ENV->txn_begin
    * DB_TXN_SNAPSHOT:
    *	DB_ENV->set_flags, DB_ENV->txn_begin, DB->cursor
    *
    * !!!
    * The DB_READ_COMMITTED and DB_READ_UNCOMMITTED bit masks can't be changed
    * without also changing the masks for the flags that can be OR'd into DB
    * access method and cursor operation values.
    */
    public const int DB_AUTO_COMMIT = 0x02000000;       /* Implied transaction. */
    public const int DB_READ_COMMITTED = 0x04000000;    /* Degree 2 isolation. */
    public const int DB_READ_UNCOMMITTED = 0x08000000;  /* Degree 1 isolation. */
    public const int DB_TXN_SNAPSHOT = 0x10000000;      /* Snapshot isolation. */

    /* Flags common to db_env_create and db_create. */
    public const int DB_CXX_NO_EXCEPTIONS = 0x0000001;  /* C++: return error values. */

    #endregion

    #region Db Flags

    /* Flags private to db_create.
    *	   Shared flags up to 0x0000001 */
    public const int DB_XA_CREATE = 0x0000002;      /* Open in an XA environment. */

    /* Flags private to DB->open.
    *	   Shared flags up to 0x0002000 */
    public const int DB_EXCL = 0x0004000;           /* Exclusive open (O_EXCL). */
    public const int DB_FCNTL_LOCKING = 0x0008000;  /* UNDOC: fcntl(2) locking. */
    public const int DB_NO_AUTO_COMMIT = 0x0010000; /* Override env-wide AUTOCOMMIT. */
    public const int DB_RDWRMASTER = 0x0020000;     /* UNDOC: allow subdb master open R/W */
    public const int DB_WRITEOPEN = 0x0040000;      /* UNDOC: open with write lock. */

    /* Flags private to DB->set_flags.
    *	   Shared flags up to 0x00002000 */
    public const int DB_CHKSUM = 0x00004000;        /* Do checksumming */
    public const int DB_DUP = 0x00008000;           /* Btree, Hash: duplicate keys. */
    public const int DB_DUPSORT = 0x00010000;       /* Btree, Hash: duplicate keys. */
    public const int DB_ENCRYPT = 0x00020000;       /* Btree, Hash: duplicate keys. */
    public const int DB_INORDER = 0x00040000;       /* Queue: strict ordering on consume. */
    public const int DB_RECNUM = 0x00080000;        /* Btree: record numbers. */
    public const int DB_RENUMBER = 0x00100000;      /* Recno: renumber on insert/delete. */
    public const int DB_REVSPLITOFF = 0x00200000;   /* Btree: turn off reverse splits. */
    public const int DB_SNAPSHOT = 0x00400000;      /* Recno: snapshot the input. */

    /*
    * Flags private to DB->associate.
    *	   Shared flags up to 0x0002000 */
    public const int DB_IMMUTABLE_KEY = 0x0004000;  /* Secondary key is immutable. */
    /*	      Shared flags at 0x1000000 */

    /*
    * Flags private to DB->compact.
    *	   Shared flags up to 0x00002000 */
    public const int DB_FREELIST_ONLY = 0x00004000; /* Just sort and truncate. */
    public const int DB_FREE_SPACE = 0x00008000;    /* Free space . */

    #endregion

    #region DB Access Methods and Cursor Operation

    /* Each value is an operation code to which additional bit flags are added. */
    public const int DB_AFTER = 1;              /* c_put() */
    public const int DB_APPEND = 2;             /* put() */
    public const int DB_BEFORE = 3;             /* c_put() */
    public const int DB_CONSUME = 4;            /* get() */
    public const int DB_CONSUME_WAIT = 5;       /* get() */
    public const int DB_CURRENT = 6;            /* c_get(); c_put(); DB_LOGC->get() */
    public const int DB_FIRST = 7;              /* c_get(); DB_LOGC->get() */
    public const int DB_GET_BOTH = 8;           /* get(); c_get() */
    public const int DB_GET_BOTHC = 9;          /* c_get() (internal) */
    public const int DB_GET_BOTH_RANGE = 10;    /* get(); c_get() */
    public const int DB_GET_RECNO = 11;         /* c_get() */
    public const int DB_JOIN_ITEM = 12;         /* c_get(); do not do primary lookup */
    public const int DB_KEYFIRST = 13;          /* c_put() */
    public const int DB_KEYLAST = 14;           /* c_put() */
    public const int DB_LAST = 15;              /* c_get(); DB_LOGC->get() */
    public const int DB_NEXT = 16;              /* c_get(); DB_LOGC->get() */
    public const int DB_NEXT_DUP = 17;          /* c_get() */
    public const int DB_NEXT_NODUP = 18;        /* c_get() */
    public const int DB_NODUPDATA = 19;         /* put(); c_put() */
    public const int DB_NOOVERWRITE = 20;       /* put() */
    public const int DB_NOSYNC = 21;            /* close() */
    public const int DB_POSITION = 22;          /* c_dup() */
    public const int DB_PREV = 23;              /* c_get(); DB_LOGC->get() */
    public const int DB_PREV_NODUP = 24;        /* c_get(); DB_LOGC->get() */
    public const int DB_SET = 25;               /* c_get(); DB_LOGC->get() */
    public const int DB_SET_LOCK_TIMEOUT = 26;  /* set_timout() */
    public const int DB_SET_RANGE = 27;         /* c_get() */
    public const int DB_SET_RECNO = 28;         /* get(); c_get() */
    public const int DB_SET_TXN_NOW = 29;       /* set_timout() (internal) */
    public const int DB_SET_TXN_TIMEOUT = 30;   /* set_timout() */
    public const int DB_UPDATE_SECONDARY = 31;  /* c_get(); c_del() (internal) */
    public const int DB_WRITECURSOR = 32;       /* cursor() */
    public const int DB_WRITELOCK = 33;         /* cursor() (internal) */
    /* Masks for flags that can be OR'd into DB access method and cursor operation values. */
    public const int DB_MULTIPLE = 0x10000000;      /* Return multiple data values. */
    public const int DB_MULTIPLE_KEY = 0x20000000;  /* Return multiple data/key pairs. */
    public const int DB_RMW = 0x40000000;           /* Acquire write lock immediately. */

    #endregion

    #region Misc Db Flags

    public const int DB_JOIN_NOSORT = 0x0000001;    /* Don't try to optimize join. */
    public const int DB_AM_SECONDARY = 0x02000000;  /* Database is a secondary index. */

    #endregion

    #region Transaction Flags

    public const int DB_TXN_NOWAIT = 0x0004000;     /* Do not wait for locks in this TXN. */
    public const int DB_TXN_SYNC = 0x0008000;       /* Always sync log on commit. */

    #endregion

    #region Environment Flags

    /* Flags private to db_env_create.
    *	   Shared flags up to 0x0000001 */
    public const int DB_RPCCLIENT = 0x0000002;          /* An RPC client environment. */

    /* Flags private to DB_ENV->set_encrypt. */
    public const int DB_ENCRYPT_AES = 0x0000001;        /* AES, assumes SHA1 checksum */

    /* Flags private to DB_ENV->set_flags.
    *    Shared flags up to 0x00002000 */
    public const int DB_CDB_ALLDB = 0x00004000;         /* Set CDB locking per environment. */
    public const int DB_DIRECT_DB = 0x00008000;         /* Don't buffer databases in the OS. */
    public const int DB_DIRECT_LOG = 0x00010000;        /* Don't buffer log files in the OS. */
    public const int DB_DSYNC_DB = 0x00020000;          /* Set O_DSYNC on the databases. */
    public const int DB_DSYNC_LOG = 0x00040000;         /* Set O_DSYNC on the log. */
    public const int DB_LOG_AUTOREMOVE = 0x00080000;    /* Automatically remove log files. */
    public const int DB_LOG_INMEMORY = 0x00100000;      /* Store logs in buffers in memory. */
    public const int DB_NOLOCKING = 0x00200000;         /* Set locking/mutex behavior. */
    public const int DB_NOPANIC = 0x00400000;           /* Set panic state per DB_ENV. */
    public const int DB_OVERWRITE = 0x00800000;         /* Overwrite unlinked region files. */
    public const int DB_PANIC_ENVIRONMENT = 0x01000000; /* Set panic state per environment. */
    /*	      Shared flags at 0x02000000 */
    /*	      Shared flags at 0x04000000 */
    /*	      Shared flags at 0x08000000 */
    /*	      Shared flags at 0x10000000 */
    public const int DB_REGION_INIT = 0x20000000;       /* Page-fault regions on open. */
    public const int DB_TIME_NOTGRANTED = 0x40000000;   /* Return NOTGRANTED on timeout. */
    public const int DB_YIELDCPU = unchecked((int)0x80000000);     /* Yield the CPU (a lot). */

    /* Flags private to DB_ENV->open.
    *	   Shared flags up to 0x0000800 */
    public const int DB_INIT_CDB = 0x0004000;           /* Concurrent Access Methods. */
    public const int DB_INIT_LOCK = 0x0008000;          /* Initialize locking. */
    public const int DB_INIT_LOG = 0x0010000;           /* Initialize logging. */
    public const int DB_INIT_MPOOL = 0x0020000;         /* Initialize mpool. */
    public const int DB_INIT_REP = 0x0040000;           /* Initialize replication. */
    public const int DB_INIT_TXN = 0x0080000;           /* Initialize transactions. */
    public const int DB_LOCKDOWN = 0x0100000;           /* Lock memory into physical core. */
    public const int DB_PRIVATE = 0x0200000;            /* DB_ENV is process local. */
    public const int DB_RECOVER_FATAL = 0x0400000;      /* Run catastrophic recovery. */
    public const int DB_REGISTER = 0x0800000;           /* Multi-process registry. */
    public const int DB_SYSTEM_MEM = 0x1000000;         /* Use system-backed memory. */
    public const int DB_JOINENV = 0x0;                  /* Compatibility. */

    /* Event notification types. */
    public const int DB_EVENT_NO_SUCH_EVENT = 0;  /* out-of-band sentinel value */
    public const int DB_EVENT_PANIC = 1;
    public const int DB_EVENT_REP_CLIENT = 2;
    public const int DB_EVENT_REP_MASTER = 3;
    public const int DB_EVENT_REP_NEWMASTER = 4;
    public const int DB_EVENT_REP_STARTUPDONE = 5;
    public const int DB_EVENT_WRITE_FAILED = 6;

    /* Currently, the verbose list is a bit field with room for 32
    * entries.  There's no reason that it needs to be limited, if
    * there are ever more than 32 entries, convert to a bit array. */
    public const int DB_VERB_DEADLOCK = 0x0001;         /* Deadlock detection information. */
    public const int DB_VERB_RECOVERY = 0x0002;         /* Recovery information. */
    public const int DB_VERB_REGISTER = 0x0004;         /* DB_REGISTER support information. */
    public const int DB_VERB_REPLICATION = 0x0008;      /* Replication information. */
    public const int DB_VERB_WAITSFOR = 0x0010;         /* Dump waits-for table. */

    #endregion

    #region Logging Flags

    /* Flag values for DB_ENV->log_archive(). */
    public const int DB_ARCH_ABS = 0x001;         /* Absolute pathnames. */
    public const int DB_ARCH_DATA = 0x002;        /* Data files. */
    public const int DB_ARCH_LOG = 0x004;         /* Log files. */
    public const int DB_ARCH_REMOVE = 0x008;      /* Remove log files. */
    /* Flag values for DB_ENV->log_put(). */
    public const int DB_FLUSH = 0x001;            /* Flush data to disk (public). */

    #endregion

    #region Locking Flags

    /* Deadlock detector modes; used in the DB_ENV structure to configure the
     * locking subsystem. */
    public const int DB_LOCK_NORUN = 0;
    public const int DB_LOCK_DEFAULT = 1;         /* Default policy. */
    public const int DB_LOCK_EXPIRE = 2;          /* Only expire locks, no detection. */
    public const int DB_LOCK_MAXLOCKS = 3;        /* Select locker with max locks. */
    public const int DB_LOCK_MAXWRITE = 4;        /* Select locker with max writelocks. */
    public const int DB_LOCK_MINLOCKS = 5;        /* Select locker with min locks. */
    public const int DB_LOCK_MINWRITE = 6;        /* Select locker with min writelocks. */
    public const int DB_LOCK_OLDEST = 7;          /* Select oldest locker. */
    public const int DB_LOCK_RANDOM = 8;          /* Select random locker. */
    public const int DB_LOCK_YOUNGEST = 9;        /* Select youngest locker. */

    /* Flag values for lock_vec(), lock_get(). */
    public const int DB_LOCK_NOWAIT = 0x002;      /* Don't wait on unavailable lock. */

    #endregion

    #region Memory Pool Flags

    /* Flags values for DB_MPOOLFILE->set_flags. */
    public const int DB_MPOOL_NOFILE = 0x001;     /* Never open a backing file. */
    public const int DB_MPOOL_UNLINK = 0x002;     /* Unlink the file on last close. */

    /* Flags private to DB_MPOOLFILE->open.
     *	   Shared flags up to 0x0000800 */
    public const int DB_DIRECT = 0x0004000;       /* Don't buffer the file in the OS. */
    public const int DB_ODDFILESIZE = 0x0010000;  /* Truncate file to N * pgsize. */

    /* Flag values for DB_MPOOLFILE->get. */
    public const int DB_MPOOL_CREATE = 0x001;     /* Create a page. */
    public const int DB_MPOOL_DIRTY = 0x002;      /* Get page for an update. */
    public const int DB_MPOOL_EDIT = 0x004;       /* Modify without copying. */
    public const int DB_MPOOL_FREE = 0x008;       /* Free page if present. */
    public const int DB_MPOOL_LAST = 0x010;       /* Return the last page. */
    public const int DB_MPOOL_NEW = 0x020;        /* Create a new page. */

    /* Flag values for DB_MPOOLFILE->put, DB_MPOOLFILE->set. */
    public const int DB_MPOOL_DISCARD = 0x001;    /* Don't cache the page. */

    #endregion

    #region Sequence Flags

    public const int DB_SEQ_DEC = 0x00000001;     /* Decrement sequence. */
    public const int DB_SEQ_INC = 0x00000002;     /* Increment sequence. */
    public const int DB_SEQ_WRAP = 0x00000008;    /* Wrap sequence at min/max. */

    #endregion

    #region Stats Flags

    public const int DB_FAST_STAT = 0x0000001;          /* Don't traverse the database. */
    public const int DB_STAT_ALL = 0x0000002;           /* Print: Everything. */
    public const int DB_STAT_CLEAR = 0x0000004;         /* Clear stat after returning values. */
    public const int DB_STAT_LOCK_CONF = 0x0000008;     /* Print: Lock conflict matrix. */
    public const int DB_STAT_LOCK_LOCKERS = 0x0000010;  /* Print: Lockers. */
    public const int DB_STAT_LOCK_OBJECTS = 0x0000020;  /* Print: Lock objects. */
    public const int DB_STAT_LOCK_PARAMS = 0x0000040;   /* Print: Lock parameters. */
    public const int DB_STAT_MEMP_HASH = 0x0000080;     /* Print: Mpool hash buckets. */
    public const int DB_STAT_SUBSYSTEM = 0x0000200;     /* Print: Subsystems too. */

    #endregion

    #region Verify Flags

    public const int DB_AGGRESSIVE = 0x0000001;       /* Salvage whatever could be data.*/
    public const int DB_NOORDERCHK = 0x0000002;       /* Skip sort order/hashing check. */
    public const int DB_ORDERCHKONLY = 0x0000004;     /* Only perform the order check. */
    public const int DB_PR_PAGE = 0x0000008;          /* Show page contents (-da). */
    public const int DB_PR_RECOVERYTEST = 0x0000010;  /* Recovery test (-dr). */
    public const int DB_PRINTABLE = 0x0000020;        /* Use printable format for salvage. */
    public const int DB_SALVAGE = 0x0000040;          /* Salvage what looks like data. */
    public const int DB_UNREF = 0x0000080;            /* Report unreferenced pages. */

    #endregion

    #region Replication Flags

    /* rep_config flag values. */
    public const int DB_REP_CONF_BULK = 0x0001;         /* Bulk transfer. */
    public const int DB_REP_CONF_DELAYCLIENT = 0x0002;  /* Delay client synchronization. */
    public const int DB_REP_CONF_NOAUTOINIT = 0x0004;   /* No automatic client init. */
    public const int DB_REP_CONF_NOWAIT = 0x0008;       /* Don't wait, return error. */

    /* Replication Framework timeout configuration values. */
    public const int DB_REP_ACK_TIMEOUT = 1;
    public const int DB_REP_ELECTION_TIMEOUT = 2;
    public const int DB_REP_ELECTION_RETRY = 3;
    public const int DB_REP_CONNECTION_RETRY = 4;

    /* Flags private to DB->set_rep_transport's send callback. */
    public const int DB_REP_ANYWHERE = 0x0000001;       /* Message can be serviced anywhere. */
    public const int DB_REP_NOBUFFER = 0x0000002;       /* Do not buffer this message. */
    public const int DB_REP_PERMANENT = 0x0000004;      /* Important--app. may want to flush. */
    public const int DB_REP_REREQUEST = 0x0000008;      /* This msg already been requested. */

    /* Operation code values for rep_start and/or repmgr_start.  Just one of the
    *  following values should be passed in the flags parameter.  (If we ever need
    *  additional, independent bit flags for these methods, we can start allocating
    *  them from the high-order byte of the flags word, as we currently do elsewhere
    *  for DB_AFTER through DB_WRITELOCK and DB_AUTO_COMMIT, etc.) */
    public const int DB_REP_CLIENT = 1;
    public const int DB_REP_ELECTION = 2;
    public const int DB_REP_FULL_ELECTION = 3;
    public const int DB_REP_MASTER = 4;

    /* Flag value for repmgr_add_remote_site. */
    public const int DB_REPMGR_PEER = 0x01;

    /* Acknowledgement policies. */
    public const int DB_REPMGR_ACKS_ALL = 1;
    public const int DB_REPMGR_ACKS_ALL_PEERS = 2;
    public const int DB_REPMGR_ACKS_NONE = 3;
    public const int DB_REPMGR_ACKS_ONE = 4;
    public const int DB_REPMGR_ACKS_ONE_PEER = 5;
    public const int DB_REPMGR_ACKS_QUORUM = 6;

    /* Replication Manager site status. */
    public const int DB_REPMGR_CONNECTED = 0x01;
    public const int DB_REPMGR_DISCONNECTED = 0x02;

    #endregion

    #region Mutex Flags

    public const int DB_MUTEX_PROCESS_ONLY = 0x08;    /* Mutex private to a process. */
    public const int DB_MUTEX_SELF_BLOCK = 0x10;      /* Must be able to block self. */

    #endregion
  }

#endif

  public enum DbType: int
  {
    BTree = 1,
    Hash = 2,
    Recno = 3,
    Queue = 4,
    Unknown = 5      /* Figure it out on open. */
  }

  [Flags]
  public enum DbFlags: int
  {
    None = 0,
    ChkSum = DbConst.DB_CHKSUM,
    Encrypt = DbConst.DB_ENCRYPT,
    TxnNotDurable = DbConst.DB_TXN_NOT_DURABLE,
    Dup = DbConst.DB_DUP,
    DupSort = DbConst.DB_DUPSORT,
    RecNum = DbConst.DB_RECNUM,
    RevSplitOff = DbConst.DB_REVSPLITOFF,
    InOrder = DbConst.DB_INORDER,
    Renumber = DbConst.DB_RENUMBER,
    Snapshot = DbConst.DB_SNAPSHOT
  }

  [Flags]
  public enum StatFlags: int
  {
    None = 0,
    Clear = DbConst.DB_STAT_CLEAR
  }

  [Flags]
  public enum StatPrintFlags: int
  {
    None = 0,
    All = DbConst.DB_STAT_ALL,
    Clear = DbConst.DB_STAT_CLEAR,
  }

  public enum EncryptMode: int
  {
    None = 0,
    Encrypt_AES = DbConst.DB_ENCRYPT_AES
  }

  public enum CallbackStatus
  {
    Success = DbRetVal.SUCCESS,
    Failure
  }

  /// <summary>Represents key/data pairs for enumerating over cursors or bulk records.</summary>
  /// <remarks>It's actually a triple.</remarks>
  public class KeyDataPair
  {
    public DbEntry Key;
    public DbEntry PKey;
    public DbEntry Data;
  }

  /// <summary>Indicates if <c>Dispose()</c> or <c>Close()</c> should be called.</summary>
  public enum Usage
  {
    Close = 0,
    KeepOpen
  }

  public enum ReadStatus: int
  {
    Success = DbRetVal.SUCCESS,
    NotFound = DbRetVal.NOTFOUND,
    KeyEmpty = DbRetVal.KEYEMPTY,
    BufferSmall = DbRetVal.BUFFER_SMALL
  }

  public enum WriteStatus: int
  {
    Success = DbRetVal.SUCCESS,
    NotFound = DbRetVal.NOTFOUND,
    KeyExist = DbRetVal.KEYEXIST
  }

  public enum DeleteStatus: int
  {
    Success = DbRetVal.SUCCESS,
    NotFound = DbRetVal.NOTFOUND,
    KeyEmpty = DbRetVal.KEYEMPTY
  }

  public enum TimeoutKind: int
  {
    LockTimeout = DbConst.DB_SET_LOCK_TIMEOUT,
    TxnTimeout = DbConst.DB_SET_TXN_TIMEOUT
  }

  public struct DataSize
  {
    internal UInt32 gigaBytes;
    internal UInt32 bytes;

    public DataSize(int gigaBytes, int bytes) {
      this.gigaBytes = unchecked((UInt32)gigaBytes);
      this.bytes = unchecked((UInt32)bytes);
    }

    public int GigaBytes {
      get { return unchecked((int)gigaBytes); }
    }

    public int Bytes {
      get { return unchecked((int)bytes); }
    }
  }

  public struct CacheSize
  {
    internal UInt32 gigaBytes;
    internal UInt32 bytes;
    internal int numCaches;

    internal CacheSize(UInt32 gigaBytes, UInt32 bytes, int ncaches) {
      this.gigaBytes = gigaBytes;
      this.bytes = bytes;
      this.numCaches = ncaches;
    }

    public CacheSize(int gigaBytes, int bytes, int ncaches) {
      this.gigaBytes = unchecked((UInt32)gigaBytes);
      this.bytes = unchecked((UInt32)bytes);
      this.numCaches = ncaches;
    }

    public int GigaBytes {
      get { return unchecked((int)gigaBytes); }
    }

    public int Bytes {
      get { return unchecked((int)bytes); }
    }

    public int NumCaches {
      get { return numCaches; }
    }
  }

  /// <summary>CLS compliant wrapper for <c>DB_LSN</c>.</summary>
  public struct Lsn
  {
    internal DB_LSN lsn;

    public static unsafe int Compare(Lsn lsn0, Lsn lsn1) {
      return LibDb.log_compare(&lsn0.lsn, &lsn1.lsn);
    }

    public Lsn(int file, int offset) {
      lsn.file = unchecked((UInt32)file);
      lsn.offset = unchecked((UInt32)offset);
    }

    [CLSCompliant(false)]
    public Lsn(DB_LSN lsn) {
      this.lsn = lsn;
    }

    /* File ID. */
    public int File {
      get { return unchecked((int)lsn.file); }
    }

    /* File offset. */
    public int Offset {
      get { return unchecked((int)lsn.offset); }
    }
  }
}
