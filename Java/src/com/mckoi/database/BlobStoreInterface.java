/**
 * com.mckoi.database.BlobStoreInterface  21 Jan 2003
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

import java.io.IOException;
import com.mckoi.database.global.Ref;

/**
 * A very restricted interface for accessing a blob store.  This is used by a
 * MasterTableDataSource implementation to query and resolve blob information.
 *
 * @author Tobias Downer
 */

public interface BlobStoreInterface {

  /**
   * Given a large object reference identifier, generates a Ref implementation
   * that provides access to the information in the large object.  The Ref
   * implementation returned by this object is a read-only static object.
   * This may return either a BlobRef or a ClobRef object depending on the
   * type of the object.
   */
  Ref getLargeObject(long reference_id) throws IOException;

  /**
   * Tells the BlobStore that a static reference has been established in a
   * table to the blob referenced by the given id.  This is used to count
   * references to a blob, and possibly clean up a blob if there are no
   * references remaining to it.
   */
  void establishReference(long reference_id);
  
  /**
   * Tells the BlobStore that a static reference has been released to the
   * given blob.  This would typically be called when the row in the database
   * is removed.
   */
  void releaseReference(long reference_id);
  
}

