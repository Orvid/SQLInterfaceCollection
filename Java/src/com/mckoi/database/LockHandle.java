/**
 * com.mckoi.database.LockHandle  11 May 1998
 *
 * Mckoi SQL Database ( http://www.mckoi.com/database )
 * Copyright (C) 2000, 2001, 2002  Diehl and Associates, Inc.
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * Version 2 as published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License Version 2 for more details.
 *
 * You should have received a copy of the GNU General Public License
 * Version 2 along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 * Change Log:
 * 
 * 
 */

package com.mckoi.database;

import com.mckoi.debug.*;

/**
 * This represents a handle for a series of locks that a query has over the
 * tables in a database.  It is returned by the 'LockingMechanism' object
 * after the 'lockTables' method is used.
 * <p>
 * @author Tobias Downer
 */

public final class LockHandle {

  /**
   * The array of Lock objects that are being used in this locking process.
   */
  private Lock[] lock_list;

  /**
   * A temporary index used during initialisation of object to add locks.
   */
  private int lock_index;

  /**
   * Set when the 'unlockAll' method is called for the first time.
   */
  private boolean unlocked;

  /**
   * The DebugLogger object that we log debug messages to.
   */
  private final DebugLogger debug;

  /**
   * The Constructor.  Takes the number of locks that will be put into this
   * handle.
   */
  LockHandle(int lock_count, DebugLogger logger) {
    this.debug = logger;
    lock_list = new Lock[lock_count];
    lock_index = 0;
    unlocked = false;
  }

  /**
   * Adds a new lock to the locks for this handle.
   * NOTE: This method does not need to be synchronized because synchronization
   *   is handled by the 'LockingMechanism.lockTables' method.
   */
  void addLock(Lock lock) {
    lock_list[lock_index] = lock;
    ++lock_index;
  }

  /**
   * Unlocks all the locks in this handle.  This removes the locks from its
   * table queue.
   * NOTE: This method does not need to be synchronized because synchronization
   *   is handled by the 'LockingMechanism.unlockTables' method.
   */
  void unlockAll() {
    if (!unlocked) {
      for (int i = lock_list.length - 1; i >= 0; --i) {
        lock_list[i].release();
      }
      unlocked = true;
    }
  }

  /**
   * Blocks until access to the given DataTable object is safe.  It blocks
   * using either the read or read/write privs that it has been given.
   * Note that this method is public and is a method that is intended to be
   * used outside the locking mechanism.
   * We also provide an 'access_type' field which is set to the type of access
   * that is happening for this check.  This is either Lock.READ or Lock.WRITE.
   * NOTE: Any call to this method after the first call should be
   *  instantanious.
   */
  public void checkAccess(DataTable table, int access_type) {
    for (int i = lock_list.length - 1; i >= 0; --i) {
      Lock l = lock_list[i];
      if (l.getTable() == table) {
        l.checkAccess(access_type);
        return;
      }
    }
    throw new RuntimeException(
        "The given DataTable was not found in the lock list for this handle");
  }

  /**
   * On garbage collection, this will call 'unlockAll' just in case the
   * program did not use the 'LockingMechanism.unlockTables' method in error.
   * This should ensure the database does not deadlock.  This method is a
   * 'just in case' clause.
   */
  public void finalize() {
    if (!unlocked) {
      unlockAll();
      debug.write(Lvl.ERROR, this, "Finalize released a table lock - " +
        "This indicates that there is a serious error.  Locks should " +
        "only have a very short life span.  The 'unlockAll' method should " +
        "have been called before finalization.  " + toString());
    }
  }

  public String toString() {
    StringBuffer str = new StringBuffer("LockHandle: ");
    for (int i = 0; i < lock_list.length; ++i) {
      str.append(lock_list[i].toString());
    }
    return new String(str);
  }

}
