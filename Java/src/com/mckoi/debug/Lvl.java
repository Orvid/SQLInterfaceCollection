/**
 * com.mckoi.debug.Lvl  28 Mar 2002
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

package com.mckoi.debug;

/**
 * Debug level static values.
 *
 * @author Tobias Downer
 */

public interface Lvl {

  /**
   * Some sample debug levels.
   */
  public final static int INFORMATION = 10;    // General processing 'noise'
  public final static int WARNING     = 20;    // A message of some importance
  public final static int ALERT       = 30;    // Crackers, etc
  public final static int ERROR       = 40;    // Errors, exceptions
  public final static int MESSAGE     = 10000; // Always printed messages
                                               // (not error's however)

}
