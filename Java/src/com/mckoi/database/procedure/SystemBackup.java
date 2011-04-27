/**
 * com.mckoi.database.procedure.SystemBackup  27 Feb 2003
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

package com.mckoi.database.procedure;

import com.mckoi.database.ProcedureConnection;
import com.mckoi.database.ProcedureException;
import java.io.File;
import java.io.IOException;

/**
 * A stored procedure that backs up the entire database to the given directory
 * in the file system.  Requires one parameter, the locate to back up the
 * database to.
 *
 * @author Tobias Downer
 */

public class SystemBackup {

  /**
   * The stored procedure invokation method.
   */
  public static String invoke(ProcedureConnection db_connection,
                              String path) {

    File f = new File(path);
    if (!f.exists() || !f.isDirectory()) {
      throw new ProcedureException("Path '" + path +
                                   "' doesn't exist or is not a directory.");
    }

    try {
      db_connection.getDatabase().liveCopyTo(f);
      return path;
    }
    catch (IOException e) {
      e.printStackTrace();
      throw new ProcedureException("IO Error: " + e.getMessage());
    }

  }

}

