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
using System.Collections.Generic;
using System.Text;
using Db4objects.Db4o.Diagnostic;

namespace Db4oUnit.Extensions.Util
{
	public class DiagnosticCollector<T> : IDiagnosticListener
	{
		public void OnDiagnostic(IDiagnostic d)
		{
			if (typeof(T) == d.GetType())
			{
				_diagnostics.Add(d);
			}
		}

		public IList<IDiagnostic> Diagnostics
		{
			get { return _diagnostics; }
		}

		public bool Empty
		{
			get { return _diagnostics.Count == 0; }
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (IDiagnostic diagnostic in _diagnostics)
			{
				sb.Append(diagnostic + "\r\n");
			}

			return sb.ToString();
		}

		private readonly IList<IDiagnostic> _diagnostics = new List<IDiagnostic>();
	}
}
