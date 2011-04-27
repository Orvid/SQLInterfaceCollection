/**
 * com.mckoi.database.interpret.Schema  14 Sep 2001
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
 * Statement container that handles the CREATE SCHEMA and DROP SCHEMA
 * statements.
 *
 * @author Tobias Downer
 */

public class Schema extends Statement {

  /**
   * The type (either 'create' or 'drop').
   */
  String type;

  /**
   * The name of the schema.
   */
  String schema_name;

  // ---------- Implemented from Statement ----------

  public void prepare() throws DatabaseException {
    type = (String) cmd.getObject("type");
    schema_name = (String) cmd.getObject("schema_name");
  }

  public Table evaluate() throws DatabaseException {

    DatabaseQueryContext context = new DatabaseQueryContext(database);

    String com = type.toLowerCase();

    if (!database.getDatabase().canUserCreateAndDropSchema(
                                                context, user, schema_name)) {
      throw new UserAccessException(
                         "User not permitted to create or drop schema.");
    }

    // Is this a create schema command?
    if (com.equals("create")) {
      boolean ignore_case = database.isInCaseInsensitiveMode();
      SchemaDef schema =
                  database.resolveSchemaCase(schema_name, ignore_case);
      if (schema == null) {
        // Create the schema
        database.createSchema(schema_name, "USER");
        // Set the default grants for the schema
        database.getGrantManager().addGrant(Privileges.SCHEMA_ALL_PRIVS,
                    GrantManager.SCHEMA, schema_name, user.getUserName(),
                    true, Database.INTERNAL_SECURE_USERNAME);
      }
      else {
        throw new DatabaseException("Schema '" + schema_name +
                                    "' already exists.");
      }
    }
    // Is this a drop schema command?
    else if (com.equals("drop")) {
      boolean ignore_case = database.isInCaseInsensitiveMode();
      SchemaDef schema =
                  database.resolveSchemaCase(schema_name, ignore_case);
      // Only allow user to drop USER typed schemas
      if (schema == null) {
        throw new DatabaseException(
                 "Schema '" + schema_name + "' does not exist.");
      }
      else if (schema.getType().equals("USER")) {
        // Check if the schema is empty.
        TableName[] all_tables = database.getTableList();
        String resolved_schema_name = schema.getName();
        for (int i = 0; i < all_tables.length; ++i) {
          if (all_tables[i].getSchema().equals(resolved_schema_name)) {
            throw new DatabaseException(
                          "Schema '" + schema_name + "' is not empty.");
          }
        }
        // Drop the schema
        database.dropSchema(schema.getName());
        // Revoke all the grants for the schema
        database.getGrantManager().revokeAllGrantsOnObject(
                                       GrantManager.SCHEMA, schema.getName());
        
      }
      else {
        throw new DatabaseException(
                                 "Can not drop schema '" + schema_name + "'");
      }
    }
    else {
      throw new DatabaseException("Unrecognised schema command.");
    }

    return FunctionTable.resultTable(context, 0);

  }


}
