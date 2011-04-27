/**
 * com.mckoi.database.V1HeapStoreSystem  20 Feb 2003
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

import com.mckoi.store.*;
import java.io.IOException;
import java.util.HashMap;

/**
 * An implementation of StoreSystem that stores all persistent data on the
 * heap using HeapStore objects.
 *
 * @author Tobias Downer
 */

class V1HeapStoreSystem implements StoreSystem {

  /**
   * A mapping from name to Store object for this heap store system.
   */
  private HashMap name_store_map;

  /**
   * A mapping from Store object to name.
   */
  private HashMap store_name_map;

  
  /**
   * Constructor.
   */
  V1HeapStoreSystem() {
    name_store_map = new HashMap();
    store_name_map = new HashMap();
  }
  
  
  public boolean storeExists(String name) {
    return (name_store_map.get(name) != null);
  }
  
  public Store createStore(String name) {
    if (!storeExists(name)) {
      HeapStore store = new HeapStore();
      name_store_map.put(name, store);
      store_name_map.put(store, name);
      return store;
    }
    else {
      throw new RuntimeException("Store exists: " + name);
    }
  }

  public Store openStore(String name) {
    HeapStore store = (HeapStore) name_store_map.get(name);
    if (store == null) {
      throw new RuntimeException("Store does not exist: " + name);
    }
    return store;
  }

  public boolean closeStore(Store store) {
    if (store_name_map.get(store) == null) {
      throw new RuntimeException("Store does not exist.");
    }
    return true;
  }

  public boolean deleteStore(Store store) {
    String name = (String) store_name_map.remove(store);
    name_store_map.remove(name);
    return true;
  }

  public void setCheckPoint() {
    // Check point logging not necessary with heap store
  }
  
  // ---------- Locking ----------

  public void lock(String lock_name) throws IOException {
    // Not required because heap memory is not a shared resource that can be
    // accessed by multiple JVMs
  }

  public void unlock(String lock_name) throws IOException {
    // Not required because heap memory is not a shared resource that can be
    // accessed by multiple JVMs
  }

}

