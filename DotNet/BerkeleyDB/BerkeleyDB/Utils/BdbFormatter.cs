/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Kds.Serialization;
using Kds.Serialization.Buffer;

namespace BerkeleyDb.Serialization
{
  /// <summary><see cref="BEFormatter"/> subclass with a convenience API to directly
  /// serialize to and deserialize from <see cref="DbEntry"/> instances.</summary>
  /// <remarks><list type="bullet">
  /// <item>The <see cref="DbEntry"/> instances created through this API must obviously
  ///   have an underlying byte buffer. For this purpose a pool of buffers is provided
  ///   - see the <see cref="BdbFormatter"> constructors</see>. As there is a limited
  ///   number of buffers, one has to make sure that not more than the available number
  ///   of buffers is in use concurrently. Every call to one of the <see cref="ToDbEntry"/>
  ///   methods will use the next buffer in the pool in a round-robin fashion.</item>
  /// <item>There is one limitation compared to using the buffer serialization API:
  ///   One cannot serialize/deserialize multiple objects (value or reference types) within one
  ///   call to <see cref="ToDbEntry"/> or <see cref="FromDbEntry"/>, as <see cref="DbEntry"/>
  ///   instances are not meant to be re-used - all their properties are set in the constructor,
  ///   they just wrap up all the information that has to go with a byte buffer to be useful.
  ///   However, using the byte buffer directly, it is possible to serialize and deserialize
  ///   multiple objects, as shown in the second example.</item>
  /// </list></remarks>
  /// <example>In this example we add a <c>Vendor</c> to the database, keyed by the
  /// vendor's name. Then we open a cursor on the database and read all vendors into a list.
  /// <code>
  /// Db db = new Db(DbCreateFlags.None);
  /// vendorDb = (DbBTree)db.Open(null, VendorDbName, null, DbType.BTree, Db.OpenFlags.Create, 0);
  /// ...
  /// BdbFormatter fmt = new BdbFormatter(2, 256);  // two buffers of 256 bytes
  /// new VendorField(fmt, true);
  /// new SalesRepField(fmt, true);
  /// ...
  /// DbEntry keyEntry = fmt.ToDbEntry&lt;string>(vendor.Name);
  /// DbEntry dataEntry = fmt.ToDbEntry&lt;Vendor>(vendor);
  /// WriteStatus status = vendorDb.Put(null, ref keyEntry, ref dataEntry);
  /// if (status != WriteStatus.Success)
  ///   throw new ApplicationException("Put failed");
  /// ...
  /// List&lt;Vendor> vendors = new List&lt;Vendor>();
  /// // using a foreach loop (which closes the cursor automatically)
  /// foreach (KeyDataPair entry in vendorDb.OpenCursor(null, DbFileCursor.CreateFlags.None)) {
  ///   Vendor vendor = null;
  ///   fmt.FromDbEntry&lt;Vendor>(ref vendor, ref entry.Data);
  ///   vendors.Add(vendor);
  /// }
  /// </code></example>
  /// <example>In this example we add three names in one database record keyed by an integer
  /// and read them back based on the key.
  /// <code>
  /// Db db = new Db(DbCreateFlags.None);
  /// nameDb = (DbBTree)db.Open(null, NameDbName, null, DbType.BTree, Db.OpenFlags.Create, 0);
  /// ...
  /// BdbFormatter fmt = new BdbFormatter(1, 32);
  /// byte[] dataBuffer = new byte[256];
  /// ...
  /// DbEntry keyEntry = fmt.ToDbEntry&lt;int>(231);
  /// int index = 0;
  /// fmt.InitSerialization(dataBuffer, index);
  /// fmt.Serialize&lt;string>("John");
  /// fmt.Serialize&lt;string>("Jane");
  /// fmt.Serialize&lt;string>("Sean");
  /// index = fmt.FinishSerialization();
  /// DbEntry dataEntry = DbEntry.InOut(dataBuffer, 0, index);
  /// WriteStatus ws = nameDb.Put(null, ref keyEntry, ref dataEntry);
  /// if (ws != WriteStatus.Success)
  ///   throw new ApplicationException("Put failed");
  /// ...
  /// string name1, name2, name3;
  /// dataEntry = DbEntry.Out(dataBuffer);
  /// ReadStatus rs = nameDb.Get(null, ref keyEntry, ref dataEntry);
  /// if (rs != ReadStatus.Success)
  ///   throw new ApplicationException("Get failed");
  /// fmt.InitDeserialization(dataEntry.Buffer, dataEntry.Start);
  /// fmt.Deserialize&lt;string>(ref name1);
  /// fmt.Deserialize&lt;string>(ref name2);
  /// fmt.Deserialize&lt;string>(ref name3);
  /// index = fmt.FinishDeserialization();
  /// </code></example>
  public class BdbFormatter: BEFormatter
  {
    private byte[][] buffers;
    private int curBuffer;

    /// <summary>Creates a new <see cref="BdbFormatter"/> instance.</summary>
    /// <param name="bufCount">Number of buffers to create. Muste be > 0.</param>
    /// <param name="bufSize">Size of buffers. Must be > 0.</param>
    public BdbFormatter(int bufCount, int bufSize) : base() {
      if (bufCount <= 0)
        throw new ArgumentException("Must have at least one buffer.", "bufCount");
      if (bufSize <= 0)
        throw new ArgumentException("Invalid buffer size.", "bufSize");
      buffers = new byte[bufCount][];
      for (int indx = 0; indx < buffers.Length; indx++)
        buffers[indx] = new byte[bufSize];
      curBuffer = 0;
    }

    /// <summary>Creates a new <see cref="BdbFormatter"/> instance.</summary>
    /// <param name="buffers">Array of buffers to use. Must not be empty.</param>
    public BdbFormatter(byte[][] buffers) : base() {
      Buffers = buffers;
    }

    /// <summary>Returns the next buffer from the buffer pool.</summary>
    /// <remarks>Buffers are returned in a round-robin fashion. It is
    /// the responsibility of the application to have only as many buffers
    /// in concurrent use as there are buffers in the pool.</remarks>
    /// <returns>A byte buffer.</returns>
    public byte[] NextBuffer() {
      curBuffer++;
      if (curBuffer >= buffers.Length)
        curBuffer = 0;
      return buffers[curBuffer];
    }

    /// <summary>Returns the number of buffers.</summary>
    public int BufferCount {
      get { return buffers.Length; }
    }

    /// <summary>Returns the buffer at index <c>indx</c>.</summary>
    /// <param name="indx">Index of requested buffer.</param>
    /// <returns>Buffer at index <c>indx</c>.</returns>
    public byte[] GetBuffer(int indx) {
      return buffers[indx];
    }

    /// <summary>Buffers to be used concurrently.</summary>
    /// <remarks>The buffers are used in a round-robin fashion. That is, if we
    /// have N buffers, then there will not be a buffer conflict for the last N
    /// calls to any of the <see cref="ToDbEntry{T}(T)"/>, <see cref="ValuesToDbEntry{T}(T[])"/>
    /// or <see cref="ObjectsToDbEntry{T}(T[])"/> methods.</remarks>
    public byte[][] Buffers {
      set {
        if (value == null)
          throw new ArgumentNullException("value");
        if (value.Length == 0)
          throw new ArgumentException("No buffers provided.", "value");
        for (int indx = 0; indx < value.Length; indx++)
          if (value[indx] == null || value[indx].Length == 0)
            throw new ArgumentException("Some buffers are empty.", "value");
        buffers = value;
        curBuffer = 0;
      }
    }

    #region Serialize To DbEntry

    #region Single Instances

    /// <overloads>Serializes an object graph into a <see cref="DbEntry"/> instance.</overloads>
    /// <summary>Serializes a nullable value type into a <see cref="DbEntry"/> instance
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize, or <c>null</c>.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// serializes the <c>value </c>argument.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized object graph.</returns>
    public DbEntry ToDbEntry<T>(T? value, ValueField<T, Formatter> field)
      where T: struct 
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      field.Serialize(value);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes a nullable value type into a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize, or <c>null</c>.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized object graph.</returns>
    public DbEntry ToDbEntry<T>(T? value)
      where T: struct 
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return ToDbEntry(value, field);
    }

    /// <summary>Serializes a value type into a <see cref="DbEntry"/> instance
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.
    /// However, this does not allow to pass <c>nulls</c>. Therefore the
    /// companion method <see cref="NullToDbEntry"/> is provided.</remarks>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// serializes the <c>value</c>argument.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized object graph.</returns>
    public DbEntry ToDbEntry<T>(ref T value, ValueField<T, Formatter> field)
      where T: struct 
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      field.Serialize(ref value);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes a value type into a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.
    /// However, this does not allow to pass <c>nulls</c>. Therefore the companion
    /// method <see cref="NullToDbEntry"/> is provided.</remarks>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized object graph.</returns>
    public DbEntry ToDbEntry<T>(ref T value)
      where T: struct 
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return ToDbEntry(ref value, field);
    }

    /// <summary>Serializes a <c>null</c> into a <see cref="DbEntry"/> instance.</summary>
    /// <remarks>Companion method for <see cref="ToDbEntry{T}(ref T)">
    /// ToDbEntry&lt;T>(ref T)</see>.</remarks>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// a serialized <c>null</c>.</returns>
    public DbEntry NullToDbEntry() {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      SerializeNull();
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes a reference type into a <see cref="DbEntry"/> instance
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Reference type to serialize.</typeparam>
    /// <param name="obj">Object to serialize.</param>
    /// <param name="field"><see cref="ReferenceField{T, Formatter}"/> instance that
    /// serializes the <c>obj</c>argument.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized object graph.</returns>
    public DbEntry ToDbEntry<T>(T obj, ReferenceField<T, Formatter> field)
      where T: class
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      field.Serialize(obj);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes a reference type into a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Reference type to serialize.</typeparam>
    /// <param name="obj">Object to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized object graph.</returns>
    public DbEntry ToDbEntry<T>(T obj)
      where T: class
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      return ToDbEntry(obj, field);
    }

    #endregion Single Instances

    #region Value Type Arrays

    /// <overloads>Serializes value type sequences (arrays, collections) into a
    /// <see cref="DbEntry"/> instance.</overloads>
    /// <summary>Serializes value type arrays using a specific
    /// <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized array.</returns>
    public DbEntry ValuesToDbEntry<T>(T[] value, ValueField<T, Formatter> field)
      where T: struct
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      SerializeValues<T>(value, field);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes value type arrays using the default
    /// <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized array.</returns>
    public DbEntry ValuesToDbEntry<T>(T[] value)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return ValuesToDbEntry(value, field);
    }

    /// <summary>Serializes arrays of nullable value types using a specific
    /// <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized array.</returns>
    public DbEntry ValuesToDbEntry<T>(T?[] value, ValueField<T, Formatter> field)
      where T: struct
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      SerializeValues<T>(value, field);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes arrays of nullable value types using the default
    /// <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized array.</returns>
    public DbEntry ValuesToDbEntry<T>(T?[] value)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return ValuesToDbEntry(value, field);
    }

    #endregion Value Type Arrays

    #region Value Type Collections

    /// <summary>Serializes value type sequences accessible through <see cref="IList{T}"/>
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized collection.</returns>
    public DbEntry ValuesToDbEntry<T>(IList<T> value, ValueField<T, Formatter> field)
      where T: struct
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      SerializeValues<T>(value, field);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IList{T}"/>
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized collection.</returns>
    public DbEntry ValuesToDbEntry<T>(IList<T> value)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return ValuesToDbEntry(value, field);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IEnumerable{T}"/>
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized collection.</returns>
    public DbEntry ValuesToDbEntry<T>(IEnumerable<T> value, ValueField<T, Formatter> field)
      where T: struct
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      SerializeValues<T>(value, field);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IEnumerable{T}"/>
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized collection.</returns>
    public DbEntry ValuesToDbEntry<T>(IEnumerable<T> value)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return ValuesToDbEntry(value, field);
    }

    #endregion Value Type Collections

    #region Reference Type Arrays

    /// <overloads>Serializes reference type sequences (arrays, collections)
    /// into a <see cref="DbEntry"/> instance.</overloads>
    /// <summary>Serializes reference type arrays using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of array elements - must be reference type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized array.</returns>
    public DbEntry ObjectsToDbEntry<T>(T[] value, ReferenceField<T, Formatter> field)
      where T: class
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      SerializeObjects<T>(value, field);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes reference type arrays using the default <see cref="Field{T, F}"/>
    /// instance registered for that type.</summary>
    /// <typeparam name="T">Type of array elements - must be reference type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized array.</returns>
    public DbEntry ObjectsToDbEntry<T>(T[] value)
      where T: class
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      return ObjectsToDbEntry(value, field);
    }

    #endregion Reference Type Arrays

    #region Reference Type Collections

    /// <summary>Serializes reference type sequences accessible through <see cref="IList{T}"/>
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized collection.</returns>
    public DbEntry ObjectsToDbEntry<T>(IList<T> value, ReferenceField<T, Formatter> field)
      where T: class
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      SerializeObjects<T>(value, field);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IList{T}"/>
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized collection.</returns>
    public DbEntry ObjectsToDbEntry<T>(IList<T> value)
      where T: class
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      return ObjectsToDbEntry(value, field);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IEnumerable{T}"/>
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized collection.</returns>
    public DbEntry ObjectsToDbEntry<T>(IEnumerable<T> value, ReferenceField<T, Formatter> field)
      where T: class
    {
      int index = 0;
      byte[] buffer = NextBuffer();
      InitSerialization(buffer, index);
      SerializeObjects<T>(value, field);
      index = FinishSerialization();
      return DbEntry.InOut(buffer, 0, index);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IEnumerable{T}"/>
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <returns>A properly initialized <see cref="DbEntry"/> instance containing
    /// the serialized collection.</returns>
    public DbEntry ObjectsToDbEntry<T>(IEnumerable<T> value)
      where T: class
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      return ObjectsToDbEntry(value, field);
    }

    #endregion Reference Type Collections

    #endregion Serialize To DbEntry

    #region Deserialize From DbEntry

    #region Single Instances

    /// <overloads>Deserializes an object graph from a <see cref="DbEntry"/> instance.</overloads>
    /// <summary>Deserializes a nullable value type from a <see cref="DbEntry"/> instance
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    /// <returns>Deserialized struct instance, or <c>null</c>.</returns>
    public T? FromDbEntry<T>(ref DbEntry entry, ValueField<T, Formatter> field)
      where T: struct
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      T? result = field.Deserialize();
      index = FinishDeserialization();
      return result;
    }

    /// <summary>Deserializes a nullable value type from a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <returns>Deserialized struct instance, or <c>null</c>.</returns>
    public T? FromDbEntry<T>(ref DbEntry entry)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return FromDbEntry(ref entry, field);
    }

    /// <summary>Deserializes a value type from a <see cref="DbEntry"/> instance
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.</remarks>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="value">Struct instance to deserialize.</param>
    /// <param name="isNull">Returns <c>true</c> if a <c>null</c> is deserialized,
    /// in which case the <c>value</c> parameter is ignored.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    public void FromDbEntry<T>(ref T value, out bool isNull, ref DbEntry entry, ValueField<T, Formatter> field)
      where T: struct 
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      field.Deserialize(ref value, out isNull);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes a value type from a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.</remarks>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="value">Struct instance to deserialize.</param>
    /// <param name="isNull">Returns <c>true</c> if a <c>null</c> is deserialized,
    /// in which case the <c>value</c> parameter is ignored.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    public void FromDbEntry<T>(ref T value, out bool isNull, ref DbEntry entry)
      where T: struct 
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      FromDbEntry(ref value, out isNull, ref entry, field);
    }

    /// <summary>Deserializes a reference type from a <see cref="DbEntry"/> instance
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Reference type to deserialize.</typeparam>
    /// <param name="obj">Object to deserialize, or <c>null</c> - in which
    /// case a new object will be instantiated.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ReferenceField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    public void FromDbEntry<T>(ref T obj, ref DbEntry entry, ReferenceField<T, Formatter> field)
      where T: class
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      field.Deserialize(ref obj);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes a reference type from a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Reference type to deserialize.</typeparam>
    /// <param name="obj">Object to deserialize, or <c>null</c> - in which
    /// case a new object will be instantiated.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    public void FromDbEntry<T>(ref T obj, ref DbEntry entry)
      where T: class
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      FromDbEntry(ref obj, ref entry, field);
    }

    #endregion Single Instances

    #region Value Type Arrays

    /// <overloads>Deserializes value type sequences (arrays, collections) from
    /// a <see cref="DbEntry"/> instance.</overloads>
    /// <summary>Deserializes value type arrays from a <see cref="DbEntry"/> instance
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    public void ValuesFromDbEntry<T>(ref T[] value, ref DbEntry entry, ValueField<T, Formatter> field)
      where T: struct 
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      DeserializeValues(ref value, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes value type arrays from a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    public void ValuesFromDbEntry<T>(ref T[] value, ref DbEntry entry)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      ValuesFromDbEntry(ref value, ref entry, field);
    }

    /// <summary>Deserializes arrays of nullable value types from a <see cref="DbEntry"/>
    /// instance using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    public void ValuesFromDbEntry<T>(ref T?[] value, ref DbEntry entry, ValueField<T, Formatter> field)
      where T: struct
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      DeserializeValues(ref value, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes arrays of nullable value types from a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    public void ValuesFromDbEntry<T>(ref T?[] value, ref DbEntry entry)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      ValuesFromDbEntry(ref value, ref entry, field);
    }

    #endregion Value Type Arrays

    #region Value Type Collections

    /// <summary>Deserializes any kind of value type collection through call-backs
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns another delegate to add sequence elements to the collection.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    public void ValuesFromDbEntry<T>(InitValueSequence<T> initSequence, ref DbEntry entry, ValueField<T, Formatter> field)
      where T: struct
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      DeserializeValues(initSequence, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of value type collection through call-backs
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns another delegate to add sequence elements to the collection.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    public void ValuesFromDbEntry<T>(InitValueSequence<T> initSequence, ref DbEntry entry)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      ValuesFromDbEntry(initSequence, ref entry, field);
    }

    /// <summary>Deserializes any kind of nullable value type collection through call-backs
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns another delegate to add sequence elements to the collection.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    public void ValuesFromDbEntry<T>(InitSequence<T?> initSequence, ref DbEntry entry, ValueField<T, Formatter> field)
      where T: struct
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      DeserializeValues(initSequence, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of nullable value type collection through call-backs
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns another delegate to add sequence elements to the collection.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    public void ValuesFromDbEntry<T>(InitSequence<T?> initSequence, ref DbEntry entry)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      ValuesFromDbEntry(initSequence, ref entry, field);
    }

    #endregion Value Type Collections

    #region Reference Type Arrays

    /// <overloads>Deserializes reference type sequences (arrays, collections) from
    /// a <see cref="DbEntry"/> instance.</overloads>
    /// <summary>Deserializes reference type arrays from a <see cref="DbEntry"/>
    /// instance using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="obj">Reference type array to deserialize.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ReferenceField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    public void ObjectsFromDbEntry<T>(ref T[] obj, ref DbEntry entry, ReferenceField<T, Formatter> field)
      where T: class 
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      DeserializeObjects(ref obj, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes reference type arrays from a <see cref="DbEntry"/> instance
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="obj">Reference type array to deserialize.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    public void ObjectsFromDbEntry<T>(ref T[] obj, ref DbEntry entry)
      where T: class
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      ObjectsFromDbEntry(ref obj, ref entry, field);
    }

    #endregion Reference Type Arrays

    #region Reference Type Collections

    /// <summary>Deserializes any kind of reference type collection through call-backs
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns another delegate to add sequence elements to the collection.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    /// <param name="field"><see cref="ReferenceField{T, Formatter}"/> instance that
    /// deserializes the <c>entry</c>argument.</param>
    public void ObjectsFromDbEntry<T>(InitSequence<T> initSequence, ref DbEntry entry, ReferenceField<T, Formatter> field)
      where T: class 
    {
      int index = entry.Start;
      InitDeserialization(entry.Buffer, index);
      DeserializeObjects(initSequence, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of reference type collection through call-backs
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns another delegate to add sequence elements to the collection.</param>
    /// <param name="entry"><see cref="DbEntry"/> instance containing the serialized data.</param>
    public void ObjectsFromDbEntry<T>(InitSequence<T> initSequence, ref DbEntry entry)
      where T: class 
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      ObjectsFromDbEntry(initSequence, ref entry, field);
    }

    #endregion Reference Type Collections

    #endregion Deserialize From DbEntry
  }
}