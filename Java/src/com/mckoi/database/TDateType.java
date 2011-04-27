/**
 * com.mckoi.database.TDateType  31 Jul 2002
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

import java.util.Date;

/**
 * An implementation of TType for date objects.
 *
 * @author Tobias Downer
 */

public class TDateType extends TType {

  static final long serialVersionUID = 1494137367081481985L;

  /**
   * Constructs the type.
   */
  public TDateType(int sql_type) {
    super(sql_type);
  }

  public boolean comparableTypes(TType type) {
    return (type instanceof TDateType);
  }
  
  public int compareObs(Object ob1, Object ob2) {
    return ((Date) ob1).compareTo((Date) ob2);
  }

  public int calculateApproximateMemoryUse(Object ob) {
    return 4 + 8;
  }

  public Class javaClass() {
    return Date.class;
  }

}
