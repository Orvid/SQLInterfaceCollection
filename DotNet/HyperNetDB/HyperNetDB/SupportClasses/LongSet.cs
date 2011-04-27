using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Data;
namespace System.Collections
{
	/// <summary>
	/// LongSet.
	/// </summary>
	public class LongSet : IEnumerable//, ICloneable
	{
		private long[] keys = null;
		private int _size;
        /// <summary>
        /// Ctor
        /// </summary>
		public LongSet( )
		{
			keys = new long[16];
			_size=0;
		}
        /// <summary>
        /// Long set from another long set
        /// </summary>
        /// <param name="ls"></param>
		public LongSet( LongSet ls )
		{
			keys = new long[ls.keys.Length];
			ls.keys.CopyTo(keys,0);
			_size=ls._size;
		}
        /// <summary>
        /// Index of key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
		public int IndexOfKey(long key)
		{
			int i = Array.BinarySearch(keys, 0, _size, key);
			if (i < 0)
			{
				return -1;
			}
			else
			{
				return i;
			}
		}
        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
		public LongSet Clone()
		{
			LongSet ls = new LongSet(this);
			return ls;
		}
        /// <summary>
        /// Count
        /// </summary>
		public int Count
		{
			get
			{
				return _size;
			}
		}
        /// <summary>
        /// Exist key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
		public bool Exist( long key )
		{
			int i = IndexOfKey(key);
			if (i >= 0)
			{
				return true;//values[i];
			}
			else
			{
				return false;//null;
			}

		}
        /// <summary>
        /// Gets one element
        /// </summary>
        /// <returns></returns>
		public long GetOne()
		{
			if(_size==0) throw new Exception();
			return keys[0];
		}
        /// <summary>
        /// Contains
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
		public bool Contains( long key )
		{
			return Exist(key);
		}
        /// <summary>
        /// Exists key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
		public bool ContainsKey( long key )
		{
			return Exist(key);
		}
        /// <summary>
        /// Adds a key if not exist
        /// </summary>
        /// <param name="key"></param>
		public void Add(long key)
		{
			int i = Array.BinarySearch(keys, 0, _size, key);
			if (i >= 0)
			{
				return;
			}
			Insert(~i, key);
		}
        /// <summary>
        /// [INTERNAL]
        /// </summary>
        /// <param name="index"></param>
        /// <param name="key"></param>
		private void Insert(int index, long key)
		{
			if (_size == (int)keys.Length)
			{
				EnsureCapacity(_size + 1);
			}
			if (index < _size)
			{
				Array.Copy(keys, index, keys, index + 1, _size - index);
			}
			keys[index] = key;
			_size++;
		}
		private void EnsureCapacity(int min)
		{
			int i = ((int)keys.Length != 0) ? ((int)keys.Length * 2) : 16;
			if (i < min)
			{
				i = min;
			}
			Capacity = i;
		}
        /// <summary>
        /// Key capacity
        /// </summary>
		public int Capacity
		{
			get
			{
				return (int)keys.Length;
			}
			set
			{
				if (value != (int)keys.Length)
				{
					if (value < _size)
					{
						throw new Exception();//ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
					}
					if (value > 0)
					{
						long[] locals1 = new long[value];
						
						if (_size > 0)
						{
							Array.Copy(keys, 0, locals1, 0, _size);
						
						}
						keys = locals1;
				
						return;
					}
					keys = new long[16];
		
				}
			}
		}
        /// <summary>
        /// Unions 2 sets
        /// </summary>
        /// <param name="ls"></param>
        /// <returns></returns>
		public LongSet Union( LongSet ls )
		{
			if(this._size>ls._size)
				return ls.Union(this);
			else
			{
				LongSet rv = new LongSet(ls);
				foreach(long v in keys)
				{
					if(!ls.Exist(v))
						rv.Add(v);
				}
				return rv;
			}
		}
        /// <summary>
        /// Substract keys
        /// </summary>
        /// <param name="ls"></param>
        /// <returns></returns>
		public LongSet Minus( LongSet ls )
		{
			LongSet rv = new LongSet(this);
			foreach(long v in keys)
			{
				if(ls.Exist(v))
					rv.Remove(v);
			}
			return rv;
		}
        /// <summary>
        /// Intersects sets
        /// </summary>
        /// <param name="ls"></param>
        /// <returns></returns>
		public LongSet Intersect( LongSet ls )
		{
			if(this._size>ls._size)
				return ls.Union(this);
			else
			{
				LongSet rv = new LongSet();
				foreach(long v in keys)
				{
					if(ls.Exist(v))
						rv.Add(v);
				}
				return rv;
			}
		}
        /// <summary>
        /// Removes a key at position
        /// </summary>
        /// <param name="index"></param>
		public void RemoveAt(int index)
		{
			if (index < 0 || index >= _size)
			{
				throw new Exception();//ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			_size--;
			if (index < _size)
			{
				Array.Copy(keys, index + 1, keys, index, _size - index);
				//Array.Copy(values, index + 1, values, index, _size - index);
			}
			//keys[_size] = null;
			//values[_size] = null;
			//version++;
		}
        /// <summary>
        /// Removes a key
        /// </summary>
        /// <param name="key"></param>
		public void Remove(long key)
		{
			int i = IndexOfKey(key);
			if (i >= 0)
			{
				RemoveAt(i);
			}
		}
		#region IEnumerable Members
        /// <summary>
        /// Enumerator
        /// </summary>
        /// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			return new ArrayListEnumerator(keys,0,_size);
			
		}
		private class ArrayListEnumerator: IEnumerator, ICloneable
		{
			private long[] list;

			private int index;

			private int endIndex;



			private object currentElement;

			private int startIndex;


			public virtual object Current
			{
				get
				{
					object local = currentElement;
					if (local != list)
					{
						return local;
					}
					return null;
//					if (index <= startIndex)
//					{
//						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
//					}
//					else
//					{
//						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
//					}
				}
			}

			internal ArrayListEnumerator(long[] list, int index, int count)
			{
				this.list = list;
				this.index = index;
				endIndex = index + count;

				startIndex = index;
				currentElement = list;
			}

			public virtual object Clone()
			{
				return base.MemberwiseClone();
			}

			public virtual bool MoveNext()
			{

				if (index < endIndex)
				{
					currentElement = list[index];
					index++;
					return true;
				}
				index = endIndex + 1;
				currentElement = list;
				return false;
			}

			public virtual void Reset()
			{

				index = startIndex;
				currentElement = list;
			}
		}

		#endregion
//		#region ICloneable Members
//
//		object System.ICloneable.Clone()
//		{
//			return this.c
//		}
//
//		#endregion
	}
}
