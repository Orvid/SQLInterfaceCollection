/**
 * com.mckoi.database.TArrayType  26 Jul 2002
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
 * An implementation of TType for an expression array.
 *
 * @author Tobias Downer
 */

public class TArrayType extends TType {

  static final long serialVersionUID = 6551509064212831922L;

  /**
   * Constructs the type.
   */
  public TArrayType() {
    // There is no SQL type for a query plan node so we make one up here
    super(SQLTypes.ARRAY);
  }

  public boolean comparableTypes(TType type) {
    throw new Error("Query Plan types should not be compared.");
  }
  
  public int compareObs(Object ob1, Object ob2) {
    throw new Error("Query Plan types should not be compared.");
  }
  
  public int calculateApproximateMemoryUse(Object ob) {
    return 5000;
  }
  
  public Class javaClass() {
    return Expression[].class;
  }

}
