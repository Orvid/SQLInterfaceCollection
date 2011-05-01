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
using System.IO;
using Db4oUnit;
using Db4oUnit.Extensions.Fixtures;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Consistency;
using Db4objects.Db4o.Defragment;
using Db4objects.Db4o.Foundation;
using Db4objects.Db4o.Internal;

namespace Db4oUnit.Extensions.Fixtures
{
	public class Db4oConsistencyCheckSolo : Db4oSolo
	{
		protected override IObjectContainer CreateDatabase(IConfiguration config)
		{
			Check(CloneConfig(config));
			Defrag(CloneConfig(config));
			Check(CloneConfig(config));
			return base.CreateDatabase(config);
		}

		protected override void PreClose()
		{
			base.PreClose();
			if (Db() != null && !Db().IsClosed())
			{
				Db().Close();
			}
			Check(CloneConfiguration());
			Defrag(CloneConfiguration());
			Check(CloneConfiguration());
		}

		public override bool Accept(Type clazz)
		{
			return base.Accept(clazz) && !typeof(IOptOutDefragSolo).IsAssignableFrom(clazz);
		}

		private void Check(IConfiguration config)
		{
			IObjectContainer db = base.CreateDatabase(config);
			ConsistencyChecker.ConsistencyReport report = new ConsistencyChecker(db).CheckSlotConsistency
				();
			CloseAndWait(db);
			if (!report.Consistent())
			{
				throw new TestException(report.ToString(), null);
			}
		}

		private void Defrag(IConfiguration config)
		{
			Sharpen.IO.File origFile = new Sharpen.IO.File(GetAbsolutePath());
			if (origFile.Exists())
			{
				try
				{
					string backupFile = GetAbsolutePath() + ".defrag.backup";
					IIdMapping mapping = new InMemoryIdMapping();
					// new
					// BTreeIDMapping(getAbsolutePath()+".defrag.mapping",4096,1,1000);
					DefragmentConfig defragConfig = new DefragmentConfig(GetAbsolutePath(), backupFile
						, mapping);
					defragConfig.ForceBackupDelete(true);
					// FIXME Cloning is ugly - wrap original in Decorator within
					// DefragContext instead?
					IConfiguration clonedConfig = (IConfiguration)((IDeepClone)config).DeepClone(null
						);
					defragConfig.Db4oConfig(clonedConfig);
					Db4objects.Db4o.Defragment.Defragment.Defrag(defragConfig, new _IDefragmentListener_65
						());
				}
				catch (IOException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}
		}

		private sealed class _IDefragmentListener_65 : IDefragmentListener
		{
			public _IDefragmentListener_65()
			{
			}

			public void NotifyDefragmentInfo(DefragmentInfo info)
			{
				Sharpen.Runtime.Err.WriteLine(info);
			}
		}

		private IConfiguration CloneConfig(IConfiguration config)
		{
			return (IConfiguration)((IDeepClone)config).DeepClone(null);
		}

		private void CloseAndWait(IObjectContainer db)
		{
			db.Close();
			try
			{
				((ObjectContainerBase)db).ThreadPool().Join(3000);
			}
			catch (Exception)
			{
			}
		}
	}
}
