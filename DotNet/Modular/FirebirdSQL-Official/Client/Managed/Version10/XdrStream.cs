/*
 *	Firebird ADO.NET Data provider for .NET and Mono 
 * 
 *	   The contents of this file are subject to the Initial 
 *	   Developer's Public License Version 1.0 (the "License"); 
 *	   you may not use this file except in compliance with the 
 *	   License. You may obtain a copy of the License at 
 *	   http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *	   Software distributed under the License is distributed on 
 *	   an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either 
 *	   express or implied. See the License for the specific 
 *	   language governing rights and limitations under the License.
 * 
 *	Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *	All Rights Reserved.
 *   
 *  Contributors:
 *    Jiri Cincura (jiri@cincura.net)
 */

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Globalization;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version10
{
	internal class XdrStream : Stream
	{
		#region � Static Fields �

		private static byte[] fill;
		private static byte[] pad;

		#endregion

		#region � Static Properties �

		internal static byte[] Fill
		{
			get
			{
				if (fill == null)
				{
					fill = new byte[32767];
					for (int i = 0; i < fill.Length; i++)
					{
						fill[i] = 32;
					}
				}

				return fill;
			}
		}

		private static byte[] Pad
		{
			get
			{
				if (pad == null)
				{
					pad = new byte[] { 0, 0, 0, 0 };
				}

				return pad;
			}
		}

		#endregion

		#region � Fields �

		private Charset charset;
		private Stream innerStream;
		private int operation;

		#endregion

		#region � Stream Properties �

		public override bool CanWrite
		{
			get { return this.innerStream.CanWrite; }
		}

		public override bool CanRead
		{
			get { return this.innerStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return this.innerStream.CanSeek; }
		}

		public override long Position
		{
			get { return this.innerStream.Position; }
			set { this.innerStream.Position = value; }
		}

		public override long Length
		{
			get { return this.innerStream.Length; }
		}

		#endregion

		#region � Constructors �

		public XdrStream()
			: this(Charset.DefaultCharset)
		{ }

		public XdrStream(Charset charset)
			: this(new MemoryStream(), charset)
		{ }

		public XdrStream(byte[] buffer, Charset charset)
			: this(new MemoryStream(buffer), charset)
		{ }

		public XdrStream(Stream innerStream, Charset charset)
			: base()
		{
			this.innerStream = innerStream;
			this.charset = charset;
			this.operation = -1;

			GC.SuppressFinalize(innerStream);
		}

		#endregion

		#region � Stream methods �

		public override void Close()
		{
			try
			{
				if (this.innerStream != null)
				{
					this.innerStream.Close();
				}
			}
			catch
			{
			}
			finally
			{
				this.charset = null;
				this.innerStream = null;
			}
		}

		public override void Flush()
		{
			this.CheckDisposed();

			this.innerStream.Flush();
		}

		public override void SetLength(long length)
		{
			this.CheckDisposed();

			this.innerStream.SetLength(length);
		}

		public override long Seek(long offset, System.IO.SeekOrigin loc)
		{
			this.CheckDisposed();

			return this.innerStream.Seek(offset, loc);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			this.CheckDisposed();

			if (this.CanRead)
			{
				return this.innerStream.Read(buffer, offset, count);
			}

			throw new InvalidOperationException("Read operations are not allowed by this stream");
		}

		public override void WriteByte(byte value)
		{
			this.CheckDisposed();

			this.innerStream.WriteByte(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.CheckDisposed();

			if (this.CanWrite)
			{
				this.innerStream.Write(buffer, offset, count);
			}
			else
			{
				throw new InvalidOperationException("Write operations are not allowed by this stream");
			}
		}

		public byte[] ToArray()
		{
			this.CheckDisposed();

			if (this.innerStream is MemoryStream)
			{
				return ((MemoryStream)this.innerStream).ToArray();
			}

			throw new InvalidOperationException();
		}

		#endregion

		#region � Operation Identification Methods �

		public virtual int ReadOperation()
		{
			int op = (this.operation >= 0) ? this.operation : this.ReadNextOperation();

			this.operation = -1;

			return op;
		}

		public virtual int ReadNextOperation()
		{
			do
			{
				/* loop	as long	as we are receiving	dummy packets, just
				 * throwing	them away--note	that if	we are a server	we won't
				 * be receiving	them, but it is	better to check	for	them at
				 * this	level rather than try to catch them	in all places where
				 * this	routine	is called 
				 */
				this.operation = this.ReadInt32();
			} while (this.operation == IscCodes.op_dummy);

			return this.operation;
		}

		#endregion

		#region � XDR Read Methods �

		public byte[] ReadBytes(int count)
		{
			byte[] buffer = new byte[count];

			int toRead = count;
			int currentlyRead = -1;
			while (toRead > 0 && currentlyRead != 0)
			{
				toRead -= (currentlyRead = this.Read(buffer, count - toRead, toRead));
			}

			return buffer;
		}

		public byte[] ReadOpaque(int length)
		{
			byte[] buffer = this.ReadBytes(length);

			int padLength = ((4 - length) & 3);
			if (padLength > 0)
			{
				this.Read(Pad, 0, padLength);
			}

			return buffer;
		}

		public byte[] ReadBuffer()
		{
			return this.ReadOpaque(this.ReadInt32());
		}

		public string ReadString()
		{
			return this.ReadString(this.charset);
		}

		public string ReadString(int length)
		{
			return this.ReadString(this.charset, length);
		}

		public string ReadString(Charset charset)
		{
			return this.ReadString(charset, this.ReadInt32());
		}

		public string ReadString(Charset charset, int length)
		{
			byte[] buffer = this.ReadOpaque(length);

			return charset.GetString(buffer, 0, buffer.Length);
		}

		public short ReadInt16()
		{
			return Convert.ToInt16(this.ReadInt32());
		}

		public int ReadInt32()
		{
			return IPAddress.HostToNetworkOrder(BitConverter.ToInt32(this.ReadBytes(4), 0));
		}

		public long ReadInt64()
		{
			return IPAddress.HostToNetworkOrder(BitConverter.ToInt64(this.ReadBytes(8), 0));
		}

		public Guid ReadGuid(int length)
		{
			return new Guid(this.ReadOpaque(length));
		}

		public float ReadSingle()
		{
			return BitConverter.ToSingle(BitConverter.GetBytes(this.ReadInt32()), 0);
		}

		public double ReadDouble()
		{
			return BitConverter.ToDouble(BitConverter.GetBytes(this.ReadInt64()), 0);
		}

		public DateTime ReadDateTime()
		{
			DateTime date = this.ReadDate();
			TimeSpan time = this.ReadTime();

			return new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
		}

		public DateTime ReadDate()
		{
			return TypeDecoder.DecodeDate(this.ReadInt32());
		}

		public TimeSpan ReadTime()
		{
			return TypeDecoder.DecodeTime(this.ReadInt32());
		}

		public decimal ReadDecimal(int type, int scale)
		{
			decimal value = 0;

			switch (type & ~1)
			{
				case IscCodes.SQL_SHORT:
					value = TypeDecoder.DecodeDecimal(this.ReadInt16(), scale, type);
					break;

				case IscCodes.SQL_LONG:
					value = TypeDecoder.DecodeDecimal(this.ReadInt32(), scale, type);
					break;

				case IscCodes.SQL_QUAD:
				case IscCodes.SQL_INT64:
					value = TypeDecoder.DecodeDecimal(this.ReadInt64(), scale, type);
					break;

				case IscCodes.SQL_DOUBLE:
				case IscCodes.SQL_D_FLOAT:
					value = Convert.ToDecimal(this.ReadDouble());
					break;
			}

			return value;
		}

		public object ReadValue(DbField field)
		{
			object fieldValue = null;
			Charset innerCharset = (this.charset.Name != "NONE") ? this.charset : field.Charset;

			switch (field.DbDataType)
			{
				case DbDataType.Char:
					if (field.Charset.IsOctetsCharset)
					{
						fieldValue = this.ReadOpaque(field.Length);
					}
					else
					{
						string s = this.ReadString(innerCharset, field.Length);

						if ((field.Length % field.Charset.BytesPerCharacter) == 0 &&
							s.Length > field.CharCount)
						{
							fieldValue = s.Substring(0, field.CharCount);
						}
						else
						{
							fieldValue = s;
						}
					}
					break;

				case DbDataType.VarChar:
					if (field.Charset.IsOctetsCharset)
					{
						fieldValue = this.ReadBuffer();
					}
					else
					{
						fieldValue = this.ReadString(innerCharset);
					}
					break;

				case DbDataType.SmallInt:
					fieldValue = this.ReadInt16();
					break;

				case DbDataType.Integer:
					fieldValue = this.ReadInt32();
					break;

				case DbDataType.Array:
				case DbDataType.Binary:
				case DbDataType.Text:
				case DbDataType.BigInt:
					fieldValue = this.ReadInt64();
					break;

				case DbDataType.Decimal:
				case DbDataType.Numeric:
					fieldValue = this.ReadDecimal(field.DataType, field.NumericScale);
					break;

				case DbDataType.Float:
					fieldValue = this.ReadSingle();
					break;

				case DbDataType.Guid:
					fieldValue = this.ReadGuid(field.Length);
					break;

				case DbDataType.Double:
					fieldValue = this.ReadDouble();
					break;

				case DbDataType.Date:
					fieldValue = this.ReadDate();
					break;

				case DbDataType.Time:
					fieldValue = this.ReadTime();
					break;

				case DbDataType.TimeStamp:
					fieldValue = this.ReadDateTime();
					break;
			}

			int sqlInd = this.ReadInt32();

			if (sqlInd == 0)
			{
				return fieldValue;
			}
			else if (sqlInd == -1)
			{
				return null;
			}
			else
			{
				throw new IscException("invalid sqlind value: " + sqlInd);
			}
		}

		#endregion

		#region � XDR Write Methods �

		public void WriteOpaque(byte[] buffer)
		{
			this.WriteOpaque(buffer, buffer.Length);
		}

		public void WriteOpaque(byte[] buffer, int length)
		{
			if (buffer != null && length > 0)
			{
				this.Write(buffer, 0, buffer.Length);
				this.Write(Fill, 0, length - buffer.Length);
				this.Write(Pad, 0, ((4 - length) & 3));
			}
		}

		public void WriteBuffer(byte[] buffer)
		{
			this.WriteBuffer(buffer, buffer == null ? 0 : buffer.Length);
		}

		public void WriteBuffer(byte[] buffer, int length)
		{
			this.Write(length);

			if (buffer != null && length > 0)
			{
				this.Write(buffer, 0, length);
				this.Write(Pad, 0, ((4 - length) & 3));
			}
		}

		public void WriteBlobBuffer(byte[] buffer)
		{
			int length = buffer.Length;	// 2 for short for buffer length

			if (length > short.MaxValue)
			{
				throw (new IOException()); //Need a	value???
			}

			this.Write(length + 2);
			this.Write(length + 2);	//bizarre but true!	three copies of	the	length
			this.WriteByte((byte)((length >> 0) & 0xff));
			this.WriteByte((byte)((length >> 8) & 0xff));
			this.Write(buffer, 0, length);

			this.Write(Pad, 0, ((4 - length + 2) & 3));
		}

		public void WriteTyped(int type, byte[] buffer)
		{
			int length;

			if (buffer == null)
			{
				this.Write(1);
				this.WriteByte((byte)type);
				length = 1;
			}
			else
			{
				length = buffer.Length + 1;
				this.Write(length);
				this.WriteByte((byte)type);
				this.Write(buffer, 0, buffer.Length);
			}
			this.Write(Pad, 0, ((4 - length) & 3));
		}

		public void Write(string value)
		{
			byte[] buffer = this.charset.GetBytes(value);

			this.WriteBuffer(buffer, buffer.Length);
		}

		public void Write(short value)
		{
			this.Write((int)value);
		}

		public void Write(int value)
		{
			this.Write(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value)), 0, 4);
		}

		public void Write(long value)
		{
			this.Write(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value)), 0, 8);
		}

		public void Write(float value)
		{
			byte[] buffer = BitConverter.GetBytes(value);

			this.Write(BitConverter.ToInt32(buffer, 0));
		}

		public void Write(double value)
		{
			byte[] buffer = BitConverter.GetBytes(value);

			this.Write(BitConverter.ToInt64(buffer, 0));
		}

		public void Write(decimal value, int type, int scale)
		{
			object numeric = TypeEncoder.EncodeDecimal(value, scale, type);

			switch (type & ~1)
			{
				case IscCodes.SQL_SHORT:
					this.Write((short)numeric);
					break;

				case IscCodes.SQL_LONG:
					this.Write((int)numeric);
					break;

				case IscCodes.SQL_QUAD:
				case IscCodes.SQL_INT64:
					this.Write((long)numeric);
					break;

				case IscCodes.SQL_DOUBLE:
				case IscCodes.SQL_D_FLOAT:
					this.Write((double)value);
					break;
			}
		}

		public void Write(bool value)
		{
			this.Write((short)(value ? 1 : 0));
		}

		public void Write(DateTime value)
		{
			this.WriteDate(value);
			this.WriteTime(TypeHelper.DateTimeToTimeSpan(value));
		}

		public void WriteDate(DateTime value)
		{
			this.Write(TypeEncoder.EncodeDate(Convert.ToDateTime(value)));
		}

		public void WriteTime(TimeSpan value)
		{
			this.Write(TypeEncoder.EncodeTime(value));
		}

		public void Write(Descriptor descriptor)
		{
			for (int i = 0; i < descriptor.Count; i++)
			{
				this.Write(descriptor[i]);
			}
		}

		public void Write(DbField param)
		{
			try
			{
				if (param.DbDataType != DbDataType.Null)
				{
					param.FixNull();

					switch (param.DbDataType)
					{
						case DbDataType.Char:
							if (param.Charset.IsOctetsCharset)
							{
								this.WriteOpaque(param.DbValue.GetBinary(), param.Length);
							}
							else
							{
								string svalue = param.DbValue.GetString();

								if ((param.Length % param.Charset.BytesPerCharacter) == 0 &&
									svalue.Length > param.CharCount)
								{
									throw new IscException(new[] { IscCodes.isc_arith_except, IscCodes.isc_string_truncation });
								}

								this.WriteOpaque(param.Charset.GetBytes(svalue), param.Length);
							}
							break;

						case DbDataType.VarChar:
							if (param.Charset.IsOctetsCharset)
							{
								this.WriteOpaque(param.DbValue.GetBinary(), param.Length);
							}
							else
							{
								string svalue = param.DbValue.GetString();

								if ((param.Length % param.Charset.BytesPerCharacter) == 0 &&
									svalue.Length > param.CharCount)
								{
									throw new IscException(new[] { IscCodes.isc_arith_except, IscCodes.isc_string_truncation });
								}

								byte[] data = param.Charset.GetBytes(svalue);

								this.WriteBuffer(data, data.Length);
							}
							break;

						case DbDataType.SmallInt:
							this.Write(param.DbValue.GetInt16());
							break;

						case DbDataType.Integer:
							this.Write(param.DbValue.GetInt32());
							break;

						case DbDataType.BigInt:
						case DbDataType.Array:
						case DbDataType.Binary:
						case DbDataType.Text:
							this.Write(param.DbValue.GetInt64());
							break;

						case DbDataType.Decimal:
						case DbDataType.Numeric:
							this.Write(
								param.DbValue.GetDecimal(),
								param.DataType,
								param.NumericScale);
							break;

						case DbDataType.Float:
							this.Write(param.DbValue.GetFloat());
							break;

						case DbDataType.Guid:
							this.WriteOpaque(param.DbValue.GetGuid().ToByteArray());
							break;

						case DbDataType.Double:
							this.Write(param.DbValue.GetDouble());
							break;

						case DbDataType.Date:
							this.Write(param.DbValue.GetDate());
							break;

						case DbDataType.Time:
							this.Write(param.DbValue.GetTime());
							break;

						case DbDataType.TimeStamp:
							this.Write(param.DbValue.GetDate());
							this.Write(param.DbValue.GetTime());
							break;

						case DbDataType.Boolean:
							this.Write(Convert.ToBoolean(param.Value));
							break;

						default:
							throw new IscException("Unknown sql data type: " + param.DataType);
					}
				}

				this.Write(param.NullFlag);
			}
			catch (IOException)
			{
				throw new IscException(IscCodes.isc_net_write_err);
			}
		}

		#endregion

		#region � Private Methods �

		private void CheckDisposed()
		{
			if (this.innerStream == null)
			{
				throw new ObjectDisposedException("The XdrStream is closed.");
			}
		}

		#endregion
	}
}
