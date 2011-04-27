/**
 * com.mckoi.util.ShellUserTerminal  12 Apr 2001
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

package com.mckoi.util;

/**
 * An implementation of UserTerminal that uses the shell terminal via
 * System.in and System.out.
 *
 * @author Tobias Downer
 */

public class ShellUserTerminal implements UserTerminal {

  // ---------- Implemented from UserTerminal ----------

  public void print(String str) {
    System.out.print(str);
    System.out.flush();
  }

  public void println(String str) {
    System.out.println(str);
  }

  public int ask(String question, String[] options, int default_answer) {
    // TODO
    return default_answer;
  }

}
