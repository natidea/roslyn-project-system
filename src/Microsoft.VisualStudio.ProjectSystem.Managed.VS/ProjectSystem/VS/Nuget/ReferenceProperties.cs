﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    internal class ReferenceProperties : VsItemList<IVsReferenceProperty>, IVsReferenceProperties
    {
        public ReferenceProperties() : base() { }

        public ReferenceProperties(IEnumerable<IVsReferenceProperty> collection) : base(collection) { }

        protected override String GetKeyForItem(IVsReferenceProperty value) => value.Name;
    }
}
