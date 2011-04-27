/* Copyright (c) 2002 by Insight Enterprise Systems, Inc., and by Jason Smith. */ 
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections
{
	/// <summary>
	/// Implements a <c>Set</c> based on a list.  Performance is much better for very small lists 
	/// than either <c>HashedSet</c> or <c>SortedSet</c>.  However, performance degrades rapidly as 
	/// the data-set gets bigger.  Use a <c>HybridSet</c> instead if you are not sure your data-set
	/// will always remain very small.  Iteration produces elements in the order they were added.
	/// </summary>
	public class ListSet : DictionarySet
	{
		/// <summary>
		/// Creates a new set instance based on a list.
		/// </summary>
		public ListSet()
		{
			m_InternalSet = new ListDictionary();
		}

		/// <summary>
		/// Creates a new set instance based on a list and
		/// initializes it based on a collection of elements.
		/// </summary>
		/// <param name="initialValues">A collection of elements that defines the initial set contents.</param>
		public ListSet(ICollection initialValues) : this()
		{
			this.AddAll(initialValues);
		}
	}
}
