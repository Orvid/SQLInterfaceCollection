/**
 * com.mckoi.database.control.AbstractDBConfig  29 Mar 2002
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

package com.mckoi.database.control;

import java.io.File;
import java.util.Hashtable;

/**
 * An abstract implementation of DBConfig.
 *
 * @author Tobias Downer
 */

public class AbstractDBConfig implements DBConfig {

  /**
   * The current base path of the database configuration.
   */
  private File current_path;

  /**
   * The Hashtable mapping from configuration key to value for the key.
   */
  private Hashtable key_map;

  /**
   * Constructs the DBConfig.
   */
  public AbstractDBConfig(File current_path) {
    this.current_path = current_path;
    this.key_map = new Hashtable();
  }

  /**
   * Returns the default value for the configuration property with the given
   * key.
   */
  protected String getDefaultValue(String property_key) {
    // This abstract implementation returns null for all default keys.
    return null;
  }

  /**
   * Sets the configuration value for the key property key.
   */
  protected void setValue(String property_key, String val) {
    key_map.put(property_key, val);
  }

  // ---------- Implemented from DBConfig ----------

  public File currentPath() {
    return current_path;
  }

  public String getValue(String property_key) {
    // If the key is in the map, return it here
    String val = (String) key_map.get(property_key);
    if (val == null) {
      return getDefaultValue(property_key);
    }
    return val;
  }

  public DBConfig immutableCopy() {
    AbstractDBConfig immutable_copy = new AbstractDBConfig(current_path);
    immutable_copy.key_map = (Hashtable) key_map.clone();
    return immutable_copy;
  }

}
