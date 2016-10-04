﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    internal class TargetFrameworks : VsItemList<IVsTargetFrameworkInfo>, IVsTargetFrameworks
    {
        public TargetFrameworks() : base() { }

        public TargetFrameworks(IEnumerable<IVsTargetFrameworkInfo> collection) : base(collection) { }

        protected override String GetKeyForItem(IVsTargetFrameworkInfo value) => value.TargetFrameworkMoniker;
    }
}
