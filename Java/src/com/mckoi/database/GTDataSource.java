/**
 * com.mckoi.database.GTDataSource  27 Apr 2001
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
 * A base class for a dynamically generated data source.  While this inherits
 * MutableTableDataSource (so we can make a DataTable out of it) a GTDataSource
 * derived class may not be mutable.  For example, an implementation of this
 * class may produce a list of a columns in all tables.  You would typically
 * not want a user to change this information unless they run a DML command.
 *
 * @author Tobias Downer
 */

abstract class GTDataSource implements MutableTableDataSource {

  /**
   * The TransactionSystem object for this table.
   */
  private TransactionSystem system;


  /**
   * Constructor.
   */
  public GTDataSource(TransactionSystem system) {
    this.system = system;
  }

  /**
   * Returns a TObject that represents a value for the given column in this
   * table.  The Object must be of a compatible class to store in the type
   * of the column defined.
   */
  protected TObject columnValue(int column, Object ob) {
    TType type = getDataTableDef().columnAt(column).getTType();
    return new TObject(type, ob);
  }

  // ---------- Implemented from TableDataSource ----------

  public TransactionSystem getSystem() {
    return system;
  }

  public abstract DataTableDef getDataTableDef();

  public abstract int getRowCount();

  public RowEnumeration rowEnumeration() {
    return new SimpleRowEnumeration(getRowCount());
  }

  public SelectableScheme getColumnScheme(int column) {
    return new BlindSearch(this, column);
  }

  public abstract TObject getCellContents(final int column, final int row);

  // ---------- Implemented from MutableTableDataSource ----------

  public int addRow(RowData row_data) {
    throw new RuntimeException("Functionality not available.");
  }

  public void removeRow(int row_index) {
    throw new RuntimeException("Functionality not available.");
  }

  public int updateRow(int row_index, RowData row_data) {
    throw new RuntimeException("Functionality not available.");
  }

  public MasterTableJournal getJournal() {
    throw new RuntimeException("Functionality not available.");
  }

  public void flushIndexChanges() {
    throw new RuntimeException("Functionality not available.");
  }
  
  public void constraintIntegrityCheck() {
    throw new RuntimeException("Functionality not available.");
  }

  public void dispose() {
  }

  public void addRootLock() {
    // No need to lock roots
  }

  public void removeRootLock() {
    // No need to lock roots
  }

  // ---------- Static ----------

  /**
   * Convenience methods for constructing a DataTableDef for the dynamically
   * generated table.
   */
  protected static DataTableColumnDef stringColumn(String name) {
    DataTableColumnDef column = new DataTableColumnDef();
    column.setName(name);
    column.setNotNull(true);
    column.setSQLType(SQLTypes.VARCHAR);
    column.setSize(Integer.MAX_VALUE);
    column.setScale(-1);
    column.setIndexScheme("BlindSearch");
    column.initTTypeInfo();
    return column;
  }

  protected static DataTableColumnDef booleanColumn(String name) {
    DataTableColumnDef column = new DataTableColumnDef();
    column.setName(name);
    column.setNotNull(true);
    column.setSQLType(SQLTypes.BIT);
    column.setSize(-1);
    column.setScale(-1);
    column.setIndexScheme("BlindSearch");
    column.initTTypeInfo();
    return column;
  }

  protected static DataTableColumnDef numericColumn(String name) {
    DataTableColumnDef column = new DataTableColumnDef();
    column.setName(name);
    column.setNotNull(true);
    column.setSQLType(SQLTypes.NUMERIC);
    column.setSize(-1);
    column.setScale(-1);
    column.setIndexScheme("BlindSearch");
    column.initTTypeInfo();
    return column;
  }

  protected static DataTableColumnDef dateColumn(String name) {
    DataTableColumnDef column = new DataTableColumnDef();
    column.setName(name);
    column.setNotNull(true);
    column.setSQLType(SQLTypes.TIMESTAMP);
    column.setSize(-1);
    column.setScale(-1);
    column.setIndexScheme("BlindSearch");
    column.initTTypeInfo();
    return column;
  }

}
