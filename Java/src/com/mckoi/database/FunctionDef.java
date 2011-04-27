/**
 * com.mckoi.database.FunctionDef  07 Sep 2001
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
 * A definition of a function including its name and parameters.  A FunctionDef
 * can easily be transformed into a Function object via a set of
 * FunctionFactory instances.
 * <p>
 * NOTE: This object is NOT immutable or thread-safe.  A FunctionDef should not
 *   be shared among different threads.
 *
 * @author Tobias Downer
 */

public final class FunctionDef implements java.io.Serializable, Cloneable {

  static final long serialVersionUID = 3339781003247956829L;

  /**
   * The name of the function.
   */
  private String name;

  /**
   * The list of parameters for the function.
   */
  private Expression[] params;

  /**
   * A cached Function object that was generated when this FunctionDef was
   * looked up.  Note that the Function object is transient.
   */
  private transient Function cached_function;
  
  
  /**
   * Constructs the FunctionDef.
   */
  public FunctionDef(String name, Expression[] params) {
    this.name = name;
    this.params = params;
  }

  /**
   * The name of the function.  For example, 'MIN' or 'CONCAT'.
   */
  public String getName() {
    return name;
  }
  
  /**
   * The list of parameters that are passed to the function.  For example,
   * a concat function may have 7 parameters ('There', ' ', 'are', ' ', 10,
   * ' ', 'bottles.')
   */
  public Expression[] getParameters() {
    return params;
  }

  /**
   * Returns true if this function is an aggregate, or the parameters are
   * aggregates.  It requires a QueryContext object to lookup the function in
   * the function factory database.
   */
  public boolean isAggregate(QueryContext context) {
    FunctionLookup fun_lookup = context.getFunctionLookup();
    boolean is_aggregate = fun_lookup.isAggregate(this);
    if (is_aggregate) {
      return true;
    }
    // Look at params
    Expression[] params = getParameters();
    for (int i = 0; i < params.length; ++i) {
      is_aggregate = params[i].hasAggregateFunction(context);
      if (is_aggregate) {
        return true;
      }
    }
    // No
    return false;
  }

  /**
   * Returns a Function object from this FunctionDef.  Note that two calls to
   * this method will produce the same Function object, however the same
   * Function object will not be produced over multiple instances of
   * FunctionDef even when they represent the same thing.
   */
  public Function getFunction(QueryContext context) {
    if (cached_function != null) {
      return cached_function;
    }
    else {
      FunctionLookup lookup = context.getFunctionLookup();
      cached_function = lookup.generateFunction(this);
      if (cached_function == null) {
        throw new StatementException("Function '" + getName() +
                                     "' doesn't exist.");
      }
      return cached_function;
    }
  }
  
  /**
   * Performs a deep clone of this object.
   */
  public Object clone() throws CloneNotSupportedException {
    FunctionDef v = (FunctionDef) super.clone();
    // Deep clone the parameters
    Expression[] exps = (Expression[]) ((Expression[]) v.params).clone();
    // Clone each element of the array
    for (int n = 0; n < exps.length; ++n) {
      exps[n] = (Expression) exps[n].clone();
    }
    v.params = exps;
    v.cached_function = null;
    return v;
  }

  /**
   * Human understandable string, used for the column title.
   */
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
