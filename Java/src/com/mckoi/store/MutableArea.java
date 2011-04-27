/**
 * com.mckoi.store.MutableArea  08 Jun 2003
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
 * An interface for an area that can be modified.  Any changes made to an area
 * may or may not be immediately reflected in already open areas with the same
 * id.  The specification does guarentee that after the 'checkOutAndClose'
 * method is invoked that any new Area or MutableArea objects created by the
 * backing store will contain the changes.
 *
 * @author Tobias Downer
 */

public interface MutableArea extends Area {

  /**
   * Checks out all changes made to this area.  This should be called after a
   * series of updates have been made to the area and the final change is to
   * be 'finalized'.  When this method returns, any new Area or MutableArea
   * objects created by the backing store will contain the changes made to this
   * object.  Any changes made to the Area may or may not be made to any
   * already existing areas.
   * <p>
   * In a logging implementation, this may flush out the changes made to the
   * area in a log.
   */
  void checkOut() throws IOException;

  // ---------- Various put methods ----------

  void put(byte b) throws IOException;

  void put(byte[] buf, int off, int len) throws IOException;

  void put(byte[] buf) throws IOException;

  void putShort(short s) throws IOException;

  void putInt(int i) throws IOException;

  void putLong(long l) throws IOException;

  void putChar(char c) throws IOException;

}

