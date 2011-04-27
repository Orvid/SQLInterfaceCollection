/**
 * com.mckoi.database.global.TypeUtil  01 Aug 2000
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

/**
 * Utility for converting to and from 'Types' objects.
 *
 * @author Tobias Downer
 */

public class TypeUtil {

  /**
   * Converts from a Class object to a type as specified in Types.
   */
  public static int toDBType(Class clazz) {
    if (clazz == String.class) {
      return Types.DB_STRING;
    }
    else if (clazz == java.math.BigDecimal.class) {
      return Types.DB_NUMERIC;
    }
    else if (clazz == java.util.Date.class) {
      return Types.DB_TIME;
    }
    else if (clazz == Boolean.class) {
      return Types.DB_BOOLEAN;
    }
    else if (clazz == ByteLongObject.class) {
      return Types.DB_BLOB;
    }
    else {
      return Types.DB_OBJECT;
    }
  }

  /**
   * Converts from a db type to a Class object.
   */
  public static Class toClass(int type) {
    if (type == Types.DB_STRING) {
      return String.class;
    }
    else if (type == Types.DB_NUMERIC) {
      return java.math.BigDecimal.class;
    }
    else if (type == Types.DB_TIME) {
      return java.util.Date.class;
    }
    else if (type == Types.DB_BOOLEAN) {
      return Boolean.class;
    }
    else if (type == Types.DB_BLOB) {
      return ByteLongObject.class;
    }
    else if (type == Types.DB_OBJECT) {
      return Object.class;
    }
    else {
      throw new Error("Unknown type.");
    }
  }




}
