/**
 * com.mckoi.database.jdbc.MStreamableBlob  22 Jan 2003
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
import java.sql.Blob;
import java.sql.SQLException;

/**
 * A Blob that is a large object that may be streamed from the server directly
 * to this object.  A blob that is streamable is only alive for the lifetime of
 * the result set it is part of.  If the underlying result set that contains
 * this streamable blob is closed then this blob is no longer valid.
 *
 * @author Tobias Downer
 */

class MStreamableBlob extends AbstractStreamableObject implements Blob {

  /**
   * Constructs the blob.
   */
  MStreamableBlob(MConnection connection, int result_set_id, byte type,
                 long streamable_object_id, long size) {
    super(connection, result_set_id, type, streamable_object_id, size);
  }

  // ---------- Implemented from Blob ----------

  public long length() throws SQLException {
    return rawSize();
  }

  public byte[] getBytes(long pos, int length) throws SQLException {
    // First byte is at position 1 according to JDBC Spec.
    --pos;
    if (pos < 0 || pos + length > length()) {
      throw new SQLException("Out of bounds.");
    }

    // The buffer we are reading into
    byte[] buf = new byte[length];
    InputStream i_stream = getBinaryStream();
    try {
      i_stream.skip(pos);
      for (int i = 0; i < length; ++i) {
        buf[i] = (byte) i_stream.read();
      }
    }
    catch (IOException e) {
      e.printStackTrace(System.err);
      throw new SQLException("IO Error: " + e.getMessage());
    }

    return buf;
  }

  public InputStream getBinaryStream() throws SQLException {
    return new StreamableObjectInputStream(rawSize());
  }

  public long position(byte[] pattern, long start) throws SQLException {
    throw MSQLException.unsupported();
  }

  public long position(Blob pattern, long start) throws SQLException {
    throw MSQLException.unsupported();
  }

  //#IFDEF(JDBC3.0)

  // -------------------------- JDBC 3.0 -----------------------------------

  public int setBytes(long pos, byte[] bytes) throws SQLException {
    
    throw MSQLException.unsupported();
  }

  public int setBytes(long pos, byte[] bytes, int offset, int len)
                                                        throws SQLException {
    throw MSQLException.unsupported();
  }

  public java.io.OutputStream setBinaryStream(long pos) throws SQLException {
    throw MSQLException.unsupported();
  }

  public void truncate(long len) throws SQLException {
    throw MSQLException.unsupported();
  }

    public void free() throws SQLException {
        //To change body of implemented methods use File | Settings | File Templates.
    }

    public InputStream getBinaryStream(long pos, long length) throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    //#ENDIF

}

