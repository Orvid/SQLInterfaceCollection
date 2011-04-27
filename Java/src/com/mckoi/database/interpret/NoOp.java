/**
 * com.mckoi.database.interpret.NoOp  14 Sep 2001
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
 * A no operation statement.
 *
 * @author Tobias Downer
 */

public class NoOp extends Statement {

  // ---------- Implemented from Statement ----------

  public void prepare() throws DatabaseException {
    // Nothing to prepare
  }

  public Table evaluate() throws DatabaseException {
    // No-op returns a result value of '0'
    return FunctionTable.resultTable(new DatabaseQueryContext(database), 0);
  }

}
