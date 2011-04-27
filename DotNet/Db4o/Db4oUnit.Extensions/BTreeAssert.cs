/* This file is part of the db4o object database http://www.db4o.com

Copyright (C) 2004 - 2010  Versant Corporation http://www.versant.com

db4o is free software; you can redistribute it and/or modify it under
the terms of version 3 of the GNU General Public License as published
by the Free Software Foundation.

db4o is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or
FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
for more details.

You should have received a copy of the GNU General Public License along
with this program.  If not, see http://www.gnu.org/licenses/. */
using System.Collections;
using Db4oUnit;
using Db4oUnit.Extensions;
using Db4objects.Db4o.Foundation;
using Db4objects.Db4o.Internal;
using Db4objects.Db4o.Internal.Btree;
using Db4objects.Db4o.Internal.Freespace;
using Db4objects.Db4o.Internal.Handlers;
using Db4objects.Db4o.Internal.Ids;
using Db4objects.Db4o.Internal.Slots;

namespace Db4oUnit.Extensions
{
	public class BTreeAssert
	{
		public static void TraverseKeys(IBTreeRange result, IVisitor4 visitor)
		{
			IEnumerator i = result.Keys();
			while (i.MoveNext())
			{
				visitor.Visit(i.Current);
			}
		}

		public static void AssertKeys(Transaction transaction, BTree btree, int[] keys)
		{
			ExpectingVisitor visitor = ExpectingVisitor.CreateExpectingVisitor(keys);
			btree.TraverseKeys(transaction, visitor);
			visitor.AssertExpectations();
		}

		public static void AssertEmpty(Transaction transaction, BTree tree)
		{
			ExpectingVisitor visitor = new ExpectingVisitor(new object[0]);
			tree.TraverseKeys(transaction, visitor);
			visitor.AssertExpectations();
			Assert.AreEqual(0, tree.Size(transaction));
		}

		public static void DumpKeys(Transaction trans, BTree tree)
		{
			tree.TraverseKeys(trans, new _IVisitor4_37());
		}

		private sealed class _IVisitor4_37 : IVisitor4
		{
			public _IVisitor4_37()
			{
			}

			public void Visit(object obj)
			{
				Sharpen.Runtime.Out.WriteLine(obj);
			}
		}

		public static int FillSize(BTree btree)
		{
			return btree.NodeSize() + 1;
		}

		public static int[] NewBTreeNodeSizedArray(BTree btree, int value)
		{
			return IntArrays4.Fill(new int[FillSize(btree)], value);
		}

		public static void AssertRange(int[] expectedKeys, IBTreeRange range)
		{
			Assert.IsNotNull(range);
			ExpectingVisitor visitor = ExpectingVisitor.CreateSortedExpectingVisitor(expectedKeys
				);
			TraverseKeys(range, visitor);
			visitor.AssertExpectations();
		}

		public static BTree CreateIntKeyBTree(ObjectContainerBase container, int id, int 
			nodeSize)
		{
			return new BTree(container.SystemTransaction(), id, new IntHandler(), nodeSize);
		}

		public static void AssertSingleElement(Transaction trans, BTree btree, object element
			)
		{
			Assert.AreEqual(1, btree.Size(trans));
			IBTreeRange result = btree.SearchRange(trans, element);
			ExpectingVisitor expectingVisitor = new ExpectingVisitor(new object[] { element }
				);
			BTreeAssert.TraverseKeys(result, expectingVisitor);
			expectingVisitor.AssertExpectations();
			expectingVisitor = new ExpectingVisitor(new object[] { element });
			btree.TraverseKeys(trans, expectingVisitor);
			expectingVisitor.AssertExpectations();
		}

		/// <exception cref="System.Exception"></exception>
		public static void AssertAllSlotsFreed(LocalTransaction trans, BTree bTree, ICodeBlock
			 block)
		{
			LocalObjectContainer container = (LocalObjectContainer)trans.Container();
			ITransactionalIdSystem idSystem = trans.IdSystem();
			IEnumerator allSlotIDs = bTree.AllNodeIds(trans.SystemTransaction());
			Collection4 allSlots = new Collection4();
			while (allSlotIDs.MoveNext())
			{
				int slotID = ((int)allSlotIDs.Current);
				Slot slot = idSystem.CurrentSlot(slotID);
				allSlots.Add(slot);
			}
			Slot bTreeSlot = idSystem.CurrentSlot(bTree.GetID());
			allSlots.Add(bTreeSlot);
			Collection4 freedSlots = new Collection4();
			IFreespaceManager freespaceManager = container.FreespaceManager();
			container.InstallDebugFreespaceManager(new FreespaceManagerForDebug(new _ISlotListener_99
				(freedSlots)));
			block.Run();
			container.InstallDebugFreespaceManager(freespaceManager);
			Assert.IsTrue(freedSlots.ContainsAll(allSlots.GetEnumerator()));
		}

		private sealed class _ISlotListener_99 : ISlotListener
		{
			public _ISlotListener_99(Collection4 freedSlots)
			{
				this.freedSlots = freedSlots;
			}

			public void OnFree(Slot slot)
			{
				freedSlots.Add(slot);
			}

			private readonly Collection4 freedSlots;
		}
	}
}
