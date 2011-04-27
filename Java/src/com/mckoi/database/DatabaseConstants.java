/**
 * com.mckoi.database.DatabaseConstants  04 May 1998
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
 * Contant static values that determine several parameters of the database
 * operation.  It is important that a database data generated from a
 * compilation from one set of constants is not used with the same database
 * with different constants.
 * <p>
 * @author Tobias Downer
 */

public interface DatabaseConstants {

  /**
   * The maximum length in characters of the string that represents the name
   * of the database.
   */
  public static final int MAX_DATABASE_NAME_LENGTH = 50;

  /**
   * The maximum length in characters of the string that represents the name
   * of a privaledge group.
   */
  public static final int MAX_PRIVGROUP_NAME_LENGTH = 50;

  /**
   * The maximum length in characters of the string that holds the table
   * name.  The table name is used to reference a Table object in a Database.
   */
  public static final int MAX_TABLE_NAME_LENGTH = 50;

  /**
   * The maximum length in characters of the string that holds the user
   * name.  The user name is used in many security and priviledge operations.
   */
  public static final int MAX_USER_NAME_LENGTH = 50;

  /**
   * The maximum length in character of the string that holds a users
   * password.  The password is used when logging into the database.
   */
  public static final int MAX_PASSWORD_LENGTH = 80;

}
