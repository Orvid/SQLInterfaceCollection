/**
 * com.mckoi.database.interpret.SelectColumn  09 Sep 2001
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
 * Represents a column selected to be in the output of a select statement.
 * This includes being either an aggregate function, a column or "*" which
 * is the entire set of columns.
 *
 * @author Tobias Downer
 */

public final class SelectColumn
            implements java.io.Serializable, StatementTreeObject, Cloneable {

  static final long serialVersionUID = 2507375247510606004L;

  /**
   * If the column represents a glob of columns (eg. 'Part.*' or '*') then
   * this is set to the glob string and 'expression' is left blank.
   */
  public String glob_name;

  /**
   * The fully resolved name that this column is given in the resulting table.
   */
  public Variable resolved_name;

  /**
   * The alias of this column string.
   */
  public String alias;

  /**
   * The expression of this column.  This is only NOT set when name == "*"
   * indicating all the columns.
   */
  public Expression expression;

  /**
   * The name of this column used internally to reference it.
   */
  public Variable internal_name;



//  /**
//   * Makes a deep clone of this object.
//   */
//  SelectColumn deepClone() {
//    SelectColumn sc = new SelectColumn();
//    sc.glob_name = glob_name;
//    sc.resolved_name = resolved_name;
//    sc.alias = alias;
//    sc.expression = new Expression(expression);
//    sc.internal_name = internal_name;
//    return sc;
//  }


  // Implemented from StatementTreeObject
  public void prepareExpressions(ExpressionPreparer preparer)
                                                  throws DatabaseException {
    if (expression != null) {
      expression.prepare(preparer);
    }
  }

  public Object clone() throws CloneNotSupportedException {
    SelectColumn v = (SelectColumn) super.clone();
    if (resolved_name != null) {
      v.resolved_name = (Variable) resolved_name.clone();
    }
    if (expression != null) {
      v.expression = (Expression) expression.clone();
    }
    if (internal_name != null) {
      v.internal_name = (Variable) internal_name.clone();
    }
    return v;
  }



  public String toString() {
    String str = "";
    if (glob_name != null) str += " GLOB_NAME = " + glob_name;
    if (resolved_name != null) str += " RESOLVED_NAME = " + resolved_name;
    if (alias != null) str += " ALIAS = " + alias;
    if (expression != null) str += " EXPRESSION = " + expression;
    if (internal_name != null) str += " INTERNAL_NAME = " + internal_name;
    return str;
  }

}
