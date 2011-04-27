/**
 * com.mckoi.database.VariableResolver  11 Jul 2000
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
 * An interface to resolve a variable name to a constant object.  This is used
 * as a way to resolve a variable into a value to use in an expression.
 *
 * @author Tobias Downer
 */

public interface VariableResolver {

  /**
   * A number that uniquely identifies the current state of the variable
   * resolver.  This typically returns the row_index of the table we are
   * resolving variables on.
   */
  public int setID();

  /**
   * Returns the value of a given variable.
   */
  public TObject resolve(Variable variable);

  /**
   * Returns the TType of object the given variable is.
   */
  public TType returnTType(Variable variable);

}
