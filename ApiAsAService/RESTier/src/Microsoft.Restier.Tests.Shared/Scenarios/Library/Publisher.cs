﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// 
    /// </summary>
    public class Publisher
    {

        public string Id { get; set; }

        public Address Addr { get; set; }

        public virtual ICollection<Book> Books { get; set; }

    }

}
