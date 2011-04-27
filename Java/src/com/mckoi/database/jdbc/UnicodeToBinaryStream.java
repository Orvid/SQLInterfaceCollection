/**
 * com.mckoi.database.jdbc.UnicodeToBinaryStream  29 Jan 2003
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

import java.io.InputStream;
import java.io.IOException;
import java.io.BufferedReader;
import java.io.Reader;

/**
 * An object that wraps around a Reader and translates the unicode stream into
 * a stream of bytes that the database is able to transfer to the database.
 * This object simply converts each char from the Reader into two bytes.  See
 * also BinaryToUnicodeReader for the Reader version of this class.
 *
 * @author Tobias Downer
 */

final class UnicodeToBinaryStream extends InputStream {

  /**
   * The Reader we are wrapping.
   */
  private Reader reader;
  
  /**
   * If this is 0 we are on the left byte of the character.  If this is 1 we
   * are on the right byte of the current character.
   */
  private int lr_byte;

  /**
   * The current character if 'lr_byte' is 1.
   */
  private int current_c;
  
  /**
   * Constructs the stream.
   */
  public UnicodeToBinaryStream(Reader reader) {
    // Note, we wrap the input Reader around a BufferedReader.
    // This is a bit lazy.  Perhaps a better implementation of this would
    // implement 'read(byte[] buf, ...)' and provide its own buffering.
    this.reader = new BufferedReader(reader);
    lr_byte = 0;
  }
  
  /**
   * Reads the next character from the stream.
   */
  public int read() throws IOException {
    if (lr_byte == 0) {
      current_c = reader.read();
      if (current_c == -1) {
        return -1;
      }
      lr_byte = 1;
      return (current_c >> 8) & 0x0FF;
    }
    else {
      lr_byte = 0;
      return current_c & 0x0FF;
    }
  }

  public int available() throws IOException {
    return 0;
  }

}

