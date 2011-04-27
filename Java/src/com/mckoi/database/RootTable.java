/**
 * com.mckoi.database.RootTable  22 Sep 2000
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
 * Interface that is implemented by all Root tables.  A Root table is a non-
 * virtual table that represents table data in its lowest form.  When the
 * Table.resolveToRawTable method is called, if it encounters a table that
 * implements RootTable then it does not attempt to decend further to
 * extract the underlying tables.
 * <p>
 * This interface is used for unions.
 *
 * @author Tobias Downer
 */

public interface RootTable {

  /**
   * This is function is used to check that two root tables are identical.
   * This is used if we need to chect that the form of the table is the same.
   * Such as in a union operation, when we can only union two tables with
   * the identical columns.
   */
  boolean typeEquals(RootTable table);


}
