﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    internal class TargetFrameworkInfo : IVsTargetFrameworkInfo
    {
        public IVsReferenceItems PackageReferences { get; set; }

        public IVsReferenceItems ProjectReferences { get; set; }

        public String TargetFrameworkMoniker { get; set; }
    }
}
