/**
 * com.mckoi.tools.DBConglomerateRepairTool  11 Apr 2001
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

package com.mckoi.tools;

import com.mckoi.database.*;
import com.mckoi.util.CommandLine;
import com.mckoi.util.ShellUserTerminal;
import com.mckoi.database.control.*;
import java.io.*;

/**
 * A command line repair tool for repairing a corrupted conglomerate.
 *
 * @author Tobias Downer
 */

public class DBConglomerateRepairTool {

  private static void repair(String path, String name) {

    ShellUserTerminal terminal = new ShellUserTerminal();

    TransactionSystem system = new TransactionSystem();
    DefaultDBConfig config = new DefaultDBConfig();
    config.setDatabasePath(path);
    config.setLogPath("");
    config.setMinimumDebugLevel(50000);
    // We do not use the NIO API for repairs for safety.
    config.setValue("do_not_use_nio_api", "enabled");
    system.setDebugOutput(new StringWriter());
    system.init(config);
    final TableDataConglomerate conglomerate =
                     new TableDataConglomerate(system, system.storeSystem());
    // Check it.
    conglomerate.fix(name, terminal);

    // Dispose the transaction system
    system.dispose();
  }

  /**
   * Prints the syntax.
   */
  private static void printSyntax() {
    System.out.println("DBConglomerateRepairTool -path [data directory] " +
                       "[-name [database name]]");
  }

  /**
   * Application start point.
   */
  public static void main(String[] args) {
    CommandLine cl = new CommandLine(args);

    String path = cl.switchArgument("-path");
    String name = cl.switchArgument("-name", "DefaultDatabase");

    if (path == null) {
      printSyntax();
      System.out.println("Error: -path not found on command line.");
      System.exit(-1);
    }

    // Start the tool.
    repair(path, name);

  }


}
