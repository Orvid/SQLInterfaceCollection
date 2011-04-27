/**
 * com.mckoi.database.control.DefaultDBConfig  29 Mar 2002
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

package com.mckoi.database.control;

import java.io.File;
import java.io.InputStream;
import java.io.BufferedInputStream;
import java.io.IOException;
import java.io.FileInputStream;
import java.net.URL;
import java.net.URLConnection;
import java.util.Hashtable;
import java.util.Properties;
import java.util.Enumeration;

/**
 * Implements a default database configuration that is useful for setting up
 * a database.  This configuration object is mutable.  Configuration properties
 * can be set by calling the 'setxxx' methods.
 *
 * @author Tobias Downer
 */

public class DefaultDBConfig extends AbstractDBConfig {

  /**
   * Constructs the configuration.
   *
   * @param current_path the current path of the configuration in the file system.  This is
   *   useful if the configuration is based on a file with relative paths set
   *   in it.
   */
  public DefaultDBConfig(File current_path) {
    super(current_path);
  }

  /**
   * Constructs the configuration with the current system path as the
   * configuration path.
   */
  public DefaultDBConfig() {
    this(new File("."));
  }

  /**
   * Gets the default value for the given property value.
   */
  protected String getDefaultValue(String property_key) {
    ConfigProperty property =
                          (ConfigProperty) CONFIG_DEFAULTS.get(property_key);
    if (property == null) {
      return null;
    }
    else {
      return property.getDefaultValue();
    }
  }

  /**
   * Overwrites the configuration key with the given value.
   */
  public void setValue(String property_key, String value) {
    super.setValue(property_key, value);
  }

  /**
   * Loads all the configuration values from the given InputStream.  The
   * input stream must be formatted in a standard properties format.
   */
  public void loadFromStream(InputStream input) throws IOException {
    Properties config = new Properties();
    config.load(new BufferedInputStream(input));
    // For each property in the file
    Enumeration senum = config.propertyNames();
    while (senum.hasMoreElements()) {
      // Set the property value in this configuration.
      String property_key = (String) senum.nextElement();
      setValue(property_key, config.getProperty(property_key));
    }
  }

  /**
   * Loads all the configuration settings from a configuration file.  Useful if
   * you want to load a default configuration from a 'db.conf' file.  The
   * file must be formatted in a standard properties format.
   */
  public void loadFromFile(File configuration_file) throws IOException {
    FileInputStream file_in = new FileInputStream(configuration_file);
    loadFromStream(file_in);
    file_in.close();
  }

  /**
   * Loads all the configuration values from the given URL.  The file must be
   * formatted in a standard properties format.
   */
  public void loadFromURL(URL configuration_url) throws IOException {
    InputStream url_in = configuration_url.openConnection().getInputStream();
    loadFromStream(url_in);
    url_in.close();
  }

  // ---------- Variable helper setters ----------

  /**
   * Sets the path of the database.
   */
  public void setDatabasePath(String path) {
    setValue("database_path", path);
  }

  /**
   * Sets the path of the log.
   */
  public void setLogPath(String path) {
    setValue("log_path", path);
  }

  /**
   * Sets that the engine ignores case for identifiers.
   */
  public void setIgnoreIdentifierCase(boolean status) {
    setValue("ignore_case_for_identifiers", status ? "enabled" : "disabled");
  }

  /**
   * Sets that the database is read only.
   */
  public void setReadOnly(boolean status) {
    setValue("read_only", status ? "enabled" : "disabled");
  }

  /**
   * Sets the minimum debug level for output to the debug log file.
   */
  public void setMinimumDebugLevel(int debug_level) {
    setValue("debug_level", "" + debug_level);
  }



  // ---------- Statics ----------

  /**
   * A Hashtable of default configuration values.  This maps from property_key
   * to ConfigProperty object that describes the property.
   */
  private static Hashtable CONFIG_DEFAULTS = new Hashtable();

  /**
   * Adds a default property to the CONFIG_DEFAULTS map.
   */
  private static void addDefProperty(ConfigProperty property) {
    CONFIG_DEFAULTS.put(property.getKey(), property);
  }

  /**
   * Sets up the CONFIG_DEFAULTS map with default configuration values.
   */
  static {
    addDefProperty(new ConfigProperty("database_path", "./data", "PATH"));

//    addDefProperty(new ConfigProperty("log_path", "./log", "PATH"));

    addDefProperty(new ConfigProperty("root_path", "jvm", "STRING"));

    addDefProperty(new ConfigProperty("jdbc_server_port", "9157", "STRING"));

    addDefProperty(new ConfigProperty(
                      "ignore_case_for_identifiers", "disabled", "BOOLEAN"));

    addDefProperty(new ConfigProperty(
                                   "regex_library", "gnu.regexp", "STRING"));

    addDefProperty(new ConfigProperty("data_cache_size", "4194304", "INT"));

    addDefProperty(new ConfigProperty(
                                     "max_cache_entry_size", "8192", "INT"));

    addDefProperty(new ConfigProperty(
                            "lookup_comparison_list", "enabled", "BOOLEAN"));

    addDefProperty(new ConfigProperty("maximum_worker_threads", "4", "INT"));

    addDefProperty(new ConfigProperty(
                            "dont_synch_filesystem", "disabled", "BOOLEAN"));

    addDefProperty(new ConfigProperty(
                 "transaction_error_on_dirty_select", "enabled", "BOOLEAN"));

    addDefProperty(new ConfigProperty("read_only", "disabled", "BOOLEAN"));

    addDefProperty(new ConfigProperty(
                                     "debug_log_file", "debug.log", "FILE"));

    addDefProperty(new ConfigProperty("debug_level", "20", "INT"));

    addDefProperty(new ConfigProperty(
                                  "table_lock_check", "enabled", "BOOLEAN"));

  }

  // ---------- Inner classes ----------

  /**
   * An object the describes a single configuration property and the default
   * value for it.
   */
  private static class ConfigProperty {

    private String key;
    private String default_value;
    private String type;
    private String comment;

    ConfigProperty(String key, String default_value, String type,
                   String comment) {
      this.key = key;
      this.default_value = default_value;
      this.type = type;
      this.comment = comment;
    }

    ConfigProperty(String key, String default_value, String type) {
      this(key, default_value, type, null);
    }

    String getKey() {
      return key;
    }

    String getDefaultValue() {
      return default_value;
    }

    String getType() {
      return type;
    }

    String getComment() {
      return comment;
    }

  }

}
