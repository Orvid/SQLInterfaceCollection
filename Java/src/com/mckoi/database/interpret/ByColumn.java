/**
 * com.mckoi.database.interpret.ByColumn  09 Sep 2001
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

/**
 * Object used to represent a column in the 'order by' and 'group by'
 * clauses of a select statement.
 *
 * @author Tobias Downer
 */

public final class ByColumn
            implements java.io.Serializable, StatementTreeObject, Cloneable {

  static final long serialVersionUID = 8194415767416200855L;

  /**
   * The name of the column in the 'by'.
   */
  public Variable name;

  /**
   * The expression that we are ordering by.
   */
  public Expression exp;

  /**
   * If 'order by' then true if sort is ascending (default).
   */
  public boolean ascending = true;


  public void prepareExpressions(ExpressionPreparer preparer)
                                                  throws DatabaseException {
    if (exp != null) {
      exp.prepare(preparer);
    }
  }

  public Object clone() throws CloneNotSupportedException {
    ByColumn v = (ByColumn) super.clone();
    if (name != null) {
      v.name = (Variable) name.clone();
    }
    if (exp != null) {
      v.exp = (Expression) exp.clone();
    }
    return v;
  }

  public String toString() {
    return "ByColumn(" + name + ", " + exp + ", " + ascending + ")";
  }

}
