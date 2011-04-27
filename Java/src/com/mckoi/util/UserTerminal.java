/**
 * com.mckoi.util.UserTerminal  12 Apr 2001
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
 * An interface that represents a terminal that is asked questions in human
 * and machine understandable terms, and sends answers.  This interface is
 * intended for an interface in which the user is asked questions, or for an
 * automated tool.
 *
 * @author Tobias Downer
 */

public interface UserTerminal {

  /**
   * Outputs a string of information to the terminal.
   */
  public void print(String str);

  /**
   * Outputs a string of information and a newline to the terminal.
   */
  public void println(String str);

  /**
   * Asks the user a question from the 'question' string.  The 'options' list
   * is the list of options that the user may select from.  The
   * 'default_answer' is the option that is selected by default.
   */
  public int ask(String question, String[] options, int default_answer);

}
