/**
 * com.mckoi.database.global.StandardMessages  22 May 1998
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
 * This class contains a number of standard messages that are displayed
 * throughout the operation of the database.  They are put into a single class
 * to allow for easy future modification.
 *
 * @author Tobias Downer
 */

public final class StandardMessages {

  /**
   * The name of the author (me).
   */
  public static String AUTHOR = "Tobias Downer";

  /**
   * The standard copyright message.
   */
  public static String COPYRIGHT =
       "Copyright (C) 2000 - 2004 Diehl and Associates, Inc.  " +
       "All rights reserved.";

  /**
   * The global version number of the database system.
   */
  public static String VERSION = "1.0.3";

  /**
   * The global name of the system.
   */
  public static String NAME = "Mckoi SQL Database ( " + VERSION + " )";

}
