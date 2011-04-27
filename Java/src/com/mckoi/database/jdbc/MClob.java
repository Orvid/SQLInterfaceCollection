/**
 * com.mckoi.database.jdbc.MClob  31 Jan 2003
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

import java.sql.SQLException;
import java.sql.Clob;
import java.io.StringReader;
import java.io.Reader;

/**
 * An implementation of java.sql.Clob over a java.util.String object.
 *
 * @author Tobias Downer
 */

class MClob implements Clob {

  /**
   * The string the Clob is based on.
   */
  private String str;

  /**
   * Constructs the Clob implementation.
   */
  public MClob(String str) {
    this.str = str;
  }

  // ---------- Implemented from Clob ----------
  
  public long length() throws SQLException {
    return str.length();
  }

  public String getSubString(long pos, int length) throws SQLException {
    int p = (int) (pos - 1);
    return str.substring(p, p + length);
  }

  public Reader getCharacterStream() throws SQLException {
    return new StringReader(str);
  }

  public java.io.InputStream getAsciiStream() throws SQLException {
    return new AsciiInputStream(getCharacterStream());
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

