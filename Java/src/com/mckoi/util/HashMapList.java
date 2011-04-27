/**
 * com.mckoi.util.HashMapList  02 Oct 2000
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

package com.mckoi.util;

import java.util.*;

/**
 * A HashMap that maps from a source to a list of items for that source.  This
 * is useful as a searching mechanism where the list of searched items are
 * catagorised in the mapped list.
 *
 * @author Tobias Downer
 */

public class HashMapList {

  private static final List EMPTY_LIST = Arrays.asList(new Object[0]);

  private HashMap map;

  /**
   * Constructs the map.
   */
  public HashMapList() {
    map = new HashMap();
  }

  /**
   * Puts a value into the map list.
   */
  public void put(Object key, Object val) {
    ArrayList list = (ArrayList) map.get(key);
    if (list == null) {
      list = new ArrayList();
    }
    list.add(val);
    map.put(key, list);
  }

  /**
   * Returns the list of values that are in the map under this key.  Returns
   * an empty list if no key map found.
   */
  public List get(Object key) {
    ArrayList list = (ArrayList) map.get(key);
    if (list != null) {
      return list;
    }
    return EMPTY_LIST;
  }

  /**
   * Removes the given value from the list with the given key.
   */
  public boolean remove(Object key, Object val) {
    ArrayList list = (ArrayList) map.get(key);
    if (list == null) {
      return false;
    }
    boolean status = list.remove(val);
    if (list.size() == 0) {
      map.remove(key);
    }
    return status;
  }

  /**
   * Clears the all the values for the given key.  Returns the List of
   * items that were stored under this key.
   */
  public List clear(Object key) {
    ArrayList list = (ArrayList) map.remove(key);
    if (list == null) {
      return new ArrayList();
    }
    return list;
  }

  /**
   * The Set of all keys.
   */
  public Set keySet() {
    return map.keySet();
  }

  /**
   * Returns true if the map contains the key.
   */
  public boolean containsKey(Object key) {
    return map.containsKey(key);
  }

}
