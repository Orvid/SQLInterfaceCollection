/**
 * com.mckoi.store.Area  02 Sep 2002
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

package com.mckoi.store;

import java.io.IOException;

/**
 * An interface for access the contents of an area of a store.  The area object
 * maintains a pointer that can be manipulated and read from.
 *
 * @author Tobias Downer
 */

public interface Area {

  /**
   * Returns the unique identifier that represents this area.
   */
  long getID();
  
  /**
   * Returns the current position of the pointer within the area.  The position
   * starts at beginning of the area.
   */
  int position();
  
  /**
   * Returns the capacity of the area.
   */
  int capacity();
  
  /**
   * Sets the position within the area.
   */
  void position(int position) throws IOException;
  
  /**
   * Copies 'size' bytes from the current position of this Area to the
   * destination AreaWriter.
   */
  void copyTo(AreaWriter destination_writer, int size) throws IOException;
  
  // ---------- The get methods ----------
  // Note that these methods will all increment the position by the size of the
  // element read.  For example, 'getInt' will increment the position by 4.

  byte get() throws IOException;

  void get(byte[] buf, int off, int len) throws IOException;
  
  short getShort() throws IOException;
  
  int getInt() throws IOException;
  
  long getLong() throws IOException;
  
  char getChar() throws IOException;

}

