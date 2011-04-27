/**
 * com.mckoi.database.AbstractDataTable  06 Apr 1998
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
 * This is the abstract class implemented by a DataTable like table.  Both
 * DataTable and DataTableFilter objects extend this object.
 * <p>
 * @author Tobias Downer
 */

public abstract class AbstractDataTable extends Table implements RootTable {

  /**
   * Returns the fully resolved table name.
   */
  public TableName getTableName() {
    return getDataTableDef().getTableName();
  }

  // ---------- Implemented from Table ----------
  
  /**
   * This function is used to check that two tables are identical.
   * We first check the table names are identical.  Then check the column
   * filter is the same.
   */
  public boolean typeEquals(RootTable table) {
    if (table instanceof AbstractDataTable) {
      AbstractDataTable dest = (AbstractDataTable) table;
      return (getTableName().equals(dest.getTableName()));
    }
    else {
      return (this == table);
    }
  }


  /**
   * Returns a string that represents this table.
   */
  public String toString() {
    return getTableName().toString() + "[" + getRowCount() + "]";
  }

}
