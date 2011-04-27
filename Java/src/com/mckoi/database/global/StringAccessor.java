/**
 * com.mckoi.database.global.StringAccessor  30 Jan 2003
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

package com.mckoi.database.global;

import java.io.Reader;

/**
 * An interface used by the engine to access and process strings.  This
 * interface allows us to access the contents of a string that may be
 * implemented in several different ways.  For example, a string may be
 * represented as a java.lang.String object in memeory, or it may be
 * represented as an ASCII sequence in a store.
 *
 * @author Tobias Downer
 */

public interface StringAccessor {

  /**
   * Returns the number of characters in the string.
   */
  public int length();

  /**
   * Returns a Reader that allows the string to be read sequentually from
   * start to finish.
   */
  public Reader getReader();

  /**
   * Returns this string as a java.lang.String object.  Some care may be
   * necessary with this call because a very large string will require a lot
   * space on the heap.
   */
  public String toString();

}

