/**
 * com.mckoi.database.interpret.Misc  14 Sep 2001
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

import com.mckoi.database.*;
import java.util.ArrayList;
import java.util.List;

/**
 * Misc statements that I couldn't be bothered to roll a new Statement class
 * for.  These have to be exceptional statements that do not read or write
 * to any tables and run in exclusive mode.
 *
 * @author Tobias Downer
 */

public class Misc extends Statement {

  /**
   * Set to true if this statement is a shutdown statement.
   */
  boolean shutdown = false;



  // ---------- Implemented from Statement ----------

  public void prepare() throws DatabaseException {
    Object command = cmd.getObject("command");
    shutdown = command.equals("shutdown");
  }

  public Table evaluate() throws DatabaseException {

    DatabaseQueryContext context = new DatabaseQueryContext(database);

    // Is this a shutdown statement?
    if (shutdown == true) {

      // Check the user has privs to shutdown...
      if (!database.getDatabase().canUserShutDown(context, user)) {
        throw new UserAccessException(
                 "User not permitted to shut down the database.");
      }

      // Shut down the database system.
      database.getDatabase().startShutDownThread();

      // Return 0 to indicate we going to be closing shop!
      return FunctionTable.resultTable(context, 0);

    }

    return FunctionTable.resultTable(context, 0);
  }


}
