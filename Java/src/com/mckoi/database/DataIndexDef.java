/**
 * com.mckoi.database.DataIndexDef  07 Sep 2002
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

import java.io.DataInput;
import java.io.DataOutput;
import java.io.IOException;

/**
 * Represents index meta-information on a table.  This information is part of
 * DataIndexSetDef and is stored with the contents of a table.
 *
 * @author Tobias Downer
 */

public class DataIndexDef {

  /**
   * The name of this index.
   */
  private String index_name;

  /**
   * The list of column name that this index represents.  For example, if this
   * is a composite primary key, this would contain each column name in the
   * primary key.
   */
  private String[] column_names;

  /**
   * Returns the index set pointer of this index.  This value is used when
   * requesting the index from an IndexSet.
   */
  private int index_pointer;

  /**
   * The type of Index this is.  Currently only 'BLIST' is supported.
   */
  private String index_type;

  /**
   * True if this index may only contain unique values.
   */
  private boolean unique;

  /**
   * Constructor.
   */
  public DataIndexDef(String index_name, String[] column_names,
                      int index_pointer, String index_type, boolean unique) {

    this.index_name = index_name;
    this.column_names = (String[]) column_names.clone();
    this.index_pointer = index_pointer;
    this.index_type = index_type;
    this.unique = unique;

  }

  public DataIndexDef(DataIndexDef def) {
    this(def.index_name, def.column_names, def.index_pointer, def.index_type,
         def.unique);
  }
  
  /**
   * Returns the name of this index.
   */
  public String getName() {
    return index_name;
  }

  /**
   * Returns the column names that make up this index.
   */
  public String[] getColumnNames() {
    return column_names;
  }

  /**
   * Returns the pointer to the index in the IndexSet.
   */
  public int getPointer() {
    return index_pointer;
  }
  
  /**
   * Returns a String that describes the type of index this is.
   */
  public String getType() {
    return index_type;
  }
  
  /**
   * Returns true if this is a unique index.
   */
  public boolean isUniqueIndex() {
    return unique;
  }
  
  /**
   * Writes this object to the given DataOutputStream.
   */
  public void write(DataOutput dout) throws IOException {
    dout.writeInt(1);
    dout.writeUTF(index_name);
    dout.writeInt(column_names.length);
    for (int i = 0; i < column_names.length; ++i) {
      dout.writeUTF(column_names[i]);
    }
    dout.writeInt(index_pointer);
    dout.writeUTF(index_type);
    dout.writeBoolean(unique);
  }

  /**
   * Reads a DataIndexDef from the given DataInput object.
   */
  public static DataIndexDef read(DataInput din) throws IOException {
    int version =  din.readInt();
    if (version != 1) {
      throw new IOException("Don't understand version.");
    }
    String index_name = din.readUTF();
    int sz = din.readInt();
    String[] cols = new String[sz];
    for (int i = 0; i < sz; ++i) {
      cols[i] = din.readUTF();
    }
    int index_pointer = din.readInt();
    String index_type = din.readUTF();
    boolean unique = din.readBoolean();
    
    return new DataIndexDef(index_name, cols,
                            index_pointer, index_type, unique);
  }
  
}

