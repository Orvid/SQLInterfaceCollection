/**
 * com.mckoi.database.jdbc.MConnection  20 Jul 2000
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

package com.mckoi.database.jdbc;

import java.io.*;
import java.sql.*;
import java.util.Properties;
import java.util.Vector;
import java.util.StringTokenizer;
import com.mckoi.database.global.ColumnDescription;
import com.mckoi.database.global.ObjectTransfer;
import com.mckoi.database.global.StreamableObject;
import com.mckoi.util.ByteArrayUtil;
import java.util.Hashtable;
//#IFDEF(JDBC2.0)
import java.util.Map;
//#ENDIF

/**
 * JDBC implementation of the connection object to a Mckoi database.  The
 * implementation specifics for how the connection talks with the database
 * is left up to the implementation of DatabaseInterface.
 * <p>
 * This object is thread safe.  It may be accessed safely from concurrent
 * threads.
 *
 * @author Tobias Downer
 */

public class MConnection implements Connection, DatabaseCallBack {

  /**
   * A cache of all rows retrieved from the server.  This cuts down the
   * number of requests to the server by caching rows that are accessed
   * frequently.  Note that cells are only cached within a ResultSet bounds.
   * Two different ResultSet's will not share cells in the cache.
   */
  private RowCache row_cache;

  /**
   * The JDBC URL used to make this connection.
   */
  private String url;

  /**
   * SQL warnings for this connection.
   */
  private SQLWarning head_warning;

  /**
   * Set to true if the connection is closed.
   */
  private boolean is_closed;

  /**
   * Set to true if the connection is in auto-commit mode.  (By default,
   * auto_commit is enabled).
   */
  private boolean auto_commit;

  /**
   * The interface to the database.
   */
  private DatabaseInterface db_interface;

  /**
   * The list of trigger listeners registered with the connection.
   */
  private Vector trigger_list;

  /**
   * A Thread that handles all dispatching of trigger events to the JDBC
   * client.
   */
  //private TriggerDispatchThread trigger_thread;

  /**
   * If the ResultSet.getObject method should return the raw object type (eg.
   * BigDecimal for Integer, String for chars, etc) then this is set to false.
   * If this is true (the default) the 'getObject' methods return the
   * correct object types as specified by the JDBC specification.
   */
  private boolean strict_get_object;

  /**
   * If the ResultSetMetaData.getColumnName method should return a succinct
   * form of the column name as most JDBC implementations do, this should
   * be set to false (the default).  If old style verbose column names should
   * be returned for compatibility with older Mckoi applications, this is
   * set to true.
   */
  private boolean verbose_column_names;

  /**
   * This is set to true if the MResultSet column lookup methods are case
   * insensitive.  This should be set to true for any database that has
   * case insensitive identifiers.
   */
  private boolean case_insensitive_identifiers;

  /**
   * A mapping from a streamable object id to InputStream used to represent
   * the object when being uploaded to the database engine.
   */
  private Hashtable s_object_hold;

  /**
   * An unique id count given to streamable object being uploaded to the
   * server.
   */
  private long s_object_id;
  
  

  // For synchronization in this object,
  private Object lock = new Object();



  /**
   * Constructor.
   */
  public MConnection(String url, DatabaseInterface db_interface,
                     int cache_size, int max_size) {
    this.url = url;
    this.db_interface = db_interface;
    is_closed = true;
    auto_commit = true;
    trigger_list = new Vector();
    strict_get_object = true;
    verbose_column_names = false;
    case_insensitive_identifiers = false;
    row_cache = new RowCache(cache_size, max_size);
    s_object_hold = new Hashtable();
    s_object_id = 0;
  }

  /**
   * Toggles strict get object.
   * <p>
   * If the 'getObject' method should return the raw object type (eg.
   * BigDecimal for Integer, String for chars, etc) then this is set to false.
   * If this is true (the default) the 'getObject' methods return the
   * correct object types as specified by the JDBC specification.
   * <p>
   * The default is true.
   */
  public void setStrictGetObject(boolean status) {
    strict_get_object = status;
  }

  /**
   * Returns true if strict get object is enabled (default).
   */
  public boolean isStrictGetObject() {
    return strict_get_object;
  }

  /**
   * Toggles verbose column names from ResultSetMetaData.
   * <p>
   * If this is set to true, getColumnName will return 'APP.Part.id' for a
   * column name.  If it is false getColumnName will return 'id'.  This
   * property is for compatibility with older Mckoi applications.
   */
  public void setVerboseColumnNames(boolean status) {
    verbose_column_names = status;
  }

  /**
   * Returns true if ResultSetMetaData should return verbose column names.
   */
  public boolean verboseColumnNames() {
    return verbose_column_names;
  }

  /**
   * Toggles whether this connection is handling identifiers as case
   * insensitive or not.  If this is true then 'getString("app.id")' will
   * match against 'APP.id', etc.
   */
  public void setCaseInsensitiveIdentifiers(boolean status) {
    case_insensitive_identifiers = status;
  }

  /**
   * Returns true if the database has case insensitive identifiers.
   */
  public boolean isCaseInsensitiveIdentifiers() {
    return case_insensitive_identifiers;
  }


//  private static void printByteArray(byte[] array) {
//    System.out.println("Length: " + array.length);
//    for (int i = 0; i < array.length; ++i) {
//      System.out.print(array[i]);
//      System.out.print(", ");
//    }
//  }

  /**
   * Returns the row Cache object for this connection.
   */
  protected final RowCache getRowCache() {
    return row_cache;
  }

  /**
   * Adds a new SQLWarning to the chain.
   */
  protected final void addSQLWarning(SQLWarning warning) {
    synchronized (lock) {
      if (head_warning == null) {
        head_warning = warning;
      }
      else {
        head_warning.setNextWarning(warning);
      }
    }
  }

  /**
   * Closes this connection by calling the 'dispose' method in the database
   * interface.
   */
  public final void internalClose() throws SQLException {
    synchronized (lock) {
      if (!isClosed()) {
        try {
          db_interface.dispose();
        }
        finally {
          is_closed = true;
        }
      }
    }
  }

  /**
   * Returns this MConnection wrapped in a MckoiConnection object.
   */
  MckoiConnection getMckoiConnection() {
    return new MckoiConnection(this);
  }

  /**
   * Attempts to login to the database interface with the given default schema,
   * username and password.  If the authentication fails an SQL exception is
   * generated.
   */
  public void login(String default_schema, String username, String password)
                                                         throws SQLException {

    synchronized (lock) {
      if (!is_closed) {
        throw new SQLException(
                        "Unable to login to connection because it is open.");
      }
    }

    if (username == null || username.equals("") ||
        password == null || password.equals("")) {
      throw new SQLException("username or password have not been set.");
    }

    // Set the default schema to username if it's null
    if (default_schema == null) {
      default_schema = username;
    }

    // Login with the username/password
    boolean li = db_interface.login(default_schema, username, password, this);
    synchronized (lock) {
      is_closed = !li;
    }
    if (!li) {
      throw new SQLException("User authentication failed for: " + username);
    }

    // Determine if this connection is case insensitive or not,
    setCaseInsensitiveIdentifiers(false);
    Statement stmt = createStatement();
    ResultSet rs = stmt.executeQuery("SHOW CONNECTION_INFO");
    while (rs.next()) {
      String key = rs.getString(1);
      if (key.equals("case_insensitive_identifiers")) {
        String val = rs.getString(2);
        setCaseInsensitiveIdentifiers(val.equals("true"));
      }
      else if (key.equals("auto_commit")) {
        String val = rs.getString(2);
        auto_commit = val.equals("true");
      }
    }
    rs.close();
    stmt.close();

  }

  // ---------- Package Protected ----------

  /**
   * Returns the url string used to make this connection.
   */
  String getURL() {
    return url;
  }

  /**
   * Logs into the JDBC server running on a remote machine.  Throws an
   * exception if user authentication fails.
   */
  void login(Properties info, String default_schema) throws SQLException {

    String username = info.getProperty("user", "");
    String password = info.getProperty("password", "");

    login(default_schema, username, password);
  }

//  /**
//   * Cancels a result set that is downloading.
//   */
//  void cancelResultSet(MResultSet result_set) throws SQLException {
//    disposeResult(result_set.getResultID());
//
////    connection_thread.disposeResult(result_set.getResultID());
//  }

  /**
   * Uploads any streamable objects found in an SQLQuery into the database.
   */
  private void uploadStreamableObjects(SQLQuery sql) throws SQLException {
    
    // Push any streamable objects that are present in the query onto the
    // server.
    Object[] vars = sql.getVars();
    try {
      for (int i = 0; i < vars.length; ++i) {
        // For each streamable object.
        if (vars[i] != null && vars[i] instanceof StreamableObject) {
          // Buffer size is fixed to 64 KB
          final int BUF_SIZE = 64 * 1024;

          StreamableObject s_object = (StreamableObject) vars[i];
          long offset = 0;
          final byte type = s_object.getType();
          final long total_len = s_object.getSize();
          final long id = s_object.getIdentifier();
          final byte[] buf = new byte[BUF_SIZE];

          // Get the InputStream from the StreamableObject hold
          Object sob_id = new Long(id);
          InputStream i_stream = (InputStream) s_object_hold.get(sob_id);
          if (i_stream == null) {
            throw new RuntimeException(
                "Assertion failed: Streamable object InputStream is not available.");
          }

          while (offset < total_len) {
            // Fill the buffer
            int index = 0;
            final int block_read =
                       (int) Math.min((long) BUF_SIZE, (total_len - offset));
            int to_read = block_read;
            while (to_read > 0) {
              int count = i_stream.read(buf, index, to_read);
              if (count == -1) {
                throw new IOException("Premature end of stream.");
              }
              index += count;
              to_read -= count;
            }

            // Send the part of the streamable object to the database.
            db_interface.pushStreamableObjectPart(type, id, total_len,
                                                  buf, offset, block_read);
            // Increment the offset and upload the next part of the object.
            offset += block_read;
          }

          // Remove the streamable object once it has been written
          s_object_hold.remove(sob_id);

//        [ Don't close the input stream - we may only want to put a part of
//          the stream into the database and keep the file open. ]
//          // Close the input stream
//          i_stream.close();

        }
      }
    }
    catch (IOException e) {
      e.printStackTrace(System.err);
      throw new SQLException("IO Error pushing large object to server: " +
                             e.getMessage());
    }
  }

  /**
   * Sends the batch of SQLQuery objects to the database to be executed.  The
   * given array of MResultSet will be the consumer objects for the query
   * results.  If a query succeeds then we are guarenteed to know that size of
   * the result set.
   * <p>
   * This method blocks until all of the queries have been processed by the
   * database.
   */
  void executeQueries(SQLQuery[] queries, MResultSet[] results)
                                                          throws SQLException {
    // For each query
    for (int i = 0; i < queries.length; ++i) {
      executeQuery(queries[i], results[i]);
    }
  }

  /**
   * Sends the SQL string to the database to be executed.  The given MResultSet
   * is the consumer for the results from the database.  We are guarenteed
   * that if the query succeeds that we know the size of the result set and
   * at least first first row of the set.
   * <p>
   * This method will block until we have received the result header
   * information.
   */
  void executeQuery(SQLQuery sql, MResultSet result_set) throws SQLException {

    uploadStreamableObjects(sql);
    // Execute the query,
    QueryResponse resp = db_interface.execQuery(sql);

    // The format of the result
    ColumnDescription[] col_list = new ColumnDescription[resp.getColumnCount()];
    for (int i = 0; i < col_list.length; ++i) {
      col_list[i] = resp.getColumnDescription(i);
    }
    // Set up the result set to the result format and update the time taken to
    // execute the query on the server.
    result_set.connSetup(resp.getResultID(), col_list, resp.getRowCount());
    result_set.setQueryTime(resp.getQueryTimeMillis());

  }

  /**
   * Called by MResultSet to query a part of a result from the server.  Returns
   * a List that represents the result from the server.
   */
  ResultPart requestResultPart(int result_id, int start_row, int count_rows)
                                                          throws SQLException {
    return db_interface.getResultPart(result_id, start_row, count_rows);
  }

  /**
   * Requests a part of a streamable object from the server.
   */
  StreamableObjectPart requestStreamableObjectPart(int result_id,
         long streamable_object_id, long offset, int len) throws SQLException {
    return db_interface.getStreamableObjectPart(result_id,
                                          streamable_object_id, offset, len);
  }
  
  /**
   * Disposes of the server-side resources associated with the result set with
   * result_id.  This should be called either before we start the download of
   * a new result set, or when we have finished with the resources of a result
   * set.
   */
  void disposeResult(int result_id) throws SQLException {
    // Clear the row cache.
    // It would be better if we only cleared row entries with this
    // table_id.  We currently clear the entire cache which means there will
    // be traffic created for other open result sets.
//    System.out.println(result_id);
//    row_cache.clear();
    // Only dispose if the connection is open
    if (!is_closed) {
      db_interface.disposeResult(result_id);
    }
  }

  /**
   * Adds a TriggerListener that listens for all triggers events with the name
   * given.  Triggers are created with the 'CREATE TRIGGER' syntax.
   */
  void addTriggerListener(String trigger_name, TriggerListener listener) {
    synchronized (trigger_list) {
      trigger_list.addElement(trigger_name);
      trigger_list.addElement(listener);
    }
  }

  /**
   * Removes the TriggerListener for the given trigger name.
   */
  void removeTriggerListener(String trigger_name, TriggerListener listener) {
    synchronized (trigger_list) {
      for (int i = trigger_list.size() - 2; i >= 0; i -= 2) {
        if (trigger_list.elementAt(i).equals(trigger_name) &&
            trigger_list.elementAt(i + 1).equals(listener)) {
          trigger_list.removeElementAt(i);
          trigger_list.removeElementAt(i);
        }
      }
    }
  }

  
  /**
   * Creates a StreamableObject on the JDBC client side given an InputStream,
   * and length and a type.  When this method returns, a StreamableObject
   * entry will be added to the hold.
   */
  StreamableObject createStreamableObject(InputStream x,
                                          int length, byte type) {
    long ob_id;
    synchronized (s_object_hold) {
      ob_id = s_object_id;
      ++s_object_id;
      // Add the stream to the hold and get the unique id
      s_object_hold.put(new Long(ob_id), x);
    }
    // Create and return the StreamableObject
    return new StreamableObject(type, length, ob_id);
  }

  /**
   * Removes the StreamableObject from the hold on the JDBC client.  This should
   * be called when the MPreparedStatement closes.
   */
  void removeStreamableObject(StreamableObject s_object) {
    s_object_hold.remove(new Long(s_object.getIdentifier()));
  }
  
  
  // ---------- Implemented from DatabaseCallBack ----------

  // NOTE: For JDBC standalone apps, the thread that calls this will be a
  //   WorkerThread.
  //   For JDBC client/server apps, the thread that calls this will by the
  //   connection thread that listens for data from the server.
  public void databaseEvent(int event_type, String event_message) {
    if (event_type == 99) {
      //if (trigger_thread == null) {
      //  trigger_thread = new TriggerDispatchThread();
      // trigger_thread.start();
      //}
      //trigger_thread.dispatchTrigger(event_message);
    }
    else {
      throw new Error("Unrecognised database event: " + event_type);
    }

//    System.out.println("[com.mckoi.jdbc.MConnection] Event received:");
//    System.out.println(event_type);
//    System.out.println(event_message);
  }


  // ---------- Implemented from Connection ----------

  public Statement createStatement() throws SQLException {
    return new MStatement(this);
  }

  public PreparedStatement prepareStatement(String sql) throws SQLException {
    return new MPreparedStatement(this, sql);
  }

  public CallableStatement prepareCall(String sql) throws SQLException {
    throw MSQLException.unsupported();
  }
                                              
  public String nativeSQL(String sql) throws SQLException {
    // We don't do any client side parsing of the sql statement.
    return sql;
  }

  public void setAutoCommit(boolean autoCommit) throws SQLException {
    // The SQL to put into auto-commit mode.
    ResultSet result;
    if (autoCommit) {
      result = createStatement().executeQuery("SET AUTO COMMIT ON");
      auto_commit = true;
      result.close();
    }
    else {
      result = createStatement().executeQuery("SET AUTO COMMIT OFF");
      auto_commit = false;
      result.close();
    }
  }

  public boolean getAutoCommit() throws SQLException {
    return auto_commit;
//    // Query the database for this info.
//    ResultSet result;
//    result = createStatement().executeQuery(
//                           "SHOW CONNECTION_INFO WHERE var = 'auto_commit'");
//    boolean auto_commit_mode = false;
//    if (result.next()) {
//      auto_commit_mode = result.getString(2).equals("true");
//    }
//    result.close();
//    return auto_commit_mode;
  }

  public void commit() throws SQLException {
    ResultSet result;
    result = createStatement().executeQuery("COMMIT");
    result.close();
  }

  public void rollback() throws SQLException {
    ResultSet result;
    result = createStatement().executeQuery("ROLLBACK");
    result.close();
  }

  public void close() throws SQLException {

    if (!isClosed()) {
      internalClose();
    }

//    if (!isClosed()) {
//      try {
//        internalClose();
//      }
//      finally {
//        MDriver.connectionClosed(this);
//      }
//    }

//    synchronized (lock) {
//      if (!isClosed()) {
//        try {
//          db_interface.dispose();
//          MDriver.connectionClosed(this);
//        }
//        finally {
//          is_closed = true;
//        }
//      }
//    }
  }

  public boolean isClosed() throws SQLException {
    synchronized (lock) {
      return is_closed;
    }
  }

  //======================================================================
  // Advanced features:

  public DatabaseMetaData getMetaData() throws SQLException {
    return new MDatabaseMetaData(this);
  }

  public void setReadOnly(boolean readOnly) throws SQLException {
    // Hint ignored
  }

  public boolean isReadOnly() throws SQLException {
    // Currently we don't support read locked transactions.
    return false;
  }

  public void setCatalog(String catalog) throws SQLException {
    // Silently ignored ;-)
  }

  public String getCatalog() throws SQLException {
    // Catalog's not supported
    return null;
  }

  public void setTransactionIsolation(int level) throws SQLException {
    if (level != TRANSACTION_SERIALIZABLE) {
      throw new SQLException("Only 'TRANSACTION_SERIALIZABLE' supported.");
    }
  }

  public int getTransactionIsolation() throws SQLException {
    return TRANSACTION_SERIALIZABLE;
  }

  public SQLWarning getWarnings() throws SQLException {
    synchronized (lock) {
      return head_warning;
    }
  }

  public void clearWarnings() throws SQLException {
    synchronized (lock) {
      head_warning = null;
    }
  }

//#IFDEF(JDBC2.0)

  //--------------------------JDBC 2.0-----------------------------

  public Statement createStatement(int resultSetType,
                               int resultSetConcurrency) throws SQLException {
    Statement statement = createStatement();
    // PENDING - set default result set type and result set concurrency for
    //   statement
    return statement;
  }

  public PreparedStatement prepareStatement(String sql, int resultSetType,
                               int resultSetConcurrency) throws SQLException {
    PreparedStatement statement = prepareStatement(sql);
    // PENDING - set default result set type and result set concurrency for
    //   statement
    return statement;
  }

  public CallableStatement prepareCall(String sql, int resultSetType,
                               int resultSetConcurrency) throws SQLException {
    throw MSQLException.unsupported();
  }

  // ISSUE: I can see using 'Map' here is going to break compatibility with
  //   Java 1.1.  Even though testing with 1.1.8 on Linux and NT turned out
  //   fine, I have a feeling some verifiers on web browsers aren't going to
  //   like this.
  public Map getTypeMap() throws SQLException {
    throw MSQLException.unsupported();
  }

  public void setTypeMap(Map map) throws SQLException {
    throw MSQLException.unsupported();
  }

//#ENDIF

//#IFDEF(JDBC3.0)

  //--------------------------JDBC 3.0-----------------------------

  public void setHoldability(int holdability) throws SQLException {
    // Currently holdability can not be set to CLOSE_CURSORS_AT_COMMIT though
    // it could be implemented.
    if (holdability == ResultSet.CLOSE_CURSORS_AT_COMMIT) {
      throw new SQLException(
                     "CLOSE_CURSORS_AT_COMMIT holdability is not supported.");
    }
  }

  public int getHoldability() throws SQLException {
    return ResultSet.HOLD_CURSORS_OVER_COMMIT;
  }

  public Savepoint setSavepoint() throws SQLException {
    throw MSQLException.unsupported();
  }

  public Savepoint setSavepoint(String name) throws SQLException {
    throw MSQLException.unsupported();
  }

  public void rollback(Savepoint savepoint) throws SQLException {
    throw MSQLException.unsupported();
  }

  public void releaseSavepoint(Savepoint savepoint) throws SQLException {
    throw MSQLException.unsupported();
  }

  public Statement createStatement(int resultSetType, int resultSetConcurrency,
                        int resultSetHoldability) throws SQLException {
    // Currently holdability can not be set to CLOSE_CURSORS_AT_COMMIT though
    // it could be implemented.
    if (resultSetHoldability == ResultSet.CLOSE_CURSORS_AT_COMMIT) {
      throw new SQLException(
                     "CLOSE_CURSORS_AT_COMMIT holdability is not supported.");
    }
    return createStatement(resultSetType, resultSetConcurrency);
  }

  public PreparedStatement prepareStatement(
                 String sql, int resultSetType, int resultSetConcurrency,
                 int resultSetHoldability) throws SQLException {
    // Currently holdability can not be set to CLOSE_CURSORS_AT_COMMIT though
    // it could be implemented.
    if (resultSetHoldability == ResultSet.CLOSE_CURSORS_AT_COMMIT) {
      throw new SQLException(
                     "CLOSE_CURSORS_AT_COMMIT holdability is not supported.");
    }
    return prepareStatement(sql, resultSetType, resultSetConcurrency);
  }

  public CallableStatement prepareCall(String sql, int resultSetType,
     int resultSetConcurrency, int resultSetHoldability) throws SQLException {
    throw MSQLException.unsupported();
  }

  public PreparedStatement prepareStatement(String sql, int autoGeneratedKeys)
                                                         throws SQLException {
    throw MSQLException.unsupported();
  }

  public PreparedStatement prepareStatement(String sql, int columnIndexes[])
                                                         throws SQLException {
    throw MSQLException.unsupported();
  }

  public PreparedStatement prepareStatement(String sql, String columnNames[])
                                                         throws SQLException {
    throw MSQLException.unsupported();
  }

    public Clob createClob() throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public Blob createBlob() throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public NClob createNClob() throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public SQLXML createSQLXML() throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public boolean isValid(int timeout) throws SQLException {
        return false;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public void setClientInfo(String name, String value) throws SQLClientInfoException {
        //To change body of implemented methods use File | Settings | File Templates.
    }

    public void setClientInfo(Properties properties) throws SQLClientInfoException {
        //To change body of implemented methods use File | Settings | File Templates.
    }

    public String getClientInfo(String name) throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public Properties getClientInfo() throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public Array createArrayOf(String typeName, Object[] elements) throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public Struct createStruct(String typeName, Object[] attributes) throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public <T> T unwrap(Class<T> iface) throws SQLException {
        return null;  //To change body of implemented methods use File | Settings | File Templates.
    }

    public boolean isWrapperFor(Class<?> iface) throws SQLException {
        return false;  //To change body of implemented methods use File | Settings | File Templates.
    }
//#ENDIF

  // ---------- Inner classes ----------

  /**
   * The thread that handles all dispatching of trigger events.
   */
  private class TriggerDispatchThread extends Thread {

    private Vector trigger_messages_queue = new Vector();

    TriggerDispatchThread() {
      setDaemon(true);
      setName("Mckoi - Trigger Dispatcher");
    }

    /**
     * Dispatches a trigger message to the listeners.
     */
    private void dispatchTrigger(String event_message) {
      synchronized (trigger_messages_queue) {
        trigger_messages_queue.addElement(event_message);
        trigger_messages_queue.notifyAll();
      }
    }

    // Thread run method
    public void run() {

      while (true) {
        try {
          String message;
          synchronized (trigger_messages_queue) {
            while (trigger_messages_queue.size() == 0) {
              try {
                trigger_messages_queue.wait();
              }
              catch (InterruptedException e) { /* ignore */ }
            }
            message = (String) trigger_messages_queue.elementAt(0);
            trigger_messages_queue.removeElementAt(0);
          }

          // 'message' is a message to process...
          // The format of a trigger message is:
          // "[trigger_name] [trigger_source] [trigger_fire_count]"
//          System.out.println("TRIGGER EVENT: " + message);

          StringTokenizer tok = new StringTokenizer(message, " ");
          String trigger_name = (String) tok.nextElement();
          String trigger_source = (String) tok.nextElement();
          String trigger_fire_count = (String) tok.nextElement();

          Vector fired_triggers = new Vector();
          // Create a list of Listener's that are listening for this trigger.
          synchronized (trigger_list) {
            for (int i = 0; i < trigger_list.size(); i += 2) {
              String to_listen_for = (String) trigger_list.elementAt(i);
              if (to_listen_for.equals(trigger_name)) {
                TriggerListener listener =
                             (TriggerListener) trigger_list.elementAt(i + 1);
                // NOTE, we can't call 'listener.triggerFired' here because
                // it's not a good idea to call user code when we are
                // synchronized over 'trigger_list' (deadlock concerns).
                fired_triggers.addElement(listener);
              }
            }
          }

          // Fire them triggers.
          for (int i = 0; i < fired_triggers.size(); ++i) {
            TriggerListener listener =
                                (TriggerListener) fired_triggers.elementAt(i);
            listener.triggerFired(trigger_name);
          }

        }
        catch (Throwable t) {
          t.printStackTrace(System.err);
        }

      }

    }

  }

}
