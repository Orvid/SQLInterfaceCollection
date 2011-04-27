/**
 * com.mckoi.database.GTCurrentConnectionsDataSource  23 Mar 2002
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

import java.util.ArrayList;
import java.util.Date;

/**
 * An implementation of MutableTableDataSource that presents the current
 * list of connections on the database.
 * <p>
 * NOTE: This is not designed to be a long kept object.  It must not last
 *   beyond the lifetime of a transaction.
 *
 * @author Tobias Downer
 */

final class GTCurrentConnectionsDataSource extends GTDataSource {

  /**
   * The DatabaseConnection object that this is table is modelling the
   * information within.
   */
  private DatabaseConnection database;

  /**
   * The list of info keys/values in this object.
   */
  private ArrayList key_value_pairs;

  /**
   * Constructor.
   */
  public GTCurrentConnectionsDataSource(DatabaseConnection connection) {
    super(connection.getSystem());
    this.database = connection;
    this.key_value_pairs = new ArrayList();
  }

  /**
   * Initialize the data source.
   */
  public GTCurrentConnectionsDataSource init() {

    UserManager user_manager = database.getDatabase().getUserManager();
    // Synchronize over the user manager while we inspect the information,
    synchronized (user_manager) {
      for (int i = 0; i < user_manager.userCount(); ++i) {
        User user = user_manager.userAt(i);
        key_value_pairs.add(user.getUserName());
        key_value_pairs.add(user.getConnectionString());
        key_value_pairs.add(new Date(user.getLastCommandTime()));
        key_value_pairs.add(new Date(user.getTimeConnected()));
      }
    }

    return this;
  }

  // ---------- Implemented from GTDataSource ----------

  public DataTableDef getDataTableDef() {
    return DEF_DATA_TABLE_DEF;
  }

  public int getRowCount() {
    return key_value_pairs.size() / 4;
  }

  public TObject getCellContents(final int column, final int row) {
    switch (column) {
      case 0:  // username
        return columnValue(column, (String) key_value_pairs.get(row * 4));
      case 1:  // host_string
        return columnValue(column, (String) key_value_pairs.get((row * 4) + 1));
      case 2:  // last_command
        return columnValue(column, (Date) key_value_pairs.get((row * 4) + 2));
      case 3:  // time_connected
        return columnValue(column, (Date) key_value_pairs.get((row * 4) + 3));
      default:
        throw new Error("Column out of bounds.");
    }
  }

  // ---------- Overwritten from GTDataSource ----------

  public void dispose() {
    super.dispose();
    key_value_pairs = null;
    database = null;
  }

  // ---------- Static ----------

  /**
   * The data table def that describes this table of data source.
   */
  static final DataTableDef DEF_DATA_TABLE_DEF;

  static {

    DataTableDef def = new DataTableDef();
    def.setTableName(
             new TableName(Database.SYSTEM_SCHEMA, "sUSRCurrentConnections"));

    // Add column definitions
    def.addColumn(stringColumn("username"));
    def.addColumn(stringColumn("host_string"));
    def.addColumn(dateColumn("last_command"));
    def.addColumn(dateColumn("time_connected"));

    // Set to immutable
    def.setImmutable();

    DEF_DATA_TABLE_DEF = def;

  }

}
