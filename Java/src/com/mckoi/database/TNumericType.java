/**
 * com.mckoi.database.TNumericType  26 Jul 2002
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

import com.mckoi.util.BigNumber;

/**
 * An implementation of TType for a number.
 *
 * @author Tobias Downer
 */

public final class TNumericType extends TType {

  static final long serialVersionUID = -5133489773377747175L;

  /**
   * The size of the number.
   */
  private int size;
  
  /**
   * The scale of the number.
   */
  private int scale;
  
  
  /**
   * Constructs a type with the given sql_type value, the size,
   * and the scale of the number.  Note that the 'sql_type' MUST be a numeric
   * SQL type (FLOAT, INTEGER, DOUBLE, etc).
   */
  public TNumericType(int sql_type, int size, int scale) {
    super(sql_type);
    this.size = size;
    this.scale = scale;
  }

  /**
   * Returns the size of the number (-1 is don't care).
   */
  public int getSize() {
    return size;
  }
  
  /**
   * Returns the scale of the number (-1 is don't care).
   */
  public int getScale() {
    return scale;
  }

  // ---------- Implemented from TType ----------
  
  public boolean comparableTypes(TType type) {
    return (type instanceof TNumericType ||
            type instanceof TBooleanType);
  }

  public int compareObs(Object ob1, Object ob2) {
    BigNumber n1 = (BigNumber) ob1;
    BigNumber n2;

    if (ob2 instanceof BigNumber) {
      n2 = (BigNumber) ob2;
    }
    else {
      n2 = ob2.equals(Boolean.TRUE) ?
                        BigNumber.BIG_NUMBER_ONE : BigNumber.BIG_NUMBER_ZERO;
    }
    
    return n1.compareTo(n2);
  }

  public int calculateApproximateMemoryUse(Object ob) {
    // A heuristic - it's difficult to come up with an accurate number
    // for this.
    return 25 + 16;
  }

  public Class javaClass() {
    return BigNumber.class;
  }

}
