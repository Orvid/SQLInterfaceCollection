/**
 * com.mckoi.database.GroupResolver  14 Jul 2000
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

import com.mckoi.util.IntegerVector;

/**
 * Similar to VariableResolver, this method is used by grouping Functions to
 * find information about the current group being evaluated (used for
 * evaluating aggregate functions).
 *
 * @author Tobias Downer
 */

public interface GroupResolver {

  /**
   * A number that uniquely identifies this group from all the others in the
   * set of groups.
   */
  public int groupID();

  /**
   * The total number of set items in this group.
   */
  public int size();

  /**
   * Returns the value of a variable of a group.  The set index signifies the
   * set item of the group.  For example, if the group contains 10 items, then
   * set_index may be between 0 and 9.  Return types must be either
   * a String, BigDecimal or Boolean.
   */
  public TObject resolve(Variable variable, int set_index);

  /**
   * Returns a VariableResolver that can be used to resolve variable in the
   * get set of the group.  The object returned is undefined after the next
   * call to this method.
   */
  public VariableResolver getVariableResolver(int set_index);

}
