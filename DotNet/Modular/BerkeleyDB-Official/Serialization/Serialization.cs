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
using System.Reflection;
using System.Diagnostics;

namespace Kds.Serialization
{
  /// <summary>
  /// A concrete implementation of this abstract base-class, together with corresponding
  /// implementations of <see cref="ValueField&lt;T, S>"/> and <see cref="ReferenceField&lt;T, S>"/>
  /// is used to serialize and deserialize an object graph.
  /// </summary>
  /// <remarks>
  /// The concrete subclass determines not only the binary layout of the serialized
  /// object graph, but also the output target, like - for instance a stream or memory buffer.
  /// </remarks>
  /// <typeparam name="F">Recursive type parameter that helps using the
  /// correct subclasses of <c>Formatter</c> and <see cref="Field&lt;T, F>"/> together.</typeparam>
  public abstract class Formatter<F> where F: Formatter<F>
  {
    /// <summary>Writes a <c>SerialStatus</c> value to the serialization target.</summary>
    /// <param name="value">Value to write.</param>
    protected internal abstract void WriteStatus(SerialStatus value);

    
    /// <summary>Writes an object reference to the serialization target.</summary>
    /// <remarks>The implementation may decide not to use all 64 bits of the handle value.</remarks>
    /// <param name="value">Object handle to write.</param>
    protected internal abstract void WriteObjRef(Int64 value);

    /// <summary>Writes a length prefix to the serialization target.</summary>
    /// <remarks>This is usually needed whe a sequence of objects is serilaized,
    /// like for instance an array or a hash table.</remarks>
    /// <param name="value">Length value to write.</param>
    protected internal abstract void WriteCount(Int32 value);

    /// <summary>Reads a <c>SerialStatus</c> value from the serialized target.</summary>
    /// <returns>Value to read.</returns>
    protected internal abstract SerialStatus ReadStatus();

    /// <summary>Reads an object reference from the serialized target.</summary>
    /// <returns>Object handle to read.</returns>
    protected internal abstract Int64 ReadObjRef();

    /// <summary>Skips an object reference while deserialing.</summary>
    protected internal abstract void SkipObjRef();

    /// <summary>Reads a length prefix from the serialized target.</summary>
    /// <returns>Length prefix.</returns>
    protected internal abstract Int32 ReadCount();

    /// <summary>
    /// Opens (registers) a given object reference and returns a new handle for it.
    /// If the object is already registered, the existing handle is returned.
    /// </summary>
    /// <remarks>Is needed to detect if an object has already been serialized - to
    /// prevent it from being (de)serialized multiple times - and to deal with circular
    /// references which could lead to infinite recursion.</remarks>
    /// <param name="obj">Object to get the handle for.</param>
    /// <param name="handle">Handle to be returned for the object.</param>
    /// <returns><c>true</c> if the object handle already exists, <c>false</c>
    /// if this was the first call for a particular object.</returns>
    protected internal abstract bool OpenReference(object obj, out Int64 handle);

    /// <summary>Returns the object (reference) for a given object handle.</summary>
    /// <param name="handle">Object handle.</param>
    /// <returns>Object reference, or <c>null</c> if the handle is invalid.</returns>
    protected internal abstract object GetReference(Int64 handle);

    /// <summary>Registers pre-existing objects referenced by the object graph.</summary>
    /// <remarks>This is required when specific objects that are referenced by
    /// an object graph should not be serialized, as they will also already exist
    /// when the object graph is deserialized.</remarks>
    /// <param name="objects">Array of pre-existing objects. The number and order of
    /// these objects must be exactly the same for serialization and deserialization.</param>
    public abstract void SetPermanentReferences(params object[] objects);

    /// <summary>Retrieves the <see cref="Field&lt;T, F>"/> instance registered for
    /// serializing a specific data type.</summary>
    /// <typeParam name="T">Data type to be serialized or deserialized.</typeParam>
    /// <returns><see cref="Field&lt;T, F>"/> instance associated with data type.</returns>
    /// <seealso cref="RegisterField{T}(Field{T, F})"/>
    public abstract Field<T, F> GetField<T>();

    /// <summary>Override to implement the actual field registration.</summary>
    /// <seealso cref="RegisterField{T}(Field&lt;T, F>)"/>
    /// <typeparam name="T">Data type associated with field.</typeparam>
    /// <param name="field"><see cref="Field&lt;T, F>"/> instance to be registered.</param>
    /// <returns><c>true</c> if the field instance was successfully registered, <c>false</c>
    /// if another field instance was already registered for data type <c>T</c>.</returns>
    protected abstract bool InternalRegisterField<T>(Field<T, F> field);

    /// <summary>Registers a <see cref="Field&lt;T, F>"/> instance to be used as the
    /// default field to serialize and deserialize a given data type <c>T</c>.</summary>
    /// <typeparam name="T">Data type to serialize.</typeparam>
    /// <param name="field"><see cref="Field&lt;T, F>"/> instance associated with data type.</param>
    internal void RegisterField<T>(Field<T, F> field) {
      if (!InternalRegisterField<T>(field)) {
        string msg = "A default field is already registered for type '{0}'.";
        throw new SerializationException(string.Format(msg, typeof(T).ToString()));
      }
    }

    #region Skipping Fields

    private int[] skipPath;
    internal int skipLevel;
    internal int inSequence;

    /// <overloads>
    /// <summary>On deserialization, skips a specific data type.</summary>
    /// <remarks>Never call this method outside of a <see cref="Field&lt;T, F>.SkipValue"/>
    /// override, as it needs the initialization performed by <see cref="SkipMembers{T}"/>.</remarks>
    /// </overloads>
    /// <summary>Uses a specified <see cref="Field&lt;T, F>"/> instance instead of the default.</summary>
    /// <typeparam name="T">Data type associated with field.</typeparam>
    /// <param name="field">Field associated with data type of member to skip.</param>
    /// <returns><c>true</c> if skipping should continue, <c>false otherwise.</c></returns>
    public bool Skip<T>(Field<T, F> field) {
      bool result = true;
      if (inSequence > 0)
        field.Skip();
      else {
        result = skipPath[skipLevel]-- > 0;
        // if we are not at the last level, let's enter this field,
        // even if it is not to be skipped as a whole
        if (result || skipLevel < skipPath.Length - 1)
          field.Skip();
      }
      return result;
    }

    /// <summary>Uses the default <see cref="Field&lt;T, F>"/> instance.</summary>
    /// <typeparam name="T">Data type to skip.</typeparam>
    /// <returns><c>true</c> if skipping should continue, <c>false otherwise.</c></returns>
    public bool Skip<T>() {
      Field<T, F> field = GetField<T>();
      return Skip(field);
    }

    private void DoSkipSequence<T>(Field<T, F> field) {
      SerialStatus status = ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          break;
        case SerialStatus.Reference:
          throw new SerializationException("Sequences cannot be referenced.");
        case SerialStatus.Value:
          int count = ReadCount();
          inSequence++;
          try {
            for (int indx = 0; indx < count; indx++)
              field.Skip();
          }
          finally {
            inSequence--;
          }
          break;
      }
    }

    /// <overloads>
    /// <summary>On deserialization, skips sequences of a specific data type.</summary>
    /// <remarks>Never call this method outside of a <see cref="Field&lt;T, F>.SkipValue"/>
    /// override, as it needs the initialization performed by <see cref="SkipMembers{T}"/>.</remarks>
    /// </overloads>
    /// <summary>Uses a specified <see cref="Field&lt;T, F>"/> instance instead of the default.</summary>
    /// <typeparam name="T">Data type associated with field.</typeparam>
    /// <param name="field">Field associated with data type of sequence elements to skip.</param>
    /// <returns><c>true</c> if skipping should continue, <c>false otherwise.</c></returns>
    public bool SkipSequence<T>(Field<T, F> field) {
      bool result = true;
      if (inSequence > 0)
        DoSkipSequence(field);
      else {
        result = skipPath[skipLevel]-- > 0;
        // if we are not at the last level, let's enter this field,
        // even if it is not to be skipped as a whole
        if (result || skipLevel < skipPath.Length - 1)
          DoSkipSequence<T>(field);
      }
      return result;
    }

    /// <summary>Uses the default <see cref="Field&lt;T, F>"/> instance.</summary>
    /// <typeparam name="T">Data type of sequence elements to skip.</typeparam>
    /// <returns><c>true</c> if skipping should continue, <c>false otherwise.</c></returns>
    public bool SkipSequence<T>() {
      Field<T, F> field = GetField<T>();
      return SkipSequence<T>(field);
    }

    /// <overloads>
    /// <summary>
    /// Skips recursively over children of the root object while deserializing an
    /// object graph. Allows partial deserialization at a given point in the graph.
    /// </summary>
    /// <remarks>There are several restrictions to consider:
    /// <list type="bullet">
    /// <item>One must not skip a member which is referenced in the part of
    ///   the graph that needs to be deserialized, as the object reference would not
    ///   be matched to a deserialized object. This might throw an invalid cast
    ///   exception when deserializing the rest, or worse, return with no error.</item>
    /// <item>A sequence (serialized array or collection) counts as one field, that
    ///   is, it is not possible to skip into the middle of a sequence.</item>
    /// <item>Call <c>SkipMembers</c> only for the root object, and only once
    ///   for a given deserialization.</item>
    /// </list>
    /// </remarks>
    /// <example>How to determine the path to a given member of a child object is best
    /// illustrated using an example:
    /// <code>
    /// class A
    /// {
    ///   private int num;
    ///   private string name;
    ///   private B child;
    ///   private float price;
    ///   ...
    /// }
    /// 
    /// class B
    /// {
    ///   private string name;
    ///   private int count;
    ///   ...
    /// }
    /// 
    /// BEFormatter fmt = new BEFormatter();
    /// A obj = new A();
    /// ...
    /// // serialize members in order of declaration - not shown
    /// fmt.Serialize(obj, buffer, ref index);
    /// ...
    /// // extract obj.child.name
    /// string name;
    /// fmt.InitDeserialization(buffer, index);
    /// // child is the 3rd member in A, name is the first member in B
    /// fmt.SkipMembers&lt;A>(new int[] {2, 0});
    /// fmt.Deserialize&lt;string>(ref name);
    /// fmt.index = FinishDeserialization(); 
    /// ...
    /// </code>
    /// </example>
    /// </overloads>
    /// <summary>Uses a specified <see cref="Field&lt;T, F>"/> instance instead of the default.</summary>
    /// <typeparam name="T">Data type associated with field.</typeparam>
    /// <param name="field">Field associated with root object in graph.</param>
    /// <param name="path">Path in the graph which is to be skipped. Node indexes
    /// are 0-based.</param>
    /// <returns><c>true</c> if successful, <c>false</c> if path is invalid.</returns>
    public bool SkipMembers<T>(Field<T, F> field, params int[] path) {
      skipPath = path;
      skipLevel = -1;
      inSequence = 0;
      field.Skip();
      return path[path.Length - 1] < 0;
    }

    /// <summary>Uses the default <see cref="Field&lt;T, F>"/> instance.</summary>
    /// <typeparam name="T">Data type associated with field.</typeparam>
    /// <param name="path">Path in the graph which is to be skipped.</param>
    /// <returns><c>true</c> if successful, <c>false</c> if path is invalid.</returns>
    public bool SkipMembers<T>(params int[] path) {
      Field<T, F> field = GetField<T>();
      return SkipMembers(field, path);
    }

    #endregion

    #region Serialize Single Instance

    /// <overloads>Serializes values of a specific type.</overloads>
    /// <summary>Serializes nullable value types.</summary>
    /// <typeparam name="T">Value type that is to be serialized.</typeparam>
    /// <param name="value">Value type instance to serialize.</param>
    public void Serialize<T>(T? value)
      where T: struct
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      field.Serialize(value);
    }

    /// <summary>Serializes large value types.</summary>
    /// <remarks>Value type copying is avoided due to the use of a 'ref' parameter,
    /// but a <c>null</c> cannot be passed. See <see cref="SerializeNull"/>.</remarks>
    /// <typeparam name="T">Value type that is to be serialized.</typeparam>
    /// <param name="value">Value type instance to serialize.</param>
    public void Serialize<T>(ref T value)
      where T: struct
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      field.Serialize(ref value);
    }

    /// <summary>Serializes <c>null</c>.</summary>
    /// <remarks>Useful for large value types. This is a companion method to
    /// <see cref="Serialize&lt;T>(ref T)">Serialize&lt;T>(ref T value)</see>.</remarks>
    public void SerializeNull() {
      WriteStatus(SerialStatus.Null);
    }

    /// <summary>Serializes reference types.</summary>
    /// <typeparam name="T">Reference type to be serialized.</typeparam>
    /// <param name="obj">Object to serialize.</param>
    public void Serialize<T>(T obj)
      where T: class 
    {
      ReferenceField<T, F> field = (ReferenceField<T, F>)GetField<T>();
      field.Serialize(obj);
    }

    #endregion

    #region Deserialize Single Instance

    /// <overloads>Deserializes values of a specific type.</overloads>
    /// <summary>Deserializes nullable types.</summary>
    /// <typeparam name="T">Value type that is to be deserialized.</typeparam>
    public T? Deserialize<T>()
      where T: struct
    {
      T? result;
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      result = field.Deserialize();
      return result;
    }

    /// <summary>Deserializes large value types.</summary>
    /// <remarks>Value type copying is avoided due to the use of a 'ref' parameter.</remarks>
    /// <typeparam name="T">Value type that is to be deserialized.</typeparam>
    /// <param name="value">Value to serialize, passed by reference.</param>
    /// <param name="isNull">Indicates <c>null</c>. If this parameter returns <c>true</c>
    /// then the <c>value</c> has not been modified.</param>
    public void Deserialize<T>(ref T value, out bool isNull)
      where T: struct 
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      field.Deserialize(ref value, out isNull);
    }

    /// <summary>Deserializes reference types.</summary>
    /// <remarks>If a <c>null</c> argument is passed in, then a new object will be instantiated.</remarks>
    /// <typeparam name="T">Reference type that is to be deserialized.</typeparam>
    /// <param name="obj">Object to deserialize, or <c>null</c>.</param>
    public void Deserialize<T>(ref T obj)
      where T: class
    {
      ReferenceField<T, F> field = (ReferenceField<T, F>)GetField<T>();
      field.Deserialize(ref obj);
    }

    #endregion

    #region Serialize Value Type Sequences

    #region Serialize Value Type Arrays

    /// <overloads>Serializes sequences (arrays, collections) of a given type.</overloads>
    /// <summary>Serializes value type arrays using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    public void SerializeValues<T>(T[] value, ValueField<T, F> field)
      where T: struct 
    {
      if (value == null) {
        WriteStatus(SerialStatus.Null);
        return;
      }
      WriteStatus(SerialStatus.Value);
      WriteCount(value.Length);
      for (int indx = 0; indx < value.Length; indx++)
        field.Serialize(ref value[indx]);
    }

    /// <summary>Serializes value type arrays using the default <see cref="Field{T, F}"/>
    /// instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    public void SerializeValues<T>(T[] value)
      where T: struct
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      SerializeValues<T>(value, field);
    }

    /// <summary>Serializes arrays of nullable value types using a specific
    /// <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    public void SerializeValues<T>(T?[] value, ValueField<T, F> field)
      where T: struct 
    {
      if (value == null) {
        WriteStatus(SerialStatus.Null);
        return;
      }
      WriteStatus(SerialStatus.Value);
      WriteCount(value.Length);
      for (int indx = 0; indx < value.Length; indx++)
        field.Serialize(value[indx]);
    }

    /// <summary>Serializes arrays of nullable value types using the default <see cref="Field{T, F}"/>
    /// instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    public void SerializeValues<T>(T?[] value)
      where T: struct 
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      SerializeValues<T>(value, field);
    }

    #endregion Serialize Value Type Arrays

    #region Serialize Value Type Collections

    /// <summary>Serializes value type sequences accessible through <see cref="IList{T}"/>
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    public void SerializeValues<T>(IList<T> value, ValueField<T, F> field)
      where T: struct
    {
      if (ReferenceEquals(value, null)) {
        WriteStatus(SerialStatus.Null);
        return;
      }
      WriteStatus(SerialStatus.Value);
      WriteCount(value.Count);
      foreach (T element in value)
        field.Serialize(element);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IList{T}"/>
    /// using the default <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    public void SerializeValues<T>(IList<T> value)
      where T: struct 
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      SerializeValues<T>(value, field);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IEnumerable{T}"/>
    /// using a specific <see cref="Field{T, F}"/> instance</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    public void SerializeValues<T>(IEnumerable<T> value, ValueField<T, F> field)
      where T: struct 
    {
      if (ReferenceEquals(value, null)) {
        WriteStatus(SerialStatus.Null);
        return;
      }
      WriteStatus(SerialStatus.Value);
      IEnumerator<T> valEnum = value.GetEnumerator();
      Int32 count = 0;
      while (valEnum.MoveNext())
        count++;
      WriteCount(count);
      valEnum.Reset();
      while (valEnum.MoveNext())
        field.Serialize(valEnum.Current);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IEnumerable{T}"/>
    /// using the default <see cref="Field{T, F}"/> instance registered for the underlying type</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    public void SerializeValues<T>(IEnumerable<T> value)
      where T: struct 
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      SerializeValues<T>(value, field);
    }

    #endregion Serialize Value Type Collections

    #endregion Serialize Value Type Sequences

    #region Deserialize Value Type Sequences

    #region Deserialize Value Type Arrays

    /// <overloads>Deserializes sequences (arrays, collections) of a given type.</overloads>
    /// <summary>Deserializes value type arrays using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    public void DeserializeValues<T>(ref T[] value, ValueField<T, F> field)
      where T: struct 
    {
      SerialStatus status = ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          value = null;
          break;
        case SerialStatus.Reference:
          throw new SerializationException("Value types cannot be referenced.");
        case SerialStatus.Value:
          Int32 count = ReadCount();
          value = new T[count];
          for (int indx = 0; indx < count; indx++) {
            bool isNull;
            Deserialize<T>(ref value[indx], out isNull);
            if (isNull)
              throw new SerializationException("Non-nullable value type: " + typeof(T).FullName + ".");          }
          break;
      }
    }

    /// <summary>Deserializes value type arrays using the default <see cref="Field{T, F}"/>
    /// instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    public void DeserializeValues<T>(ref T[] value)
      where T: struct
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      DeserializeValues<T>(ref value, field);
    }

    /// <summary>Deserializes arrays of nullable value types using a specific
    /// <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    public void DeserializeValues<T>(ref T?[] value, ValueField<T, F> field)
      where T: struct
    {
      SerialStatus status = ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          value = null;
          break;
        case SerialStatus.Reference:
          throw new SerializationException("Value types cannot be referenced.");
        case SerialStatus.Value:
          Int32 count = ReadCount();
          value = new T?[count];
          for (int indx = 0; indx < count; indx++)
            value[indx] = field.Deserialize();
          break;
      }
    }

    /// <summary>Deserializes arrays of nullable value types using the default
    /// <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    public void DeserializeValues<T>(ref T?[] value)
      where T: struct
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      DeserializeValues<T>(ref value, field);
    }

    #endregion Deserialize Value Type Arrays

    #region Deserialize Value Type Collections

    /// <summary>Deserializes any kind of value type collection through call-backs
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    public void DeserializeValues<T>(InitValueSequence<T> initSequence, ValueField<T, F> field)
      where T: struct
    {
      SerialStatus status = ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          initSequence(-1);
          break;
        case SerialStatus.Reference:
          throw new SerializationException("Value types cannot be referenced.");
        case SerialStatus.Value:
          Int32 count = ReadCount();
          AddValueItem<T> addItem = initSequence(count);
          T value = new T();
          for (int indx = 0; indx < count; indx++) {
            bool isNull;
            Deserialize<T>(ref value, out isNull);
            addItem(ref value, isNull);
          }
          break;
      }
    }

    /// <summary>Deserializes any kind of value type collection through call-backs
    /// using the default <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    public void DeserializeValues<T>(InitValueSequence<T> initSequence)
      where T: struct
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      DeserializeValues<T>(initSequence, field);
    }

    /// <summary>Deserializes any kind of nullable value type collection through call-backs
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    public void DeserializeValues<T>(InitSequence<T?> initSequence, ValueField<T, F> field)
      where T: struct
    {
      SerialStatus status = ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          initSequence(-1);
          break;
        case SerialStatus.Reference:
          throw new SerializationException("Value types cannot be referenced.");
        case SerialStatus.Value:
          Int32 count = ReadCount();
          AddItem<T?> addItem = initSequence(count);
          for (int indx = 0; indx < count; indx++) {
            T? value = field.Deserialize();
            addItem(value);
          }
          break;
      }
    }

    /// <summary>Deserializes any kind of nullable value type collection through call-backs
    /// using the default <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    public void DeserializeValues<T>(InitSequence<T?> initSequence)
      where T: struct
    {
      ValueField<T, F> field = (ValueField<T, F>)GetField<T>();
      DeserializeValues<T>(initSequence, field);
    }

    #endregion Deserialize Value Type Collections

    #endregion Deserialize Value Type Sequences

    #region Serialize Reference Type Sequences

    #region Serialize Reference Type Arrays

    /// <overloads>Serializes sequences (arrays, collections) of a reference type.</overloads>
    /// <summary>Serializes reference type arrays using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of array elements - must be reference type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    public void SerializeObjects<T>(T[] value, ReferenceField<T, F> field)
      where T: class
    {
      if (value == null) {
        WriteStatus(SerialStatus.Null);
        return;
      }
      WriteStatus(SerialStatus.Value);
      WriteCount(value.Length);
      for (int indx = 0; indx < value.Length; indx++)
        field.Serialize(value[indx]);
    }

    /// <summary>Serializes reference type arrays using the default
    /// <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of array elements - must be reference type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    public void SerializeObjects<T>(T[] value)
      where T: class 
    {
      ReferenceField<T, F> field = (ReferenceField<T, F>)GetField<T>();
      SerializeObjects<T>(value, field);
    }

    #endregion Serialize Reference Type Arrays

    #region Serialize Reference Type Collections

    /// <summary>Serializes reference type sequences accessible through <see cref="IList{T}"/>
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    public void SerializeObjects<T>(IList<T> value, ReferenceField<T, F> field)
      where T: class 
    {
      if (ReferenceEquals(value, null)) {
        WriteStatus(SerialStatus.Null);
        return;
      }
      WriteStatus(SerialStatus.Value);
      WriteCount(value.Count);
      foreach (T element in value)
        field.Serialize(element);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IList{T}"/>
    /// using the default <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    public void SerializeObjects<T>(IList<T> value)
      where T: class 
    {
      ReferenceField<T, F> field = (ReferenceField<T, F>)GetField<T>();
      SerializeObjects<T>(value, field);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IEnumerable{T}"/>
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    public void SerializeObjects<T>(IEnumerable<T> value, ReferenceField<T, F> field)
      where T: class 
    {
      if (ReferenceEquals(value, null)) {
        WriteStatus(SerialStatus.Null);
        return;
      }
      WriteStatus(SerialStatus.Value);
      IEnumerator<T> valEnum = value.GetEnumerator();
      Int32 count = 0;
      while (valEnum.MoveNext())
        count++;
      WriteCount(count);
      valEnum.Reset();
      while (valEnum.MoveNext())
        field.Serialize(valEnum.Current);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IEnumerable{T}"/>
    /// using the default <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    public void SerializeObjects<T>(IEnumerable<T> value)
      where T: class 
    {
      ReferenceField<T, F> field = (ReferenceField<T, F>)GetField<T>();
      SerializeObjects<T>(value, field);
    }

    #endregion Serialize Reference Type Collections

    #endregion Serialize Reference Type Sequences

    #region Deserialize Reference Type Sequences

    #region Deserialize Reference Type Arrays

    /// <overloads>Deserializes sequences (arrays, collections) of a refence type.</overloads>
    /// <summary>Deserializes reference type arrays using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value">Reference type array to deserialize.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    public void DeserializeObjects<T>(ref T[] value, ReferenceField<T, F> field)
      where T: class 
    {
      SerialStatus status = ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          value = null;
          break;
        case SerialStatus.Reference:
          throw new SerializationException("Sequences cannot be referenced.");
        case SerialStatus.Value:
          Int32 count = ReadCount();
          value = new T[count];
          for (int indx = 0; indx < count; indx++)
            field.Deserialize(ref value[indx]);
          break;
      }
    }

    /// <summary>Deserializes reference type arrays using the default
    /// <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="obj">Reference type array to deserialize.</param>
    public void DeserializeObjects<T>(ref T[] obj)
      where T: class 
    {
      ReferenceField<T, F> field = (ReferenceField<T, F>)GetField<T>();
      DeserializeObjects(ref obj, field);
    }

    #endregion Deserialize Reference Type Arrays

    #region Deserialize Reference Type Collections

    /// <summary>Deserializes any kind of reference type collection through call-backs
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    public void DeserializeObjects<T>(InitSequence<T> initSequence, ReferenceField<T, F> field)
      where T: class 
    {
      SerialStatus status = ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          initSequence(-1);
          break;
        case SerialStatus.Reference:
          throw new SerializationException("Sequences cannot be referenced.");
        case SerialStatus.Value:
          Int32 count = ReadCount();
          AddItem<T> addItem = initSequence(count);
          for (int indx = 0; indx < count; indx++) {
            T value = null;
            field.Deserialize(ref value);
            addItem(value);
          }
          break;
      }
    }

    /// <summary>Deserializes any kind of reference type collection through call-backs
    /// using the default <see cref="Field{T, F}"/> instance registered for the underlying type.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    public void DeserializeObjects<T>(InitSequence<T> initSequence)
      where T: class 
    {
      ReferenceField<T, F> field = (ReferenceField<T, F>)GetField<T>();
      DeserializeObjects(initSequence, field);
    }

    #endregion Deserialize Reference Type Collections

    #endregion Deserialize Reference Type Sequences
  }

  /// <summary>
  /// Concrete implementations of this abstract base-class, together with a corresponding
  /// implementation of <see cref="Kds.Serialization.Formatter&lt;F>"/>, are used to serialize
  /// and deserialize an object graph.
  /// </summary>
  /// <remarks>A concrete subclass of <see cref="Field&lt;T, F>"/> is intended to
  /// serialize/deserialize one specific class or value type.</remarks>
  /// <typeparam name="T">Data type to be serialized and deserialized.</typeparam>
  /// <typeparam name="F">Subclass of <see cref="Kds.Serialization.Formatter&lt;F>"/>
  /// which cooperates with this class.</typeparam>
  public abstract class Field<T, F>
    where F: Formatter<F>
  {
    internal F fmt;

    /// <summary>Creates a new <see cref="Field{T,F}"/> instance and associates it with 
    /// a <see cref="Formatter{F}"/> instance.</summary>
    /// <param name="fmt">The <see cref="Formatter{F}"/> instance to be associated
    /// with the <see cref="Field{T,F}"/> instance.</param>
    /// <param name="isDefault">If <c>true</c>, the <see cref="Field{T,F}"/> instance will
    /// be registered with the <see cref="Formatter{F}"/> as the default field to use for
    /// serializing instances of type <c>T</c>.</param>
    protected Field(F fmt, bool isDefault) {
      if (fmt == null)
        throw new ArgumentNullException("fmt");
      if (isDefault)
        fmt.RegisterField<T>(this);
      this.fmt = fmt;
    }

    /// <summary><see cref="Kds.Serialization.Formatter&lt;F>"/> instance associated with field.</summary>
    public F Fmt {
      get { return fmt; }
    }

    /// <summary>Creates new copy of field instance.</summary>
    /// <remarks>By default this is a shallow copy, but this can be changed
    /// in an overriding implementation.</remarks>
    /// <returns>The cloned field instance.</returns>
    public virtual Field<T, F> Clone() {
      return (Field<T, F>)MemberwiseClone();
    }

    #region Skip

    /// <summary>Skips serialized field value without deserializing it. Useful
    /// for partial deserialization.</summary>
    /// <remarks>For a given target data type, this must be implemented by calling
    /// any of the overloaded versions of <see cref="Kds.Serialization.Formatter{F}.Skip{T}"/>
    /// for its serialized members, in the exact same order as they have been serialized.
    /// Whenever any of these calls returns <c>false</c>, then <c>SkipValue()</c> must
    /// return immediately.</remarks>
    /// <example>Here is an example of such an implementation:
    /// <code>
    /// protected override void SkipValue() {
    ///   if (Fmt.Skip&lt;string>())
    ///   if (Fmt.Skip&lt;string>())
    ///   if (Fmt.Skip&lt;string>())
    ///   if (Fmt.Skip&lt;string>())
    ///   if (Fmt.Skip&lt;float>())
    ///   Fmt.Skip&lt;int>();
    /// }
    /// </code>
    /// </example>
    protected abstract void SkipValue();

    // Skip instead of deserializing field
    internal void Skip() {
      SerialStatus status = Fmt.ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          break;
        case SerialStatus.Reference:
          // throw new SerializationException("Illegal to skip object references.");
          Fmt.SkipObjRef();
          break;
        case SerialStatus.Value:
          if (Fmt.inSequence > 0)
            SkipValue();
          else {
            Fmt.skipLevel++;
            try {
              SkipValue();
            }
            finally {
              Fmt.skipLevel--;
            }
          }
          break;
      }
    }

    #endregion Skip
  }

  /// <summary>Base field class for serializing value types.</summary>
  /// <typeparam name="T">Restricts serializable type to value types.</typeparam>
  /// <typeparam name="F">Associates this class with a <see cref="Formatter&lt;F>"/> subclass.</typeparam>
  public abstract class ValueField<T, F>: Field<T, F>
    where T: struct
    where F: Formatter<F>
  {
    protected ValueField(F fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <summary>Specifies how and in which order struct members are serialized.</summary>
    /// <remarks>
    /// For any given member data type, this must be implemented by calling one of the
    /// overloaded versions of <see cref="Formatter{F}.Serialize{T}"/>,
    /// <see cref="Formatter{F}.SerializeValues{T}"/> or <see cref="Formatter{F}.SerializeObjects{T}"/>.
    /// </remarks>
    /// <example>Here is an example of such an implementation:
    /// <code>
    /// protected override void SerializeValue(ref StockItem value) {
    ///   Fmt.Serialize&lt;string>(value.name);
    ///   Fmt.Serialize&lt;string>(value.category);
    ///   Fmt.Serialize&lt;string>(value.vendor);
    ///   Fmt.Serialize&lt;string>(value.sku);
    ///   Fmt.Serialize&lt;float>(value.price);
    ///   Fmt.Serialize&lt;int>(value.quantity);
    /// }
    /// </code>
    /// </example>
    /// <param name="value">The struct instance to serialize.</param>
    protected abstract void SerializeValue(ref T value);

    /// <summary>Specifies how and in which order struct members are deserialized.</summary>
    /// <remarks>
    /// For any given member data type, this must be implemented by calling one of the
    /// overloaded versions of <see cref="Formatter{F}.Deserialize{T}"/>,
    /// <see cref="Formatter{F}.DeserializeValues{T}"/> or <see cref="Formatter{F}.DeserializeObjects{T}"/>.
    /// The order in which members are deserialized must match the order of serialization.
    /// </remarks>
    /// <example>Here is an example of such an implementation:
    /// <code>
    /// protected override void DeserializeValue(ref StockItem value) {
    ///   Fmt.Deserialize&lt;string>(ref value.name);
    ///   Fmt.Deserialize&lt;string>(ref value.category);
    ///   Fmt.Deserialize&lt;string>(ref value.vendor);
    ///   Fmt.Deserialize&lt;string>(ref value.sku);
    ///   value.price = Fmt.Deserialize&lt;float>();
    ///   value.quantity = Fmt.Deserialize&lt;int>();
    /// }
    /// </code>
    /// </example>
    /// <param name="value">The struct instance to deserialize.</param>
    protected abstract void DeserializeValue(ref T value);

    /// <summary>Serializes a nullable value type.</summary>
    /// <remarks>
    /// This method should only be called when a field instance is used directly
    /// by the application, otherwise the corresponding <see cref="Formatter{F}"/>
    /// will take care of calling it through its overloaded versions of 
    /// <see cref="Formatter{F}.Serialize{T}"/>, <see cref="Formatter{F}.SerializeValues{T}"/>
    /// or <see cref="Formatter{F}.SerializeObjects{T}"/>.
    /// </remarks>
    /// <param name="value">Struct instance to serialize, or <c>null</c>.</param>
    public void Serialize(T? value) {
      if (value.HasValue) {
        Fmt.WriteStatus(SerialStatus.Value);
        T val = value.Value; // Value is a property, cannot be passed by reference
        SerializeValue(ref val);  
      }
      else
        Fmt.WriteStatus(SerialStatus.Null);
    }

    /// <summary>Serializes a value type.</summary>
    /// <remarks>
    /// This method should only be called when a field instance is used directly
    /// by the application, otherwise the corresponding <see cref="Formatter{F}"/>
    /// will take care of calling it through its overloaded versions of 
    /// <see cref="Formatter{F}.Serialize{T}"/>, <see cref="Formatter{F}.SerializeValues{T}"/>
    /// or <see cref="Formatter{F}.SerializeObjects{T}"/>.
    /// <para>Passing the value type by reference avoids copy overhead which is useful
    /// for large value types. However, this does not allow to pass <c>nulls</c>.
    /// To serialize a <c>null</c>, call <see cref="Formatter{F}.SerializeNull()"/>.</para>
    /// </remarks>
    /// <param name="value">The struct instance to serialize. Passed by reference to avoid copy overhead.</param>
    public void Serialize(ref T value) {
      Fmt.WriteStatus(SerialStatus.Value);
      SerializeValue(ref value);
    }

    /// <summary>Deserializes a nullable struct.</summary>
    /// <remarks>This method should only be called when a non-default field instance
    /// is used directly by the application, otherwise the corresponding
    /// <see cref="Formatter{F}"/> will take care of calling it through its
    /// overloaded versions of <see cref="Formatter{F}.Deserialize{T}"/>,
    /// <see cref="Formatter{F}.DeserializeValues{T}"/> or <see cref="Formatter{F}.DeserializeObjects{T}"/>.    
    /// </remarks>
    /// <returns>The deserialized struct, or <c>null</c>.</returns>
    public T? Deserialize() {
      SerialStatus status = Fmt.ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          return null;
        case SerialStatus.Reference:
          throw new SerializationException("Value types cannot be referenced.");
        case SerialStatus.Value:
          T value = new T();
          DeserializeValue(ref value);
          return value;
      }
      return null;
    }

    /// <summary>Deserializes a struct.</summary>
    /// <remarks>
    /// This method should only be called when a non-default field instance
    /// is used directly by the application, otherwise the corresponding
    /// <see cref="Formatter{F}"/> will take care of calling it through its
    /// overloaded versions of <see cref="Formatter{F}.Deserialize{T}"/>,
    /// <see cref="Formatter{F}.DeserializeValues{T}"/> or <see cref="Formatter{F}.DeserializeObjects{T}"/>. 
    /// Useful for large value types because the <c>value</c> parameter is passed by reference.
    /// </remarks>
    /// <param name="value">The object to deserialize, or <c>null</c>.</param>
    /// <param name="isNull">Indicates if the return value is <c>null</c>.
    /// If <c>true</c>, the <c>value</c> argument will not be modified.</param>
    public void Deserialize(ref T value, out bool isNull) {
      SerialStatus status = Fmt.ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          isNull = true;
          return;
        case SerialStatus.Reference:
          throw new SerializationException("Value types cannot be referenced.");
        case SerialStatus.Value:
          DeserializeValue(ref value);
          break;
      }
      isNull = false;
    }
  }

  /// <summary>Base field class for serializing reference types.</summary>
  /// <typeparam name="T">Restricts serializable type to reference types.</typeparam>
  /// <typeparam name="F">Associates this class with a <see cref="Formatter&lt;F>"/> subclass.</typeparam>
  public abstract class ReferenceField<T, F>: Field<T, F>
    where T: class
    where F: Formatter<F>
  {
    protected ReferenceField(F fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <summary>Specifies how and in which order class members are serialized.</summary>
    /// <remarks>
    /// For any given member data type, this must be implemented by calling one of the
    /// overloaded versions of <see cref="Formatter{F}.Serialize{T}"/>,
    /// <see cref="Formatter{F}.SerializeValues{T}"/> or <see cref="Formatter{F}.SerializeObjects{T}"/>.
    /// </remarks>
    /// <example>Here is an example of such an implementation:
    /// <code>
    /// protected override void SerializeValue(StockItem value) {
    ///   Fmt.Serialize&lt;string>(value.name);
    ///   Fmt.Serialize&lt;string>(value.category);
    ///   Fmt.Serialize&lt;string>(value.vendor);
    ///   Fmt.Serialize&lt;string>(value.sku);
    ///   Fmt.Serialize&lt;float>(value.price);
    ///   Fmt.Serialize&lt;int>(value.quantity);
    /// }
    /// </code>
    /// </example>
    /// <param name="value">The object to serialize. Must not be <c>null</c>.</param>
    protected abstract void SerializeValue(T value);

    /// <summary>Creates and/or initializes an instance of the field's associated type.</summary>
    /// <remarks>May deserialize part or all of the instance - as described for
    /// <see cref="DeserializeMembers(T)"/>, but <b>must not</b> deserialize members
    /// that may - directly or indirectly - refer back to the instance. That part of
    /// deserialization must be left to <see cref="DeserializeMembers(T)"/>.</remarks>
    /// <example>Here is an example of such an implementation:
    /// <code>
    /// protected override void DeserializeInstance(ref StockItem instance) {
    ///   if (instance == null)
    ///     instance = new StockItem();
    /// }
    /// </code>
    /// </example>
    /// <param name="instance">Instance to initialize. Can be <c>null</c>, in which
    /// case a new instance must be created.</param>
    protected abstract void DeserializeInstance(ref T instance);

    /// <summary>Specifies how and in which order class members are deserialized.</summary>
    /// <remarks>
    /// For any given member data type, this must be implemented by calling one of the
    /// overloaded versions of <see cref="Formatter{F}.Deserialize{T}"/>,
    /// <see cref="Formatter{F}.DeserializeValues{T}"/> or <see cref="Formatter{F}.DeserializeObjects{T}"/>.
    /// The order in which members are deserialized must match the order of serialization.
    /// </remarks>
    /// <example>Here is an example of such an implementation:
    /// <code>
    /// protected override void DeserializeMembers(StockItem instance) {
    ///   Fmt.Deserialize&lt;string>(ref instance.name);
    ///   Fmt.Deserialize&lt;string>(ref instance.category);
    ///   Fmt.Deserialize&lt;string>(ref instance.vendor);
    ///   Fmt.Deserialize&lt;string>(ref instance.sku);
    ///   instance.price = Fmt.Deserialize&lt;float>();
    ///   instance.quantity = Fmt.Deserialize&lt;int>();
    /// }
    /// </code>
    /// </example>
    /// <param name="instance">The object to serialize. Must not be <c>null</c>.</param>
    protected virtual void DeserializeMembers(T instance) { }

    /// <summary>Serializes an object.</summary>
    /// <remarks>
    /// This method should only be called when a field instance is used directly
    /// by the application, otherwise the corresponding <see cref="Formatter{F}"/>
    /// will take care of calling it through its overloaded versions of 
    /// <see cref="Formatter{F}.Serialize{T}"/>, <see cref="Formatter{F}.SerializeValues{T}"/>
    /// or <see cref="Formatter{F}.SerializeObjects{T}"/>.
    /// </remarks>
    /// <param name="value">The object to serialize.</param>
    public void Serialize(T value) {
      if (ReferenceEquals(value, null)) {
        Fmt.WriteStatus(SerialStatus.Null);
        return;
      }
      Int64 handle;
      if (Fmt.OpenReference(value, out handle)) {
        Fmt.WriteStatus(SerialStatus.Reference);
        Fmt.WriteObjRef(handle);
      }
      else {
        Fmt.WriteStatus(SerialStatus.Value);
        SerializeValue(value);
      }
    }

    /// <summary>Deserializes an object.</summary>
    /// <remarks>
    /// This method should only be called when a field instance is used directly
    /// by the application, otherwise the corresponding <see cref="Formatter{F}"/>
    /// will take care of calling it through its overloaded versions of 
    /// <see cref="Formatter{F}.Serialize{T}"/>, <see cref="Formatter{F}.SerializeValues{T}"/>
    /// or <see cref="Formatter{F}.SerializeObjects{T}"/>.
    /// </remarks>
    /// <param name="value">The object to deserialize, or <c>null</c>.</param>
    public void Deserialize(ref T value) {
      Int64 handle;
      SerialStatus status = Fmt.ReadStatus();
      switch (status) {
        case SerialStatus.Null:
          value = null;
          break;
        case SerialStatus.Reference:
          handle = Fmt.ReadObjRef();
          object obj = Fmt.GetReference(handle);
          if (ReferenceEquals(obj, null))
            throw new SerializationException("Cannot find object reference for handle.");
          value = (T)obj;
          break;
        case SerialStatus.Value:
          DeserializeInstance(ref value);
          if (Fmt.OpenReference(value, out handle))
            throw new SerializationException("Object deserialized multiple times.");
          DeserializeMembers(value);
          break;
      }
    }
  }

  /// <summary>Represents the different states of a serialized field.</summary>
  public enum SerialStatus
  {
    /// <summary>A <c>null</c> has been serialized.</summary>
    Null = 0,
    /// <summary>An object handle/reference has been serialized.</summary>
    Reference,
    /// <summary>An object or struct value has been serialized.</summary>
    Value
  }

  /// <summary>Call-back delegate to use when adding elements to a sequence/collection
  /// that is being deserialized.</summary>
  /// <remarks>Must be called exactly as often as specified though the <c>size</c>
  /// argument in the previous call to <see cref="InitSequence{T}"/>.</remarks>
  /// <typeparam name="T">Type of sequence elements.</typeparam>
  /// <param name="item">Sequence element to add.</param>
  public delegate void AddItem<T>(T item);

  /// <summary>Call-back delegate to be used when deserializing a sequence.</summary>
  /// <remarks>The <see cref="AddItem{T}"/> delegate returned must be called
  /// exactly <c>size</c> number of times.</remarks>
  /// <typeparam name="T">Type of sequence elements.</typeparam>
  /// <param name="size">Exact number of elements that will be added. If <c>0</c>
  /// is passed, then the collection will be empty, if <c>-1</c> is passed,
  /// then the collection is <c>null</c>, that is, no instance must be created.</param>
  /// <returns>A delegate for adding elements to the collection, if <c>size > 0</c>,
  /// or <c>null</c> otherwise.</returns>
  public delegate AddItem<T> InitSequence<T>(int size);

  /// <summary>Like <see cref="AddItem{T}"/>, but intended for large value types.</summary>
  /// <remarks>Passing the value by reference avoids copy overhead.</remarks>
  public delegate void AddValueItem<T>(ref T item, bool isNull) where T: struct;

  /// <summary>Like <see cref="InitSequence{T}"/>, but intended for large value types.</summary>
  /// <remarks>Passing the value by reference avoids copy overhead.</remarks>
  public delegate AddValueItem<T> InitValueSequence<T>(int capacity) where T: struct;

  /// <summary>Exception class that indicates invalid serialized data, or incorrect
  /// use of the serialization framework.</summary>
  public class SerializationException: ApplicationException
  {
    public SerializationException() { }

    public SerializationException(string message) : base(message) { }

    public SerializationException(string message, Exception e) : base(message, e) { }
  }

  //TODO Document that collection objects themselves should be serialized as a specific
  // (field) type with a sequence as member; this would allow for (circular) references
  // to the collection itself
}
