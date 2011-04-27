/**
 * com.mckoi.database.jdbc.AsciiInputStream  21 Jul 2000
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

/**
 * An InputStream that converts a Reader to a plain ascii stream.  This
 * cuts out the top 8 bits of the unicode char.
 *
 * @author Tobias Downer
 */

class AsciiInputStream extends InputStream { // extends InputStreamFilter {

  private Reader reader;

  public AsciiInputStream(Reader reader) {
    this.reader = reader;
  }

  public AsciiInputStream(String s) {
    this(new StringReader(s));
  }

  public int read() throws IOException {
    int i = reader.read();
    if (i == -1) return i;
    else return (i & 0x0FF);
  }

  public int read(byte[] b, int off, int len) throws IOException {
    int end = off + len;
    int read_count = 0;
    for (int i = off; i < end; ++i) {
      int val = read();
      if (val == -1) {
        if (read_count == 0) {
          return -1;
        }
        else {
          return read_count;
        }
      }
      b[i] = (byte) val;
      ++read_count;
    }
    return read_count;
  }

  public long skip(long n) throws IOException {
    return reader.skip(n);
  }

  public int available() throws IOException {
    // NOTE: This is valid according to JDBC spec.
    return 0;
  }

  public void reset() throws IOException {
    reader.reset();
  }

}
