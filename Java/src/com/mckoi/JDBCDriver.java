/**
 * com.mckoi.JDBCDriver  22 Jul 2000
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

package com.mckoi;

/**
 * Instance class that registers the mckoi JDBC driver with the JDBC
 * Driver Manager.
 * <p>
 * This class now also extends com.mckoi.database.jdbc.MDriver.
 *
 * @author Tobias Downer
 */

public class JDBCDriver extends com.mckoi.database.jdbc.MDriver {

  /**
   * Just referencing this class will register the JDBC driver.  Any objections
   * to this behaviour?
   */
  static {
    com.mckoi.database.jdbc.MDriver.register();
  }

  /**
   * Constructor.
   */
  public JDBCDriver() {
    super();
    // Or we could move driver registering here...
  }

}
