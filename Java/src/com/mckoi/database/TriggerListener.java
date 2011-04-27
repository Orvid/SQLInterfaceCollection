/**
 * com.mckoi.database.TriggerListener  02 Oct 2000
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
 * A listener that can listen for high layer trigger events.
 *
 * @author Tobias Downer
 */

public interface TriggerListener {

  /**
   * Notifies that a trigger event fired.
   *
   * @param database the DatabaseConnection that this trigger is registered
   *                 for.
   * @param trigger_evt the trigger event that was fired.
   */
  void fireTrigger(DatabaseConnection database, String trigger_name,
                   TriggerEvent trigger_evt);

}
