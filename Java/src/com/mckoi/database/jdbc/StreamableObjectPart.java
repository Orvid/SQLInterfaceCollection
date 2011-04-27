/**
 * com.mckoi.database.jdbc.StreamableObjectPart  07 Sep 2002
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

package com.mckoi.database.jdbc;

/**
 * Represents a response from the server for a section of a streamable object.
 * A streamable object can always be represented as a byte[] array and is
 * limited to String (as 2-byte unicode) and binary data types.
 *
 * @author Tobias Downer
 */

public class StreamableObjectPart {

  /**
   * The byte[] array that is the contents of the cell from the server.
   */
  private byte[] part_contents;
  
  /**
   * Constructs the ResultCellPart.  Note that the 'contents' byte array must
   * be immutable.
   */
  public StreamableObjectPart(byte[] contents) {
    this.part_contents = contents;
  }

  /**
   * Returns the contents of this ResultCellPart.
   */
  public byte[] getContents() {
    return part_contents;
  }

}

