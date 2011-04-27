/**
 * com.mckoi.database.jdbcserver.ServerConnection  21 Jul 2000
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

package com.mckoi.database.jdbcserver;

import com.mckoi.database.Database;
import com.mckoi.database.User;
import java.io.IOException;

/**
 * A server side connection with a client.  Each client that is connected
 * to the database has a ServerConnection object.
 *
 * @author Tobias Downer
 */

interface ServerConnection {

  /**
   * This should return true if it has been determined that there is an
   * entire command waiting to be serviced on this connection.  This method
   * is always run on the same thread for all connections.  It is called
   * many times a second by the connection pool server so it must execute
   * extremely fast.
   * <p>
   * ISSUE: Method is polled!  Unfortunately can't get around this because
   *   of the limitation in Java that TCP connections must block on a thread,
   *   and we can't block if we are to be servicing 100+ connections.
   */
  boolean requestPending() throws IOException;

  /**
   * Processes a pending command on the connection.  This method is called
   * from a database worker thread.  The method will block until a request
   * has been received and processed.  Note, it is not desirable is some
   * cases to allow this method to block.  If a call to 'requestPending'
   * returns true then then method is guarenteed not to block.
   * <p>
   * The first call to this method will handle the hand shaking protocol
   * between the client and server.
   * <p>
   * While this method is doing something, it can not be called again even
   * if another request arrives from the client.  All calls to this method
   * are sequential.  This method will only be called if the 'ping' method is
   * not currently being processed.
   */
  void processRequest() throws IOException;

  /**
   * Blocks until a complete command is available to be processed.  This is
   * used for a blocking implementation.  As soon as this method returns then
   * a call to 'processRequest' will process the incoming command.
   */
  void blockForRequest() throws IOException;

  /**
   * Pings the connection.  This is used to determine if the connection is
   * alive or not.  If it's not, we should throw an IOException.
   * <p>
   * This method will only be called if the 'processRequest' method is not
   * being processed.
   */
  void ping() throws IOException;

  /**
   * Closes this connection.
   */
  void close() throws IOException;


}
