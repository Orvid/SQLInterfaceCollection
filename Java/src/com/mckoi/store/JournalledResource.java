/**
 * com.mckoi.store.JournalledResource  11 Jun 2003
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

package com.mckoi.store;

import java.io.IOException;

/**
 * An interface that allows for the reading and writing of pages to/from a
 * journalled.
 *
 * @author Tobias Downer
 */

interface JournalledResource {

  /**
   * Returns the page size.
   */
  int getPageSize();

  /**
   * Returns a unique id for this resource.
   */
  long getID();
  
  /**
   * Reads a page of some previously specified size into the byte array.
   */
  void read(long page_number, byte[] buf, int off) throws IOException;

  /**
   * Writes a page of some previously specified size to the top log.  This will
   * add a single entry to the log and any 'read' operations after will contain
   * the written data.
   */
  void write(long page_number, byte[] buf, int off, int len) throws IOException;

  /**
   * Sets the new size of the resource.  This will add a single entry to the
   * log.
   */
  void setSize(long size) throws IOException;
  
  /**
   * Returns the current size of this resource.
   */
  long getSize() throws IOException;
  
  /**
   * Opens the resource.
   */
  void open(boolean read_only) throws IOException;
  
  /**
   * Closes the resource.  This will actually simply log that the resource has
   * been closed.
   */
  void close() throws IOException;

  /**
   * Deletes the resource.  This will actually simply log that the resource has
   * been deleted.
   */
  void delete() throws IOException;

  /**
   * Returns true if the resource currently exists.
   */
  boolean exists();
  
}

