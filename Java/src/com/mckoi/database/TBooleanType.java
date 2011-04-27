/**
 * com.mckoi.database.TBooleanType  26 Jul 2002
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
 * An implementation of TType for a boolean value.
 *
 * @author Tobias Downer
 */

public final class TBooleanType extends TType {

  static final long serialVersionUID = 5602396246537490259L;

  /**
   * Constructs the type.
   */
  public TBooleanType(int sql_type) {
    super(sql_type);
  }

  public boolean comparableTypes(TType type) {
    return (type instanceof TBooleanType ||
            type instanceof TNumericType);
  }
  
  public int compareObs(Object ob1, Object ob2) {

    if (ob2 instanceof BigNumber) {
      BigNumber n2 = (BigNumber) ob2;
      BigNumber n1 = ob1.equals(Boolean.FALSE) ?
                        BigNumber.BIG_NUMBER_ZERO : BigNumber.BIG_NUMBER_ONE;
      return n1.compareTo(n2);
    }
    
    if (ob1 == ob2 || ob1.equals(ob2)) {
      return 0;
    }
    else if (ob1.equals(Boolean.TRUE)) {
      return 1;
    }
    else {
      return -1;
    }
  }
  
  public int calculateApproximateMemoryUse(Object ob) {
    return 5;
  }

  
  public Class javaClass() {
    return Boolean.class;
  }

}
