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
	public class TestException : Exception
	{
        public TestException(string message, Exception reason) : base(message, reason)
        {
        }

		public TestException(Exception reason) : base(reason.Message, reason)
		{
		}

#if !CF && !SILVERLIGHT
		public TestException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{
		}
#endif

		public Exception GetReason()
		{
			return InnerException;
		}
		
		override public string ToString()
		{
			if (null != InnerException) return InnerException.ToString();
			return base.ToString();
		}
	}
}
