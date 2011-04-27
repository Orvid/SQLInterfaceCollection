/**
 * com.mckoi.database.User  22 Jul 2000
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

import com.mckoi.util.Cache;

/**
 * Encapsulates the information about a single user logged into the system.
 * The class provides access to information in the user database.
 * <p>
 * This object also serves as a storage for session state information.  For
 * example, this object stores the triggers that this session has created.
 * <p>
 * NOTE: This object is not immutable.  The same user may log into the system
 *   and it will result in a new User object being created.
 *
 * @author Tobias Downer
 */

public final class User {

  /**
   * The name of the user.
   */
  private String user_name;

  /**
   * The database object that this user is currently logged into.
   */
  private Database database;

  /**
   * The connection string that identifies how this user is connected to the
   * database.
   */
  private String connection_string;

  /**
   * The time this user connected.
   */
  private long time_connected;

  /**
   * The last time this user executed a command on the connection.
   */
  private long last_command_time;

  /**
   * The Constructor.  This takes a user name and gets the privs for them.
   * <p>
   * Note that this method should only be created from within a Database
   * object.
   */
  User(String user_name, Database database,
       String connection_string, long time_connected) {
    this.user_name = user_name;
    this.database = database;
    this.connection_string = connection_string;
    this.time_connected = time_connected;
    this.last_command_time = time_connected;
  }

  /**
   * Returns the name of the user.
   */
  public String getUserName() {
    return user_name;
  }

  /**
   * Returns the string that describes how this user is connected to the
   * engine.  This is set by the protocol layer.
   */
  public String getConnectionString() {
    return connection_string;
  }

  /**
   * Returns the time the user connected.
   */
  public long getTimeConnected() {
    return time_connected;
  }

  /**
   * Returnst the last time a command was executed by this user.
   */
  public long getLastCommandTime() {
    return last_command_time;
  }

  /**
   * Returns the Database object that this user belongs to.
   */
  public Database getDatabase() {
    return database;
  }

  /**
   * Refreshes the last time a command was executed by this user.
   */
  public final void refreshLastCommandTime() {
    last_command_time = System.currentTimeMillis();
  }

  /**
   * Logs out this user object.  This will log the user out of the user manager.
   */
  public void logout() {
    // Clear all triggers for this user,
    UserManager user_manager = database.getUserManager();
    if (user_manager != null) {
      user_manager.userLoggedOut(this);
    }
  }

}
