/**
 * com.mckoi.database.TableCommitModificationEvent  25 Feb 2003
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

import com.mckoi.util.IntegerVector;

/**
 * An object that encapsulates all row modification information about a table
 * when a change to the table is about to be committed.  The object provides
 * information about what rows in the table were changed
 * (inserted/updated/deleted).
 *
 * @author Tobias Downer
 */

public class TableCommitModificationEvent {

  /**
   * A SimpleTransaction that can be used to query tables in the database -
   * the view of which will be the view when the transaction is committed.
   */
  private SimpleTransaction transaction;

  /**
   * The name of the table that is being changed.
   */
  private TableName table_name;

  /**
   * A normalized list of all rows that were added by the transaction being
   * committed.
   */
  private int[] added_rows;

  /**
   * A normalized list of all rows that were removed by the transaction being
   * committed.
   */
  private int[] removed_rows;

  /**
   * Constructs the event.
   */
  public TableCommitModificationEvent(SimpleTransaction transaction,
                          TableName table_name, int[] added, int[] removed) {
    this.transaction = transaction;
    this.table_name = table_name;
    this.added_rows = added;
    this.removed_rows = removed;
  }

  /**
   * Returns the Transaction that represents the view of the database when
   * the changes to the table have been committed.
   */
  public SimpleTransaction getTransaction() {
    return transaction;
  }

  /**
   * Returns the name of the table.
   */
  public TableName getTableName() {
    return table_name;
  }

  /**
   * Returns the normalized list of all rows that were inserted or updated
   * in this table of the transaction being committed.  This is a normalized
   * list which means if a row is inserted and then deleted in the transaction
   * then it is not considered important and does not appear in this list.
   */
  public int[] getAddedRows() {
    return added_rows;
  }

  /**
   * Returns the normalized list of all rows that were deleted or updated 
   * in this table of the transaction being committed.  This is a normalized
   * list which means if a row is inserted and then deleted in the transaction
   * then it is not considered important and does not appear in this list.
   */
  public int[] getRemovedRows() {
    return removed_rows;
  }

}
