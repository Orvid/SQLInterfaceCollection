/**
 * com.mckoi.database.CellInputStream  22 Nov 2000
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

package com.mckoi.database;

import java.io.*;

/**
 * An implementation of CellInput that reads data from an underlying stream.
 *
 * @author Tobias Downer
 */

final class CellInputStream implements CellInput {

  /**
   * The parent input stream.
   */
  private InputStream parent_stream;

  /**
   * The Constructor.
   */
  CellInputStream(InputStream parent_stream) {
    setParentStream(parent_stream);
  }

  /**
   * Sets the parent input stream for this stream.  This allows us to
   * recycle this object.
   */
  public void setParentStream(InputStream parent_stream) {
    this.parent_stream = parent_stream;
  }

  public int read() throws IOException {
    return parent_stream.read();
  }

  public int read(byte b[], int off, int len) throws IOException {
    return parent_stream.read(b, off, len);
  }

  public long skip(long n) throws IOException {
    return parent_stream.skip(n);
  }

  public int available() throws IOException {
    return parent_stream.available();
  }

  public void mark(int readAheadLimit) throws IOException {
    parent_stream.mark(readAheadLimit);
  }

  public void reset() throws IOException {
    parent_stream.reset();
  }

  public void close() throws IOException {
    parent_stream.close();
  }


  // ---------- Implemented from DataInput ----------

  public void readFully(byte[] b) throws IOException {
    read(b, 0, b.length);
  }

  public void readFully(byte b[], int off, int len) throws IOException {
    read(b, off, len);
  }

  public int skipBytes(int n) throws IOException {
    return (int) skip(n);
  }

  public boolean readBoolean() throws IOException {
    return (read() != 0);
  }

  public byte readByte() throws IOException {
    return (byte) read();
  }

  public int readUnsignedByte() throws IOException {
    return read();
  }

  public short readShort() throws IOException {
    int ch1 = read();
    int ch2 = read();
    return (short)((ch1 << 8) + (ch2 << 0));
  }

  public int readUnsignedShort() throws IOException {
    int ch1 = read();
    int ch2 = read();
    return (ch1 << 8) + (ch2 << 0);
  }

  public char readChar() throws IOException {
    int ch1 = read();
    int ch2 = read();
    return (char)((ch1 << 8) + (ch2 << 0));
  }

  private char[] char_buffer;

  public String readChars(int length) throws IOException {
    if (length <= 8192) {
      if (char_buffer == null) {
        char_buffer = new char[8192];
      }
      for (int i = 0; i < length; ++i) {
        char_buffer[i] = readChar();
      }
      return new String(char_buffer, 0, length);
    }
    else {
      StringBuffer chrs = new StringBuffer(length);
      for (int i = length; i > 0; --i) {
        chrs.append(readChar());
      }
      return new String(chrs);
    }
  }

  public int readInt() throws IOException {
    int ch1 = read();
    int ch2 = read();
    int ch3 = read();
    int ch4 = read();
    return (int)((ch1 << 24) + (ch2 << 16) +
                 (ch3 << 8)  + (ch4 << 0));
  }

  public long readLong() throws IOException {
    return ((long)(readInt()) << 32) + (readInt() & 0xFFFFFFFFL);
  }

  public float readFloat() throws IOException {
    return Float.intBitsToFloat(readInt());
  }

  public double readDouble() throws IOException {
    return Double.longBitsToDouble(readLong());
  }

  public String readLine() throws IOException {
    throw new Error("Not implemented.");
  }

  public String readUTF() throws IOException {
    throw new Error("Not implemented.");
  }

}
