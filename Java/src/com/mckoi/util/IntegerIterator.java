/**
 * com.mckoi.util.IntegerIterator  02 Jul 2000
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

package com.mckoi.util;

/**
 * An iterator for a list of integer's.
 *
 * @author Tobias Downer
 */

public interface IntegerIterator {

  /**
   * Returns <tt>true</tt> if this list iterator has more elements when
   * traversing the list in the forward direction. (In other words, returns
   * <tt>true</tt> if <tt>next</tt> would return an element rather than
   * throwing an exception.)
   */
  boolean hasNext();

  /**
   * Returns the next element in the list.  This method may be called
   * repeatedly to iterate through the list, or intermixed with calls to
   * <tt>previous</tt> to go back and forth.  (Note that alternating calls
   * to <tt>next</tt> and <tt>previous</tt> will return the same element
   * repeatedly.)
   */
  int next();

  /**
   * Returns <tt>true</tt> if this list iterator has more elements when
   * traversing the list in the reverse direction.  (In other words, returns
   * <tt>true</tt> if <tt>previous</tt> would return an element rather than
   * throwing an exception.)
   */
  boolean hasPrevious();

  /**
   * Returns the previous element in the list.  This method may be called
   * repeatedly to iterate through the list backwards, or intermixed with
   * calls to <tt>next</tt> to go back and forth.  (Note that alternating
   * calls to <tt>next</tt> and <tt>previous</tt> will return the same
   * element repeatedly.)
   */
  int previous();

  /**
   * Removes from the list the last element returned by the iterator.
   * This method can be called only once per call to <tt>next</tt>.  The
   * behavior of an iterator is unspecified if the underlying collection is
   * modified while the iteration is in progress in any way other than by
   * calling this method.
   * <p>
   * Some implementations of IntegerIterator may choose to not implement this
   * method, in which case an appropriate exception is generated.
   */
  void remove();

}
