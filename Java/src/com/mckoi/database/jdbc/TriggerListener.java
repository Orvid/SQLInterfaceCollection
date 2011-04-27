/**
 * com.mckoi.database.jdbc.TriggerListener  04 Oct 2000
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
 * A listener that is notified when the trigger being listened to is fired.
 *
 * @author Tobias Downer
 */

public interface TriggerListener {

  /**
   * Notifies this listener that the trigger with the name has been fired.
   * Trigger's are specified via the SQL syntax and a trigger listener can
   * be registered via MckoiConnection.
   *
   * @param trigger_name the name of the trigger that fired.
   */
  void triggerFired(String trigger_name);

}
