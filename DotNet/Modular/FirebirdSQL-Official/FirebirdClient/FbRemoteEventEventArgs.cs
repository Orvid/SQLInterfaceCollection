﻿/*
 *  Firebird ADO.NET Data provider for .NET and Mono 
 * 
 *     The contents of this file are subject to the Initial 
 *     Developer's Public License Version 1.0 (the "License"); 
 *     you may not use this file except in compliance with the 
 *     License. You may obtain a copy of the License at 
 *     http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *     Software distributed under the License is distributed on 
 *     an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either 
 *     express or implied.  See the License for the specific 
 *     language governing rights and limitations under the License.
 * 
 *  Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System;

namespace FirebirdSql.Data.FirebirdClient
{
    public sealed class FbRemoteEventEventArgs : System.ComponentModel.CancelEventArgs
    {
        #region · Fields ·

        private string name;
        private int counts;

        #endregion

        #region · Properties ·

        public string Name
        {
            get { return this.name; }
        }

        public int Counts
        {
            get { return this.counts; }
        }

        #endregion

        #region · Constructors ·

        public FbRemoteEventEventArgs(string name, int counts)
            : this(name, counts, false)
        { }

        public FbRemoteEventEventArgs(string name, int counts, bool cancel)
            : base(cancel)
        {
            this.name = name;
            this.counts = counts;
        }

        #endregion
    }
}
