/**
 * com.mckoi.database.Assignment  18 Jul 2000
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

package com.mckoi.database;

/**
 * An assignment from a variable to an expression.  For example;<p>
 * <pre>
 *   value_of = value_of * 1.10
 *   name = concat("CS-", name)
 *   description = concat("LEGACY: ", upper(number));
 * </pre>
 *
 * @author Tobias Downer
 */

public final class Assignment
            implements StatementTreeObject, java.io.Serializable, Cloneable {

  static final long serialVersionUID = 498589698743066869L;

  /**
   * The Variable that is the lhs of the assignment.
   */
  private Variable variable;

  /**
   * Set expression that is the rhs of the assignment.
   */
  private Expression expression;

  /**
   * Constructs the assignment.
   */
  public Assignment(Variable variable, Expression expression) {
    this.variable = variable;
    this.expression = expression;
  }

  /**
   * Returns the variable for this assignment.
   */
  public Variable getVariable() {
    return variable;
  }

  /**
   * Returns the Expression for this assignment.
   */
  public Expression getExpression() {
    return expression;
  }

  // ---------- Implemented from StatementTreeObject ----------
  public void prepareExpressions(ExpressionPreparer preparer)
                                                    throws DatabaseException {
    if (expression != null) {
      expression.prepare(preparer);
    }
  }

  public Object clone() throws CloneNotSupportedException {
    Assignment v = (Assignment) super.clone();
    v.variable = (Variable) variable.clone();
    v.expression = (Expression) expression.clone();
    return v;
  }

}
