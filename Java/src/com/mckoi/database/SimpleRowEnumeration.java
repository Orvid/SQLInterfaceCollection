/**
 * com.mckoi.database.SimpleRowEnumeration  19 Sep 2002
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

/**
 * A RowEnumeration implementation that represents a sequence of rows that
 * can be referenced in incremental order between 0 and row_count (exclusive).
 * A Table that returns a SimpleRowEnumeration is guarenteed to provide valid
 * TObject values via the 'getCellContents' method between rows 0 and
 * getRowCount().
 *
 * @author Tobias Downer
 */

public final class SimpleRowEnumeration implements RowEnumeration {

  /**
   * The current index.
   */
  private int index = 0;
  
  /**
   * The number of rows in the enumeration.
   */
  final int row_count_store;

  /**
   * Constructs the RowEnumeration.
   */
  public SimpleRowEnumeration(int row_count) {
    row_count_store = row_count;
  }

  public final boolean hasMoreRows() {
    return (index < row_count_store);
  }

  public final int nextRowIndex() {
    ++index;
    return index - 1;
  }

}

