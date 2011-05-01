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
#if SILVERLIGHT

using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Internal.Config;
using Db4objects.Db4o.IO;

namespace Db4oUnit.Extensions.Fixtures
{
	public class SilverlightFixture : AbstractSoloDb4oFixture
	{
		protected override void DoClean()
		{
			if (null != _storage)
			{
				_storage.Delete(DatabaseFileName);
			}
		}

		public override string Label()
		{
			return BuildLabel("Silverlight Solo");
		}

		public override void Defragment()
		{
			Defragment(DatabaseFileName);
		}

		protected override IObjectContainer CreateDatabase(IConfiguration config)
		{
			_storage = config.Storage;
			return Db4oFactory.OpenFile(config, DatabaseFileName);
		}

		protected override IConfiguration NewConfiguration()
		{
			var config = base.NewConfiguration();
			var embeddedConfig = new EmbeddedConfigurationImpl(config);
			embeddedConfig.AddConfigurationItem(new SilverlightSupport());

			return config;
		}

		public override bool Accept(System.Type clazz)
		{
			if (typeof(IOptOutSilverlight).IsAssignableFrom(clazz)) return false;
			return base.Accept(clazz);
		}

		private const string DatabaseFileName = "SilverlightDatabase.db4o";
		private IStorage _storage;
	}
}

#endif