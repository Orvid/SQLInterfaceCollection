/**
 * com.mckoi.database.regexbridge.JavaRegex  06 Mar 2002
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

package com.mckoi.database.regexbridge;

import com.mckoi.database.Table;
import com.mckoi.database.TObject;
import com.mckoi.util.IntegerVector;
import java.util.regex.*;

/**
 * A bridge to the internal Java regular expression library that was introduced
 * in Java 1.4.  This bridge will only work if the regular expression API
 * is available in the class library.  It is not available in 1.3 and 1.2.
 *
 * @author Tobias Downer
 */

public class JavaRegex implements com.mckoi.database.RegexLibrary {

  public boolean regexMatch(String regular_expression, String expression_ops,
                            String value) {
    try {
      // PENDING: Compile and cache most commonly used regular expressions...

      int c_flags = 0;
      if (expression_ops != null) {
        if (expression_ops.indexOf('i') != -1) {
          c_flags += Pattern.CASE_INSENSITIVE;
        }
        if (expression_ops.indexOf('s') != -1) {
          c_flags += Pattern.DOTALL;
        }
        if (expression_ops.indexOf('m') != -1) {
          c_flags += Pattern.MULTILINE;
        }
      }

      Pattern pattern = Pattern.compile(regular_expression, c_flags);
      Matcher matcher = pattern.matcher(value);
      return matcher.matches();
    }
    catch (PatternSyntaxException e) {
      // Incorrect syntax means we always match to false,
      return false;
    }
  }

  public IntegerVector regexSearch(Table table, int column,
                           String regular_expression, String expression_ops) {
    // Get the ordered column,
    IntegerVector row_list = table.selectAll(column);
    // The result matched rows,
    IntegerVector result_list = new IntegerVector();

    // Make into a new list that matches the pattern,
    Pattern pattern;
    try {
      // PENDING: Compile and cache most commonly used regular expressions...

      int c_flags = 0;
      if (expression_ops != null) {
        if (expression_ops.indexOf('i') != -1) {
          c_flags += Pattern.CASE_INSENSITIVE;
        }
        if (expression_ops.indexOf('s') != -1) {
          c_flags += Pattern.DOTALL;
        }
        if (expression_ops.indexOf('m') != -1) {
          c_flags += Pattern.MULTILINE;
        }
      }

      pattern = Pattern.compile(regular_expression, c_flags);
    }
    catch (PatternSyntaxException e) {
      // Incorrect syntax means we always match to an empty list,
      return result_list;
    }

    // For each row in the column, test it against the regular expression.
    int size = row_list.size();
    for (int i = 0; i < size; ++i) {
      int row_index = row_list.intAt(i);
      TObject cell = table.getCellContents(column, row_index);
      // Only try and match against non-null cells.
      if (!cell.isNull()) {
        Object ob = cell.getObject();
        String str = ob.toString();
        // If the column matches the regular expression then return it,
        if (pattern.matcher(str).matches()) {
          result_list.addInt(row_index);
        }
      }
    }

    return result_list;
  }

}
