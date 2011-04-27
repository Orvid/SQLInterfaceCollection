/**
 * com.mckoi.database.TransactionException  22 Nov 2000
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
 * Thrown when a transaction error happens.  This can only be thrown during
 * the commit process of a transaction.
 *
 * @author Tobias Downer
 */

public class TransactionException extends Exception {

  // The types of transaction exceptions.

  /**
   * Thrown when a transaction deletes or updates a row that another
   * transaction has committed a change to.
   */
  public final static int ROW_REMOVE_CLASH = 1;

  /**
   * Thrown when a transaction drops or alters a table that another transaction
   * has committed a change to.
   */
  public final static int TABLE_REMOVE_CLASH = 2;

  /**
   * Thrown when a transaction adds/removes/modifies rows from a table that
   * has been dropped by another transaction.
   */
  public final static int TABLE_DROPPED = 3;

  /**
   * Thrown when a transaction selects data from a table that has committed
   * changes to it from another transaction.
   */
  public final static int DIRTY_TABLE_SELECT = 4;

  /**
   * Thrown when a transaction conflict occurs and would cause duplicate tables
   * to be created.
   */
  public final static int DUPLICATE_TABLE = 5;



  /**
   * The type of error.
   */
  private int type;

  public TransactionException(int type, String message) {
    super(message);
    this.type = type;
  }

  /**
   * Returns the type of transaction error this is.
   */
  public int getType() {
    return type;
  }


}
