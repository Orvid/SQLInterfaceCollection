/**
 * com.mckoi.database.interpret.Set  14 Sep 2001
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
import java.math.BigDecimal;
import com.mckoi.database.*;

/**
 * The SQL SET statement.  Sets properties within the current local database
 * connection such as auto-commit mode.
 *
 * @author Tobias Downer
 */

public class Set extends Statement {

  /**
   * The type of set this is.
   */
  String type;

  /**
   * The variable name of this set statement.
   */
  String var_name;

  /**
   * The Expression that is the value to assign the variable to
   * (if applicable).
   */
  Expression exp;

  /**
   * The value to assign the value to (if applicable).
   */
  String value;



  // ---------- Implemented from Statement ----------

  public void prepare() throws DatabaseException {
    type = (String) cmd.getObject("type");
    var_name = (String) cmd.getObject("var_name");
    exp = (Expression) cmd.getObject("exp");
    value = (String) cmd.getObject("value");
  }

  public Table evaluate() throws DatabaseException {

    DatabaseQueryContext context = new DatabaseQueryContext(database);

    String com = type.toLowerCase();

    if (com.equals("varset")) {
      database.setVar(var_name, exp);
    }
    else if (com.equals("isolationset")) {
      value = value.toLowerCase();
      database.setTransactionIsolation(value);
    }
    else if (com.equals("autocommit")) {
      value = value.toLowerCase();
      if (value.equals("on") ||
          value.equals("1")) {
        database.setAutoCommit(true);
      }
      else if (value.equals("off") ||
               value.equals("0")) {
        database.setAutoCommit(false);
      }
      else {
        throw new DatabaseException("Unrecognised value for SET AUTO COMMIT");
      }
    }
    else if (com.equals("schema")) {
      // It's particularly important that this is done during exclusive
      // lock because SELECT requires the schema name doesn't change in
      // mid-process.

      // Change the connection to the schema
      database.setDefaultSchema(value);

    }
    else {
      throw new DatabaseException("Unrecognised set command.");
    }

    return FunctionTable.resultTable(context, 0);

  }


}
