/**
 * com.mckoi.database.jdbc.MStreamableClob  31 Jan 2003
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
import java.sql.Clob;
import java.sql.SQLException;

/**
 * A Clob that is a large object that may be streamed from the server directly
 * to this object.  A clob that is streamable is only alive for the lifetime of
 * the result set it is part of.  If the underlying result set that contains
 * this streamable clob is closed then this clob is no longer valid.
 *
 * @author Tobias Downer
 */

class MStreamableClob extends AbstractStreamableObject implements Clob {

  /**
   * Constructs the Clob.
   */
  MStreamableClob(MConnection connection, int result_set_id, byte type,
                 long streamable_object_id, long size) {
    super(connection, result_set_id, type, streamable_object_id, size);
  }

  // ---------- Implemented from Blob ----------

  public long length() throws SQLException {
    if (getType() == 4) {
      return rawSize() / 2;
    }
    return rawSize();
  }

  public String getSubString(long pos, int length) throws SQLException {
    int p = (int) (pos - 1);
    Reader reader = getCharacterStream();
    try {
      reader.skip(p);
      StringBuffer buf = new StringBuffer(length);
      for (int i = 0; i < length; ++i) {
        int c = reader.read();
        buf.append((char) c);
      }
      return new String(buf);
    }
    catch (IOException e) {
      e.printStackTrace(System.err);
      throw new SQLException("IO Error: " + e.getMessage());
    }
  }

  public Reader getCharacterStream() throws SQLException {
    if (getType() == 3) {
      return new AsciiReader(new StreamableObjectInputStream(rawSize()));
    }
    else if (getType() == 4) {
      return new BinaryToUnicodeReader(
                             new StreamableObjectInputStream(rawSize()));
    }
    else {
      throw new SQLException("Unknown type.");
    }
  }

  public java.io.InputStream getAsciiStream() throws SQLException {
    if (getType() == 3) {
      return new StreamableObjectInputStream(rawSize());
    }
    else if (getType() == 4) {
      return new AsciiInputStream(getCharacterStream());
    }
    else {
      throw new SQLException("Unknown type.");
    }
  }

  public long position(String searchstr, long start) throws SQLException {
    throw MSQLException.unsupported();
  }
  
  public long position(Clob searchstr, long start) throws SQLException {
    throw MSQLException.unsupported();
  }

  //#IFDEF(JDBC3.0)

  //---------------------------- JDBC 3.0 -----------------------------------

  public int setString(long pos, String str) throws SQLException {
    throw MSQLException.unsupported();
  }

  public int setString(long pos, String str, int offset, int len)
                                                          throws SQLException {
    throw MSQLException.unsupported();
  }

  public java.io.OutputStream setAsciiStream(long pos) throws SQLException {
    throw MSQLException.unsupported();
  }

  public java.io.Writer setCharacterStream(long pos) throws SQLException {
    throw MSQLException.unsupported();
  }

  public void truncate(long len) throws SQLException {
    throw MSQLException.unsupported();
  }

    public void free() throws SQLException {
        //To change body of implemented methods use File | Settings | File Templates.
    }

    public Reader getCharacterStream(long pos, long length) throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    //#ENDIF
  
}

