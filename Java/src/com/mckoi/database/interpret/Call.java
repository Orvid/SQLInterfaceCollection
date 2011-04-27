/**
 * com.mckoi.database.interpret.Call  15 Sep 2002
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
import java.io.*;
import java.util.ArrayList;
import java.util.List;

/**
 * A statement that calls a procedure, and returns a resultant table.  This 
 * is used to perform some sort of function over the database.  For example,
 * "CALL SYSTEM_MAKE_BACKUP('/my_backups/1')" makes a copy of the database in
 * the given directory on the disk.
 *
 * @author Tobias Downer
 */

public class Call extends Statement {

  // ---------- Implemented from Statement ----------

  public void prepare() throws DatabaseException {
  }

  public Table evaluate() throws DatabaseException {

    DatabaseQueryContext context = new DatabaseQueryContext(database);

    String proc_name = (String) cmd.getObject("proc_name");
    Expression[] args = (Expression[]) cmd.getObject("args");

    // Get the procedure manager
    ProcedureManager manager = database.getProcedureManager();
    ProcedureName name;

    TableName p_name = null;

    // If no schema def given in the procedure name, first check for the
    // function in the SYS_INFO schema.
    if (proc_name.indexOf(".") == -1) {
      // Resolve the procedure name into a TableName object.    
      String schema_name = database.getCurrentSchema();
      TableName tp_name = TableName.resolve(Database.SYSTEM_SCHEMA, proc_name);
      tp_name = database.tryResolveCase(tp_name);
      
      // If exists then use this
      if (manager.procedureExists(tp_name)) {
        p_name = tp_name;
      }
    }

    if (p_name == null) {
      // Resolve the procedure name into a TableName object.    
      String schema_name = database.getCurrentSchema();
      TableName tp_name = TableName.resolve(schema_name, proc_name);
      tp_name = database.tryResolveCase(tp_name);

      // Does the schema exist?
      boolean ignore_case = database.isInCaseInsensitiveMode();
      SchemaDef schema =
                  database.resolveSchemaCase(tp_name.getSchema(), ignore_case);
      if (schema == null) {
        throw new DatabaseException("Schema '" + tp_name.getSchema() +
                                    "' doesn't exist.");
      }
      else {
        tp_name = new TableName(schema.getName(), tp_name.getName());
      }

      // If this doesn't exist then generate the error
      if (!manager.procedureExists(tp_name)) {
        throw new DatabaseException("Stored procedure '" + proc_name +
                                    "' was not found.");
      }

      p_name = tp_name;
    }

    // Does the procedure exist in the system schema?
    name = new ProcedureName(p_name);

    // Check the user has privs to use this stored procedure
    if (!database.getDatabase().canUserExecuteStoredProcedure(context,
                                                     user, name.toString())) {
      throw new UserAccessException("User not permitted to call: " + proc_name);
    }

    // Evaluate the arguments
    TObject[] vals = new TObject[args.length];
    for (int i = 0; i < args.length; ++i) {
      if (args[i].isConstant()) {
        vals[i] = args[i].evaluate(null, null, context);
      }
      else {
        throw new StatementException(
                         "CALL argument is not a constant: " + args[i].text());
      }
    }

    // Invoke the procedure
    TObject result = manager.invokeProcedure(name, vals);

    // Return the result of the procedure,
    return FunctionTable.resultTable(context, result);

  }

}

