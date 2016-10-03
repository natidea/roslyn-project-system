﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    internal static class ProjectRestoreInfoBuilder
    {
        internal static IVsProjectRestoreInfo Build(ImmutableList<IProjectValueVersions> updates)
        {
            string baseIntermediatePath = null;
            var targetFrameworks = new TargetFrameworks();
            
            foreach (IProjectVersionedValue<IProjectSubscriptionUpdate> update in updates)
            {
                var configurationChanges = update.Value.ProjectChanges[ConfigurationGeneral.SchemaName];
                baseIntermediatePath = baseIntermediatePath ?? configurationChanges.After.Properties["BaseIntermediateOutputPath"];
                string targetFrameworkMoniker = configurationChanges.After.Properties["TargetFrameworkMoniker"];

                if (targetFrameworks.Item(targetFrameworkMoniker) == null)
                {
                    var projectReferencesChanges = update.Value.ProjectChanges[ProjectReference.SchemaName];
                    var packageReferencesChanges = update.Value.ProjectChanges[PackageReference.SchemaName];

                    targetFrameworks.Add(new TargetFrameworkInfo
                    {
                        TargetFrameworkMoniker = targetFrameworkMoniker,
                        ProjectReferences = GetReferences(projectReferencesChanges.After.Items),
                        PackageReferences = GetReferences(packageReferencesChanges.After.Items)
                    });
                }
            }

            return new ProjectRestoreInfo
            {
                BaseIntermediatePath = baseIntermediatePath,
                TargetFrameworks = targetFrameworks
            };
        }

        private static IVsReferenceItems GetReferences(IImmutableDictionary<String, IImmutableDictionary<String, String>> items)
        {
            var refItems = new ReferenceItems();
            refItems.AddRange(items.Select(p => new ReferenceItem
            {
                Name = p.Key,
                Properties = new ReferenceProperties(p.Value.Select(v => new ReferenceProperty
                {
                    Name = v.Key, Value = v.Value
                })) 
            }));
            return refItems;
        }
    }
}