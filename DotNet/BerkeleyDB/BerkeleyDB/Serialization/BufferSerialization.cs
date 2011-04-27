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
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Kds.Serialization.Buffer
{
  /// <summary>
  /// Implementation of abstract class <see cref="Kds.Serialization.Formatter{F}"/>
  /// that uses a byte buffer as serialization/deserialization target.
  /// </summary>
  /// <remarks><list type="bullet">
  /// <item>The cooperating descendants of <see cref="ValueField{T}"/> and
  ///   <see cref="ReferenceField{T}"/> are designed to generate serialized representations
  ///   that can be used for lexical sorting.</item>
  /// <item>The serialization API exposed by <see cref="Formatter{F}"/> is not sufficent by itself,
  ///   as it contains no notion of the serialization medium - e.g. a stream or buffer.
  ///   Any implementation of this API has to add medium related methods. In this case
  ///   the methods are <see cref="InitSerialization(byte[], int)"/>, <see cref="FinishSerialization()"/>,
  ///   <see cref="InitDeserialization(byte[], int)"/> and <see cref="FinishDeserialization()"/>.
  ///   Check the example for how they are used.</item>
  /// </list></remarks>
  /// <example>Basic usage of byte buffer oriented serialization:
  /// <code>
  /// byte[] buffer = new byte[2048];
  /// BEFormatter fmt = new BEFormatter();
  /// new StringField(fmt, true);  // default serializer for strings
  /// new IntField(fmt, true);     // default serializer for integers
  /// ...
  /// int index = 0;
  /// fmt.InitSerialization(buffer, index);
  /// fmt.Serialize&lt;string>(name);
  /// fmt.Serialize&lt;int>(age);
  /// index = fmt.FinishSerialization(); 
  /// ...
  /// // do some work
  /// ...
  /// index = 0;
  /// fmt.InitDeserialization(buffer, index);
  /// fmt.Deserialize&lt;string>(ref name);
  /// age = fmt.Deserialize&lt;int>();
  /// index = fmt.FinishDeserialization(); 
  /// </code></example>
  public class Formatter: Kds.Serialization.Formatter<Formatter>
  {
    private byte[] statusBuffer;
    internal byte[] valueBuffer;

    // save status buffer for re-use
    private byte[] statusBuf;

    private int statusIndx;  // two-bit index (four per byte)
    private int startIndx;   // index where (de)serialization starts
    internal int valueIndx;  // byte index

    private int openObjCount;
    private object[] openObjects;               // maps object handle to object
    private Dictionary<object, int> openObjMap; // maps object to object handle
    private List<object> permanentObjects;

    private Dictionary<Type, object> fieldRegistry;

    public Formatter() {
      openObjCount = 0;
      openObjects = new object[0];
      openObjMap = new Dictionary<object, int>();
      permanentObjects = new List<object>();
      fieldRegistry = new Dictionary<Type, object>();
    }

    /// <summary>Resets the object handle table.</summary>
    /// <remarks>Must be called before each serialization or deserialization.</remarks>
    protected void ResetOpenObjects() {
      if (openObjects.Length < permanentObjects.Count)
        openObjects = new object[permanentObjects.Count];
      else
        Array.Clear(openObjects, 0, openObjects.Length);
      openObjMap.Clear();
      permanentObjects.CopyTo(openObjects);
      openObjCount = permanentObjects.Count;
      for (int indx = 0; indx < openObjCount; indx++)
        openObjMap[openObjects[indx]] = indx;
    }

    /// <summary>Bitmask for serializing <see cref="SerialStatus"/> values.</summary>
    [CLSCompliant(false)]
    protected const uint twoBitIndexMask = 0x00000003;
    /// <summary>Bitmask for serializing <see cref="SerialStatus"/> values.</summary>
    protected static readonly byte[] slotMasks = { 0x03, 0x0C, 0x30, 0xC0 };

    #region Implementation Overrides

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.WriteStatus"/>
    protected internal override void WriteStatus(SerialStatus value) {
      unchecked {
        int byteIndex = statusIndx >> 2;
        int size = byteIndex + 1;
        if (statusBuffer.Length < size) {
          size = size << 1;
          Array.Resize<byte>(ref statusBuffer, size);
        }
        byte byteTwoBitIndex = (byte)(statusIndx & twoBitIndexMask);
        // clear two-bit slot
        int bufByte = statusBuffer[byteIndex] & ~slotMasks[byteTwoBitIndex];
        // set bits in slot
        bufByte |= (int)value << (byteTwoBitIndex << 1);
        statusBuffer[byteIndex] = (byte)bufByte;
      }
      statusIndx++;
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.WriteObjRef"/>
    protected internal override void WriteObjRef(Int64 value) {
      BEConverter.ToBytes((int)value, valueBuffer, ref valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.WriteCount"/>
    protected internal override void WriteCount(int value) {
      BEConverter.ToBytes(value, valueBuffer, ref valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.ReadStatus"/>
    protected internal override SerialStatus ReadStatus() {
      SerialStatus result;
      unchecked {
        int byteIndex = statusIndx >> 2;
        byte byteTwoBitIndex = (byte)(statusIndx & twoBitIndexMask);
        // mask out the interesting bits in the slot
        int bufByte = statusBuffer[byteIndex] & slotMasks[byteTwoBitIndex];
        result = (SerialStatus)(bufByte >> (byteTwoBitIndex << 1));
      }
      statusIndx++;
      return result;
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.ReadObjRef"/>
    protected internal override Int64 ReadObjRef() {
      return (long)BEConverter.ToInt32(valueBuffer, ref valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.SkipObjRef"/>
    protected internal override void SkipObjRef() {
      valueIndx += sizeof(int);
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.ReadCount"/>
    protected internal override int ReadCount() {
      return BEConverter.ToInt32(valueBuffer, ref valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.OpenReference"/>
    protected internal override bool OpenReference(object obj, out Int64 handle) {
      int hndl;
      if (openObjMap.TryGetValue(obj, out hndl)) {
        handle = hndl;
        return true;
      }
      else {
        openObjCount++;
        if (openObjCount >= openObjects.Length)
          Array.Resize<object>(ref openObjects, openObjCount << 1);
        openObjects[openObjCount] = obj;
        openObjMap[obj] = openObjCount;
        handle = openObjCount;
        return false;
      }
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.GetReference"/>
    protected internal override object GetReference(Int64 handle) {
      return openObjects[handle];
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.InternalRegisterField"/>
    protected override bool InternalRegisterField<T>(Field<T, Formatter> field) {
      bool result = !fieldRegistry.ContainsKey(typeof(T));
      if (result)
        fieldRegistry[typeof(T)] = field;
      return result;
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.SetPermanentReferences"/>
    public override void SetPermanentReferences(params object[] objects) {
      foreach (object obj in objects) {
        if (permanentObjects.Contains(obj))
          throw new SerializationException("Permanent object already registered.");
        permanentObjects.Add(obj);
      }
    }

    /// <inheritdoc cref="Kds.Serialization.Formatter{F}.GetField{T}"/>
    public override Field<T, Formatter> GetField<T>() {
      object field;
      Type dataType = typeof(T);
      if (!fieldRegistry.TryGetValue(dataType, out field))
        throw new SerializationException("No field registered for type '" + dataType.Name + "'.");
      return (Field<T, Formatter>)field;
    }

    #endregion

    /* currently we do not resize the buffer
    // call to make sure that buffer for serialization is large enough
    public void CheckValueBuffer(int requiredSize) {
      requiredSize = requiredSize + valueIndx;
      if (valueBuf.Length < requiredSize) {
        requiredSize += requiredSize >> 1;  // go 50% beyond
        Array.Resize<byte>(ref valueBuf, requiredSize);
      }
    }
    */

    #region Serialization API

    private static int nextStatusByte(int statusIndx) {
      return (statusIndx + 3) >> 2;
    }

    /// <summary>Initializes serialization process.</summary>
    /// <remarks>
    /// The calls to the serialization methods of <see cref="Formatter{F}"/> must be bracketed
    /// between calls to <see cref="InitSerialization"/> and <see cref="FinishSerialization"/>.
    /// Any objects serialized between these two calls will have shared object references
    /// serialized only once, avoiding multiple serialization of the same object.
    /// <para>Used together with <see cref="FinishSerialization"/>.</para>
    /// </remarks>
    /// <example>In this example, <c>SalesRep</c> instances shared by <c>Vendor</c>
    /// instances are only serialized once.
    /// <code>
    /// BEFormatter fmt = new BEFormatter();
    /// new VendorField(fmt, true);
    /// new SalesRepField(fmt, true);
    /// ...
    /// int index = 0;
    /// fmt.InitSerialization(buffer, index);
    /// fmt.Serialize&lt;Vendor>(vendor1);
    /// fmt.Serialize&lt;Vendor>(vendor2);
    /// fmt.Serialize&lt;Vendor>(vendor3);
    /// index = fmt.FinishSerialization(); 
    /// </code></example>
    /// <seealso cref="FinishSerialization"/>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index of buffer. Serialization will start writing
    /// to the buffer at this index.</param>
    public void InitSerialization(byte[] buffer, int index) {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      ResetOpenObjects();

      statusIndx = 0;
      startIndx = index;
      valueIndx = index + sizeof(uint); // leave space for length prefix
      if (statusBuf == null)
        statusBuf = new byte[4];
      statusBuffer = statusBuf;
      valueBuffer = buffer;
    }

    /// <summary>Completes serialization process.</summary>
    /// <remarks>See <see cref="InitSerialization"/>. Must be called right after
    /// the last object was serialized. At that point, object references cannot
    /// be shared with previously serialized objects anymore, even if they both
    /// target the same buffer.</remarks>
    /// <example>See <see cref="InitSerialization"/>.</example>
    /// <seealso cref="InitSerialization"/>
    /// <returns>End index. Points to the next byte following the end of the
    /// serialized data in the buffer.</returns>
    public int FinishSerialization() {
      // save status buffer for re-use
      statusBuf = statusBuffer;

      int bufIndx = startIndx;
      // size should not include prefix size!
      int bufSize = valueIndx - bufIndx - sizeof(uint);
      // write length prefix to start of value buffer
      BEConverter.ToBytes(bufSize, valueBuffer, ref bufIndx);
      int endIndex = valueIndx + nextStatusByte(statusIndx);
      if (valueBuffer.Length < endIndex)
        throw new IndexOutOfRangeException("Buffer size too small.");

      // copy status buffer
      bufIndx = valueIndx;
      bufSize = nextStatusByte(statusIndx);
      System.Buffer.BlockCopy(statusBuffer, 0, valueBuffer, bufIndx, bufSize);

      valueBuffer = null;
      return endIndex;
    }

#if BUFFER_API
    #region Single Instances

    /// <overloads>Serializes an object graph into a byte buffer.</overloads>
    /// <summary>Serializes a nullable value type into a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize, or <c>null</c>.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// serializes the <c>value </c>argument.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void Serialize<T>(T? value, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct
    {
      InitSerialization(buffer, index);
      field.Serialize(value);
      index = FinishSerialization();
    }

    /// <summary>Serializes a nullable value type into a byte buffer using
    /// the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize, or <c>null</c>.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void Serialize<T>(T? value, byte[] buffer, ref int index)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      Serialize<T>(value, field, buffer, ref index);
    }

    /// <summary>Serializes a value type into a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.
    /// However, this does not allow to pass <c>nulls</c>. Therefore the companion
    /// method <see cref="SerializeNull"/> is provided.</remarks>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// serializes the <c>value </c>argument.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void Serialize<T>(ref T value, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct 
    {
      InitSerialization(buffer, index);
      field.Serialize(ref value);
      index = FinishSerialization();
    }

    /// <summary>Serializes a value type into a byte buffer using the default
    /// <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.
    /// However, this does not allow to pass <c>nulls</c>. Therefore the companion
    /// method <see cref="SerializeNull"/> is provided.</remarks>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void Serialize<T>(ref T value, byte[] buffer, ref int index)
      where T: struct 
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      Serialize<T>(ref value, field, buffer, ref index);
    }

    /// <summary>Serializes a <c>null</c> into a byte buffer.</summary>
    /// <remarks>Companion method for <see cref="Serialize{T}(ref T, byte[], ref int)">
    /// Serialize&lt;T>(ref T, byte[], ref int)</see>.</remarks>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// serialized <c>null</c>.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeNull(byte[] buffer, ref int index) {
      InitSerialization(buffer, index);
      SerializeNull();
      index = FinishSerialization();
    }

    /// <summary>Serializes a reference type into a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Reference type to serialize.</typeparam>
    /// <param name="obj">Object to serialize.</param>
    /// <param name="field"><see cref="ReferenceField{T, Formatter}"/> instance that
    /// serializes the <c>obj</c>argument.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void Serialize<T>(T obj, ReferenceField<T, Formatter> field, byte[] buffer, ref int index) 
      where T: class 
    {
      InitSerialization(buffer, index);
      field.Serialize(obj);
      index = FinishSerialization();
    }

    /// <summary>Serializes a reference type into a byte buffer using the default
    /// <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Reference type to serialize.</typeparam>
    /// <param name="obj">Object to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void Serialize<T>(T obj, byte[] buffer, ref int index) 
      where T: class 
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      Serialize<T>(obj, field, buffer, ref index);
    }

    #endregion Single Instances

    #region Value Type Arrays

    /// <overloads>Serializes sequences (arrays, collections) of a given type
    /// using a specific <see cref="Field{T, F}"/> instance.</overloads>
    /// <summary>Serializes value type arrays.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeValues<T>(T[] value, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct 
    {
      InitSerialization(buffer, index);
      SerializeValues<T>(value, field);
      index = FinishSerialization();
    }

    /// <overloads>Serializes sequences (arrays, collections) of a value type
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</overloads>
    /// <summary>Serializes value type arrays.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeValues<T>(T[] value, byte[] buffer, ref int index)
      where T: struct
    {
      InitSerialization(buffer, index);
      SerializeValues<T>(value);
      index = FinishSerialization();
    }

    /// <summary>Serializes arrays of nullable value types.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeValues<T>(T?[] value, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct
    {
      InitSerialization(buffer, index);
      SerializeValues<T>(value, field);
      index = FinishSerialization();
    }

    /// <summary>Serializes arrays of nullable value types.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeValues<T>(T?[] value, byte[] buffer, ref int index)
      where T: struct 
    {
      InitSerialization(buffer, index);
      SerializeValues<T>(value);
      index = FinishSerialization();
    }

    #endregion Value Type Arrays

    #region Value Type Collections

    /// <summary>Serializes value type sequences accessible through <see cref="IList{T}"/>.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeValues<T>(IList<T> value, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct
    {
      InitSerialization(buffer, index);
      SerializeValues<T>(value, field);
      index = FinishSerialization();
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IList{T}"/>.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeValues<T>(IList<T> value, byte[] buffer, ref int index)
      where T: struct 
    {
      InitSerialization(buffer, index);
      SerializeValues<T>(value);
      index = FinishSerialization();
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IEnumerable{T}"/></summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeValues<T>(IEnumerable<T> value, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct 
    {
      InitSerialization(buffer, index);
      SerializeValues<T>(value, field);
      index = FinishSerialization();
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IEnumerable{T}"/></summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeValues<T>(IEnumerable<T> value, byte[] buffer, ref int index)
      where T: struct 
    {
      InitSerialization(buffer, index);
      SerializeValues<T>(value);
      index = FinishSerialization();
    }

    #endregion Value Type Collections

    #region Reference Type Arrays

    /// <summary>Serializes reference type arrays.</summary>
    /// <typeparam name="T">Type of array elements - must be reference type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(T[] value, ReferenceField<T, Formatter> field, byte[] buffer, ref int index)
      where T: class
    {
      InitSerialization(buffer, index);
      SerializeObjects<T>(value, field);
      index = FinishSerialization();
    }

    /// <overloads>Serializes sequences (arrays, collections) of a reference type
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</overloads>
    /// <summary>Serializes reference type arrays.</summary>
    /// <typeparam name="T">Type of array elements - must be reference type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(T[] value, byte[] buffer, ref int index)
      where T: class
    {
      InitSerialization(buffer, index);
      SerializeObjects<T>(value);
      index = FinishSerialization();
    }

    #endregion Reference Type Arrays

    #region Reference Type Collections

    /// <summary>Serializes reference type sequences accessible through <see cref="IList{T}"/>.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(IList<T> value, ReferenceField<T, Formatter> field, byte[] buffer, ref int index)
      where T: class 
    {
      InitSerialization(buffer, index);
      SerializeObjects<T>(value, field);
      index = FinishSerialization();
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IList{T}"/>.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(IList<T> value, byte[] buffer, ref int index)
      where T: class
    {
      InitSerialization(buffer, index);
      SerializeObjects<T>(value);
      index = FinishSerialization();
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IEnumerable{T}"/></summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(IEnumerable<T> value, ReferenceField<T, Formatter> field, byte[] buffer, ref int index)
      where T: class 
    {
      InitSerialization(buffer, index);
      SerializeObjects<T>(value, field);
      index = FinishSerialization();
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IEnumerable{T}"/></summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="buffer">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(IEnumerable<T> value, byte[] buffer, ref int index)
      where T: class
    {
      InitSerialization(buffer, index);
      SerializeObjects<T>(value);
      index = FinishSerialization();
    }

    #endregion Reference Type Collections
#endif  // BUFFER_API

    #endregion Serialization API

    #region Deserialization API

    /// <summary>Initializes deserialization process.</summary>
    /// <remarks>
    /// The calls to the deserialization methods of <see cref="Formatter{F}"/> must be bracketed
    /// between calls to <see cref="InitDeserialization"/> and <see cref="FinishDeserialization"/>.
    /// The same data types must be deserialized in the same order as were serialized between
    /// the original calls to <see cref="InitSerialization"/> and <see cref="FinishSerialization"/>.
    /// Used together with <see cref="FinishDeserialization"/>.
    /// </remarks>
    /// <example>In this example, <c>SalesRep</c> instances shared by <c>Vendor</c>
    /// instances were only serialized once, and thus must be deserialized the same way.
    /// <code>
    /// BEFormatter fmt = new BEFormatter();
    /// new VendorField(fmt, true);
    /// new SalesRepField(fmt, true);
    /// ...
    /// int index = 0;
    /// fmt.InitDeserialization(buffer, index);
    /// // get the field only once - more efficient than calling fmt.Deserialize()
    /// VendorField vendorField = (VendorField)fmt.GetField&lt;Vendor>();
    /// vendorField.Deserialize(ref vendor1);
    /// vendorField.Deserialize(ref vendor2);
    /// vendorField.Deserialize(ref vendor3);
    /// index = fmt.FinishDeserialization(); 
    /// </code></example>
    /// <example>In this example, the sales rep phone number is extracted from a serialized
    /// <c>Vendor</c> instance, using partial deserialization by skipping nodes in the object
    /// graph. The sales rep is the 7th serialized member in <c>Vendor</c>, and the phone
    /// number is the 2nd serialized member in <c>SalesRep</c>. 
    /// <code>
    /// BEFormatter fmt = new BEFormatter();
    /// new VendorField(fmt, true);
    /// new SalesRepField(fmt, true);
    /// ...
    /// int index = 0;
    /// fmt.InitDeserialization(buffer, index);
    /// string phoneNo = "";
    /// if (fmt.SkipMembers&lt;Vendor>(6, 1)) {
    ///   fmt.Deserialize&lt;string>(ref phoneNo);
    /// }
    /// else
    ///   throw new ApplicationException("Member path invalid.");
    /// index = fmt.FinishDeserialization();
    /// </code></example>
    /// <seealso cref="FinishDeserialization"/>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void InitDeserialization(byte[] buffer, int index) {
      ResetOpenObjects();
      valueBuffer = buffer;
      valueIndx = index;

      // read size of values, initialize statusIndx
      int bufSize = BEConverter.ToInt32(buffer, ref valueIndx);
      statusIndx = (valueIndx + bufSize) << 2; // statusByte << 2
      statusBuffer = buffer;
    }

    /// <summary>Completes deserialization process.</summary>
    /// <remarks>See <see cref="InitDeserialization"/>. This call must mirror the
    /// original call to <see cref="FinishSerialization"/>, that is, it must occur
    /// right after the last object was deserialized.</remarks>
    /// <example>See <see cref="InitDeserialization"/>.</example>
    /// <seealso cref="InitDeserialization"/>
    /// <returns>End index. Points to the next byte following the end of the
    /// serialized data in the buffer.</returns>
    public int FinishDeserialization() {
      int endIndex = nextStatusByte(statusIndx);
      statusBuffer = null;
      valueBuffer = null;
      return endIndex;
    }

#if BUFFER_API
    #region Single Instances

    /// <overloads>Deserializes an object graph from a byte buffer.</overloads>
    /// <summary>Deserializes a nullable value type from a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>buffer</c> contents.</param>
    /// <returns>Deserialized struct instance, or <c>null</c>.</returns>
    public T? Deserialize<T>(byte[] buffer, ref int index, ValueField<T, Formatter> field)
      where T: struct
    {
      InitDeserialization(buffer, index);
      T? result = field.Deserialize();
      index = FinishDeserialization();
      return result;
    }

    /// <summary>Deserializes a nullable value type from a byte buffer using the
    /// default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    /// <returns>Deserialized struct instance, or <c>null</c>.</returns>
    public T? Deserialize<T>(byte[] buffer, ref int index)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return Deserialize<T>(buffer, ref index, field);
    }

    /// <summary>Deserializes a value type from a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.</remarks>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="value">Struct instance to deserialize.</param>
    /// <param name="isNull">Returns <c>true</c> if a <c>null</c> is deserialized,
    /// in which case the <c>value</c> parameter is ignored.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>buffer</c> contents.</param>
    public void Deserialize<T>(ref T value, out bool isNull, byte[] buffer, ref int index, ValueField<T, Formatter> field)
      where T: struct 
    {
      InitDeserialization(buffer, index);
      field.Deserialize(ref value, out isNull);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes a value type from a byte buffer using the default
    /// <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.</remarks>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="value">Struct instance to deserialize.</param>
    /// <param name="isNull">Returns <c>true</c> if a <c>null</c> is deserialized,
    /// in which case the <c>value</c> parameter is ignored.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void Deserialize<T>(ref T value, out bool isNull, byte[] buffer, ref int index)
      where T: struct 
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      Deserialize<T>(ref value, out isNull, buffer, ref index, field);
    }

    /// <summary>Deserializes a reference type from a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Reference type to deserialize.</typeparam>
    /// <param name="obj">Object to deserialize, or <c>null</c> - in which
    /// case a new object will be instantiated.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    /// <param name="field"><see cref="ReferenceField{T, Formatter}"/> instance that
    /// deserializes the <c>buffer</c> contents.</param>
    public void Deserialize<T>(ref T obj, byte[] buffer, ref int index, ReferenceField<T, Formatter> field)
      where T: class
    {
      InitDeserialization(buffer, index);
      field.Deserialize(ref obj);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes a reference type from a byte buffer using the
    /// default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Reference type to deserialize.</typeparam>
    /// <param name="obj">Object to deserialize, or <c>null</c> - in which
    /// case a new object will be instantiated.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void Deserialize<T>(ref T obj, byte[] buffer, ref int index)
      where T: class
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      Deserialize<T>(ref obj, buffer, ref index, field);
    }

    #endregion Single Instances

    #region Value Type Arrays

    /// <overloads>Deserializes sequences (arrays, collections) of a given type
    /// using a specific <see cref="Field{T, F}"/> instance.</overloads>
    /// <summary>Deserializes value type arrays.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeValues<T>(ref T[] value, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct 
    {
      InitDeserialization(buffer, index);
      DeserializeValues<T>(ref value, field);
      index = FinishDeserialization();
    }

    /// <overloads>Deserializes sequences (arrays, collections) of a value type
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</overloads>
    /// <summary>Deserializes value type arrays.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeValues<T>(ref T[] value, byte[] buffer, ref int index)
      where T: struct
    {
      InitDeserialization(buffer, index);
      DeserializeValues<T>(ref value);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes arrays of nullable value types.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeValues<T>(ref T?[] value, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct
    {
      InitDeserialization(buffer, index);
      DeserializeValues<T>(ref value, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes arrays of nullable value types.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize. Passed  by reference
    /// to avoid copying overhead - suitable for large value types.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeValues<T>(ref T?[] value, byte[] buffer, ref int index)
      where T: struct
    {
      InitDeserialization(buffer, index);
      DeserializeValues<T>(ref value);
      index = FinishDeserialization();
    }

    #endregion Value Type Arrays

    #region Value Type Collections

    /// <summary>Deserializes any kind of value type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeValues<T>(InitValueSequence<T> initSequence, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct
    {
      InitDeserialization(buffer, index);
      DeserializeValues<T>(initSequence, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of value type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeValues<T>(InitValueSequence<T> initSequence, byte[] buffer, ref int index)
      where T: struct
    {
      InitDeserialization(buffer, index);
      DeserializeValues<T>(initSequence);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of nullable value type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeValues<T>(InitSequence<T?> initSequence, ValueField<T, Formatter> field, byte[] buffer, ref int index)
      where T: struct
    {
      InitDeserialization(buffer, index);
      DeserializeValues<T>(initSequence, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of nullable value type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeValues<T>(InitSequence<T?> initSequence, byte[] buffer, ref int index)
      where T: struct
    {
      InitDeserialization(buffer, index);
      DeserializeValues<T>(initSequence);
      index = FinishDeserialization();
    }

    #endregion Value Type Collections

    #region Reference Type Arrays

    /// <summary>Deserializes reference type arrays.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="obj">Reference type array to deserialize.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeObjects<T>(ref T[] obj, ReferenceField<T, Formatter> field, byte[] buffer, ref int index)
      where T: class 
    {
      InitDeserialization(buffer, index);
      DeserializeObjects<T>(ref obj, field);
      index = FinishDeserialization();
    }

    /// <overloads>Deserializes sequences (arrays, collections) of a refence type
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</overloads>
    /// <summary>Deserializes reference type arrays.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="obj">Reference type array to deserialize.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeObjects<T>(ref T[] obj, byte[] buffer, ref int index)
      where T: class
    {
      InitDeserialization(buffer, index);
      DeserializeObjects<T>(ref obj);
      index = FinishDeserialization();
    }

    #endregion Reference Type Arrays

    #region Reference Type Collections

    /// <summary>Deserializes any kind of reference type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeObjects<T>(InitSequence<T> initSequence, ReferenceField<T, Formatter> field, byte[] buffer, ref int index)
      where T: class 
    {
      InitDeserialization(buffer, index);
      DeserializeObjects<T>(initSequence, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of reference type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="buffer">Target buffer.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void DeserializeObjects<T>(InitSequence<T> initSequence, byte[] buffer, ref int index)
      where T: class 
    {
      InitDeserialization(buffer, index);
      DeserializeObjects<T>(initSequence);
      index = FinishDeserialization();
    }

    #endregion Reference Type Collections
#endif  // BUFFER_API

    #endregion Deserialization API
  }

  #region Value Type Fields

  /// <summary>Base class for all <see cref="ValueField{T, F}"/> classes
  /// that are designed to cooperate with <see cref="Formatter"/>.</summary>
  /// <typeparam name="T">Value type that the class will serialize/deserialize.</typeparam>
  public abstract class ValueField<T>: ValueField<T, Formatter>
    where T: struct
  {
    protected ValueField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <summary>Gives access to <see cref="Formatter">Formatter's</see>
    /// internal <c>valueBuffer</c> field.</summary>
    protected byte[] ValueBuffer {
      get { return Fmt.valueBuffer; }
    }

    /// <summary>Gives access to <see cref="Formatter">Formatter's</see>
    /// internal <c>valueIndex</c> field.</summary>
    protected int ValueIndex {
      get { return Fmt.valueIndx; }
      set { Fmt.valueIndx = value; }
    }
  }

  /// <summary>Field representing <c>Byte</c> values.</summary>
  public class ByteField: ValueField<Byte>
  {
    public ByteField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref byte value) {
      Fmt.valueBuffer[Fmt.valueIndx++] = value;
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref byte value) {
      value = Fmt.valueBuffer[Fmt.valueIndx++];
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx++;
    }
  }

  /// <summary>Field representing <c>Boolean</c> values.</summary>
  /// <remarks>Stored as <c>Byte</c> values <c>0xFF, 0x00</c>.</remarks>
  public class BoolField: ValueField<Boolean>
  {
    public BoolField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref bool value) {
      Fmt.valueBuffer[Fmt.valueIndx++] = value ? (byte)0xFF : (byte)0;
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref bool value) {
      value = Fmt.valueBuffer[Fmt.valueIndx++] == 0 ? false : true;
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx++;
    }
  }

  /// <summary>Field representing <c>SByte</c> values.</summary>
  /// <remarks>Sign bit reversed to have negative numbers sorted first.</remarks>
  [CLSCompliant(false)]
  public class SByteField: ValueField<SByte>
  {
    public SByteField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref sbyte value) {
      Fmt.valueBuffer[Fmt.valueIndx++] = unchecked((byte)(-value));
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref sbyte value) {
      value = unchecked((sbyte)-Fmt.valueBuffer[Fmt.valueIndx++]);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx++;
    }
  }

  /// <summary>Field representing <c>UInt16</c> values.</summary>
  [CLSCompliant(false)]
  public class UShortField: ValueField<UInt16>
  {
    public UShortField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref ushort value) {
      BEConverter.ToBytes(value, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref ushort value) {
      value = BEConverter.ToUInt16(Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(ushort);
    }
  }

  /// <summary>Field representing <c>Int16</c> values.</summary>
  /// <remarks>Signed integer numbers are serialized in MSB (most significant byte first,
  /// big-endian) order with their sign bit (high-order bit) inverted to cause negative
  /// numbers to be sorted first when comparing values as unsigned byte arrays, as done
  /// in a database.</remarks>
  public class ShortField: ValueField<Int16>
  {
    public ShortField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref short value) {
      BEConverter.ToBytes(unchecked((short)-value), Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref short value) {
      value = unchecked((short)-BEConverter.ToInt16(Fmt.valueBuffer, ref Fmt.valueIndx));
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(short);
    }
  }

  /// <summary>Field representing <c>Char</c> values.</summary>
  public class CharField: ValueField<Char>
  {
    public CharField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref char value) {
      BEConverter.ToBytes(unchecked((ushort)value), Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref char value) {
      value = unchecked((char)BEConverter.ToUInt16(Fmt.valueBuffer, ref Fmt.valueIndx));
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(ushort);
    }
  }

  /// <summary>Field representing <c>UInt32</c> values.</summary>
  [CLSCompliant(false)]
  public class UIntField: ValueField<UInt32>
  {
    public UIntField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref uint value) {
      BEConverter.ToBytes(value, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref uint value) {
      value = BEConverter.ToUInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(uint);
    }
  }

  /// <summary>Field representing <c>Int32</c> values.</summary>
  /// <remarks>Signed integer numbers are serialized in MSB (most significant byte first,
  /// big-endian) order with their sign bit (high-order bit) inverted to cause negative
  /// numbers to be sorted first when comparing values as unsigned byte arrays, as done
  /// in a database.</remarks>
  public class IntField: ValueField<Int32>
  {
    public IntField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref int value) {
      BEConverter.ToBytes(-value, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref int value) {
      value = -BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(int);
    }
  }

  /// <summary>Field representing <c>UInt64</c> values.</summary>
  [CLSCompliant(false)]
  public class ULongField: ValueField<UInt64>
  {
    public ULongField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref ulong value) {
      BEConverter.ToBytes(value, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref ulong value) {
      value = BEConverter.ToUInt64(Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(ulong);
    }
  }

  /// <summary>Field representing <c>Int64</c> values.</summary>
  /// <remarks>Signed integer numbers are serialized in MSB (most significant byte first,
  /// big-endian) order with their sign bit (high-order bit) inverted to cause negative
  /// numbers to be sorted first when comparing values as unsigned byte arrays, as done
  /// in a database.</remarks>
  public class LongField: ValueField<Int64>
  {
    public LongField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref long value) {
      BEConverter.ToBytes(-value, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref long value) {
      value = -BEConverter.ToInt64(Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(long);
    }
  }

  /// <summary>Field representing <c>Decimal</c> values.</summary>
  /// <remarks>No sign bit reversion here, as the first part contains the scaling
  /// exponent, which grows larger the smaller the number is. It is recommended
  /// to provide a comparison function.</remarks>
  public class DecimalField: ValueField<Decimal>
  {
    public DecimalField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref decimal value) {
      BEConverter.ToBytes(value, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref decimal value) {
      value = BEConverter.ToDecimal(Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += 4 * sizeof(UInt32);  // serialized as four 32bit values
    }
  }

  /// <summary>Field representing <c>Single</c> values.</summary>
  /// <remarks>Stored in standard IEEE 754 bit representation.</remarks>
  public class SingleField: ValueField<Single>
  {
    public SingleField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref float value) {
      BEConverter.ToBytes(value, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref float value) {
      value = BEConverter.ToSingle(Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(UInt32);  // serialized as 32bit value
    }
  }

  /// <summary>Field representing <c>Double</c> values.</summary>
  /// <remarks>Stored in standard IEEE 754 bit representation.</remarks>
  public class DoubleField: ValueField<Double>
  {
    public DoubleField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref double value) {
      BEConverter.ToBytes(value, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref double value) {
      value = BEConverter.ToDouble(Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(UInt64);  // serialized as 64bit value
    }
  }

  /// <summary>Field representing UTC <c>DateTime</c> values.</summary>
  /// <remarks>Serialized as 64bit integer. Converts to UTC time when Value property
  /// is assigned. Time values are measured in 100-nanosecond units called ticks,
  /// and a particular date is the number of ticks since 12:00 midnight, January 1,
  /// 0001 A.D. (C.E.) in the Gregorian calendar.</remarks>
  public class DateTimeField: ValueField<DateTime>
  {
    public DateTimeField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ref DateTime value) {
      BEConverter.ToBytes(value.Ticks, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ValueField{T, F}.DeserializeValue"/>
    protected override void DeserializeValue(ref DateTime value) {
      long ticks = BEConverter.ToInt64(Fmt.valueBuffer, ref Fmt.valueIndx);
      value = new DateTime(ticks, DateTimeKind.Utc);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += sizeof(long);
    }
  }

  #endregion

  #region Reference Type Fields

  /// <summary>Base class for all <see cref="ReferenceField{T, F}"/> classes
  /// that are designed to cooperate with <see cref="Formatter"/>.</summary>
  /// <typeparam name="T">Reference type that the class will serialize/deserialize.</typeparam>
  public abstract class ReferenceField<T>: ReferenceField<T, Formatter>
    where T: class
  {
    protected ReferenceField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <summary>Gives access to <see cref="Formatter">Formatter's</see>
    /// internal <c>valueBuffer</c> field.</summary>
    protected byte[] ValueBuffer {
      get { return Fmt.valueBuffer; }
    }

    /// <summary>Gives access to <see cref="Formatter">Formatter's</see>
    /// internal <c>valueIndex</c> field.</summary>
    protected int ValueIndex {
      get { return Fmt.valueIndx; }
      set { Fmt.valueIndx = value; }
    }
  }

  /// <summary>Field representing <c>byte</c> arrays.</summary>
  public class BlobField: ReferenceField<byte[]>
  {
    public BlobField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(byte[] value) {
      BEConverter.ToBytes(value.Length, ValueBuffer, ref Fmt.valueIndx);
      System.Buffer.BlockCopy(value, 0, ValueBuffer, Fmt.valueIndx, value.Length);
      Fmt.valueIndx += value.Length;
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref byte[] instance) {
      int len = BEConverter.ToInt32(ValueBuffer, ref Fmt.valueIndx);
      if (instance == null || instance.Length != len)
        instance = new byte[len];
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeMembers"/>
    protected override void DeserializeMembers(byte[] value) {
      System.Buffer.BlockCopy(Fmt.valueBuffer, Fmt.valueIndx, value, 0, value.Length);
      Fmt.valueIndx += value.Length;
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      int len = BEConverter.ToInt32(ValueBuffer, ref Fmt.valueIndx);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing fixed size <c>byte</c> arrays.</summary>
  /// <remarks>Fixed size byte buffer field (as opposed to <see cref="BlobField"/>).</remarks>
  public class BinaryField: ReferenceField<byte[]>
  {
    private int size;

    public BinaryField(Formatter fmt, bool isDefault, int size) : base(fmt, isDefault) {
      this.size = size;
    }

    /// <summary>Size (fixed) of <c>byte</c> array to be serialized/deserialized.</summary>
    public int Size {
      get { return size; }
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(byte[] value) {
      int len = value.Length;
      if (len > size)
        len = size;
      System.Buffer.BlockCopy(value, 0, Fmt.valueBuffer, Fmt.valueIndx, len);
      int valIndx = Fmt.valueIndx + len;
      // for missing bytes pad with zeros
      for (; len < size; len++)
        Fmt.valueBuffer[valIndx++] = 0;
      Fmt.valueIndx = valIndx;
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref byte[] instance) {
      if (instance == null || instance.Length != size)
        instance = new byte[size];
      System.Buffer.BlockCopy(Fmt.valueBuffer, Fmt.valueIndx, instance, 0, size);
      Fmt.valueIndx += size;
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += size;
    }
  }

  /// <summary>Field representing Unicode <c>string</c> values.</summary>
  /// <remarks>Serializes string value in UTF-8 encoding, without null-terminator.
  /// If the string value itself contains separators (e.g. null-terminators) one
  /// can store multiple strings with it.</remarks>
  public class StringField: ReferenceField<String>
  {
    UTF8Encoding utf8;

    public StringField(Formatter fmt, bool isDefault) : base(fmt, isDefault) {
      utf8 = new UTF8Encoding();
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(string value) {
      int valIndx = Fmt.valueIndx + sizeof(uint);  // make space for length prefix
      int len = utf8.GetBytes(value, 0, value.Length, Fmt.valueBuffer, valIndx);
      // write length prefix
      valIndx = Fmt.valueIndx;
      BEConverter.ToBytes(len, Fmt.valueBuffer, ref valIndx); 
      Fmt.valueIndx = valIndx + len;
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref string instance) {
      int len = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      instance = utf8.GetString(Fmt.valueBuffer, Fmt.valueIndx, len);
      Fmt.valueIndx += len;
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      int len = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing <c>UInt16</c> arrays.</summary>
  [CLSCompliant(false)]
  public class UShortArrayField: ReferenceField<UInt16[]>
  {
    public UShortArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(ushort[] value) {
      int len = value.Length * sizeof(ushort);
      // write length prefix
      BEConverter.ToBytes(len, Fmt.valueBuffer, ref Fmt.valueIndx);
      BEConverter.ToBytes(value, 0, value.Length, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref ushort[] instance) {
      int count = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      count = count / sizeof(ushort);
      instance = new ushort[count];
      BEConverter.ToUInt16Array(instance, 0, count, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      int len = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      Fmt.valueIndx += len;
    }
  }

  [StructLayout(LayoutKind.Explicit)]
  internal struct UShortCharUnion
  {
    [FieldOffset(0)]
    public readonly UInt16[] UShortValue;

    [FieldOffset(0)]
    public readonly Char[] CharValue;

    public UShortCharUnion(UInt16[] value) {
      CharValue = null;  // keeps the compiler happy
      UShortValue = value;
    }

    public UShortCharUnion(Char[] value) {
      UShortValue = null;  // keeps the compiler happy
      CharValue = value;
    }
  }

  /// <summary>Field representing <c>Char</c> arrays.</summary>
  /// <remarks>Serializes each character as <c>UInt16</c>, that is, in its native
  /// .NET encoding. Can be used to serialize strings encoded as UTF-16.</remarks>
  public class CharArrayField: ReferenceField<Char[]>
  {
    public CharArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(char[] value) {
      int len = value.Length * sizeof(ushort);
      // write length prefix
      BEConverter.ToBytes(len, Fmt.valueBuffer, ref Fmt.valueIndx);
      UShortCharUnion union = new UShortCharUnion(value);
      BEConverter.ToBytes(union.UShortValue, 0, value.Length, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref char[] instance) {
      int count = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      count = count / sizeof(ushort);
      instance = new char[count];
      UShortCharUnion union = new UShortCharUnion(instance);
      BEConverter.ToUInt16Array(union.UShortValue, 0, count, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      int len = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing fixed size <c>Char</c> arrays.</summary>
  public class FixedCharArrayField: ReferenceField<Char[]>
  {
    private int size;

    // size in characters (not bytes!)
    public FixedCharArrayField(Formatter fmt, bool isDefault, int size) : base(fmt, isDefault) {
      this.size = size;
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(char[] value) {
      int count = value.Length;
      if (count > size)
        count = size;
      UShortCharUnion union = new UShortCharUnion(value);
      BEConverter.ToBytes(union.UShortValue, 0, count, Fmt.valueBuffer, ref Fmt.valueIndx);
      // for missing characters pad with two zero bytes (each)
      if (count < size) {
        int valIndx = Fmt.valueIndx;
        for (; count < size; count++) {
          Fmt.valueBuffer[valIndx++] = 0;
          Fmt.valueBuffer[valIndx++] = 0;
        }
        Fmt.valueIndx = valIndx;
      }
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref char[] instance) {
      if (instance == null || instance.Length != size)
        instance = new char[size];
      UShortCharUnion union = new UShortCharUnion(instance);
      BEConverter.ToUInt16Array(union.UShortValue, 0, size, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      Fmt.valueIndx += size * sizeof(ushort);
    }
  }

  /// <summary>Field representing <c>Int16</c> arrays.</summary>
  public class ShortArrayField: ReferenceField<Int16[]>
  {
    public ShortArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(short[] value) {
      int len = value.Length * sizeof(short);
      // write length prefix
      BEConverter.ToBytes(len, Fmt.valueBuffer, ref Fmt.valueIndx);
      BEConverter.ToBytes(value, 0, value.Length, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref short[] instance) {
      int count = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      count = count / sizeof(short);
      instance = new short[count];
      BEConverter.ToInt16Array(instance, 0, count, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      int len = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing a <c>UInt32</c> arrays.</summary>
  [CLSCompliant(false)]
  public class UIntArrayField: ReferenceField<UInt32[]>
  {
    public UIntArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(uint[] value) {
      int len = value.Length * sizeof(uint);
      // write length prefix
      BEConverter.ToBytes(len, Fmt.valueBuffer, ref Fmt.valueIndx);
      BEConverter.ToBytes(value, 0, value.Length, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref uint[] instance) {
      int count = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      count = count / sizeof(uint);
      instance = new uint[count];
      BEConverter.ToUInt32Array(instance, 0, count, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      int len = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing a <c>Int32</c> arrays.</summary>
  public class IntArrayField: ReferenceField<Int32[]>
  {
    public IntArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.SerializeValue"/>
    protected override void SerializeValue(int[] value) {
      int len = value.Length * sizeof(int);
      // write length prefix
      BEConverter.ToBytes(len, Fmt.valueBuffer, ref Fmt.valueIndx);
      BEConverter.ToBytes(value, 0, value.Length, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.ReferenceField{T, F}.DeserializeInstance"/>
    protected override void DeserializeInstance(ref int[] instance) {
      int count = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      count = count / sizeof(int);
      instance = new int[count];
      BEConverter.ToInt32Array(instance, 0, count, Fmt.valueBuffer, ref Fmt.valueIndx);
    }

    /// <inheritdoc cref="Kds.Serialization.Field{T, F}.SkipValue"/>
    protected override void SkipValue() {
      int len = BEConverter.ToInt32(Fmt.valueBuffer, ref Fmt.valueIndx);
      Fmt.valueIndx += len;
    }
  }

  #endregion

  /// <summary><see cref="Formatter"/> subclass with default fields pre-registered
  /// for the basic types.</summary>
  public class BEFormatter: Formatter
  {
    private void RegisterBEConverters() {
      new ByteField(this, true);
      new SByteField(this, true);
      new BoolField(this, true);
      new UShortField(this, true);
      new ShortField(this, true);
      new CharField(this, true);
      new UIntField(this, true);
      new IntField(this, true);
      new ULongField(this, true);
      new LongField(this, true);
      new SingleField(this, true);
      new DoubleField(this, true);
      new DecimalField(this, true);
      new DateTimeField(this, true);
      // frequently used reference fields
      new BlobField(this, true);
      new StringField(this, true);
      new UShortArrayField(this, true);
      new ShortArrayField(this, true);
      new CharArrayField(this, true);
      new UIntArrayField(this, true);
      new IntArrayField(this, true);
    }

    public BEFormatter(): base() {
      RegisterBEConverters();
    }
  }
}