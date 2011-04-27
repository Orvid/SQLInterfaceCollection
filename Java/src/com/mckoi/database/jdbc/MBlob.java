/**
 * com.mckoi.database.jdbc.MBlob  14 Oct 2000
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

import java.sql.*;
import java.io.*;
import com.mckoi.database.global.ByteLongObject;

/**
 * An implementation of an sql.Blob object.  This implementation keeps the
 * entire Blob in memory.
 * <p>
 * <strong>NOTE:</strong> java.sql.Blob is only available in JDBC 2.0
 *
 * @author Tobias Downer
 */

class MBlob implements Blob {

  /**
   * The ByteLongObject that is a container for the data in this blob.
   */
  private ByteLongObject blob;

  /**
   * Constructs the blob.
   */
  MBlob(ByteLongObject blob) {
    this.blob = blob;
  }

  // ---------- Implemented from Blob ----------

  public long length() throws SQLException {
    return blob.length();
  }

  public byte[] getBytes(long pos, int length) throws SQLException {
    // First byte is at position 1 according to JDBC Spec.
    --pos;
    if (pos < 0 || pos + length > length()) {
      throw new SQLException("Out of bounds.");
    }

    byte[] buf = new byte[length];
    System.arraycopy(blob.getByteArray(), (int) pos, buf, 0, length);
    return buf;
  }

  public InputStream getBinaryStream() throws SQLException {
    return new ByteArrayInputStream(blob.getByteArray(), 0, (int) length());
  }

  public long position(byte[] pattern, long start) throws SQLException {
    byte[] buf = blob.getByteArray();
    int len = (int) length();
    int max = ((int) length()) - pattern.length;

    int i = (int) (start - 1);
    while (true) {
      // Look for first byte...
      while (i <= max && buf[i] != pattern[0]) {
        ++i;
      }
      // Reached end so exit..
      if (i > max) {
        return -1;
      }

      // Found first character, so look for the rest...
      int search_from = i;
      int found_index = 1;
      while ( found_index < pattern.length &&
              buf[search_from] == pattern[found_index] ) {
        ++search_from;
        ++found_index;
      }

      ++i;
      if (found_index >= pattern.length) {
        return (long) i;
      }

    }

  }

  public long position(Blob pattern, long start) throws SQLException {
    byte[] buf;
    // Optimize if MBlob,
    if (pattern instanceof MBlob) {
      buf = ((MBlob) pattern).blob.getByteArray();
    }
    else {
      buf = pattern.getBytes(0, (int) pattern.length());
    }
    return position(buf, start);
  }

  //#IFDEF(JDBC3.0)

  // -------------------------- JDBC 3.0 -----------------------------------

  public int setBytes(long pos, byte[] bytes) throws SQLException {
    throw new SQLException("BLOB updating is not supported");
  }

  public int setBytes(long pos, byte[] bytes, int offset, int len)
                                                        throws SQLException {
    throw new SQLException("BLOB updating is not supported");
  }

  public java.io.OutputStream setBinaryStream(long pos) throws SQLException {
    throw new SQLException("BLOB updating is not supported");
  }

  public void truncate(long len) throws SQLException {
    throw new SQLException("BLOB updating is not supported");
  }

    public void free() throws SQLException {
        //To change body of implemented methods use File | Settings | File Templates.
    }

    public InputStream getBinaryStream(long pos, long length) throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    //#ENDIF

}
