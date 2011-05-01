/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Runtime.InteropServices;

namespace BerkeleyDb
{
  /// <summary> Represents a shared memory pool (cache) file. </summary>
  /// <remarks>The wrapped <see cref="DB_MPOOLFILE"></see> handle is *not* free-threaded.</remarks>
  public unsafe abstract class CacheFile
  {
    #region Hash Code

    static Random rand = new Random();

    static int GetNextHashCode() {
      lock (rand) {
        return rand.Next();
      }
    }

    int hashCode = GetNextHashCode();

    public override int GetHashCode() {
      return hashCode;
    }

    #endregion

    #region Unmanaged Resources

    protected internal readonly object rscLock;

    // access to properly aligned types of size "native int" is atomic!
    internal volatile DB_MPOOLFILE* mpf = null;

    #endregion

    #region Construction, Finalization

    // must not pass null
    protected CacheFile(object rscLock) {
      this.rscLock = rscLock;
    }

    // not abstract, because CLS-compliant languages would not be able to inherit from this class
    [CLSCompliant(false)]
    protected internal virtual DB_MPOOLFILE* CheckDisposed() {
      return null;
    }

    public abstract bool IsDisposed { get; }

    #endregion

    #region Public Operations & Properties

    internal uint pageSize = 0;

    public CacheFileFlags GetFlags() {
      CacheFileFlags value;
      DbRetVal ret;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        ret = mpf->GetFlags(mpf, out value);
      }
      Util.CheckRetVal(ret);
      return value;
    }

    public void SetFlags(CacheFileFlags value, bool on) {
      DbRetVal ret;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        ret = mpf->SetFlags(mpf, value, on ? 1 : 0);
      }
      Util.CheckRetVal(ret);
    }

    public int PageSize {
      get { return unchecked((int)pageSize); }
    }

    public DataSize MaxSize {
      get {
        DataSize value;
        DbRetVal ret;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->GetMaxSize(mpf, out value.gigaBytes, out value.bytes);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->SetMaxSize(mpf, value.gigaBytes, value.bytes);
        }
        Util.CheckRetVal(ret);
      }
    }

    public CacheFilePriority Priority {
      get {
        CacheFilePriority value;
        DbRetVal ret;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->GetPriority(mpf, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->SetPriority(mpf, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    #endregion
  }

  // TODO Maybe a CacheFile should always register itself as cookie for the call-backs
  //      to be able to determine the page size

  /// <summary>Represents the shared memory pool file underlying a Berkeley DB database.</summary>
  public class DbCacheFile: CacheFile
  {
    protected Db db;

    // we lock on the Db object
    internal DbCacheFile(Db db): base(db.rscLock) {
      this.db = db;
    }

    [CLSCompliant(false)]
    protected sealed internal override unsafe DB_MPOOLFILE* CheckDisposed() {
      db.CheckDisposed();
      return mpf;
    }

    public sealed override bool IsDisposed {
      get { return db.IsDisposed; }
    }
  }

#if BDB_FULL_MPOOL_API
  /// <summary>Represents an independent shared memory pool file.</summary>
  public unsafe class EnvCacheFile: CacheFile, IDisposable
  {
    protected Env env;

  #region Unmanaged Resources

    DbRetVal releaseVal;

    // should be run in a CER, under a lock on rscLock, and not throw exceptions
    DbRetVal ReleaseUnmanagedResources() {
      // don't touch the handle if this instance is owned by a database
      if (env == null)
        return DbRetVal.SUCCESS;
      DB_MPOOLFILE* mpf = this.mpf;
      if (mpf == null)
        return DbRetVal.SUCCESS;
      // DB_MPOOLFILE->Close() could be a lengthy call, so we call Disposed() first, and the
      // CER ensures that we reach DB_MPOOLFILE->Close() without external interruption.
      // This is OK because one must not use the handle after DB_MPOOLFILE->Close() was called,
      // regardless of the return value.
      Disposed();
      DbRetVal ret = mpf->Close(mpf, 0);
      return ret;
    }

    #endregion

  #region Construction, Finalization

    internal EnvCacheFile(Env env): base(new object()) {
      this.env = env;
    }

    public const string disposedStr = "Memory pool file handle closed.";

    [CLSCompliant(false)]
    protected sealed internal override DB_MPOOLFILE* CheckDisposed() {
      // avoid multiple volatile memory access
      DB_MPOOLFILE* mpf = this.mpf;
      if (mpf == null)
        throw new ObjectDisposedException(disposedStr);
      return mpf;
    }

    // when overriding, call base method at end (using finally clause)
    protected internal virtual void Dispose(bool disposing) {
      lock (rscLock) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          releaseVal = ReleaseUnmanagedResources();
        }
      }
    }

    // does not check for mpf == null!
    void Disposed() {
      mpf = null;
      if (env != null)
        env.RemoveCacheFile(this);
    }

    public sealed override bool IsDisposed {
      get { return mpf == null; }
    }

    public DbRetVal ReleaseVal {
      get { return releaseVal; }
    }

    #endregion

  #region IDisposable Members

    public void Dispose() {
      lock (env.rscLock) {
        Dispose(true);
      }
      GC.SuppressFinalize(this);
    }

    #endregion

  #region Public Operations & Properties

    public void Close() {
      Dispose();
    }

    public void Open(string fileName, int pageSize, CacheFileOpenFlags flags, int mode) {
      DbRetVal ret;
      byte[] fileBytes = null;
      Util.StrToUtf8(fileName, ref fileBytes);
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        fixed (byte* filep = fileBytes) {
          ret = mpf->Open(mpf, filep, flags, mode, unchecked((uint)pageSize));
        }
      }
      Util.CheckRetVal(ret);
      this.pageSize = unchecked((uint)pageSize);
    }

#if BDB_4_3_29
    IntPtr GetPage(ref uint pageNo, CachePageGetFlags flags) {
      DbRetVal ret;
      void* page;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        ret = mpf->Get(mpf, ref pageNo, flags, out page);
      }
      Util.CheckRetVal(ret);
      return (IntPtr)page;
    }

    public CachePage GetPage(int pageNo, bool create) {
      uint pgNo = unchecked((uint)pageNo);
      IntPtr data = GetPage(ref pgNo, create ? CachePageGetFlags.Create : CachePageGetFlags.None); 
      return new CachePage(pgNo, data);
    }

    public CachePage GetNewPage() {
      uint pgNo = 0;
      IntPtr data = GetPage(ref pgNo, CachePageGetFlags.New);
      return new CachePage(pgNo, data);
    }

    public CachePage GetLastPage() {
      uint pgNo = 0;
      IntPtr data = GetPage(ref pgNo, CachePageGetFlags.Last);
      return new CachePage(pgNo, data);
    }
#endif

#if BDB_4_5_20
    void* GetPage(ref uint pageNo, DB_TXN* txp, CachePageGetFlags flags) {
      DbRetVal ret;
      void* page;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        ret = mpf->Get(mpf, ref pageNo, txp, flags, out page);
      }
      Util.CheckRetVal(ret);
      return page;
    }

    IntPtr GetPage(ref uint pageNo, Txn txn, CachePageGetFlags flags) {
      if (txn != null) {
        lock (txn.rscLock) {
          DB_TXN* txp = txn.CheckDisposed();
          return (IntPtr)GetPage(ref pageNo, txp, flags);
        }
      }
      else
        return (IntPtr)GetPage(ref pageNo, (DB_TXN*)null, flags);
    }

    public CachePage GetPage(int pageNo, Txn txn, bool create) {
      uint pgNo = unchecked((uint)pageNo);
      IntPtr data = GetPage(ref pgNo, txn, create ? CachePageGetFlags.Create : CachePageGetFlags.None); 
      return new CachePage(pgNo, data);
    }

    public CachePage GetNewPage(Txn txn) {
      uint pgNo = 0;
      IntPtr data = GetPage(ref pgNo, txn, CachePageGetFlags.New);
      return new CachePage(pgNo, data);
    }

    public CachePage GetLastPage(Txn txn) {
      uint pgNo = 0;
      IntPtr data = GetPage(ref pgNo, txn, CachePageGetFlags.Last);
      return new CachePage(pgNo, data);
    }
#endif

    public void PutPage(CachePage page, CachePagePutFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        ret = mpf->Put(mpf, (void*)page.data, flags);
      }
      Util.CheckRetVal(ret);
    }

    public void SetPageFlags(CachePage page, CachePagePutFlags flags) {
      DbRetVal ret;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        ret = mpf->Set(mpf, (void*)page.data, flags);
      }
      Util.CheckRetVal(ret);
    }

    public void Sync() {
      DbRetVal ret;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        ret = mpf->Sync(mpf);
      }
      Util.CheckRetVal(ret);
    }

    public int ClearLength {
      get {
        DbRetVal ret;
        uint value;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->GetClearLen(mpf, out value);
        }
        Util.CheckRetVal(ret);
        return unchecked((int)value);
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->SetClearLen(mpf, unchecked((uint)value));
        }
        Util.CheckRetVal(ret);
      }
    }

    public byte[] FileId {
      get {
        DbRetVal ret;
        byte[] value = new byte[DbConst.DB_FILE_ID_LEN];
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          fixed (byte* fidp = value) {
            ret = mpf->GetFileId(mpf, fidp);
          }
        }
        Util.CheckRetVal(ret);
        return value;
      }

      set {
        if (value.Length <= DbConst.DB_FILE_ID_LEN)
          throw new BdbException("File id length too short.");
        DbRetVal ret;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          fixed (byte* fidp = value) {
            ret = mpf->SetFileId(mpf, fidp);
          }
        }
        Util.CheckRetVal(ret);
      }
    }

    public int FileType {
      get {
        DbRetVal ret;
        int value;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->GetFileType(mpf, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->SetFileType(mpf, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    public int LsnOffset {
      get {
        DbRetVal ret;
        int value;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->GetLsnOffset(mpf, out value);
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->SetLsnOffset(mpf, value);
        }
        Util.CheckRetVal(ret);
      }
    }

    /// <summary>Page cookie for page-in and page-out call-backs.</summary>
    /// <remarks>We store a page cookie as IntPtr, which is more efficient than
    /// copying a byte array through passing a <see cref="DbEntry"/> instance.</remarks>
    public IntPtr PageCookie {
      get {
        DbRetVal ret;
        IntPtr value = IntPtr.Zero;
        DBT cookieDbt;
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->GetPageCookie(mpf, out cookieDbt);
          if (ret == DbRetVal.SUCCESS) {
            if (cookieDbt.size == sizeof(IntPtr))
              value = *(IntPtr*)cookieDbt.data;
            else if (cookieDbt.size != 0)
              throw new BdbException("Invalid page cookie.");
          }
        }
        Util.CheckRetVal(ret);
        return value;
      }
      set {
        DbRetVal ret;
        DBT cookieDbt = new DBT();
        cookieDbt.data = &value;
        cookieDbt.size = unchecked((uint)sizeof(IntPtr));
        lock (rscLock) {
          DB_MPOOLFILE* mpf = CheckDisposed();
          ret = mpf->SetPageCookie(mpf, ref cookieDbt);
        }
        Util.CheckRetVal(ret);
      }
    }

#if false  // for alternate definition of page cookie
    public void GetPageCookie(out DbEntry cookie) {
      DbRetVal ret;
      DBT cookieDbt;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        ret = mpf->GetPageCookie(mpf, out cookieDbt);
      }
      Util.CheckRetVal(ret);
      if (cookieDbt.size != 0) {
        byte[] buffer = new byte[cookieDbt.size];
        Marshal.Copy((IntPtr)cookieDbt.data, buffer, 0, unchecked((int)cookieDbt.size));
        cookie = DbEntry.Out(buffer);
      }
      else
        cookie = new DbEntry();
    }
    
    public void SetPageCookie(ref DbEntry cookie) {
      DbRetVal ret;
      lock (rscLock) {
        DB_MPOOLFILE* mpf = CheckDisposed();
        fixed (byte* cookieBufP = cookie.Buffer) {
          cookie.dbt.data = cookieBufP + cookie.Start;
          ret = mpf->SetPageCookie(mpf, ref cookie.dbt);
        }
      }
      Util.CheckRetVal(ret);
    }
#endif

    #endregion
  }
#endif

  [Flags]
  public enum CacheStatPrintFlags: int
  {
    None = 0,
    All = DbConst.DB_STAT_ALL,
    Clear = DbConst.DB_STAT_CLEAR,
    HashBuckets = DbConst.DB_STAT_MEMP_HASH
  }

#if BDB_FULL_MPOOL_API
  /// <summary>Wrapper for accessing cache page data.</summary>
  /// <remarks>There is not protection against invalid data pointers.
  /// That is, it is the applications responsibility to know when the
  /// underlying <see cref="CacheFile"/> has been closed, and to avoid
  /// accessing any of its pages after that.</remarks>
  public struct CachePage
  {
    internal IntPtr data;
    uint pageNo;

    internal CachePage(uint pageNo, IntPtr data) {
      this.pageNo = pageNo;
      this.data = data;
    }

    public int PageNo {
      get { return unchecked((int)pageNo); }
    }

    /// <summary>Data pointer.</summary>
    /// <remarks>Use the methods of the <see cref="Marshal"/> class for accessing it.
    /// It is the applications responsibility to know the size of the buffer.</remarks>
    public IntPtr Data {
      get { return data; }
    }

#if false
    /// <summary>Checks if data pointer and access range are valid.</summary>
    /// <remarks>Does not protect against invalid data pointers that have become 
    /// stale because the underlying <see cref="CacheFile"/> was closed.</remarks>
    /// <param name="pageIndx">Start index in data buffer to read from or write to.</param>
    /// <param name="length">Length of data (in bytes) to read or write.</param>
    public void CheckAccess(int pageIndx, int length) {
      if (data == IntPtr.Zero)
        throw new NullReferenceException("Cache page invalid.");
      if ((pageIndx + length) > size || pageIndx < 0)
        throw new IndexOutOfRangeException("Cache page index out of range.");
    }

    public void Read(byte[] dest, int destStart, int pageIndx, int length) {
      CheckAccess(pageIndx, length);
      Marshal.Copy((IntPtr)((long)data + pageIndx), dest, destStart, length);
    }

    public void Write(byte[] src, int srcStart, int pageIndx, int length) {
      CheckAccess(pageIndx, length);
      Marshal.Copy(src, srcStart, (IntPtr)((long)data + pageIndx), length);
    }
#endif
  }
#endif

  // CLS compliant wrapper for DB_MPOOL_STAT
  public struct CacheStats
  {
    internal DB_MPOOL_STAT cStats;

    /* Total cache size. */
    public CacheSize CacheSize {
      get { return new CacheSize(cStats.st_gbytes, cStats.st_bytes, unchecked((int)cStats.st_ncache)); }
    }

    /* Region size. (typedef uintptr_t roff_t;) */
    public long RegionSize {
      get { return cStats.st_regsize.ToInt64(); }
    }

    /* Maximum file size for mmap. */
    public int MMapSize {
      get { return unchecked((int)cStats.st_mmapsize); }
    }

    /* Maximum number of open fd's. */
    public int MaxOpenFd {
      get { return cStats.st_maxopenfd; }
    }

    /* Maximum buffers to write. */
    public int MaxWrites {
      get { return cStats.st_maxwrite; }
    }

    /* Sleep after writing max buffers. */
    public int MaxWriteSleep {
      get { return cStats.st_maxwrite_sleep; }
    }

    /* Pages from mapped files. */
    public int MappedPages {
      get { return unchecked((int)cStats.st_map); }
    }

    /* Pages found in the cache. */
    public int CacheHits {
      get { return unchecked((int)cStats.st_cache_hit); }
    }

    /* Pages not found in the cache. */
    public int CacheMisses {
      get { return unchecked((int)cStats.st_cache_miss); }
    }

    /* Pages created in the cache. */
    public int PagesCreated {
      get { return unchecked((int)cStats.st_page_create); }
    }

    /* Pages read in. */
    public int PagesIn {
      get { return unchecked((int)cStats.st_page_in); }
    }

    /* Pages written out. */
    public int PagesOut {
      get { return unchecked((int)cStats.st_page_out); }
    }

    /* Clean pages forced from the cache. */
    public int CleanPagesEvicted {
      get { return unchecked((int)cStats.st_ro_evict); }
    }

    /* Dirty pages forced from the cache. */
    public int DirtyPagesEvicted {
      get { return unchecked((int)cStats.st_rw_evict); }
    }

    /* Pages written by memp_trickle. */
    public int PagesTrickled {
      get { return unchecked((int)cStats.st_page_trickle); }
    }

    /* Total number of pages. */
    public int Pages {
      get { return unchecked((int)cStats.st_pages); }
    }

    /* Clean pages. */
    public int CleanPages {
      get { return unchecked((int)cStats.st_page_clean); }
    }

    /* Dirty pages. */
    public int DirtyPages {
      get { return unchecked((int)cStats.st_page_dirty); }
    }

    /* Number of hash buckets. */
    public int HashBuckets {
      get { return unchecked((int)cStats.st_hash_buckets); }
    }

    /* Total hash chain searches. */
    public int HashSearches {
      get { return unchecked((int)cStats.st_hash_searches); }
    }

    /* Longest hash chain searched. */
    public int HashLongest {
      get { return unchecked((int)cStats.st_hash_longest); }
    }

    /* Total hash entries searched. */
    public int HashExamined {
      get { return unchecked((int)cStats.st_hash_examined); }
    }

    /* Hash lock granted with nowait. */
    public int HashNowaits {
      get { return unchecked((int)cStats.st_hash_nowait); }
    }

    /* Hash lock granted after wait. */
    public int HashWaits {
      get { return unchecked((int)cStats.st_hash_wait); }
    }

#if BDB_4_5_20
    /* Max hash lock granted with nowait. */
    public int HashMaxNoWaits {
      get { return unchecked((int)cStats.st_hash_max_nowait); }
    }
#endif

    /* Max hash lock granted after wait. */
    public int HashMaxWaits {
      get { return unchecked((int)cStats.st_hash_max_wait); }
    }

    /* Region lock granted with nowait. */
    public int RegionNowaits {
      get { return unchecked((int)cStats.st_region_nowait); }
    }

    /* Region lock granted after wait. */
    public int RegionWaits {
      get { return unchecked((int)cStats.st_region_wait); }
    }

#if BDB_4_5_20
    /* Buffers frozen. */
    public int MvccFrozenBuffers {
      get { return unchecked((int)cStats.st_mvcc_frozen); }
    }

    /* Buffers thawed. */
    public int MvccThawedBuffers {
      get { return unchecked((int)cStats.st_mvcc_thawed); }
    }

    /* Frozen buffers freed. */
    public int MvccFreedBuffers {
      get { return unchecked((int)cStats.st_mvcc_freed); }
    }
#endif

    /* Number of page allocations. */
    public int PageAllocs {
      get { return unchecked((int)cStats.st_alloc); }
    }

    /* Buckets checked during allocation. */
    public int AllocBuckets {
      get { return unchecked((int)cStats.st_alloc_buckets); }
    }

    /* Max checked during allocation. */
    public int AllocMaxBuckets {
      get { return unchecked((int)cStats.st_alloc_max_buckets); }
    }

    /* Pages checked during allocation. */
    public int AllocPages {
      get { return unchecked((int)cStats.st_alloc_pages); }
    }

    /* Max checked during allocation. */
    public int AllocMaxPages {
      get { return unchecked((int)cStats.st_alloc_max_pages); }
    }

#if BDB_4_5_20
    /* Thread waited on buffer I/O. */
    public int IoWaits {
      get { return unchecked((int)cStats.st_io_wait); }
    }
#endif
  }

  // CLS compliant wrapper for DB_MPOOL_FSTAT
  public struct CacheFileStats
  {
    internal DB_MPOOL_FSTAT cfStats;
    private string fileName;

    internal unsafe CacheFileStats(DB_MPOOL_FSTAT* csp) {
      cfStats = *csp;
      fileName = Util.Utf8PtrToString(cfStats.file_name);
    }

    /* File name. */
    public string FileName {
      get { return fileName; }
    }

    /* Page size. */
    public int PageSize {
      get { return unchecked((int)cfStats.st_pagesize); }
    }

    /* Pages from mapped files. */
    public int PagesMapped {
      get { return unchecked((int)cfStats.st_map); }
    }

    /* Pages found in the cache. */
    public int CacheHits {
      get { return unchecked((int)cfStats.st_cache_hit); }
    }

    /* Pages not found in the cache. */
    public int CacheMisses {
      get { return unchecked((int)cfStats.st_cache_miss); }
    }

    /* Pages created in the cache. */
    public int PagesCreated {
      get { return unchecked((int)cfStats.st_page_create); }
    }

    /* Pages read in. */
    public int PagesIn {
      get { return unchecked((int)cfStats.st_page_in); }
    }

    /* Pages written out. */
    public int PagesOut {
      get { return unchecked((int)cfStats.st_page_out); }
    }
  }
}
