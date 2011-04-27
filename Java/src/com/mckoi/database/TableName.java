/**
 * com.mckoi.database.TableName  09 Mar 2001
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
 * A name of a table and any associated referencing information.  This object
 * is immutable.
 *
 * @author Tobias Downer
 */

public final class TableName implements Comparable, java.io.Serializable {

  static final long serialVersionUID = 6527135256976754916L;

  /**
   * The constant 'schema_name' that defines a schema that is unknown.
   */
  private static final String UNKNOWN_SCHEMA_NAME = "##UNKNOWN_SCHEMA##";

  /**
   * The name of the schema of the table.  This value can be 'null' which
   * means the schema is currently unknown.
   */
  private final String schema_name;

  /**
   * The name of the table.
   */
  private final String table_name;

  /**
   * Constructs the name.
   */
  public TableName(String schema_name, String table_name) {
    if (table_name == null) {
      throw new NullPointerException("'name' can not be null.");
    }
    if (schema_name == null) {
      schema_name = UNKNOWN_SCHEMA_NAME;
    }

    this.schema_name = schema_name;
    this.table_name = table_name;
  }

  public TableName(String table_name) {
    this(UNKNOWN_SCHEMA_NAME, table_name);
  }

  /**
   * Returns the schema name or null if the schema name is unknown.
   */
  public String getSchema() {
    if (schema_name.equals(UNKNOWN_SCHEMA_NAME)) {
      return null;
    }
    else {
      return schema_name;
    }
  }

  /**
   * Returns the table name.
   */
  public String getName() {
    return table_name;
  }

  /**
   * Resolves a schema reference in a table name.  If the schema in this
   * table is 'null' (which means the schema is unknown) then it is set to the
   * given schema argument.
   */
  public TableName resolveSchema(String scheman) {
    if (schema_name.equals(UNKNOWN_SCHEMA_NAME)) {
      return new TableName(scheman, getName());
    }
    return this;
  }

  /**
   * Resolves a [schema name].[table name] type syntax to a TableName
   * object.  Uses 'schemav' only if there is no schema name explicitely
   * specified.
   */
  public static TableName resolve(String schemav, String namev) {
    int i = namev.indexOf('.');
    if (i == -1) {
      return new TableName(schemav, namev);
    }
    else {
      return new TableName(namev.substring(0, i), namev.substring(i + 1));
    }
  }

  /**
   * Resolves a [schema name].[table name] type syntax to a TableName
   * object.
   */
  public static TableName resolve(String namev) {
    return resolve(UNKNOWN_SCHEMA_NAME, namev);
  }

  // ----

  /**
   * To string.
   */
  public String toString() {
    if (getSchema() != null) {
      return getSchema() + "." + getName();
    }
    return getName();
  }

  /**
   * Equality.
   */
  public boolean equals(Object ob) {
    TableName tn = (TableName) ob;
    return tn.schema_name.equals(schema_name) &&
           tn.table_name.equals(table_name);
  }

  /**
   * Equality but ignore the case.
   */
  public boolean equalsIgnoreCase(TableName tn) {
    return tn.schema_name.equalsIgnoreCase(schema_name) &&
           tn.table_name.equalsIgnoreCase(table_name);
  }

  /**
   * Comparable.
   */
  public int compareTo(Object ob) {
    TableName tn = (TableName) ob;
    int v = schema_name.compareTo(tn.schema_name);
    if (v == 0) {
      return table_name.compareTo(tn.table_name);
    }
    return v;
  }

  /**
   * Hash code.
   */
  public int hashCode() {
    return schema_name.hashCode() + table_name.hashCode();
  }

}
