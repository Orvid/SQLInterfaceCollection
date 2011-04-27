/**
 * com.mckoi.database.DatabaseProcedure  10 Aug 2000
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
 * This interface represents a database procedure that is executed on the
 * server side.  It is used to perform database specific functions that can
 * only be performed on the server.
 * <p>
 * A procedure must manage its own table locking.
 *
 * @author Tobias Downer
 */

public interface DatabaseProcedure {

  /**
   * Executes the procudure and returns the resultant table.  Note, the
   * args have to be serializable.  There may be only 0 to 16 arguments.
   * The method may throw a 'DatabaseException' if the procedure failed.
   */
  Table execute(User user, Object[] args) throws DatabaseException;

  /**
   * This returns a DataTable[] array that lists the DataTables that are read
   * during this procedure.
   */
  DataTable[] getReadTables(DatabaseConnection db) throws DatabaseException;

  /**
   * Returns a DataTable[] array that lists the DataTables that are written
   * to during this procedure.
   */
  DataTable[] getWriteTables(DatabaseConnection db) throws DatabaseException;

  /**
   * Returns the locking mode in which the database operates.  This is either
   * LockingMechanism.SHARED_MODE or LockingMechanism.EXCLUSIVE_MODE.  In most
   * cases this will be SHARED_MODE.
   */
  int getLockingMode();

  /**
   * Sets the LockHandle object for this procedure.  This should be called
   * after the tables that this procedure uses have been locked.
   */
  void setLockHandle(LockHandle lock_handle);

}
