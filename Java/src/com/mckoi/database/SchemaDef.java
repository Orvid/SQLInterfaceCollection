/**
 * com.mckoi.database.SchemaDef  29 Aug 2001
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
 * A definition of a schema.
 *
 * @author Tobias Downer
 */

public final class SchemaDef {

  /**
   * The name of the schema (eg. APP).
   */
  private String name;

  /**
   * The type of this schema (eg. SYSTEM, USER, etc)
   */
  private String type;

  /**
   * Constructs the SchemaDef.
   */
  public SchemaDef(String name, String type) {
    this.name = name;
    this.type = type;
  }

  /**
   * Returns the case correct name of the schema.
   */
  public String getName() {
    return name;
  }

  /**
   * Returns the type of this schema.
   */
  public String getType() {
    return type;
  }

  public String toString() {
    return getName();
  }
  
}
