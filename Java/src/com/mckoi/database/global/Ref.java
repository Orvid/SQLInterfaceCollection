/**
 * com.mckoi.database.global.Ref  30 Jan 2003
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

import java.io.IOException;

/**
 * An interface that represents a reference to a object that isn't stored in
 * main memory.  The reference to the object is made through the id value
 * returned by the 'getID' method.
 *
 * @author Tobias Downer
 */

public interface Ref {

  /**
   * An id used to reference this object in the context of the database.  Note
   * that once a static reference is made (or removed) to/from this object, the
   * BlobStore should be notified of the reference.  The store will remove an
   * large object that has no references to it.
   */
  long getID();

  /**
   * The type of large object that is being referenced.  2 = binary object,
   * 3 = ASCII character object, 4 = Unicode character object.
   */
  byte getType();
  
  /**
   * The 'raw' size of this large object in bytes when it is in its byte[]
   * form.  This value allows us to know how many bytes we can read from this
   * large object when it's being transferred to the client.
   */
  long getRawSize();
  
  /**
   * Reads a part of this large object from the store into the given byte
   * buffer.  This method should only be used when reading a large object
   * to transfer to the JDBC driver.  It represents the byte[] representation
   * of the object only and is only useful for transferral of the large object.
   */
  void read(long offset, byte[] buf, int length) throws IOException;
  
  /**
   * This method is used to write the contents of the large object into the
   * backing store.  This method will only work when the large object is in
   * an initial 'write' phase in which the client is pushing the contents of
   * the large object onto the server to be stored.
   */
  void write(long offset, byte[] buf, int length) throws IOException;

  /**
   * This method is called when the write phrase has completed, and it marks
   * this large object as complete.  After this method is called the large
   * object reference is a static object that can not be changed.
   */
  void complete() throws IOException;
  
}

