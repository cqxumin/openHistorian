﻿//******************************************************************************************************
//  TransactionTracker`2.cs - Gbtc
//
//  Copyright © 2014, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  3/7/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Threading;
using GSF.SortedTreeStore.Tree;

namespace GSF.SortedTreeStore.Engine.Writer
{
    public class TransactionTracker<TKey, TValue>
        where TKey : SortedTreeTypeBase<TKey>, new()
        where TValue : SortedTreeTypeBase<TValue>, new()
    {
        object m_syncRoot;
        long m_transactionSoftCommitted;
        long m_transactionHardCommitted;
        PrebufferWriter<TKey, TValue> m_prebuffer;
        FirstStageWriter<TKey, TValue> m_firstStageWriter;

        private class WaitForCommit : IDisposable
        {
            public long TransactionId { get; private set; }
            ManualResetEvent m_resetEvent;
            public WaitForCommit(long transactionId)
            {
                TransactionId = transactionId;
                m_resetEvent = new ManualResetEvent(false);
            }

            public void Wait()
            {
                m_resetEvent.WaitOne();
            }

            public void Signal()
            {
                m_resetEvent.Set();
            }

            public void Dispose()
            {
                if ((object)m_resetEvent != null)
                {
                    m_resetEvent.Dispose();
                    m_resetEvent = null;
                }
            }
        }

        List<WaitForCommit> m_waitingForSoftCommit;
        List<WaitForCommit> m_waitingForHardCommit;

        public TransactionTracker(PrebufferWriter<TKey, TValue> prebuffer, FirstStageWriter<TKey, TValue> firstStageWriter)
        {
            m_waitingForHardCommit = new List<WaitForCommit>();
            m_waitingForSoftCommit = new List<WaitForCommit>();

            m_syncRoot = new object();
            m_transactionSoftCommitted = 0;
            m_transactionHardCommitted = 0;
            m_prebuffer = prebuffer;
            m_firstStageWriter = firstStageWriter;
            m_prebuffer.RolloverComplete += TransactionSoftCommitted;
            m_firstStageWriter.SequenceNumberCommitted += TransactionHardCommitted;
        }

        void TransactionSoftCommitted(long transactionId)
        {
            lock (m_syncRoot)
            {
                m_transactionSoftCommitted = transactionId;
                for (int x = m_waitingForSoftCommit.Count - 1; x > 0; x--)
                {
                    var waiting = m_waitingForSoftCommit[x];
                    if (transactionId >= waiting.TransactionId)
                    {
                        waiting.Signal();
                        m_waitingForSoftCommit.RemoveAt(x);
                    }
                }
            }
        }

        void TransactionHardCommitted(long transactionId)
        {
            lock (m_syncRoot)
            {
                m_transactionHardCommitted = transactionId;
                for (int x = m_waitingForHardCommit.Count - 1; x > 0; x--)
                {
                    var waiting = m_waitingForHardCommit[x];
                    if (transactionId >= waiting.TransactionId)
                    {
                        waiting.Signal();
                        m_waitingForHardCommit.RemoveAt(x);
                    }
                }
            }
        }

        public void WaitForSoftCommit(long transactionId)
        {
            lock (m_syncRoot)
            {
                if (m_transactionSoftCommitted > transactionId)
                    return;
            }
            m_prebuffer.Commit(transactionId);

            using (var wait = new WaitForCommit(transactionId))
            {
                lock (m_syncRoot)
                {
                    if (m_transactionSoftCommitted > transactionId)
                        return;
                    m_waitingForSoftCommit.Add(wait);
                }
                wait.Wait();
            }
        }

        public void WaitForHardCommit(long transactionId)
        {
            bool triggerSoft = true;
            lock (m_syncRoot)
            {
                if (m_transactionHardCommitted > transactionId)
                    return;
                if (m_transactionSoftCommitted > transactionId)
                    triggerSoft = true;
            }
            if (triggerSoft)
                m_prebuffer.Commit(transactionId);
            m_firstStageWriter.Commit(transactionId);


            using (var wait = new WaitForCommit(transactionId))
            {
                lock (m_syncRoot)
                {
                    if (m_transactionHardCommitted > transactionId)
                        return;
                    m_waitingForHardCommit.Add(wait);
                }
                wait.Wait();
            }

        }
    }
}
