/**
 * com.mckoi.database.ExpressionPreparer  09 Sep 2001
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
 * An interface used to prepare an Expression object.  This interface is used
 * to mutate an element of an Expression from one form to another.  For
 * example, we may use this to translate a StatementTree object to a
 * Statement object.
 *
 * @author Tobias Downer
 */

public interface ExpressionPreparer {

  /**
   * Returns true if this preparer will prepare the given object in an
   * expression.
   */
  boolean canPrepare(Object element);

  /**
   * Returns the new translated object to be mutated from the given element.
   */
  Object prepare(Object element) throws DatabaseException;

}
