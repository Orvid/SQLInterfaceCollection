/* Copyright (c) 2002 by Insight Enterprise Systems, Inc., and by Jason Smith. */ 
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections
{
	/// <summary>
	/// <p>Implements an immutable (read-only) <c>Set</c> wrapper.</p>
	/// <p>Although this is advertised as immutable, it really isn't.  Anyone with access to the
	/// <c>basisSet</c> can still change the data-set.  So <c>GetHashCode()</c> is not implemented
	/// for this <c>Set</c>, as is the case for all <c>Set</c> implementations in this library.
	/// This design decision was based on the efficiency of not having to <c>Clone()</c> the 
	/// <c>basisSet</c> every time you wrap a mutable <c>Set</c>.</p>
	/// </summary>
	public class ImmutableSet : Set
	{
		private const string ERROR_MESSAGE = "Object is immutable.";
		private Set _basisSet;

		/// <summary>
		/// Constructs an immutable (read-only) <c>Set</c> wrapper.
		/// </summary>
		/// <param name="basisSet">The <c>Set</c> that is wrapped.</param>
		public ImmutableSet(Set basisSet)
		{
			_basisSet = basisSet;
		}

		/// <summary>
		/// Adds the specified element to this set if it is not already present.
		/// </summary>
		/// <param name="o">The object to add to the set.</param>
		/// <returns><c>true</c> is the object was added, <c>false</c> if it was already present.</returns>
		public sealed override bool Add(object o)
		{
			throw new NotSupportedException(ERROR_MESSAGE);
		}

		/// <summary>
		/// Adds all the elements in the specified collection to the set if they are not already present.
		/// </summary>
		/// <param name="c">A collection of objects to add to the set.</param>
		/// <returns><c>true</c> is the set changed as a result of this operation, <c>false</c> if not.</returns>
		public sealed override bool AddAll(ICollection c)
		{
			throw new NotSupportedException(ERROR_MESSAGE);
		}

		/// <summary>
		/// Removes all objects from the set.
		/// </summary>
		public sealed override void Clear()
		{
			throw new NotSupportedException(ERROR_MESSAGE);
		}

		/// <summary>
		/// Returns <c>true</c> if this set contains the specified element.
		/// </summary>
		/// <param name="o">The element to look for.</param>
		/// <returns><c>true</c> if this set contains the specified element, <c>false</c> otherwise.</returns>
		public sealed override bool Contains(object o)
		{
			return _basisSet.Contains(o);
		}

		/// <summary>
		/// Returns <c>true</c> if the set contains all the elements in the specified collection.
		/// </summary>
		/// <param name="c">A collection of objects.</param>
		/// <returns><c>true</c> if the set contains all the elements in the specified collection, <c>false</c> otherwise.</returns>
		public sealed override bool ContainsAll(ICollection c)
		{
			return _basisSet.ContainsAll(c);
		}

		/// <summary>
		/// Returns <c>true</c> if this set contains no elements.
		/// </summary>
		public sealed override bool IsEmpty
		{
			get{return _basisSet.IsEmpty;}
		}


		/// <summary>
		/// Removes the specified element from the set.
		/// </summary>
		/// <param name="o">The element to be removed.</param>
		/// <returns><c>true</c> if the set contained the specified element, <c>false</c> otherwise.</returns>
		public sealed override bool Remove(object o)
		{
			throw new NotSupportedException(ERROR_MESSAGE);
		}

		/// <summary>
		/// Remove all the specified elements from this set, if they exist in this set.
		/// </summary>
		/// <param name="c">A collection of elements to remove.</param>
		/// <returns><c>true</c> if the set was modified as a result of this operation.</returns>
		public sealed override bool RemoveAll(ICollection c)
		{
			throw new NotSupportedException(ERROR_MESSAGE);
		}

		/// <summary>
		/// Retains only the elements in this set that are contained in the specified collection.
		/// </summary>
		/// <param name="c">Collection that defines the set of elements to be retained.</param>
		/// <returns><c>true</c> if this set changed as a result of this operation.</returns>
		public sealed override bool RetainAll(ICollection c)
		{
			throw new NotSupportedException(ERROR_MESSAGE);
		}

		/// <summary>
		/// Copies the elements in the <c>Set</c> to an array.  The type of array needs
		/// to be compatible with the objects in the <c>Set</c>, obviously.
		/// </summary>
		/// <param name="array">An array that will be the target of the copy operation.</param>
		/// <param name="index">The zero-based index where copying will start.</param>
		public sealed override void CopyTo(Array array, int index)
		{
			_basisSet.CopyTo(array, index);
		}

		/// <summary>
		/// The number of elements contained in this collection.
		/// </summary>
		public sealed override int Count
		{
			get{return _basisSet.Count;}
		}

		/// <summary>
		/// Returns an object that can be used to synchronize use of the <c>Set</c> across threads.
		/// </summary>
		public sealed override bool IsSynchronized
		{
			get{return _basisSet.IsSynchronized;}
		}

		/// <summary>
		/// Returns an object that can be used to synchronize the <c>Set</c> between threads.
		/// </summary>
		public sealed override object SyncRoot
		{
			get{return _basisSet.SyncRoot;}
		}

		/// <summary>
		/// Gets an enumerator for the elements in the <c>Set</c>.
		/// </summary>
		/// <returns>An <c>IEnumerator</c> over the elements in the <c>Set</c>.</returns>
		public sealed override IEnumerator GetEnumerator()
		{
			return _basisSet.GetEnumerator();
		}
	}
}
