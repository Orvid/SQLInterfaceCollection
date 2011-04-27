/**
 * com.mckoi.database.RegexLibrary  13 Oct 2000
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

import com.mckoi.util.IntegerVector;

/**
 * An interface that links with a Regex library.  This interface allows
 * the database engine to use any regular expression library that this
 * interface can be implemented for.
 *
 * @author Tobias Downer
 */

public interface RegexLibrary {

  /**
   * Matches a regular expression against a string value.  If the value is
   * a match against the expression then it returns true.
   *
   * @param regular_expression the expression to match (eg. "[0-9]+").
   * @param expression_ops expression operator string that specifies various
   *   flags.  For example, "im" is like '/[expression]/im' in Perl.
   * @param value the string to test.
   */
  boolean regexMatch(String regular_expression, String expression_ops,
                     String value);

  /**
   * Performs a regular expression search on the given column of the table.
   * Returns an IntegerVector that contains the list of rows in the table that
   * matched the expression.  Returns an empty list if the expression matched
   * no rows in the column.
   *
   * @param table the table to search for matching values.
   * @param column the column of the table to search for matching values.
   * @param regular_expression the expression to match (eg. "[0-9]+").
   * @param expression_ops expression operator string that specifies various
   *   flags.  For example, "im" is like '/[expression]/im' in Perl.
   */
  IntegerVector regexSearch(Table table, int column,
                            String regular_expression, String expression_ops);

}
