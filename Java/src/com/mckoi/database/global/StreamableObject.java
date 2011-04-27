/**
 * com.mckoi.database.global.StreamableObject  07 Sep 2002
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

/**
 * An object that is streamable (such as a long binary object, or
 * a long string object).  This is passed between client and server and
 * contains basic primitive information about the object it represents.  The
 * actual contents of the object itself must be obtained through other
 * means (see com.mckoi.database.jdbc.DatabaseInterface).
 *
 * @author Tobias Downer
 */

public final class StreamableObject {

  /**
   * The type of the object.
   */
  private byte type;
  
  /**
   * The size of the object in bytes.
   */
  private long size;
  
  /**
   * The identifier that identifies this object.
   */
  private long id;

  /**
   * Constructs the StreamableObject.
   */
  public StreamableObject(byte type, long size, long id) {
    this.type = type;
    this.size = size;
    this.id = id;
  }
  
  /**
   * Returns the type of object this stub represents.  Returns 1 if it
   * represents 2-byte unicde character object, 2 if it represents binary data.
   */
  public byte getType() {
    return type;
  }

  /**
   * Returns the size of the object stream, or -1 if the size is unknown.  If
   * this represents a unicode character string, you would calculate the total
   * characters as size / 2.
   */
  public long getSize() {
    return size;
  }

  /**
   * Returns an identifier that can identify this object within some context.
   * For example, if this is a streamable object on the client side, then the
   * identifier might be the value that is able to retreive a section of the
   * streamable object from the DatabaseInterface.
   */
  public long getIdentifier() {
    return id;
  }
  
}

