/**
 * com.mckoi.database.DatabaseConstraintViolationException  02 Sep 2001
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
 * A database exception that represents a constraint violation.
 *
 * @author Tobias Downer
 */

public class DatabaseConstraintViolationException extends RuntimeException {

  // ---------- Statics ----------

  /**
   * A Primary Key constraint violation error code.
   */
  public static final int PRIMARY_KEY_VIOLATION = 20;

  /**
   * A Unique constraint violation error code.
   */
  public static final int UNIQUE_VIOLATION = 21;

  /**
   * A Check constraint violation error code.
   */
  public static final int CHECK_VIOLATION = 22;

  /**
   * A Foreign Key constraint violation error code.
   */
  public static final int FOREIGN_KEY_VIOLATION = 23;

  /**
   * A Nullable constraint violation error code (data added to not null
   * columns that was null).
   */
  public static final int NULLABLE_VIOLATION = 24;

  /**
   * Java type constraint violation error code (tried to insert a Java object
   * that wasn't derived from the java object type defined for the column).
   */
  public static final int JAVA_TYPE_VIOLATION = 25;

  /**
   * Tried to drop a table that is referenced by another source.
   */
  public static final int DROP_TABLE_VIOLATION = 26;

  /**
   * Column can't be dropped before of an reference to it.
   */
  public static final int DROP_COLUMN_VIOLATION = 27;


  /**
   * The error code.
   */
  private int error_code;

  /**
   * Constructor.
   */
  public DatabaseConstraintViolationException(int err_code, String msg) {
    super(msg);
    this.error_code = err_code;
  }

  /**
   * Returns the violation error code.
   */
  public int getErrorCode() {
    return error_code;
  }

}
