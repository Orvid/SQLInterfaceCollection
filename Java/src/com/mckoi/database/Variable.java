/**
 * com.mckoi.database.Variable  11 Jul 2000
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
 * This represents a column name that may be qualified.  This object
 * encapsulated a column name that can be fully qualified in the system.  Such
 * uses of this object would not typically be used against any context.  For
 * example, it would not be desirable to use ColumnName in DataTableDef
 * because the column names contained in DataTableDef are within a known
 * context.  This object is intended for use within parser processes where
 * free standing column names with potentially no context are required.
 * <p>
 * NOTE: This object is NOT immutable.
 *
 * @author Tobias Downer
 */

public final class Variable implements java.io.Serializable, Cloneable {

  static final long serialVersionUID = -8772800465139383297L;

  /**
   * Static that represents an unknown table name.
   */
  private static final TableName UNKNOWN_TABLE_NAME =
                                     new TableName("##UNKNOWN_TABLE_NAME##");

  /**
   * The TableName that is the context of this column.  This may be
   * UNKNOWN_TABLE_NAME if the table name is not known.
   */
  private TableName table_name;

  /**
   * The column name itself.
   */
  private String column_name;

  /**
   * Constructs the ColumnName.
   */
  public Variable(TableName table_name, String column_name) {
    if (table_name == null || column_name == null) {
      throw new NullPointerException();
    }
    this.table_name = table_name;
    this.column_name = column_name;
  }

  public Variable(String column_name) {
    this(UNKNOWN_TABLE_NAME, column_name);
  }

  public Variable(Variable v) {
    this.table_name = v.table_name;
    this.column_name = v.column_name;
  }

  /**
   * Returns the TableName context.
   */
  public TableName getTableName() {
    if (!(table_name.equals(UNKNOWN_TABLE_NAME))) {
      return table_name;
    }
    return null;
  }

  /**
   * Returns the column name context.
   */
  public String getName() {
    return column_name;
  }

  /**
   * Attempts to resolve a string '[table_name].[column]' to a Variable
   * instance.
   */
  public static Variable resolve(String name) {
    int div = name.lastIndexOf(".");
    if (div != -1) {
      // Column represents '[something].[name]'
      String column_name = name.substring(div + 1);
      // Make the '[something]' into a TableName
      TableName table_name = TableName.resolve(name.substring(0, div));
      // Set the variable name
      return new Variable(table_name, column_name);
    }
    else {
      // Column represents '[something]'
      return new Variable(name);
    }
  }

  /**
   * Attempts to resolve a string '[table_name].[column]' to a Variable
   * instance.  If the table name does not exist, or the table name schema is
   * not specified, then the schema/table name is copied from the given object.
   */
  public static Variable resolve(TableName tname, String name) {
    Variable v = resolve(name);
    if (v.getTableName() == null) {
      return new Variable(tname, v.getName());
    }
    else if (v.getTableName().getSchema() == null) {
      return new Variable(
          new TableName(tname.getSchema(), v.getTableName().getName()),
                                                              v.getName());
    }
    return v;
  }

  /**
   * Returns a ColumnName that is resolved against a table name context only
   * if the ColumnName is unknown in this object.
   */
  public Variable resolveTableName(TableName tablen) {
    if (table_name.equals(UNKNOWN_TABLE_NAME)) {
      return new Variable(tablen, getName());
    }
    else {
      return new Variable(table_name.resolveSchema(tablen.getSchema()),
                          getName());
    }
  }

  /**
   * Sets this Variable object with information from the given Variable.
   */
  public Variable set(Variable from) {
    this.table_name = from.table_name;
    this.column_name = from.column_name;
    return this;
  }

  /**
   * Sets the column name of this variable.  This should be used if the
   * variable is resolved from one form to another.
   */
  public void setColumnName(String column_name) {
    if (column_name == null) {
      throw new NullPointerException();
    }
    this.column_name = column_name;
  }

  /**
   * Sets the TableName of this variable.
   */
  public void setTableName(TableName tname) {
    if (table_name == null) {
      throw new NullPointerException();
    }
    this.table_name = tname;
  }


  // ----

  /**
   * Performs a deep clone of this object.
   */
  public Object clone() throws CloneNotSupportedException {
    return super.clone();
  }

  /**
   * To string.
   */
  public String toString() {
    if (getTableName() != null) {
      return getTableName() + "." + getName();
    }
    return getName();
  }

  /**
   * To a differently formatted string.
   */
  public String toTechString() {
    TableName tn = getTableName();
    if (tn != null) {
      return tn.getSchema() + "^" + tn.getName() + "^" + getName();
    }
    return getName();
  }
  
  /**
   * Equality.
   */
  public boolean equals(Object ob) {
    Variable cn = (Variable) ob;
    return cn.table_name.equals(table_name) &&
           cn.column_name.equals(column_name);
  }

  /**
   * Comparable.
   */
  public int compareTo(Object ob) {
    Variable cn = (Variable) ob;
    int v = table_name.compareTo(cn.table_name);
    if (v == 0) {
      return column_name.compareTo(cn.column_name);
    }
    return v;
  }

  /**
   * Hash code.
   */
  public int hashCode() {
    return table_name.hashCode() + column_name.hashCode();
  }


//  /**
//   * The name of the variable.
//   */
//  private String name;
//
//  /**
//   * Constructs the variable.
//   */
//  public Variable(String name) {
//    this.name = name;
//  }
//
//  /**
//   * Renames the variable to a new name.  This should only be used as part
//   * of resolving an variable alias or lookup.
//   */
//  public void rename(String name) {
//    this.name = name;
//  }
//
//  /**
//   * Returns the name of the variable.
//   */
//  public String getName() {
//    return name;
//  }
//
//
//
//  public String toString() {
//    return name;
//  }

}
