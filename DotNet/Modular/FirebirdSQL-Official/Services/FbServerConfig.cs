﻿/*
 *	Firebird ADO.NET Data provider for .NET and Mono 
 * 
 *	   The contents of this file are subject to the Initial 
 *	   Developer's Public License Version 1.0 (the "License"); 
 *	   you may not use this file except in compliance with the 
 *	   License. You may obtain a copy of the License at 
 *	   http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *	   Software distributed under the License is distributed on 
 *	   an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either 
 *	   express or implied. See the License for the specific 
 *	   language governing rights and limitations under the License.
 * 
 *	Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *	All Rights Reserved.
 */

using System;

namespace FirebirdSql.Data.Services
{
    public struct FbServerConfig
    {
        #region · Fields ·

        private int lockMemSize;
        private int lockSemCount;
        private int lockSignal;
        private int eventMemorySize;
        private int prioritySwitchDelay;
        private int minMemory;
        private int maxMemory;
        private int lockGrantOrder;
        private int anyLockMemory;
        private int anyLockSemaphore;
        private int anyLockSignal;
        private int anyEventMemory;
        private int lockHashSlots;
        private int deadlockTimeout;
        private int lockRequireSpins;
        private int connectionTimeout;
        private int dummyPacketInterval;
        private int ipcMapSize;
        private int defaultDbCachePages;

        #endregion

        #region · Properties ·

        public int LockMemSize
        {
            get { return this.lockMemSize; }
            set { this.lockMemSize = value; }
        }

        public int LockSemCount
        {
            get { return this.lockSemCount; }
            set { this.lockSemCount = value; }
        }

        public int LockSignal
        {
            get { return this.lockSignal; }
            set { this.lockSignal = value; }
        }

        public int EventMemorySize
        {
            get { return this.eventMemorySize; }
            set { this.eventMemorySize = value; }
        }

        public int PrioritySwitchDelay
        {
            get { return this.prioritySwitchDelay; }
            set { this.prioritySwitchDelay = value; }
        }

        public int MinMemory
        {
            get { return this.minMemory; }
            set { this.minMemory = value; }
        }

        public int MaxMemory
        {
            get { return this.maxMemory; }
            set { this.maxMemory = value; }
        }

        public int LockGrantOrder
        {
            get { return this.lockGrantOrder; }
            set { this.lockGrantOrder = value; }
        }

        public int AnyLockMemory
        {
            get { return this.anyLockMemory; }
            set { this.anyLockMemory = value; }
        }

        public int AnyLockSemaphore
        {
            get { return this.anyLockSemaphore; }
            set { this.anyLockSemaphore = value; }
        }

        public int AnyLockSignal
        {
            get { return this.anyLockSignal; }
            set { this.anyLockSignal = value; }
        }

        public int AnyEventMemory
        {
            get { return this.anyEventMemory; }
            set { this.anyEventMemory = value; }
        }

        public int LockHashSlots
        {
            get { return this.lockHashSlots; }
            set { this.lockHashSlots = value; }
        }

        public int DeadlockTimeout
        {
            get { return this.deadlockTimeout; }
            set { this.deadlockTimeout = value; }
        }

        public int LockRequireSpins
        {
            get { return this.lockRequireSpins; }
            set { this.lockRequireSpins = value; }
        }

        public int ConnectionTimeout
        {
            get { return this.connectionTimeout; }
            set { this.connectionTimeout = value; }
        }

        public int DummyPacketInterval
        {
            get { return this.dummyPacketInterval; }
            set { this.dummyPacketInterval = value; }
        }

        public int IpcMapSize
        {
            get { return this.ipcMapSize; }
            set { this.ipcMapSize = value; }
        }

        public int DefaultDbCachePages
        {
            get { return this.defaultDbCachePages; }
            set { this.defaultDbCachePages = value; }
        }

        #endregion
    }
}
