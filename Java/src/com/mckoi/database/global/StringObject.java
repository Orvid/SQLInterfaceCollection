/**
 * com.mckoi.database.global.StringObject  30 Jan 2003
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

package com.mckoi.database.global;

import java.io.Reader;
import java.io.StringReader;

/**
 * A concrete implementation of StringAccessor that uses a java.lang.String
 * object.
 *
 * @author Tobias Downer
 */

public class StringObject implements java.io.Serializable, StringAccessor {

  static final long serialVersionUID = 6066215992031250481L;
  
  /**
   * The java.lang.String object.
   */
  private String str;

  /**
   * Constructs the object.
   */
  private StringObject(String str) {
    this.str = str;
  }

  /**
   * Returns the length of the string.
   */
  public int length() {
    return str.length();
  }

  /**
   * Returns a Reader that can read from the string.
   */
  public Reader getReader() {
    return new StringReader(str);
  }

  /**
   * Returns this object as a java.lang.String object (easy!)
   */
  public String toString() {
    return str;
  }

  /**
   * Static method that returns a StringObject from the given java.lang.String.
   */
  public static StringObject fromString(String str) {
    if (str != null) {
      return new StringObject(str);
    }
    return null;
  }
  
}

