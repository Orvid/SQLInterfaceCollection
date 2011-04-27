/**
 * com.mckoi.database.TableDataSource  17 Nov 2000
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
 * This interface represents the source of data in a table.  This is an
 * abstraction that is used to read data from within a table.
 * <p>
 * The entire contents of a table can be completely represented by
 * implementations of this interface.
 *
 * @author Tobias Downer
 */

public interface TableDataSource {

  /**
   * Returns the TransactionSystem object that describes global properties
   * about the data source that generated this object.
   */
  TransactionSystem getSystem();

  /**
   * Returns a DataTableDef object that defines the layout of the table that
   * this data is in.
   * <p>
   * This may return 'null' if there is no table definition.
   */
  DataTableDef getDataTableDef();

  /**
   * Returns the number of rows in this data source.
   * <p>
   * NOTE: Returns 'n' - getCellContents(column, row) is not necessarily valid
   *   for row = [0..n].  Use 'rowEnumerator' to generate an iterator for valid
   *   row values over this data source.
   */
  int getRowCount();

  /**
   * Returns an iterator that is used to sequentually step through all valid
   * rows in this source.  The iterator is guarenteed to return exactly
   * 'getRowCount' elements.  The row elements returned by this iterator
   * are used in 'getCellContents' in the 'row' parameter.
   * <p>
   * Note that this object is only defined if entries in the table are not
   * added/remove during the lifetime of this iterator.  If entries are added
   * or removed from the table while this iterator is open, then calls to
   * 'nextRowIndex' will be undefined.
   */
  RowEnumeration rowEnumeration();

  /**
   * Returns the SelectableScheme that we use as an index for rows in the
   * given column of this source.  The SelectableScheme is used to determine
   * the relationship between cells in a column.
   * <p>
   * ISSUE: The scheme returned here should not have the 'insert' or 'remove'
   *  methods called (ie. it should be considered immutable).  Perhaps we
   *  should make a MutableSelectableScheme interface to guarentee this
   *  constraint.
   */
  SelectableScheme getColumnScheme(int column);

  /**
   * Returns an object that represents the information in the given cell
   * in the table.  This may be an expensive operation, so calls to it
   * should be kept to a minimum.  Note that the offset between two
   * rows is not necessarily 1.  Use 'rowEnumeration' to create a row iterator.
   */
  TObject getCellContents(int column, int row);

}
