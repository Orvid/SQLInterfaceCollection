/**
 * com.mckoi.database.Lock  11 May 1998
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
 * This is a lock on a table in the LockingMechanism class.  A new instance
 * of this class is created whenever a new lock for a table is made.  A Lock
 * may be either a READ lock or a WRITE lock.  A lock is within a LockingQueue
 * object.
 * <p>
 * @author Tobias Downer
 */

public final class Lock {

  /**
   * These statics are used to define whether the lock is a READ or WRITE
   * lock.
   */
  public static final int READ  = 0;
  public static final int WRITE = 1;

  /**
   * This stores the type of lock.  It is either set to 'READ' or 'WRITE'
   */
  private int type;

  /**
   * The table queue this lock is 'inside'.
   */
  private LockingQueue queue;

  /**
   * This is set to true when the 'checkAccess' method is called on this
   * lock.
   */
  private boolean was_checked;

  /**
   * The DebugLogger object that we log debug message to.
   */
  private final DebugLogger debug;

  /**
   * The Constructor.  As well as setting up the state of this object, it
   * also puts this lock into the table queue.
   */
  Lock(int type, LockingQueue queue, DebugLogger logger) {
    this.debug = logger;
    this.type = type;
    this.queue = queue;
    was_checked = false;
    queue.addLock(this);
  }

  /**
   * Returns the type of lock.
   */
  int getType() {
    return type;
  }

  /**
   * Returns the type of the lock as a string.
   */
  String getTypeAsString() {
    int type = getType();
    if (type == READ) {
      return "READ";
    }
    else {
      return "WRITE";
    }
  }

  /**
   * Returns the DataTable object this lock is locking
   */
  DataTable getTable() {
    return queue.getTable();
  }

  /**
   * Removes this lock from the queue.  This is called when lock is released
   * from the table queues.
   * NOTE: This method does not need to be synchronized because synchronization
   *   is handled by the 'LockingMechanism.unlockTables' method.
   */
  void release() {
    queue.removeLock(this);

    if (!was_checked) {
      // Prints out a warning if a lock was released from the table queue but
      // never had 'checkAccess' called for it.
      String table_name = queue.getTable().getTableName().toString();
      debug.write(Lvl.ERROR, this,
         "Lock on table '" + getTable().getTableName() +
         "' was released but never checked.  " + toString());
      debug.writeException(new RuntimeException("Lock Error Dump"));
    }
//    else {
//      // Notify table we released read/write lock
//      getTable().notifyReleaseRWLock(type);
//    }
  }

  /**
   * Checks the access for this lock.  This asks the queue that contains
   * this lock if it is currently safe to access the table.  If it is unsafe
   * for the table to be accessed, then it blocks until it is safe.  Therefore,
   * when this method returns, it is safe to access the table for this lock.
   * The 'access_type' variable contains either 'READ' or 'WRITE' and is set
   * to the type of access that is currently being done to the table.  If
   * access_type == WRITE then this.type must be WRITE.  If access_type ==
   * READ then this.type may be either READ or WRITE.
   * <p>
   * NOTE: After the first call to this method, following calls will not
   *   block.
   */
  void checkAccess(int access_type) {
    if (access_type == WRITE && this.type != WRITE) {
      throw new Error(
                 "Access error on Lock: Tried to write to a non write lock.");
    }
    if (was_checked == false) {
      queue.checkAccess(this);
      was_checked = true;
//      // Notify table we are read/write locked
//      getTable().notifyAddRWLock(type);
    }
  }

  public String toString() {
    return "[Lock] type: " + getTypeAsString() +
           "  was_checked: " + was_checked;
  }

}
