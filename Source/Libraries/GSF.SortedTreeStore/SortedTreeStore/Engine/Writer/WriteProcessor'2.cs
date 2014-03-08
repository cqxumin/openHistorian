﻿//******************************************************************************************************
//  WriteProcessor`2.cs - Gbtc
//
//  Copyright © 2013, Grid Protection Alliance.  All Rights Reserved.
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
//  1/19/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using System;
using GSF.SortedTreeStore.Tree;

namespace GSF.SortedTreeStore.Engine.Writer
{
    /// <summary>
    /// Houses all of the write operations for the historian
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class WriteProcessor<TKey, TValue>
        : IDisposable
        where TKey : SortedTreeTypeBase<TKey>, new()
        where TValue : SortedTreeTypeBase<TValue>, new()
    {
        /// <summary>
        /// An event handler that will raise any exceptions that go unhandled in the rollover process.
        /// </summary>
        public event UnhandledExceptionEventHandler Exception;
        private readonly PrebufferWriter<TKey, TValue> m_prebuffer;
        private readonly FirstStageWriter<TKey, TValue> m_stage0;
        private readonly bool m_isMemoryOnly;
        readonly TransactionTracker<TKey, TValue> m_transactionTracker;
        private WriteProcessor(PrebufferWriter<TKey, TValue> prebuffer, FirstStageWriter<TKey, TValue> stage0, bool isMemoryOnly)
        {
            m_isMemoryOnly = isMemoryOnly;
            m_prebuffer = prebuffer;
            m_stage0 = stage0;
            m_stage0.Exception += OnException;
            m_prebuffer.Exception += OnException;
            m_transactionTracker = new TransactionTracker<TKey, TValue>(m_prebuffer, m_stage0);
        }

        /// <summary>
        /// Creates an in memory place to store points added to the SortedTreeStore.
        /// </summary>
        /// <param name="list">the list where to write to</param>
        /// <param name="encoding">the encoding method to use once points have matured past the initial out of order insertion</param>
        /// <param name="prebufferDuration">the maximum number of milliseconds that a point will wait in the prebuffer before being committed to a memory archive that clients can query.</param>
        /// <param name="diskFlushInterval">the maximum number of milliseconds that a point will wait in the in-memory buffer before being flushed to the disk</param>
        /// <returns></returns>
        public static WriteProcessor<TKey, TValue> CreateInMemory(ArchiveList<TKey, TValue> list, EncodingDefinition encoding, int prebufferDuration = 100, int diskFlushInterval = 10000)
        {
            IncrementalStagingFile<TKey, TValue> incrementalStagingFile = IncrementalStagingFile<TKey, TValue>.CreateInMemory(list, encoding);
            FirstStageWriter<TKey, TValue> firstStageWriter = new FirstStageWriter<TKey, TValue>(incrementalStagingFile, diskFlushInterval);
            PrebufferWriter<TKey, TValue> prebuffer = new PrebufferWriter<TKey, TValue>(prebufferDuration, firstStageWriter.AppendData);
            return new WriteProcessor<TKey, TValue>(prebuffer, firstStageWriter, true);

        }

        /// <summary>
        /// Creates an in memory place to store points added to the SortedTreeStore.
        /// </summary>
        /// <param name="list">the list where to write to</param>
        /// <param name="encoding">the encoding method to use once points have matured past the initial out of order insertion</param>
        /// <param name="savePath">the path to save files to</param>
        /// <param name="prebufferDuration">the maximum number of milliseconds that a point will wait in the prebuffer before being committed to a memory archive that clients can query.</param>
        /// <param name="diskFlushInterval">the maximum number of milliseconds that a point will wait in the in-memory buffer before being flushed to the disk</param>
        /// <returns></returns>
        public static WriteProcessor<TKey, TValue> CreateOnDisk(ArchiveList<TKey, TValue> list, EncodingDefinition encoding, string savePath, int prebufferDuration = 100, int diskFlushInterval = 10000)
        {
            IncrementalStagingFile<TKey, TValue> incrementalStagingFile = IncrementalStagingFile<TKey, TValue>.CreateOnDisk(list, encoding, savePath);
            FirstStageWriter<TKey, TValue> firstStageWriter = new FirstStageWriter<TKey, TValue>(incrementalStagingFile, diskFlushInterval);
            PrebufferWriter<TKey, TValue> prebuffer = new PrebufferWriter<TKey, TValue>(prebufferDuration, firstStageWriter.AppendData);
            return new WriteProcessor<TKey, TValue>(prebuffer, firstStageWriter, false);
        }

        void OnException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledExceptionEventHandler handler = Exception;
            if (handler != null)
                handler(sender, e);
        }

        /// <summary>
        /// Writes the provided key/value to the engine.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>the transaction code so this write can be tracked.</returns>
        public long Write(TKey key, TValue value)
        {
            return m_prebuffer.Write(key, value);
        }

        /// <summary>
        /// Writes the provided stream to the engine.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>the transaction code so this write can be tracked.</returns>
        public long Write(TreeStream<TKey, TValue> stream)
        {
            long sequenceId = -1;
            TKey key = new TKey();
            TValue value = new TValue();
            while (stream.Read(key, value))
                sequenceId = m_prebuffer.Write(key, value);
            return sequenceId;
        }

        /// <summary>
        /// Stops the execution of the historian in a orderly mannor.
        /// </summary>
        public void Dispose()
        {
            m_prebuffer.Stop();
            m_stage0.Stop();
        }

        /// <summary>
        /// Blocks until the specified point has progressed beyond the prestage level and can be queried by the user.
        /// </summary>
        /// <param name="transactionId">the sequence number representing the desired point that was committed</param>
        public void SoftCommit(long transactionId)
        {
            m_transactionTracker.WaitForSoftCommit(transactionId);
        }

        /// <summary>
        /// Blocks until the specified point has been committed to the disk subsystem. If running in a In-Memory mode, will return
        /// as soon as it has been moved beyond the prestage level and can be queried by the user.
        /// </summary>
        /// <param name="transactionId">the sequence number representing the desired point that was committed</param>
        public void HardCommit(long transactionId)
        {
            if (m_isMemoryOnly)
            {
                SoftCommit(transactionId);
            }
            else
            {
                m_transactionTracker.WaitForHardCommit(transactionId);
            }
        }


    }
}