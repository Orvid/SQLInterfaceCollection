/**
 * com.mckoi.database.jdbc.LocalBootable  16 Aug 2000
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

import com.mckoi.database.control.DBConfig;
import java.io.File;
import java.sql.SQLException;

/**
 * An interface that is implemented by an object that boots up the database.
 * This is provided as an interface so that we aren't dependant on the
 * entire database when compiling the JDBC code.
 *
 * @author Tobias Downer
 */

public interface LocalBootable {

  /**
   * Attempts to create a new database system with the given name, and the
   * given username/password as the admin user for the system.  Once created,
   * the newly created database will be booted up.
   *
   * @param config the configuration variables.
   * @returns a DatabaseInterface for talking to the database.
   */
  DatabaseInterface create(String username, String password,
                           DBConfig config) throws SQLException;

  /**
   * Boots the database with the given configuration.
   *
   * @param config the configuration variables.
   * @returns a DatabaseInterface for talking to the database.
   */
  DatabaseInterface boot(DBConfig config) throws SQLException;

  /**
   * Attempts to test if the database exists or not.  Returns true if the
   * database exists.
   *
   * @param config the configuration variables.
   */
  boolean checkExists(DBConfig config) throws SQLException;

  /**
   * Returns true if there is a database currently booted in the current
   * JVM.  Otherwise returns false.
   */
  boolean isBooted() throws SQLException;

  /**
   * Connects this interface to the database currently running in this JVM.
   *
   * @returns a DatabaseInterface for talking to the database.
   */
  DatabaseInterface connectToJVM() throws SQLException;

}
