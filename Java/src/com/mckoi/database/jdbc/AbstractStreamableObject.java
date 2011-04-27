/**
 * com.mckoi.database.jdbc.AbstractStreamableObject  31 Jan 2003
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

import java.io.*;
import java.sql.SQLException;
import com.mckoi.util.PagedInputStream;

/**
 * An abstract class that provides various convenience behaviour for
 * creating streamable java.sql.Blob and java.sql.Clob classes.  A streamable
 * object is typically a large object that can be fetched in separate pieces
 * from the server.  A streamable object only survives for as long as the
 * ResultSet that it is part of is open.
 *
 * @author Tobias Downer
 */

abstract class AbstractStreamableObject {

  /**
   * The MConnection object that this object was returned as part of the result
   * of.
   */
  protected final MConnection connection;
  
  /**
   * The result_id of the ResultSet this clob is from.
   */
  protected final int result_set_id;
  
  /**
   * The streamable object identifier.
   */
  private final long streamable_object_id;
  
  /**
   * The type of encoding of the stream.
   */
  private final byte type;
  
  /**
   * The size of the streamable object.
   */
  private final long size;

  /**
   * Constructor.
   */
  AbstractStreamableObject(MConnection connection, int result_set_id,
                           byte type, long streamable_object_id, long size) {
    this.connection = connection;
    this.result_set_id = result_set_id;
    this.type = type;
    this.streamable_object_id = streamable_object_id;
    this.size = size;
  }

  /**
   * Returns the streamable object identifier for referencing this streamable
   * object on the server.
   */
  protected long getStreamableId() {
    return streamable_object_id;
  }
  
  /**
   * Returns the encoding type of this object.
   */
  protected byte getType() {
    return type;
  }
  
  /**
   * Returns the number of bytes in this streamable object.  Note that this
   * may not represent the actual size of the object when it is decoded.  For
   * example, a Clob may be encoded as 2-byte per character (unicode) so the
   * actual length of the clob with be size / 2.
   */
  protected long rawSize() {
    return size;
  }

  
  
  
  
  // ---------- Inner classes ----------

  /**
   * An InputStream that is used to read the data from the streamable object as
   * a basic byte encoding.  This maintains an internal buffer.
   */
  class StreamableObjectInputStream extends PagedInputStream {

    /**
     * The default size of the buffer.
     */
    private final static int B_SIZE = 64 * 1024;

    /**
     * Construct the input stream.
     */
    public StreamableObjectInputStream(long in_size) {
      super(B_SIZE, in_size);
    }

    protected void readPageContent(byte[] buf, long pos, int length)
                                                          throws IOException {
      try {
        // Request a part of the blob from the server
        StreamableObjectPart part = connection.requestStreamableObjectPart(
                       result_set_id, streamable_object_id, pos, length);
        System.arraycopy(part.getContents(), 0, buf, 0, length);
      }
      catch (SQLException e) {
        throw new IOException("SQL Error: " + e.getMessage());
      }
    }
    
  }

}

