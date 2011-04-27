/**
 * com.mckoi.database.WorkerThread  09 Sep 2000
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

import com.mckoi.debug.*;

/**
 * This is a worker thread.  This is given commands to execute by the
 * WorkerPool.
 *
 * @author Tobias Downer
 */

final class WorkerThread extends Thread {

  /**
   * If this is set to true, the server displays the time each executed
   * command took.
   */
  private static final boolean DISPLAY_COMMAND_TIME = false;

  /**
   * Set to true to turn off this worker thread.
   */
  private boolean shutdown;

  /**
   * The Runnable command we are currently processing.
   */
  private Runnable command;

  /**
   * The time the command was started.
   */
  private long start_time;

  /**
   * The WorkerPool object that this worker thread is for.
   */
  private WorkerPool worker_pool;

  /**
   * Constructs the thread.
   */
  public WorkerThread(WorkerPool worker_pool) {
    super();
//    setDaemon(true);
    setName("Mckoi - Worker");
    this.worker_pool = worker_pool;
    command = null;
    shutdown = false;
  }

  /**
   * Returns a DebugLogger object we can use to log debug messages.
   */
  public final DebugLogger Debug() {
    return worker_pool.Debug();
  }

  // ---------- Other methods ----------

  /**
   * Shuts down this worker thread.
   */
  synchronized void shutdown() {
    shutdown = true;
    notifyAll();
  }

  /**
   * Tells the worker thread that the user is executing the given command.
   */
  void execute(User user, DatabaseConnection database_connection,
               Runnable runner) {
    // This should help to prevent deadlock
    synchronized (this) {
      if (command == null) {
        this.command = runner;
        notifyAll();
      }
      else {
        throw new RuntimeException(
              "Deadlock Error, tried to execute command on running worker.");
      }
    }
  }

  /**
   * Starts executing this worker thread.
   */
  public synchronized void run() {
    while (true) {
      try {
        // Is there any command waiting to be executed?
        if (command != null) {
          try {
            // Record the time this command was started.
            start_time = System.currentTimeMillis();
            // Run the command
            command.run();
          }
          finally {
            command = null;
            // Record the time the command ended.
            long elapsed_time = System.currentTimeMillis() - start_time;
            if (DISPLAY_COMMAND_TIME) {
              System.err.print("[Worker] Completed command in ");
              System.err.print(elapsed_time);
              System.err.print(" ms.  ");
              System.err.println(this);
            }
          }
        }

        // Notifies the thread pool manager that this worker is ready
        // to go.
        worker_pool.notifyWorkerReady(this);
        // NOTE: The above command may cause a command to be posted on this
        //   worker.
        while (command == null) {
          try {
            // Wait until there is a new command to process.
            wait();
          }
          catch (InterruptedException e) { /* ignore */ }
          // Shut down if we need to...
          if (shutdown) {
            return;
          }
        }

      }
      catch (Throwable e) {
        Debug().write(Lvl.ERROR, this,
            "Worker thread interrupted because of exception:\n" +
            e.getMessage());
        Debug().writeException(e);
      }
    }
  }

}
