/* Copyright (c) 2002 by Insight Enterprise Systems, Inc., and by Jason Smith. */ 
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections
{
	/// <summary>
	/// <p><c>DictionarySet</c> is an abstract class that supports the creation of new <c>Set</c>
	/// types where the underlying data store is an <c>IDictionary</c> instance.</p> 
	///  
	/// <p>You can use any object that implements the <c>IDictionary</c> interface to hold set data.
	/// You can define your own, or you can use one of the objects provided in the Framework.   
	/// The type of <c>IDictionary</c> you choose will affect both the performance and the behavior
	/// of the <c>Set</c> using it. </p>
	/// 
	/// <p>This object overrides the <c>Equals()</c> object method, but not the <c>GetHashCode()</c>, because 
	/// the <c>DictionarySet</c> class is mutable.  Therefore, it is not safe to use as a key value in a hash table.</p>
	/// 
	/// <p>To make a <c>Set</c> typed based on your own <c>IDictionary</c>, just inherit and add the 
	/// constructors.  You can use the following code as a
	/// template for this, or take a look at one of the derived <c>Set</c> classes:</p>
	/// <code>
	///public class MySet : Set
	///{
	///    public MySet()
	///    {
	///        _set = new MyCustomDictionaryType();
	///    }
	///    public MySet(ICollection initialValues) : this()
	///    {
	///        this.AddAll(initialValues);
	///    }
	///}
	///</code>
	/// <p>It is important that at least one of your constructors takes an <c>ICollection</c> or 
	/// an <c>ISet</c> as an argument.  The base <c>Clone()</c> method uses reflection to call
	/// this method.  If you don't implement this kind of a constructor, you need to override the
	/// <c>Clone()</c> method yourself or it won't work.</p>
	/// </summary>
	public abstract class DictionarySet : Set
	{
		/// <summary>
		/// Provides the storage for elements in the <c>Set</c>, stored as the key-set
		/// of the <c>IDictionary</c> object.  Set this object in the constructor
		/// if you create your own <c>Set</c> class.  
		/// </summary>
		protected IDictionary m_InternalSet = null;
		
		private static object _object = new object();

		/// <summary>
		/// Adds the specified element to this set if it is not already present.
		/// </summary>
		/// <param name="o">The object to add to the set.</param>
		/// <returns><c>true</c> is the object was added, <c>false</c> if it was already present.</returns>
		public sealed override bool Add(object o)
		{
			if(m_InternalSet[o] != null)
				return false;
			else
			{
				//The object we are adding is just a placeholder.  The thing we are
				//really concerned with is 'o', the key.
				m_InternalSet.Add(o, _object);
				return true;
			}
		}

		/// <summary>
		/// Adds all the elements in the specified collection to the set if they are not already present.
		/// </summary>
		/// <param name="c">A collection of objects to add to the set.</param>
		/// <returns><c>true</c> is the set changed as a result of this operation, <c>false</c> if not.</returns>
		public sealed override bool AddAll(ICollection c)
		{
			bool changed = false;
			foreach(object o in c)
				changed |= this.Add(o);
			return changed;
		}

		/// <summary>
		/// Removes all objects from the set.
		/// </summary>
		public sealed override void Clear()
		{
			m_InternalSet.Clear();
		}

		/// <summary>
		/// Returns <c>true</c> if this set contains the specified element.
		/// </summary>
		/// <param name="o">The element to look for.</param>
		/// <returns><c>true</c> if this set contains the specified element, <c>false</c> otherwise.</returns>
		public sealed override bool Contains(object o)
		{
			return m_InternalSet[o] != null;
		}

		/// <summary>
		/// Returns <c>true</c> if the set contains all the elements in the specified collection.
		/// </summary>
		/// <param name="c">A collection of objects.</param>
		/// <returns><c>true</c> if the set contains all the elements in the specified collection, <c>false</c> otherwise.</returns>
		public sealed override bool ContainsAll(ICollection c)
		{
			foreach(object o in c)
			{
				if(!this.Contains(o))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns <c>true</c> if this set contains no elements.
		/// </summary>
		public sealed override bool IsEmpty
		{
			get{return m_InternalSet.Count == 0;}
		}

		/// <summary>
		/// Removes the specified element from the set.
		/// </summary>
		/// <param name="o">The element to be removed.</param>
		/// <returns><c>true</c> if the set contained the specified element, <c>false</c> otherwise.</returns>
		public sealed override bool Remove(object o)
		{
			bool contained = this.Contains(o);
			if(contained)
			{
				m_InternalSet.Remove(o);
			}
			return contained;
		}

		/// <summary>
		/// Remove all the specified elements from this set, if they exist in this set.
		/// </summary>
		/// <param name="c">A collection of elements to remove.</param>
		/// <returns><c>true</c> if the set was modified as a result of this operation.</returns>
		public sealed override bool RemoveAll(ICollection c)
		{
			bool changed = false;
			foreach(object o in c)
				changed |= this.Remove(o);
			return changed;
		}

		/// <summary>
		/// Retains only the elements in this set that are contained in the specified collection.
		/// </summary>
		/// <param name="c">Collection that defines the set of elements to be retained.</param>
		/// <returns><c>true</c> if this set changed as a result of this operation.</returns>
		public sealed override bool RetainAll(ICollection c)
		{
			//Put data from C into a set so we can use the Contains() method.
			Set cSet = new HybridSet(c);

			//We are going to build a set of elements to remove.
			Set removeSet = new HybridSet();
			
			foreach(object o in this)
			{
				//If C does not contain O, then we need to remove O from our
				//set.  We can't do this while iterating through our set, so
				//we put it into RemoveSet for later.
				if(!cSet.Contains(o))
					removeSet.Add(o);
			}

			return this.RemoveAll(removeSet);
		}


		/// <summary>
		/// Copies the elements in the <c>Set</c> to an array.  The type of array needs
		/// to be compatible with the objects in the <c>Set</c>, obviously.
		/// </summary>
		/// <param name="array">An array that will be the target of the copy operation.</param>
		/// <param name="index">The zero-based index where copying will start.</param>
		public sealed override void CopyTo(Array array, int index)
		{
			m_InternalSet.Keys.CopyTo(array, index);
		}

		/// <summary>
		/// The number of elements contained in this collection.
		/// </summary>
		public sealed override int Count
		{
			get{return m_InternalSet.Count;}		
		}

		/// <summary>
		/// None of the objects based on <c>DictionarySet</c> are synchronized.  Use the
		/// <c>SyncRoot</c> property instead.
		/// </summary>
		public sealed override bool IsSynchronized
		{
			get{return false;}
		}
		
		/// <summary>
		/// Returns an object that can be used to synchronize the <c>Set</c> between threads.
		/// </summary>
		public sealed override object SyncRoot
		{
			get{return m_InternalSet.SyncRoot;}
		}

		/// <summary>
		/// Gets an enumerator for the elements in the <c>Set</c>.
		/// </summary>
		/// <returns>An <c>IEnumerator</c> over the elements in the <c>Set</c>.</returns>
		public sealed override IEnumerator GetEnumerator()
		{
			return m_InternalSet.Keys.GetEnumerator();
		}
	}
}
