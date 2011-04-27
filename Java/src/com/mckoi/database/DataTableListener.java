/**
 * com.mckoi.database.DataTableListener  19 Aug 2000
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
 * A DataTableListener is notified of all modifications to the raw entries
 * of the data table.  This listener can be used for detecting changes in
 * VIEWs, for triggers or for caching of common queries.
 *
 * @author Tobias Downer
 */

interface DataTableListener {

  /**
   * Called before a row entry in the table is deleted.
   */
  public void rowDeleted(DataTable table, int row_index);

  /**
   * Called after a row entry in the table is added.
   */
  public void rowAdded(DataTable table, int row_index);






}
