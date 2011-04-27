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
using Db4oUnit.Extensions.Fixtures;
using Db4oUnit.Extensions.Util;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;

namespace Db4oUnit.Extensions.Fixtures
{
	public abstract class AbstractFileBasedDb4oFixture : AbstractSoloDb4oFixture
	{
		private readonly Sharpen.IO.File _databaseFile;

		public AbstractFileBasedDb4oFixture()
		{
			string fileName = FileName();
			_databaseFile = new Sharpen.IO.File(CrossPlatformServices.DatabasePath(fileName));
		}

		protected abstract string FileName();

		protected override IObjectContainer CreateDatabase(IConfiguration config)
		{
			return Db4oFactory.OpenFile(config, GetAbsolutePath());
		}

		public virtual string GetAbsolutePath()
		{
			return _databaseFile.GetAbsolutePath();
		}

		/// <exception cref="System.Exception"></exception>
		public override void Defragment()
		{
			Defragment(GetAbsolutePath());
		}

		protected override void DoClean()
		{
			if (_databaseFile.Exists())
			{
				_databaseFile.Delete();
			}
		}
	}
}
