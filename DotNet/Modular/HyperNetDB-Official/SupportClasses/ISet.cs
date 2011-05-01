/* Copyright (c) 2002 by Insight Enterprise Systems, Inc., and by Jason Smith. */ 
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections
{
	/// <summary>
	/// <p>A collection that contains no duplicate elements.  This interface models the mathematical
	/// <c>Set</c> abstraction.</p>
	/// 
	/// <p>You can use this interface to create set collections classes that do not derive from 
	/// <c>Set</c>, but this is not recommended.  <c>Set</c> provides convenient base functionality
	/// that will save you a lot of work.  You should normally work with <c>Set</c> instances, not
	/// <c>ISet</c> interfaces.  However, when you have an object that inherits from a base object
	/// not compatible with <c>Set</c>, this interface can be mighty handy.</p>
	/// 
	/// <p>The overloaded set operators for "union," "minus," "exclusive-or," and "intersect" ('+', '-', '^', and '*') 
	/// do not work if you cast your <c>Set</c> instance to the <c>ISet</c> interface.  In 
	/// order for these to work, you must be using one of the types derived from <c>Set</c>.
	/// Otherwise, use the static methods provided on <c>Set</c> to get the same functionality:
	/// <c>Union()</c>, <c>Minus()</c>, <c>ExclusiveOr()</c>, and <c>Intersect()</c>.</p>
	/// </summary>
	public interface ISet : ICollection
	{
		/// <summary>
		/// Adds the specified element to this set if it is not already present.
		/// </summary>
		/// <param name="o">The object to add to the set.</param>
		/// <returns><c>true</c> is the object was added, <c>false</c> if it was already present.</returns>
		bool Add(object o);

		/// <summary>
		/// Adds all the elements in the specified collection to the set if they are not already present.
		/// </summary>
		/// <param name="c">A collection of objects to add to the set.</param>
		/// <returns><c>true</c> is the set changed as a result of this operation, <c>false</c> if not.</returns>
		bool AddAll(ICollection c);

		/// <summary>
		/// Removes all objects from the set.
		/// </summary>
		void Clear();

		/// <summary>
		/// Returns <c>true</c> if this set contains the specified element.
		/// </summary>
		/// <param name="o">The element to look for.</param>
		/// <returns><c>true</c> if this set contains the specified element, <c>false</c> otherwise.</returns>
		bool Contains(object o);

		/// <summary>
		/// Returns <c>true</c> if the set contains all the elements in the specified collection.
		/// </summary>
		/// <param name="c">A collection of objects.</param>
		/// <returns><c>true</c> if the set contains all the elements in the specified collection, <c>false</c> otherwise.</returns>
		bool ContainsAll(ICollection c);

		/// <summary>
		/// Returns <c>true</c> if this set contains no elements.
		/// </summary>
		bool IsEmpty {get;}

		/// <summary>
		/// Removes the specified element from the set.
		/// </summary>
		/// <param name="o">The element to be removed.</param>
		/// <returns><c>true</c> if the set contained the specified element, <c>false</c> otherwise.</returns>
		bool Remove(object o);

		/// <summary>
		/// Remove all the specified elements from this set, if they exist in this set.
		/// </summary>
		/// <param name="c">A collection of elements to remove.</param>
		/// <returns><c>true</c> if the set was modified as a result of this operation.</returns>
		bool RemoveAll(ICollection c);

		/// <summary>
		/// Retains only the elements in this set that are contained in the specified collection.
		/// </summary>
		/// <param name="c">Collection that defines the set of elements to be retained.</param>
		/// <returns><c>true</c> if this set changed as a result of this operation.</returns>
		bool RetainAll(ICollection c);
	}
}
