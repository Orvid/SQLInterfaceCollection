/**
 * com.mckoi.database.jdbcserver.ConnectionPoolServer  21 Jul 2000
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

/**
 * An interface for the connection pool for a server.  This is the API for
 * a service that accepts connections via 'addConnection', waits for the
 * connection to make a request, and dispatch the request as appropriate to
 * the database engine.
 * <p>
 * This interface is used to provide different implementations for command
 * dispatching mechanisms, such as a thread per TCP user, one thread per
 * TCP connection set, UDP, etc.
 *
 * @author Tobias Downer
 */

interface ConnectionPoolServer {

  /**
   * Connects a new ServerConnection into the pool of connections to clients
   * that this server maintains.
   */
  void addConnection(ServerConnection connection);

  /**
   * Closes this connection pool server down.
   */
  void close();

}
