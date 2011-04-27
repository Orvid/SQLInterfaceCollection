/**
 * com.mckoi.database.jdbc.DatabaseCallBack  02 Oct 2000
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
 * An interface that is input to the DatabaseInterface as a way to be
 * notified of event information from inside the database.
 *
 * @author Tobias Downer
 */

public interface DatabaseCallBack {

  /**
   * Called when the database has generated an event that this user is
   * listening for.
   * <p>
   * NOTE: The thread that calls back these events is always a volatile
   *   thread that may not block.  It is especially important that no queries
   *   are executed when this calls back.  To safely act on events, it is
   *   advisable to dispatch onto another thread such as the
   *   SwingEventDispatcher thread.
   */
  void databaseEvent(int event_type, String event_message);

}
