﻿//******************************************************************************************************
//  StateMachine.cs - Gbtc
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
//  1/13/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//  2/14/2014 - Steven E. Chisholm
//       Migrated code to GSF.Core from the openHistorian project.
//       
//
//******************************************************************************************************

using System.Threading;

namespace GSF.Threading
{
    /// <summary>
    /// Helps facilitate a multithreaded state machine.
    /// </summary>
    /// <remarks>
    /// State machine variables must be integers since <see cref="Interlocked"/> methods require premitive types.
    /// </remarks>
    public class StateMachine
    {
        /// <summary>
        /// The internal state of the state machine.
        /// </summary>
        private volatile int m_state;

        /// <summary>
        /// Creates a new <see cref="StateMachine"/>
        /// </summary>
        /// <param name="initialState">the state to initially set to</param>
        public StateMachine(int initialState)
        {
            m_state = initialState;
        }

        /// <summary>
        /// Gets the current state of the State Machine. 
        /// </summary>
        public int State
        {
            get
            {
                return m_state;
            }
        }

        /// <summary>
        /// Attempts to change the state of this machine from <see cref="prevState"/> to <see cref="nextState"/>.
        /// </summary>
        /// <param name="prevState">The state to change from</param>
        /// <param name="nextState">The state to change to</param>
        /// <returns>True if the state changed from the previous state to the next state. 
        /// False if unsuccessful.</returns>
        public bool TryChangeState(int prevState, int nextState)
        {
            return (m_state == prevState && Interlocked.CompareExchange(ref m_state, nextState, prevState) == prevState);
        }

        /// <summary>
        /// Sets the value of the state. 
        /// Only call this method if you are certain that you are the only entity allowed to change 
        /// from the current state to this state. This does not implement an atomic compare exchange.
        /// </summary>
        /// <param name="state"></param>
        public void SetState(int state)
        {
            m_state = state;
        }

        /// <summary>
        /// Implicity conversion of the state to the integer value of the state.
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        public static implicit operator int(StateMachine machine)
        {
            return machine.State;
        }
    }
}