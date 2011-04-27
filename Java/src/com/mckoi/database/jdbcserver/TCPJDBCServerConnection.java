/**
 * com.mckoi.database.jdbcserver.TCPJDBCServerConnection  22 Jul 2000
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

import com.mckoi.database.jdbc.DatabaseInterface;
import com.mckoi.debug.DebugLogger;

import java.net.Socket;
import java.io.*;

/**
 * A ServerConnection that processes JDBC queries from a client from a
 * TCP Socket.
 *
 * @author Tobias Downer
 */

final class TCPJDBCServerConnection extends StreamJDBCServerConnection {

  /**
   * The socket connection with the client.
   */
  private Socket connection;

  /**
   * Is set to true when the connection to the client is closed.
   */
  private boolean is_closed = false;

  /**
   * Constructs the ServerConnection object.
   */
  TCPJDBCServerConnection(DatabaseInterface db_interface,
                     Socket socket, DebugLogger logger) throws IOException {
    super(db_interface, socket.getInputStream(),
          socket.getOutputStream(), logger);
    this.connection = socket;
  }

  /**
   * Completely closes the connection to the client.
   */
  public void close() throws IOException {
    try {
      // Dispose the processor
      dispose();
    }
    catch (Throwable e) { e.printStackTrace(); }
    // Close the socket
    connection.close();
    is_closed = true;
  }

  /**
   * Returns true if the connection to the client has been closed.
   */
  public boolean isClosed() throws IOException {
    return is_closed;
  }

}
