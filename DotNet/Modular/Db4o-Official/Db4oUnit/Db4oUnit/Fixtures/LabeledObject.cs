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
using Db4oUnit.Fixtures;

namespace Db4oUnit.Fixtures
{
	public class LabeledObject : ILabeled
	{
		private readonly object _value;

		private readonly string _label;

		public LabeledObject(object value, string label)
		{
			_value = value;
			_label = label;
		}

		public LabeledObject(object value) : this(value, null)
		{
		}

		public virtual string Label()
		{
			if (_label == null)
			{
				return _value.ToString();
			}
			return _label;
		}

		public virtual object Value()
		{
			return _value;
		}

		public static Db4oUnit.Fixtures.LabeledObject[] ForObjects(object[] values)
		{
			Db4oUnit.Fixtures.LabeledObject[] labeledObjects = new Db4oUnit.Fixtures.LabeledObject
				[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				labeledObjects[i] = new Db4oUnit.Fixtures.LabeledObject(values[i]);
			}
			return labeledObjects;
		}
	}
}
