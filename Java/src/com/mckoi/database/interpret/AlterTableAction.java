/**
 * com.mckoi.database.interpret.AlterTableAction  09 Sep 2001
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

package com.mckoi.database.interpret;

import java.util.ArrayList;
import com.mckoi.database.*;

/**
 * Represents an action in an ALTER TABLE SQL statement.
 *
 * @author Tobias Downer
 */

public final class AlterTableAction
            implements java.io.Serializable, StatementTreeObject, Cloneable {

  static final long serialVersionUID = -3180332341627416727L;

  /**
   * Element parameters to do with the action.
   */
  private ArrayList elements;

  /**
   * The action to perform.
   */
  private String action;

  /**
   * Constructor.
   */
  public AlterTableAction() {
    elements = new ArrayList();
  }

  /**
   * Set the action to perform.
   */
  public void setAction(String str) {
    this.action = str;
  }

  /**
   * Adds a parameter to this action.
   */
  public void addElement(Object ob) {
    elements.add(ob);
  }

  /**
   * Returns the name of this action.
   */
  public String getAction() {
    return action;
  }

  /**
   * Returns the ArrayList that represents the parameters of this action.
   */
  public ArrayList getElements() {
    return elements;
  }

  /**
   * Returns element 'n'.
   */
  public Object getElement(int n) {
    return elements.get(n);
  }

  // Implemented from StatementTreeObject
  public void prepareExpressions(ExpressionPreparer preparer)
                                                  throws DatabaseException {
    // This must search throw 'elements' for objects that we can prepare
    for (int i = 0; i < elements.size(); ++i) {
      Object ob = elements.get(i);
      if (ob instanceof String) {
        // Do not need to prepare this
      }
      else if (ob instanceof Expression) {
        ((Expression) ob).prepare(preparer);
      }
      else if (ob instanceof StatementTreeObject) {
        ((StatementTreeObject) ob).prepareExpressions(preparer);
      }
      else {
        throw new DatabaseException(
                                "Unrecognised expression: " + ob.getClass());
      }
    }
  }

  public Object clone() throws CloneNotSupportedException {
    // Shallow clone
    AlterTableAction v = (AlterTableAction) super.clone();
    ArrayList cloned_elements = new ArrayList();
    v.elements = cloned_elements;

    for (int i = 0; i < elements.size(); ++i) {
      Object ob = elements.get(i);
      if (ob instanceof String) {
        // Do not need to clone this
      }
      else if (ob instanceof Expression) {
        ob = ((Expression) ob).clone();
      }
      else if (ob instanceof StatementTreeObject) {
        ob = ((StatementTreeObject) ob).clone();
      }
      else {
        throw new CloneNotSupportedException(ob.getClass().toString());
      }
      cloned_elements.add(ob);
    }

    return v;
  }

}
