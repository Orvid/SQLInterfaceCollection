/**
 * com.mckoi.database.StoreSystem  03 Feb 2003
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

import com.mckoi.store.Store;
import java.io.IOException;

/**
 * An object that creates and manages the Store objects that the database engine 
 * uses to represent itself on an external medium such as a disk, and that
 * constitute the low level persistent data format.
 * <p>
 * This interface is an abstraction of the database persistence layer.  For
 * example, an implementation could represent itself as 1 file per store on a
 * disk, or as a number of stores in a single file, or as an entirely in-memory
 * database.
 *
 * @author Tobias Downer
 */

interface StoreSystem {

  /**
   * Returns true if the store with the given name exists within the system,
   * or false otherwise.
   */
  boolean storeExists(String name);
  
  /**
   * Creates and returns a new persistent Store object given the unique name of
   * the store.  If the system is read-only or the table otherwise can not be
   * created then an exception is thrown.
   * <p>
   * At the most, you should assume that this will return an implementation of
   * AbstractStore but you should not be assured of this fact.
   *
   * @param name a unique identifier string representing the name of the store.
   */
  Store createStore(String name);

  /**
   * Opens an existing persistent Store object in the system and returns the
   * Store object that contains its data.  An exception is thrown if the store
   * can not be opened.
   * <p>
   * At the most, you should assume that this will return an implementation of
   * AbstractStore but you should not be assured of this fact.
   *
   * @param name a unique identifier string representing the name of the store.
   */
  Store openStore(String name);

  /**
   * Closes a store that has been either created or opened with the
   * 'createStore' or 'openStore' methods.  Returns true if the
   * store was successfully closed.
   */
  boolean closeStore(Store store);

  /**
   * Permanently deletes a store from the system - use with care!  Returns
   * true if the store was successfully deleted and the resources associated
   * with it were freed.  Returns false if the store could not be deleted.  Note
   * that it is quite likely that a store may fail to delete in which case the
   * delete operation should be re-tried after a short timeout.
   */
  boolean deleteStore(Store store);

  /**
   * Sets a new check point at the current state of this store system.  This is
   * intended to help journalling check point and recovery systems.  A check
   * point is set whenever data is committed to the database.  Some systems
   * can be designed to be able to roll forward or backward to different
   * check points.  Each check point represents a stable state in the database
   * life cycle.
   * <p>
   * A checkpoint based system greatly improves stability because if a crash
   * occurs in an intermediate state the changes can simply be rolled back to
   * the last stable state.
   * <p>
   * An implementation may choose not to implement check points in which case
   * this would be a no-op.
   */
  void setCheckPoint();

  
  // ---------- Locking ----------

  /**
   * Attempts to lock this store system exclusively so that no other process
   * may access or change the persistent data in the store.  If this fails to
   * lock, an IOException is generated, otherwise the lock is obtained and the
   * method returns.
   */
  void lock(String lock_name) throws IOException;

  /**
   * Unlocks the exclusive access to the persistent store objects.  After this
   * method completes, access to the store system by other processes is allowed.
   */
  void unlock(String lock_name) throws IOException;

}

