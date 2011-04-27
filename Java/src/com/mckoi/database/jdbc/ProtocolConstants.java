/**
 * com.mckoi.database.jdbc.ProtocolConstants  20 Jul 2000
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

/**
 * Constants used in the JDBC database communication protocol.
 *
 * @author Tobias Downer
 */

public interface ProtocolConstants {

  /**
   * Sent as an acknowledgement to a command.
   */
  public int ACKNOWLEDGEMENT            = 5;

  /**
   * Sent if login passed.
   */
  public int USER_AUTHENTICATION_PASSED = 10;

  /**
   * Sent if login failed because username or password were invalid.
   */
  public int USER_AUTHENTICATION_FAILED = 15;

  /**
   * Operation was successful.
   */
  public int SUCCESS                    = 20;

  /**
   * Operation failed (followed by a UTF String error message).
   */
  public int FAILED                     = 25;


  /**
   * Operation threw an exception.
   */
  public int EXCEPTION                  = 30;

  /**
   * There was an authentication error.  A query couldn't be executed because
   * the user does not have enough rights.
   */
  public int AUTHENTICATION_ERROR       = 35;





  // ---------- Commands ----------

  /**
   * Query sent to the server for processing.
   */
  public int QUERY                      = 50;

  /**
   * Disposes the server-side resources associated with a result.
   */
  public int DISPOSE_RESULT             = 55;

  /**
   * Requests a section of a result from the server.
   */
  public int RESULT_SECTION             = 60;

  /**
   * Requests a section of a streamable object from the server.
   */
  public int STREAMABLE_OBJECT_SECTION  = 61;

  /**
   * Disposes of the resources associated with a streamable object on the
   * server.
   */
  public int DISPOSE_STREAMABLE_OBJECT  = 62;

  /**
   * For pushing a part of a streamable object onto the server from the client.
   */
  public int PUSH_STREAMABLE_OBJECT_PART = 63;
  

  
  
  /**
   * Ping command.
   */
  public int PING                       = 65;

  /**
   * Closes the protocol stream.
   */
  public int CLOSE                      = 70;

  /**
   * Denotes an event from the database (trigger, etc).
   */
  public int DATABASE_EVENT             = 75;

  /**
   * Denotes a server side request for information.  For example, a request for
   * a part of a streamable object.
   */
  public int SERVER_REQUEST             = 80;
  

}
