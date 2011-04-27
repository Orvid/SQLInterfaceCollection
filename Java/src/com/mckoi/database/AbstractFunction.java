/**
 * com.mckoi.database.AbstractFunction  12 Jul 2000
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

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

/**
 * An abstract implementation of Function.
 *
 * @author Tobias Downer
 */

public abstract class AbstractFunction implements Function {

  /**
   * The name of the function.
   */
  private String name;

  /**
   * The list of expressions this function has as parameters.
   */
  private Expression[] params;

  /**
   * Set to true if this is an aggregate function (requires a group).  It is
   * false by default.
   */
  private boolean is_aggregate;

  /**
   * Constructs the Function with the given expression array as parameters.
   */
  public AbstractFunction(String name, Expression[] params) {
    this.name = name;
    this.params = params;

    is_aggregate = false;
  }

  /**
   * Call this from the constructor if the function is an aggregate.
   */
  protected void setAggregate(boolean status) {
    is_aggregate = status;
  }

  /**
   * Returns the number of parameters for this function.
   */
  public int parameterCount() {
    return params.length;
  }

  /**
   * Returns the parameter at the given index in the parameters list.
   */
  public Expression getParameter(int n) {
    return params[n];
  }

  /**
   * Returns true if the param is the special case glob parameter (*).
   */
  public boolean isGlob() {
    if (params == FunctionFactory.GLOB_LIST) {
      return true;
    }
    if (params.length == 1) {
      Expression exp = params[0];
      return (exp.size() == 1 &&
              new String(exp.text()).equals("*"));
    }
    return false;
  }




  // ---------- Implemented from Function ----------

  /**
   * Returns the name of the function.  The name is a unique identifier that
   * can be used to recreate this function.  This identifier can be used to
   * easily serialize the function when grouped with its parameters.
   */
  public String getName() {
    return name;
  }

  /**
   * Returns the list of all Variable's that are used by this function.  This
   * looks up each expression in the list of parameters.  This will cascade
   * if the expressions have a Function, etc.
   */
  public List allVariables() {
    ArrayList result_list = new ArrayList();
    for (int i = 0; i < params.length; ++i) {
      List l = params[i].allVariables();
      result_list.addAll(l);
    }
    return result_list;
  }

  /**
   * Returns the list of all elements that are used by this function.  This
   * looks up each expression in the list of parameters.  This will cascade
   * if the expressions have a Function, etc.
   */
  public List allElements() {
    ArrayList result_list = new ArrayList();
    for (int i = 0; i < params.length; ++i) {
      List l = params[i].allElements();
      result_list.addAll(l);
    }
    return result_list;
  }

  /**
   * Returns whether the function is an aggregate function or not.
   */
  public final boolean isAggregate(QueryContext context) {
    if (is_aggregate) {
      return true;
    }
    else {
      // Check if arguments are aggregates
      for (int i = 0; i < params.length; ++i) {
        Expression exp = params[i];
        if (exp.hasAggregateFunction(context)) {
          return true;
        }
      }
    }
    return false;
  }

  /**
   * Prepares the parameters of the function.
   */
  public void prepareParameters(ExpressionPreparer preparer)
                                                   throws DatabaseException {
    for (int i = 0; i < params.length; ++i) {
      params[i].prepare(preparer);
    }
  }

  /**
   * The init function.  By default, we don't do anything however this should
   * be overwritten if we need to check the parameter arguments.
   */
  public void init(VariableResolver resolver) {
  }


  /**
   * By Default, we assume a function returns a Numeric object.
   */
  public TType returnTType(VariableResolver resolver, QueryContext context) {
    return returnTType();
  }

  public TType returnTType() {
    return TType.NUMERIC_TYPE;
  }

  // ---------- Convenience methods ----------
  

  public String toString() {
    StringBuffer buf = new StringBuffer();
    buf.append(name);
    buf.append('(');
    for (int i = 0; i < params.length; ++i) {
      buf.append(params[i].text().toString());
      if (i < params.length - 1) {
        buf.append(',');
      }
    }
    buf.append(')');
    return new String(buf);
  }


}
