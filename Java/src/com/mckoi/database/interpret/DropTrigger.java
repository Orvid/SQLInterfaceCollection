/**
 * com.mckoi.database.interpret.DropTrigger  14 Sep 2001
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
import com.mckoi.util.IntegerVector;
import java.util.ArrayList;
import java.util.List;

/**
 * A parsed state container for the 'DROP TRIGGER' statement.
 *
 * @author Tobias Downer
 */

public class DropTrigger extends Statement {

  /**
   * The name of this trigger.
   */
  String trigger_name;


  // ---------- Implemented from Statement ----------

  public void prepare() throws DatabaseException {
    trigger_name = (String) cmd.getObject("trigger_name");
  }

  public Table evaluate() throws DatabaseException {

    String type = (String) cmd.getObject("type");

    DatabaseQueryContext context = new DatabaseQueryContext(database);

    if (type.equals("callback_trigger")) {
      database.deleteTrigger(trigger_name);
    }
    else {

      // Convert the trigger into a table name,
      String schema_name = database.getCurrentSchema();
      TableName t_name = TableName.resolve(schema_name, trigger_name);
      t_name = database.tryResolveCase(t_name);

      ConnectionTriggerManager manager = database.getConnectionTriggerManager();
      manager.dropTrigger(t_name.getSchema(), t_name.getName());

      // Drop the grants for this object
      database.getGrantManager().revokeAllGrantsOnObject(
                                    GrantManager.TABLE, t_name.toString());
    }
    
    // Return '0' if we created the trigger.
    return FunctionTable.resultTable(context, 0);
  }


}
