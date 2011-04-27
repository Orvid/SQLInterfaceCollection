/**
 * com.mckoi.database.RowEnumeration  05 Apr 1998
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
 * This enumeration allows for access to a tables rows.  Each call to
 * 'nextRowIndex()' returns an int that can be used in the
 * 'Table.getCellContents(int row, int column)'.
 * <p>
 * @author Tobias Downer
 */

public interface RowEnumeration {

  /**
   * Determines if there are any rows left in the enumeration.
   */
  public boolean hasMoreRows();

  /**
   * Returns the next row index from the enumeration.
   */
  public int nextRowIndex();

}
