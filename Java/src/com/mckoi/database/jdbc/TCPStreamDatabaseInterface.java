/**
 * com.mckoi.database.jdbc.TCPStreamDatabaseInterface  16 Aug 2000
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

import java.io.*;
import java.sql.*;
import java.net.*;

/**
 * Connection to the database via the TCP protocol.
 *
 * @author Tobias Downer
 */

class TCPStreamDatabaseInterface extends StreamDatabaseInterface {

  /**
   * The name of the host we are connected to.
   */
  private String host;

  /**
   * The port we are connected to.
   */
  private int port;

  /**
   * The Socket connection.
   */
  private Socket socket;

  /**
   * Constructor.
   */
  TCPStreamDatabaseInterface(String host, int port) {
    this.host = host;
    this.port = port;
  }

  /**
   * Connects to the database.
   */
  void connectToDatabase() throws SQLException {
    if (socket != null) {
      throw new SQLException("Connection already established.");
    }
    try {
      // Open a socket connection to the server.
      socket = new Socket(host, port);
      // Setup the stream with the given input and output streams.
      setup(socket.getInputStream(), socket.getOutputStream());
    }
    catch (IOException e) {
      throw new SQLException(e.getMessage());
    }
  }

}
