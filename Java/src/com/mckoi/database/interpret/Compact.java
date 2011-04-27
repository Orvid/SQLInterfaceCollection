/**
 * com.mckoi.database.interpret.Compact  14 Sep 2001
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

package com.mckoi.database.interpret;

import java.util.ArrayList;
import java.util.List;
import com.mckoi.database.*;

/**
 * Statement that handles COMPACT sql command.
 *
 * @author Tobias Downer
 */

public class Compact extends Statement {

  /**
   * The name the table that we are to update.
   */
  String table_name;

  // ---------- Implemented from Statement ----------

  public void prepare() throws DatabaseException {
    table_name = (String) cmd.getObject("table_name");
  }

  public Table evaluate() throws DatabaseException {

    DatabaseQueryContext context = new DatabaseQueryContext(database);

//    TableName tname =
//                TableName.resolve(database.getCurrentSchema(), table_name);
    TableName tname = resolveTableName(table_name, database);
    // Does the table exist?
    if (!database.tableExists(tname)) {
      throw new DatabaseException("Table '" + tname + "' does not exist.");
    }

    // Does the user have privs to compact this tables?
    if (!database.getDatabase().canUserCompactTableObject(context,
                                                          user, tname)) {
      throw new UserAccessException(
         "User not permitted to compact table: " + table_name);
    }

    // Compact the table,
    database.compactTable(tname);

    // Return '0' if success.
    return FunctionTable.resultTable(context, 0);

  }


}
