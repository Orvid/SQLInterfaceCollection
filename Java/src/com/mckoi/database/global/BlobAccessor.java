/**
 * com.mckoi.database.global.BlobAccessor  20 Jan 2003
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

package com.mckoi.database.global;

/**
 * An interface that provides access to basic information about a BLOB so that
 * we may compare BLOBs implemented in different ways.
 *
 * @author Tobias Downer
 */

public interface BlobAccessor {

  /**
   * Returns the size of the BLOB.
   */
  int length();
  
  /**
   * Returns an InputStream that allows us to read the contents of the blob
   * from start to finish.  This object should be wrapped in a
   * BufferedInputStream if 'read()' type efficiency is required.
   */
  java.io.InputStream getInputStream();

}

