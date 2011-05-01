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
using System;
using Db4oUnit.Extensions;
using Db4oUnit.Extensions.Fixtures;
using Db4objects.Db4o.Ext;

namespace Db4oUnit.Extensions.Fixtures
{
	public class Db4oEmbeddedSessionFixture : AbstractFileBasedDb4oFixture, IMultiSessionFixture
	{
		private static readonly string File = "db4oEmbeddedSession.db4o";

		private readonly string _label;

		private IExtObjectContainer _session;

		public Db4oEmbeddedSessionFixture(string label)
		{
			_label = label;
		}

		public Db4oEmbeddedSessionFixture() : this("E/S")
		{
		}

		public override string Label()
		{
			return BuildLabel(_label);
		}

		public override IExtObjectContainer Db()
		{
			return _session;
		}

		protected override string FileName()
		{
			return File;
		}

		public override bool Accept(Type clazz)
		{
			if (!typeof(IDb4oTestCase).IsAssignableFrom(clazz))
			{
				return false;
			}
			if (typeof(IOptOutMultiSession).IsAssignableFrom(clazz))
			{
				return false;
			}
			if (typeof(IOptOutAllButNetworkingCS).IsAssignableFrom(clazz))
			{
				return false;
			}
			return true;
		}

		protected override void PostOpen(IDb4oTestCase testInstance)
		{
			_session = OpenNewSession(testInstance);
		}

		protected override void PreClose()
		{
			if (null != _session)
			{
				_session.Close();
			}
		}

		public virtual IExtObjectContainer OpenNewSession(IDb4oTestCase testInstance)
		{
			return FileSession().OpenSession().Ext();
		}
	}
}
