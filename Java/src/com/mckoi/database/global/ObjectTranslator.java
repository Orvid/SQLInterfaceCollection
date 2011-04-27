/**
 * com.mckoi.database.global.ObjectTranslator  09 Feb 2001
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

package com.mckoi.database.global;

import java.util.Date;
import com.mckoi.util.BigNumber;
import java.io.*;

/**
 * This object compliments ObjectTransfer and provides a method to translate
 * any object into a type the database engine can process.
 *
 * @author Tobias Downer
 */

public class ObjectTranslator {

  /**
   * Translates the given object to a type the database can process.
   */
  public static Object translate(Object ob) {
    if (ob == null) {
      return null;
    }
    else if (ob instanceof String) {
      return StringObject.fromString((String) ob);
    }
    else if (ob instanceof StringObject ||
             ob instanceof BigNumber ||
             ob instanceof Date ||
             ob instanceof ByteLongObject ||
             ob instanceof Boolean ||
             ob instanceof StreamableObject) {
      return ob;
    }
    else if (ob instanceof byte[]) {
      return new ByteLongObject((byte[]) ob);
    }
    else if (ob instanceof Serializable) {
      return serialize(ob);
    }
    else {
//      System.out.println("Ob is: (" + ob.getClass() + ") " + ob);
      throw new Error("Unable to translate object.  " +
                      "It is not a primitive type or serializable.");
    }
  }

  /**
   * Serializes the Java object to a ByteLongObject.
   */
  public static ByteLongObject serialize(Object ob) {
    try {
      ByteArrayOutputStream bout = new ByteArrayOutputStream();
      ObjectOutputStream ob_out = new ObjectOutputStream(bout);
      ob_out.writeObject(ob);
      ob_out.close();
      return new ByteLongObject(bout.toByteArray());
    }
    catch (IOException e) {
      throw new Error("Serialization error: " + e.getMessage());
    }
  }

  /**
   * Deserializes a ByteLongObject to a Java object.
   */
  public static Object deserialize(ByteLongObject blob) {
    if (blob == null) {
      return null;
    }
    else {
      try {
        ByteArrayInputStream bin =
                              new ByteArrayInputStream(blob.getByteArray());
        ObjectInputStream ob_in = new ObjectInputStream(bin);
        Object ob = ob_in.readObject();
        ob_in.close();
        return ob;
      }
      catch (ClassNotFoundException e) {
        throw new Error("Class not found: " + e.getMessage());
      }
      catch (IOException e) {
        throw new Error("De-serialization error: " + e.getMessage());
      }
    }
  }

}
