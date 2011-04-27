/**
 * com.mckoi.database.FunctionLookup  07 Sep 2001
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
 * An interface that resolves and generates a Function objects given a
 * FunctionDef object.
 *
 * @author Tobias Downer
 */

public interface FunctionLookup {

  /**
   * Generate the Function given a FunctionDef object.  Returns null if the
   * FunctionDef can not be resolved to a valid function object.  If the
   * specification of the function is invalid for some reason (the number or
   * type of the parameters is incorrect) then a StatementException is thrown.
   */
  Function generateFunction(FunctionDef function_def);

  /**
   * Returns true if the function defined by FunctionDef is an aggregate
   * function, or false otherwise.
   */
  boolean isAggregate(FunctionDef function_def);
  
}
