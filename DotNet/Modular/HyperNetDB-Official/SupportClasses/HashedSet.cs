/* Copyright (c) 2002 by Insight Enterprise Systems, Inc., and by Jason Smith. */ 
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections
{
	/// <summary>
	/// Implements a <c>Set</c> based on a hash table.  This will give the best lookup, add, and remove
	/// performance for very large data-sets, but iteration will occur in no particular order.
	/// </summary>
	public class HashedSet : DictionarySet
	{
		/// <summary>
		/// Creates a new set instance based on a hash table.
		/// </summary>
		public HashedSet()
		{
			m_InternalSet = new Hashtable();
		}

		/// <summary>
		/// Creates a new set instance based on a hash table and
		/// initializes it based on a collection of elements.
		/// </summary>
		/// <param name="initialValues">A collection of elements that defines the initial set contents.</param>
		public HashedSet(ICollection initialValues) : this()
		{
			this.AddAll(initialValues);
		}
	}
}
