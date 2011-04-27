/**
 * com.mckoi.database.RawDiagnosticTable  29 Nov 2000
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

/**
 * An interface that allows for the inspection and repair of the raw data
 * in a file.  This is used for table debugging and the repair of damaged
 * files.
 *
 * @author Tobias Downer
 */

public interface RawDiagnosticTable {

  /**
   * Statics that represent the various states of a record.
   */
  public final static int UNCOMMITTED = 1,
                          COMMITTED_ADDED = 2,
                          COMMITTED_REMOVED = 3,
                          DELETED = 4;     // ie. available for reclaimation.

  /**
   * Denotes an erroneous record state.
   */
  public final static int RECORD_STATE_ERROR = 0;

  // ---------- Query Methods ----------

  /**
   * Returns the number of physical records in the table.  This includes
   * records that are uncommitted, deleted, committed removed and committed
   * added.
   */
  int physicalRecordCount();

  /**
   * Returns the DataTableDef object that describes the logical topology of
   * the columns in this table.
   */
  DataTableDef getDataTableDef();

  /**
   * Returns the state of the given record index.  The state of a row is
   * either UNCOMMITTED, COMMITTED ADDED, COMMITTED REMOVED or DELETED.
   * record_index should be between 0 and physicalRecordCount.
   */
  int recordState(int record_index);

  /**
   * The number of bytes the record takes up on the underlying media.
   */
  int recordSize(int record_index);

  /**
   * Returns the contents of the given cell in this table.  If the system is
   * unable to return a valid cell then an exception is thrown.
   */
  TObject getCellContents(int column, int record_index);

  /**
   * Returns any misc information regarding this row as a human readable
   * string.  May return null if there is no misc information associated with
   * this record.
   */
  String recordMiscInformation(int record_index);

}
