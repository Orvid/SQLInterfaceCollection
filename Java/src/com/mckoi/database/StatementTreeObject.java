/**
 * com.mckoi.database.StatementTreeObject  09 Sep 2001
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
 * An complex object that is to be contained within a StatementTree object.
 * A statement tree object must be serializable, and it must be able to
 * reference all Expression objects so that they may be prepared.
 *
 * @author Tobias Downer
 */

public interface StatementTreeObject {

  /**
   * Prepares all expressions in this statement tree object by passing the
   * ExpressionPreparer object to the 'prepare' method of the expression.
   */
  void prepareExpressions(ExpressionPreparer preparer)
                                                    throws DatabaseException;

  /**
   * Performs a DEEP clone of this object if it is mutable, or a deep clone
   * of its mutable members.  If the object is immutable then it may return
   * 'this'.
   */
  Object clone() throws CloneNotSupportedException;

}
