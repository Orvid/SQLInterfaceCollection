/**
 * com.mckoi.database.TNullType  02 Aug 2002
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

import com.mckoi.database.global.SQLTypes;

/**
 * An implementation of TType that represents a NULL type.  A Null type is
 * an object that can't be anything else except null.
 *
 * @author Tobias Downer
 */

public class TNullType extends TType {

  static final long serialVersionUID = -271824967935043427L;

  /**
   * Constructs the type.
   */
  public TNullType() {
    // There is no SQL type for a query plan node so we make one up here
    super(SQLTypes.NULL);
  }

  public boolean comparableTypes(TType type) {
    return (type instanceof TNullType);
  }
  
  public int compareObs(Object ob1, Object ob2) {
    // It's illegal to compare NULL types with this method so we throw an
    // exception here (see method specification).
    throw new Error("compareObs can not compare NULL types.");
  }
  
  public int calculateApproximateMemoryUse(Object ob) {
    return 16;
  }
  
  public Class javaClass() {
    return Object.class;
  }

}
