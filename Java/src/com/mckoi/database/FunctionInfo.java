/**
 * com.mckoi.database.FunctionInfo  17 Aug 2001
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
 * Meta information about a function.  Used to compile information about a
 * particular function.
 *
 * @author Tobias Downer
 */

public interface FunctionInfo {

  /**
   * A type that represents a static function.  A static function is not
   * an aggregate therefore does not require a GroupResolver.  The result of
   * a static function is guarenteed the same given identical parameters over
   * subsequent calls.
   */
  public static final int STATIC      = 1;

  /**
   * A type that represents an aggregate function.  An aggregate function
   * requires the GroupResolver variable to be present in able to resolve the
   * function over some set.  The result of an aggregate function is
   * guarenteed the same given the same set and identical parameters.
   */
  public static final int AGGREGATE   = 2;

  /**
   * A function that is non-aggregate but whose return value is not guarenteed
   * to be the same given the identical parameters over subsequent calls.  This
   * would include functions such as RANDOM and UNIQUEKEY.  The result is
   * dependant on some other state (a random seed and a sequence value).
   */
  public static final int STATE_BASED = 3;



  /**
   * The name of the function as used by the SQL grammar to reference it.
   */
  String getName();

  /**
   * The type of function, either STATIC, AGGREGATE or STATE_BASED (eg. result
   * is not dependant entirely from input but from another state for example
   * RANDOM and UNIQUEKEY functions).
   */
  int getType();

  /**
   * The name of the function factory class that this function is handled by.
   * For example, "com.mckoi.database.InternalFunctionFactory".
   */
  String getFunctionFactoryName();

}
