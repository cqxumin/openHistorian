﻿//******************************************************************************************************
//  KeyValueStreamCompressionBase.cs - Gbtc
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
//  8/10/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSF.IO;
using openHistorian.Collections;

namespace openHistorian.Communications.Initialization
{
    public abstract class KeyValueStreamCompressionBase<TKey, TValue>
        where TKey : HistorianKeyBase<TKey>, new()
        where TValue : HistorianValueBase<TValue>, new()
    {

        public abstract Guid CompressionType { get; }

        public abstract void WriteEndOfStream(BinaryStreamBase stream);

        public abstract void Encode(BinaryStreamBase stream, TKey prevKey, TValue prevValue, TKey currentKey, TValue currentValue);

        public abstract unsafe bool TryDecode(BinaryStreamBase stream, TKey key, TValue value);

    }
}
