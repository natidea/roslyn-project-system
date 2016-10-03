// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    using TVersionedUpdate = IProjectVersionedValue<IProjectSubscriptionUpdate>;
    using TIdentityDictionary = IImmutableDictionary<NamedIdentity, IComparable>;

    public class ScratchNugetRestorer
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        private ImmutableHashSet<string> _watchedRules;
        private IDisposable _evaluationSubscriptionLink;

        [ImportingConstructor]
        public ScratchNugetRestorer(
            ConfiguredProject project,
            IProjectSubscriptionService projectSubscriptionService)
        {
            _project = project;
            _projectSubscriptionService = projectSubscriptionService;
            _watchedRules = Empty.OrdinalIgnoreCaseStringSet
                                 .Add(ConfigurationGeneral.SchemaName)
                                 .Add(ProjectReference.SchemaName)
                                 .Add(PackageReference.SchemaName);
        }

        private IVsHierarchy GetVsHierarchy()
        {
            return (IVsHierarchy)_project.UnconfiguredProject.Services.HostObject;
        }

        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [ConfiguredProjectAutoLoad]
        public void OnProjectLoaded()
        {
            LookAtConfiguration();

            var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(ProjectPropertyChangedAsync);
            _evaluationSubscriptionLink = this._projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                target,
                ruleNames: _watchedRules,
                initialDataAsNew: true,
                suppressVersionOnlyUpdates: true);
        }

        private void LookAtConfiguration()
        {
            var currentProjects = _project.UnconfiguredProject.LoadedConfiguredProjects;
            foreach (var project in currentProjects)
            {
                TraceUtilities.TraceVerbose($"project seen {project.ProjectConfiguration.Name}");
            }
        }

        private async Task ProjectPropertyChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> obj)
        {
            var configurationChanges = obj.Value.ProjectChanges[ConfigurationGeneral.SchemaName];
            var projectReferencesChanges = obj.Value.ProjectChanges[ProjectReference.SchemaName];
            var packageReferencesChanges = obj.Value.ProjectChanges[PackageReference.SchemaName];

            if (configurationChanges.Difference.ChangedProperties.Contains("TargetFrameworkMoniker") ||
                configurationChanges.Difference.ChangedProperties.Contains("IntermediateOutputPath") ||
                projectReferencesChanges.Difference.AnyChanges ||
                packageReferencesChanges.Difference.AnyChanges)
            {
                string targetFrameworkMoniker = configurationChanges.After.Properties["TargetFrameworkMoniker"];
                string intermediateOutputPath = configurationChanges.After.Properties["IntermediateOutputPath"];
                IImmutableDictionary<string, IImmutableDictionary<string, string>> projectReferences = projectReferencesChanges.After.Items;
                IImmutableDictionary<string, IImmutableDictionary<string, string>> packageReferences = packageReferencesChanges.After.Items;

                TraceUtilities.TraceVerbose($"project {projectReferences}");
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        #region DEMO 1

        private void Demo()
        {
            var target = new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate>>>(DemoProjectPropertyChangedAsync);
            ProjectDataSources.SyncLinkTo(
                _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(),
                _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(),
                target);
        }

        private async Task DemoProjectPropertyChangedAsync(IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate>> obj)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        #endregion

        #region DEMO 2

        private void Demo2()
        {
            var target = new ActionBlock<Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary>>(Demo2ProjectPropertyChangedAsync);
            var s1 = _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions();

            var sourceBlocks = ImmutableList<ProjectDataSources.SourceBlockAndLink<IProjectValueVersions>>.Empty
                .Add(((ISourceBlock<IProjectValueVersions>)s1.SourceBlock).SyncLinkOptions(s1.LinkOptions, s1.InitialDataAsNewForProjectSubscriptionUpdate))
                .Add(((ISourceBlock<IProjectValueVersions>)s1.SourceBlock).SyncLinkOptions(s1.LinkOptions, s1.InitialDataAsNewForProjectSubscriptionUpdate));

            var sourceBlocks2 = ImmutableList.Create(
                _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions<IProjectValueVersions>(),
                _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions<IProjectValueVersions>());

            ProjectDataSources.SyncLinkTo(sourceBlocks, target, null);
        }

        private async Task Demo2ProjectPropertyChangedAsync(Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary> sources)
        {
            var x = sources.Item1;
            foreach (TVersionedUpdate item in sources.Item1)
            {
                var configurationChanges = item.Value.ProjectChanges[ConfigurationGeneral.SchemaName];
                var projectReferencesChanges = item.Value.ProjectChanges[ProjectReference.SchemaName];
                var packageReferencesChanges = item.Value.ProjectChanges[PackageReference.SchemaName];

                if (configurationChanges.Difference.ChangedProperties.Contains("TargetFrameworkMoniker") ||
                    configurationChanges.Difference.ChangedProperties.Contains("IntermediateOutputPath") ||
                    projectReferencesChanges.Difference.AnyChanges ||
                    packageReferencesChanges.Difference.AnyChanges)
                {
                    string targetFrameworkMoniker = configurationChanges.After.Properties["TargetFrameworkMoniker"];
                    string intermediateOutputPath = configurationChanges.After.Properties["IntermediateOutputPath"];
                    IImmutableDictionary<string, IImmutableDictionary<string, string>> projectReferences = projectReferencesChanges.After.Items;
                    IImmutableDictionary<string, IImmutableDictionary<string, string>> packageReferences = packageReferencesChanges.After.Items;
                }
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        #endregion

    }

}
