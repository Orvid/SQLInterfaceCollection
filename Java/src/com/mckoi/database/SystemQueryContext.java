/**
 * com.mckoi.database.SystemQueryContext  25 Mar 2002
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
 * A QueryContext that only wraps around a TransactionSystem and does not
 * provide implementations for the 'getTable', and 'getDatabase' methods.
 *
 * @author Tobias Downer
 */

final class SystemQueryContext extends AbstractQueryContext {

  /**
   * The wrapped TransactionSystem object.
   */
  private TransactionSystem system;

  /**
   * The Transaction this is a part of.
   */
  private SimpleTransaction transaction;

  /**
   * The context schema of this context.
   */
  private String current_schema;



  /**
   * Constructs the QueryContext.
   */
  SystemQueryContext(SimpleTransaction transaction,
                     String current_schema) {
    this.transaction = transaction;
    this.system = transaction.getSystem();
    this.current_schema = current_schema;
  }

  /**
   * Returns a TransactionSystem object that is used to determine information
   * about the transactional system.
   */
  public TransactionSystem getSystem() {
    return system;
  }

  /**
   * Returns the system FunctionLookup object.
   */
  public FunctionLookup getFunctionLookup() {
    return getSystem().getFunctionLookup();
  }
  
  /**
   * Increments the sequence generator and returns the next unique key.
   */
  public long nextSequenceValue(String name) {
    TableName tn = transaction.resolveToTableName(current_schema, name,
                                                system.ignoreIdentifierCase());
    return transaction.nextSequenceValue(tn);
  }

  /**
   * Returns the current sequence value returned for the given sequence
   * generator within the connection defined by this context.  If a value was
   * not returned for this connection then a statement exception is generated.
   */
  public long currentSequenceValue(String name) {
    TableName tn = transaction.resolveToTableName(current_schema, name,
                                                system.ignoreIdentifierCase());
    return transaction.lastSequenceValue(tn);
  }

  /**
   * Sets the current sequence value for the given sequence generator.
   */
  public void setSequenceValue(String name, long value) {
    TableName tn = transaction.resolveToTableName(current_schema, name,
                                                system.ignoreIdentifierCase());
    transaction.setSequenceValue(tn, value);
  }
  
  /**
   * Returns a unique key for the given table source in the database.
   */
  public long nextUniqueID(String table_name) {
    TableName tname = TableName.resolve(current_schema, table_name);
    return transaction.nextUniqueID(tname);
  }

  /**
   * Returns the user name of the connection.
   */
  public String getUserName() {
    return "@SYSTEM";
  }

}
