/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace BerkeleyDb
{
  public static unsafe class Util
  {
    // keep in sync with user defined codes in DbRetVal
    static string[] dotNetStr = {
      "Key generator callback failed.",
      "Append record number callback failed.",
      "Duplicate comparison callback failed.",
      "BTree key comparison callback failed.",
      "BTree prefix comparison callback failed.",
      "Hash function callback failed.",
      "Feedback callback failed.",
      "Panic callback failed.",
      "Application recovery callback failed.",
      "Verify callback failed.",
      "Replication send callback failed.",
      "Cache page-in callback failed.",
      "Cache page-out callback failed.",
      "Event notification failed.",
      "IsAlive callback failed.",
      "ThreadId callback failed.",
      "ThreadIdString callback failed."
    };

    public static string DotNetStr(DbRetVal ret) {
      if (ret > DotNetLowError && ret <= (DotNetLowError + dotNetStr.Length))
        return dotNetStr[(int)ret - (int)DotNetLowError - 1];
      else
        return UnknownStr(ret);
    }

    public static string UnknownStr(DbRetVal ret) {
      return string.Format("Unknown error code: {0}.", ret);
    }

    public const DbRetVal BdbLowError = (DbRetVal)(-31000);
    public const DbRetVal DotNetLowError = (DbRetVal)(-41000);

    public static void CheckRetVal(DbRetVal ret) {
      if (ret != DbRetVal.SUCCESS) {
        string errStr;
        if (ret > BdbLowError)
          errStr = LibDb.db_strerror(ret);
        else if (ret > DotNetLowError)
          errStr = DotNetStr(ret);
        else
          errStr = UnknownStr(ret);
        throw new BdbException(ret, errStr);
      }
    }

    // calculates length of null-terminated byte string
    [CLSCompliant(false)]
    public static int ByteStrLen(byte* str) {
      byte* tmp = str;
      int len = 0;
      if (tmp != null) {
        while (*tmp != 0) tmp++;
        len = (int)(tmp - str);
      }
      return len;
    }

    // Copies null-terminated byte string into byte buffer.
    // Will resize buffer, if necessary, does nothing if ptr == null.
    [CLSCompliant(false)]
    public static int PtrToBuffer(byte* ptr, ref byte[] buffer) {
      int size = ByteStrLen(ptr);
      if (size > (buffer == null ? 0 : buffer.Length))
        Array.Resize<byte>(ref buffer, size);
      if (ptr != null)
        Marshal.Copy((IntPtr)ptr, buffer, 0, size);
      return size;
    }

    // Copies buffer to new byte string allocated on LibDb (external) heap.
    // If it represents a text string, then the buffer must already include the null terminator.
    // If str != null, then it must have been allocated by LibDb before.
    // This string must be *explicitly* deallocated after use, calling LibDb.FreeMem.
    [CLSCompliant(false)]
    public static void BufferToPtr(byte[] buffer, int start, int length, ref byte* ptr) {
      void* mem = ptr;
      // we pass null for the environment, as we don't want error call-backs
      // DbRetVal ret = LibDb.ReallocMem(null, (uint)length, ref mem);  // not exported
      if (ptr != null)
        LibDb.os_ufree(null, mem);
      DbRetVal ret;
      RuntimeHelpers.PrepareConstrainedRegions();
      try { }
      finally {
        ret = LibDb.os_umalloc(null, (uint)length, out mem);
        if (ret == DbRetVal.SUCCESS)
          ptr = (byte*)mem;
      }
      Util.CheckRetVal(ret);
      Marshal.Copy(buffer, start, (IntPtr)ptr, length);
    }

    // Creates null-terminated UTF8 string in buffer.
    // Returns length, including null-terminator.
    // Will re-allocate buffer, if necessary.
    public static int StrToUtf8(string str, UTF8Encoding utf8, ref byte[] buffer) {
      if (str == null)
        return 0;
      int count = utf8.GetMaxByteCount(str.Length);
      // increment to next 32 bit boundary, that is, by at least one and at most 4 bytes
      count = ((count >> 2) + 1) << 2;
      if (count > (buffer == null ? 0 : buffer.Length))
        Array.Resize<byte>(ref buffer, count);
      count = utf8.GetBytes(str, 0, str.Length, buffer, 0);
      // add null terminator - we have space for at least one more byte
      buffer[count] = 0;
      return count + 1;
    }

    // if str == null, then the buffer argument will be ignored
    public static int StrToUtf8(string str, ref byte[] buffer) {
      UTF8Encoding utf8 = new UTF8Encoding();
      return StrToUtf8(str, utf8, ref buffer);
    }

    // Converts null-terminated UTF-8 byte string to .NET string .
    // Will re-allocate buffer if necessary, buffer can be null.
    [CLSCompliant(false)]
    public static string Utf8PtrToString(byte* ptr, ref byte[] buffer) {
      int count = PtrToBuffer(ptr, ref buffer);
      if (count > 0)
        return new UTF8Encoding().GetString(buffer, 0, count);
      else
        return string.Empty;
    }

    [CLSCompliant(false)]
    public static string Utf8PtrToString(byte* ptr) {
      byte[] buffer = null;
      return Utf8PtrToString(ptr, ref buffer);
    }

    [CLSCompliant(false)]
    public static Db GetDb(DB* dbp) {
      if (dbp == null)
        return null;
      else {
        IntPtr gh = dbp->api_internal;
        if (gh == IntPtr.Zero)
          return null;
        else
          return (Db)((GCHandle)gh).Target;
      }
    }

    [CLSCompliant(false)]
    public static Env GetEnv(DB_ENV* evp) {
      if (evp == null)
        return null;
      else {
        IntPtr gh = evp->api_internal;
        if (gh == IntPtr.Zero)
          return null;
        else
          return (Env)((GCHandle)gh).Target;
      }
    }

    [CLSCompliant(false)]
    public static Txn GetTxn(DB_TXN* txp) {
      if (txp == null)
        return null;
      else {
        IntPtr gh = txp->api_internal;
        if (gh == IntPtr.Zero)
          return null;
        else
          return (Txn)((GCHandle)gh).Target;
      }
    }
  }

  /** <summary>Set-like container for storing unordered items.</summary> */
  public class Set<T>: ICollection<T>
  {
    private T[] items;
    private SlotStatus[] slots;
    private byte sizeLog;
    private bool autoShrink;
    private int count;
    private int usedCount;
    private IEqualityComparer<T> comparer;

    private const int initSizeLog = 6;

    /// <summary>Internal array where items are stored.</summary>
    protected T[] Items {
      get { return items; }
    }

    protected SlotStatus[] Slots {
      get { return slots; }
    }

    #region Construction

    /// <summary>Initializes new <see cref="Set&lt;T>"/> instance.</summary>
    /// <param name="sizeLog">Base 2 logarithm of the initial capacity.</param>
    /// <param name="comparer">Custom equality comparer.</param>
    public Set(byte sizeLog, IEqualityComparer<T> comparer) {
      items = new T[0];
      slots = new SlotStatus[0];
      count = 0;
      usedCount = 0;
      autoShrink = true;
      this.comparer = comparer;
      SetSizeLog(sizeLog);
    }

    public Set() : this(initSizeLog, null) { }

    public Set(byte sizeLog) : this(sizeLog, null) { }

    /// <summary>Creates a shallow copy of the instance.</summary>
    /// <returns>New cloned instance.</returns>
    public Set<T> Clone() {
      byte newSizeLog = initSizeLog;
      int newSize = (int)1 << initSizeLog;
      int threshold = count << 1;
      while (newSize < threshold) {
        newSize = newSize << 1;
        newSizeLog++;
      }
      Set<T> result = new Set<T>(newSizeLog, comparer);
      result.autoShrink = autoShrink;
      result.Union(this);
      return result;
    }

    #endregion

    #region Helpers

    /// <summary>Sets capacity of hash table as power of 2.</summary>
    /// <param name="value">Base 2 logarithm of new size of hash table.</param>
    protected void SetSizeLog(byte value) {
      int indx;
      T[] newItems;
      SlotStatus[] newSlots;
      int newSize;
      uint mask;

      newSize = (int)1 << value;
      if ((value < 2) || (newSize < (count << 1))) {
        string msg = "Size too small: {0}.";  // Resources.GetString(RsId.SizeTooSmall);
        throw new ArgumentException(String.Format(msg, value), "value");
      }

      mask = unchecked((uint)(newSize - 1));
      newItems = new T[newSize];
      newSlots = new SlotStatus[newSize];

      // re-hash: loop through existing items and insert them into new array
      for (indx = slots.Length - 1; indx >= 0; indx--) {
        if (slots[indx] == SlotStatus.Filled) {
          T item = items[indx];
          int keyHash = comparer == null ? item.GetHashCode() : comparer.GetHashCode(item);
          int newIndx = unchecked(keyHash & (int)mask);
          // don't have to check for deleted items in new slots array
          if (newSlots[newIndx] != SlotStatus.Empty)
            unchecked {
              // see comments to step in FindSlot()
              int step = ((((keyHash & (int)~mask) >> (value - 1)) &
                           (int)(mask >> 2)) | (int)1) & (int)0xFF;
              do {
                newIndx = newIndx - step;
                if (newIndx < 0) 
                  newIndx = newIndx + newSize;
              } while (newSlots[newIndx] != SlotStatus.Empty);
            }
          newItems[newIndx] = item;
          newSlots[newIndx] = SlotStatus.Filled;
        }
      }
      items = newItems;
      slots = newSlots;
      sizeLog = value;
      usedCount = count;
    }

    /// <summary>Increments item count, increasing table capacity as needed.</summary>
    protected void IncCount(bool emptySlot) {
      count++;
      if (count >= ((int)1 << (sizeLog - 1)))
        SetSizeLog((byte)(sizeLog + 1));
      else if (emptySlot)
        usedCount++;
    }

    /// <summary>Decrements item count, shrinking - <see cref="AutoShrink"/> - or
    /// rebuilding hash table as needed.</summary>
    protected void DecCount(int delta) {
      count -= delta;
      int limit = (int)1 << (sizeLog - 1);
      if (autoShrink && count < (limit >> 1) && sizeLog > 3)
        SetSizeLog((byte)(sizeLog - 1));
      else if (usedCount > limit)
        Rebuild();
    }

    /// <summary>Finds slot in hash table that matches a given key.</summary>
    /// <remarks>This method always finds a slot. The return value differentiates
    /// between unused and occupied slots, making it possible to use it for both,
    /// insertions of new items, and searches for existing ones.</remarks>
    /// <param name="key">Key to find.</param>
    /// <param name="indx">Index of matching slot to be returned.</param>
    /// <returns><c>true</c> if slot occupied, <c>false</c> if slot still unused.</returns>
    protected bool FindSlot(T key, out int indx) {
      int delIndx, startIndx;
      SlotStatus slot;
      int keyHash;
      uint mask;
      byte log;

      keyHash = comparer == null ? key.GetHashCode() : comparer.GetHashCode(key);
      log = sizeLog;
      mask = ((uint)1 << log) - 1;  // topmost bit is never set!
      indx = unchecked(keyHash & (int)mask);
      slot = slots[indx];

      // if slot is empty, then we can return right away
      if (slot == SlotStatus.Empty)
        return false;

      // otherwise, scan for an empty, deleted or duplicate slot
      delIndx = -1;
      startIndx = indx;
      /* For probing (after a collision) we need a step size relative prime
       * to the hash table size, which is a power of 2. We use double-hashing,
       * since we can calculate a second hash value cheaply by taking those bits
       * of the first hash value that were discarded (masked out) when the table
       * index was calculated: index = hash & mask, where mask = table-size-1.
       * The maximum step size should fit into a byte and be less than table-size/4.
       * It must be an odd number, since that is relative prime to a power of 2.
       */
      int step = unchecked (((((keyHash & (int)~mask) >> (log - 1)) &
                              (int)(mask >> 2)) | (int)1) & (int)0xFF);
      do {
        if (slot == SlotStatus.Deleted) {
          // remember first deleted position
          if (delIndx < 0) delIndx = indx;
        }
        else {
          T item = items[indx];
          if (comparer == null) {
            if ((key != null && key.Equals(item)) || (key == null && item == null))
              return true;
          }
          else if (comparer.Equals(key, item))
            return true;
        }
        // no match, continue
        unchecked {
          // since step is relative prime to slots.Length this will iterate over all slots
          indx = indx - step;
          if (indx < 0)
            indx = indx + (int)mask + 1;
          // if we can't find an empty slot then we have used RemoveNext() too
          // often without calling Rebuild() - let's return the first deleted slot
          if (indx == startIndx)
            break;
          slot = slots[indx];
        }
      // due to forced rebuilds (see IncCount(), DecCount()) we should always have empty slots
      } while (slot != SlotStatus.Empty);

      if (delIndx >= 0) indx = delIndx;
      return false;
    }

    #endregion

    #region Public Interface & Properties

    /// <summary>Set equality represents value semantics.</summary>
    /// <remarks>Two sets must have the same members to be considered equal.</remarks>
    /// <param name="obj">Set to compare to.</param>
    /// <returns><c>true</c> if both sets contain the same members, <c>false</c> otherwise.</returns>
    public override bool Equals(object obj) {
      return this == obj as Set<T>;
    }

    /// <summary>The hash code is the "exclusive or" of all the elements' hash codes.</summary>
    /// <remarks>Because set equality is defined to have value semantics, the hash
    /// code of a <c>Set&lt;T></c> instance can change when it is modified!</remarks>
    /// <returns>Hash code computed from all elements.</returns>
    public override int GetHashCode() {
      int hashcode = 0;
      for (int indx = 0; indx < slots.Length; indx++)
        if (slots[indx] == SlotStatus.Filled) {
          T item = items[indx];
          if (!ReferenceEquals(item, null))
            hashcode ^= item.GetHashCode();
        }
      return hashcode;
    }

    /// <summary>Inserts a T item.</summary>
    /// <returns><c>true</c> if inserted successfully, <c>false</c> if item is already present.</returns>
    /// <param name="value">Item to be stored.</param>
    public bool Insert(T value) {
      int indx;
      bool found = FindSlot(value, out indx);
      if (found)
        return false;
      else {
        SlotStatus slot = slots[indx];
        items[indx] = value;
        slots[indx] = SlotStatus.Filled;
        IncCount(slot == SlotStatus.Empty);
        return true;
      }
    }

    /// <summary>Searches for an item in the table.</summary>
    /// <returns>Valid iterator cookie > 0 if found, -1 otherwise.</returns>
    /// <param name="value">Item to find.</param>
    public int Find(T value) {
      int indx;
      if (FindSlot(value, out indx))
        return indx;
      else
        return -1;
    }

    /// <summary>Merges this set with another, creating a "union" set.</summary>
    /// <param name="otherSet">Set to create a union set with.</param>
    public void Union(Set<T> otherSet) {
      SlotStatus[] otherSlots = otherSet.slots;
      for (int indx = 0; indx < otherSlots.Length; indx++)
        if (otherSlots[indx] == SlotStatus.Filled)
          Insert(otherSet.items[indx]);
    }

    /// <summary>Intersects this set with another.</summary>
    /// <param name="otherSet">Set to intersect with.</param>
    public void Intersect(Set<T> otherSet) {
      int delta = 0;
      for (int indx = 0; indx < slots.Length; indx++)
        if (slots[indx] == SlotStatus.Filled) {
          if (!otherSet.Contains(items[indx])) {
            items[indx] = default(T);
            slots[indx] = SlotStatus.Deleted;
            delta++;
          }
        }
      DecCount(delta);
    }

    /// <summary>Subtracts another set from this set, creating a "difference" set.</summary>
    /// <param name="otherSet">Sset to subtract.</param>
    public void Difference(Set<T> otherSet) {
      int delta = 0;
      SlotStatus[] otherSlots = otherSet.slots;
      for (int otherIndx = 0; otherIndx < otherSlots.Length; otherIndx++) {
        if (otherSlots[otherIndx] == SlotStatus.Filled) {
          T otherItem = otherSet.items[otherIndx];
          int indx;
          bool found = FindSlot(otherItem, out indx);
          if (found) {
            items[indx] = default(T);
            slots[indx] = SlotStatus.Deleted;
            delta++;
          }
        }
      }
      DecCount(delta);
    }

    /// <summary>Creates a "symmetric difference" set, that is a set whose elements
    /// are either in this or in the other set, but not in both.</summary>
    /// <remarks>Equivalent to <c>(A | B) - (A &amp; B)</c>, if <c>this == A</c> and 
    /// <c>otherSet == B</c>.</remarks>
    /// <param name="otherSet">The other set to construct the "symmetric difference" set from.</param>
    public void SymmetricDiff(Set<T> otherSet) {
      int delta = 0;
      SlotStatus[] otherSlots = otherSet.slots;
      for (int otherIndx = 0; otherIndx < otherSlots.Length; otherIndx++) {
        if (otherSlots[otherIndx] == SlotStatus.Filled) {
          T otherItem = otherSet.items[otherIndx];
          int indx;
          bool found = FindSlot(otherItem, out indx);
          if (found) {
            items[indx] = default(T);
            slots[indx] = SlotStatus.Deleted;
            delta++;
          }
          else {
            SlotStatus slot = slots[indx];
            items[indx] = otherItem;
            slots[indx] = SlotStatus.Filled;
            IncCount(slot == SlotStatus.Empty);
          }
        }
      }
      DecCount(delta);
    }

    #region Operators

    /// <summary>Union operator.</summary>
    /// <returns>The union of two sets.</returns>
    public static Set<T> operator |(Set<T> a, Set<T> b) {
      if (a.Count < b.Count) {
        Set<T> tmp = a;
        a = b;
        b = tmp;
      }
      Set<T> result = a.Clone();
      result.Union(b);
      return result;
    }

    /// <summary>Intersection operator.</summary>
    /// <returns>The intersection of two sets.</returns>
    public static Set<T> operator &(Set<T> a, Set<T> b) {
      if (a.Count > b.Count) {
        Set<T> tmp = a;
        a = b;
        b = tmp;
      }
      Set<T> result = a.Clone();
      result.AutoShrink = true;
      result.Intersect(b);
      return result;
    }

    /// <summary>Difference operator.</summary>
    /// <returns>Difference of two sets.</returns>
    public static Set<T> operator -(Set<T> a, Set<T> b) {
      Set<T> result = a.Clone();
      result.AutoShrink = true;
      result.Difference(b);
      return result;
    }

    /// <summary>Symmetric difference operator.</summary>
    /// <returns>The symmetric difference of two sets.</returns>
    public static Set<T> operator ^(Set<T> a, Set<T> b) {
      if (a.Count < b.Count) {
        Set<T> tmp = a;
        a = b;
        b = tmp;
      }
      Set<T> result = a.Clone();
      result.AutoShrink = true;
      result.SymmetricDiff(b);
      return result;
    }

    /// <summary>Subset operator.</summary>
    /// <returns>True if <c>a</c> is a subset of <c>b</c>.</returns>
    public static bool operator <=(Set<T> a, Set<T> b) {
      for (int aIndx = 0; aIndx < a.slots.Length; aIndx++)
        if (a.slots[aIndx] == SlotStatus.Filled) {
          if (!b.Contains(a.items[aIndx]))
            return false;
        }
      return true;
    }

    /// <summary>Proper Subset operator.</summary>
    /// <returns>True if <c>a</c> is a proper subset of <c>b</c>.</returns>
    public static bool operator <(Set<T> a, Set<T> b) {
      return (a.Count < b.Count) && (a <= b);
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Set<T> a, Set<T> b) {
      bool aIsNull = ReferenceEquals(a, null);
      if (aIsNull == ReferenceEquals(b, null)) {
        if (aIsNull)
          return true;
        else
          return (a.Count == b.Count) && (a <= b);
      }
      else
        return false;
    }

    /// <summary>Reversed subset operator.</summary>
    public static bool operator >(Set<T> a, Set<T> b) {
      return b < a;
    }

    /// <summary>Reversed proper subset operator.</summary>
    public static bool operator >=(Set<T> a, Set<T> b) {
      return (b <= a);
    }

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Set<T> a, Set<T> b) {
      return !(a == b);
    }

    #endregion Operators

    /// <summary>Removes deleted entries to reclaim lost performance.</summary>
    public void Rebuild() {
      int indx, tempIndx;
      uint mask = ((uint)1 << sizeLog) - 1;
      T[] tempItems = new T[count];

      // re-hash: loop through existing items and store them in temporary array
      tempIndx = 0;
      for (indx = slots.Length - 1; indx >= 0; indx--) {
        if (slots[indx] == SlotStatus.Filled) {
          tempItems[tempIndx] = items[indx];
          tempIndx++;
        }
      }
      // Debug.Assert(count == tempIndx);

      // clear entries
      Array.Clear(items, 0, items.Length);
      Array.Clear(slots, 0, slots.Length);
      usedCount = count;

      for (tempIndx = tempItems.Length - 1; tempIndx >= 0; tempIndx--) {
        T item = tempItems[tempIndx];
        int keyHash = comparer == null ? item.GetHashCode() : comparer.GetHashCode(item);
        int newIndx = unchecked(keyHash & (int)mask);
        // don't have to check for deleted items in cleared slots array
        if (slots[newIndx] != SlotStatus.Empty)
          unchecked {
            // see comments to step in FindSlot()
            int step = ((((keyHash & (int)~mask) >> (sizeLog - 1)) &
                         (int)(mask >> 2)) | (int)1) & (int)0xFF;
            do {
              newIndx = newIndx - step;
              if (newIndx < 0)
                newIndx = newIndx + (int)mask + 1;
            } while (slots[newIndx] != SlotStatus.Empty);
          }
        items[newIndx] = item;
        slots[newIndx] = SlotStatus.Filled;
      }
    }

    /// <summary>Base 2 logarithm of table capacity.</summary>
    /// <remarks>The capacity must be at least twice the number of entries,
    /// which means: 2^SizeLog >= (2 * Count).</remarks>
    public byte SizeLog {
      get { return sizeLog; }
      set { SetSizeLog(value); }
    }

    /// <summary>Gets the <see cref="IEqualityComparer&lt;T>">IEqualityComparer</see>
    /// that is used to compare entries and calculate their hash codes.</summary>
    public IEqualityComparer<T> Comparer {
      get { return comparer; }
    }

    /// <summary>
    /// Allows capacity to shrink automatically when count decreases (to about 25%).
    /// </summary>
    public bool AutoShrink {
      get { return autoShrink; }
      set { autoShrink = value; }
    }

    #region Iterator Methods

    /// <summary>Returns iterator cookie initialized for starting a new iteration.</summary>
    public int StartIter() {
      return -1;
    }

    /// <summary>Checks if iterator cookie has a valid value, that is,
    /// if it points to a slot that is not empty or deleted.</summary>
    /// <param name="iter">Iterator cookie to be checked.</param>
    /// <returns><c>true</c> if valid, <c>false</c> otherwise.</returns>
    public bool IterValid(int iter) {
      return iter >= 0 && iter < slots.Length && slots[iter] == SlotStatus.Filled;
    }

    /// <summary>Retrieves next item and advances iterator.</summary>
    /// <param name="iter">Iterator cookie representing current iterator state.</param>
    /// <param name="value">Item to be retrieved - only if iterator returns <c>true</c>.</param>
    /// <returns><c>true</c> if a next item was found, or <c>false</c> if at end.</returns>
    public bool GetNext(ref int iter, ref T value) {
      for (int indx = iter + 1; indx < slots.Length; indx++) {
        if (slots[indx] == SlotStatus.Filled) {
          iter = indx;
          value = items[indx];
          return true;
        }
      }
      return false;
    }

    /// <summary>Advances iterator to next item.</summary>
    /// <param name="iter">Iterator cookie representing current iterator state.</param>
    /// <returns><c>true</c> if a next item was found, or <c>false</c> if at end.</returns>
    public bool MoveNext(ref int iter) {
      for (int indx = iter + 1; indx < slots.Length; indx++) {
        if (slots[indx] == SlotStatus.Filled) {
          iter = indx;
          return true;
        }
      }
      return false;
    }

    /// <summary>Gets item at current iterator position.</summary>
    /// <remarks>The iterator value must be the result of a successful call to
    /// <see cref="MoveNext"/> or <see cref="Find"/>. If the table was modified after
    /// the iterator value was obtained, result and behaviour are undetermined,
    /// possibly throwing an exception.</remarks>
    /// <param name="iter">Iterator cookie representing current iterator state.</param>
    /// <returns>Item at iterator position.</returns>
    public T Get(int iter) {
      return items[iter];
    }

    /// <summary>Removes item at current iterator position.</summary>
    /// <remarks>The iterator value must be the result of a successful call to
    /// <see cref="MoveNext"/> or <see cref="Find"/>. If the table was modified after
    /// the iterator value was obtained, result and behaviour are undetermined, possibly
    /// throwing an exception. This method itself may shrink or rebuild the table,
    /// thus invalidating the iterator value.</remarks>
    /// <param name="iter">Iterator cookie representing current iterator state.</param>
    /// <returns><c>true</c> if the item at the iterator position was successfully removed,
    /// or <c>false</c> if the current position does not hold an item.</returns>
    public bool Remove(int iter) {
      bool result = slots[iter] == SlotStatus.Filled;
      if (result) {
        items[iter] = default(T);
        slots[iter] = SlotStatus.Deleted;
        DecCount(1);
      }
      return result;
    }

    /// <summary>Retrieves next item, removing it from the table, and advances iterator.</summary>
    /// <remarks>This method does not shrink or rebuild the table. It is therefore strongly
    /// recommended to call <see cref="Rebuild()"/> after a sufficient number of items has
    /// been removed, in order to maintain performance.</remarks>
    /// <param name="iter">Iterator cookie representing current iterator state.</param>
    /// <param name="value">Item to be removed - only if iterator returns <c>true</c>.</param>
    /// <returns><c>true</c> if a next item was found, or <c>false</c> if at end.</returns>
    public bool RemoveNext(ref int iter, ref T value) {
      for (int indx = iter + 1; indx < slots.Length; indx++) {
        if (slots[indx] == SlotStatus.Filled) {
          iter = indx;
          value = items[indx];
          items[indx] = default(T);
          slots[indx] = SlotStatus.Deleted;
          count--;
          return true;
        }
      }
      return false;
    }

    #endregion Iterator Methods

    #region IEnumerable, IEnumerable<T> Members

    public IEnumerator<T> GetEnumerator() {
      return new Enumerator(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    #endregion

    #region ICollection<T> Members

    public void Add(T item) {
      if (!Insert(item))
        throw new ArgumentException("Duplicate item.", "item");
    }

    public void Clear() {
      Array.Clear(items, 0, items.Length);
      Array.Clear(slots, 0, slots.Length);
      count = 0;
      usedCount = 0;
    }

    public bool Contains(T item) {
      int indx;
      return FindSlot(item, out indx);
    }

    public void CopyTo(T[] array, int arrayIndex) {
      for (int indx = slots.Length - 1; indx >= 0; indx--) {
        if (slots[indx] == SlotStatus.Filled) {
          array[arrayIndex] = items[indx];
          arrayIndex++;
        }
      }
    }

    /// <remarks>If the number of items becomes too small, the hash table may be shrunk
    /// depending on the value of <see cref="AutoShrink"/>, if the number of deleted slots
    /// grows too large, the hash table is rebuilt to maintain performance.</remarks>
    public bool Remove(T value) {
      int indx;
      bool result = FindSlot(value, out indx);
      if (result) {
        items[indx] = default(T);
        slots[indx] = SlotStatus.Deleted;
        DecCount(1);
      }
      return result;
    }

    public int Count {
      get { return count; }
    }

    /// <remarks>This collection type is currently never read-only.</remarks>
    public bool IsReadOnly {
      get { return false; }
    }

    #endregion

    #endregion

    #region Nested Types

    protected enum SlotStatus
    {
      Empty = 0,
      Filled,
      Deleted
    }

    private class Enumerator: IEnumerator<T>
    {
      private Set<T> set;
      private int iter = -1;

      public Enumerator(Set<T> set) {
        this.set = set;
        this.iter = set.StartIter();
      }

      #region IEnumerator<T> Members

      public T Current {
        get {
          if (set.IterValid(iter))
            return set.Get(iter);
          else
            throw new InvalidOperationException("Enumerator position invalid.");
        }
      }

      #endregion

      #region IDisposable Members

      public void Dispose() { }

      #endregion

      #region IEnumerator Members

      object System.Collections.IEnumerator.Current {
        get { return Current; }
      }

      public bool MoveNext() {
        return set.MoveNext(ref iter);
      }

      public void Reset() {
        iter = set.StartIter();
      }

      #endregion
    }

    #endregion
  }
}

