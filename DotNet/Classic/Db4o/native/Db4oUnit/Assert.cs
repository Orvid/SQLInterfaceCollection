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

namespace Db4oUnit
{
	public delegate void CodeBlock();

	public partial class Assert
	{
		public static Exception Expect(System.Type exception, CodeBlock block)
		{
			return Assert.Expect(exception, new DelegateCodeBlock(block));
		}

		public static Exception Expect<TException>(CodeBlock block) where TException : Exception
		{
			return Assert.Expect(typeof(TException), block);
		}

		private class DelegateCodeBlock : ICodeBlock
		{
			private readonly CodeBlock _block;

			public DelegateCodeBlock(CodeBlock block)
			{
				_block = block;
			}

			public void Run()
			{
				_block();
			}
		}

		public static void InRange(double value, double from, double to)
		{
			Assert.IsTrue(value >= from && value <= to, string.Format("'{0}' not in range '{1}'..'{2}'", value, from, to));
		}
	}
}
