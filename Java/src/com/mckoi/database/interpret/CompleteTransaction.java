/**
 * com.mckoi.database.interpret.CompleteTransaction  14 Sep 2001
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
 * This represents either a COMMIT or ROLLBACK SQL command.
 *
 * @author Tobias Downer
 */

public class CompleteTransaction extends Statement {

  String command;  // This is set to either 'commit' or 'rollback'



  // ---------- Implemented from Statement ----------

  public void prepare() throws DatabaseException {
    command = (String) cmd.getObject("command");
  }

  public Table evaluate() throws DatabaseException, TransactionException {

    DatabaseQueryContext context = new DatabaseQueryContext(database);

    if (command.equals("commit")) {
//      try {
        // Commit the current transaction on this connection.
        database.commit();
//      }
//      catch (TransactionException e) {
//        // This needs to be handled better!
//        Debug.writeException(e);
//        throw new DatabaseException(e.getMessage());
//      }
      return FunctionTable.resultTable(context, 0);
    }
    else if (command.equals("rollback")) {
      // Rollback the current transaction on this connection.
      database.rollback();
      return FunctionTable.resultTable(context, 0);
    }
    else {
      throw new Error("Unrecognised transaction completion command.");
    }

  }


}
