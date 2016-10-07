﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    using global::NuGet.SolutionRestoreManager;
    using Microsoft.VisualStudio.ProjectSystem.Utilities;
    using Microsoft.VisualStudio.Shell.Interop;
    using TIdentityDictionary = IImmutableDictionary<NamedIdentity, IComparable>;

    internal class NuGetRestorer : OnceInitializedOnceDisposedAsync
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        //private readonly IVsSolutionRestoreService _solutionRestoreService;
        private IDisposable _evaluationSubscriptionLink;

        private static ImmutableHashSet<string> _watchedRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(ConfigurationGeneral.SchemaName)
            .Add(ProjectReference.SchemaName)
            .Add(PackageReference.SchemaName);

        //[ImportingConstructor]
        public NuGetRestorer(
            IUnconfiguredProjectVsServices projectVsServices/*,
            IVsSolutionRestoreService solutionRestoreService*/)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            //_solutionRestoreService = solutionRestoreService;
        }

        //[ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        //[AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal async Task OnProjectFactoryCompletedAsync()
        {
            await InitializeCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(3000).ConfigureAwait(false);

            ResetSubscriptions();

            await InitializeAsync().ConfigureAwait(false);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _evaluationSubscriptionLink?.Dispose();
            return Task.CompletedTask;
        }

        private void ResetSubscriptions()
        {
            _evaluationSubscriptionLink?.Dispose();

            var currentProjects = _projectVsServices.Project.LoadedConfiguredProjects;

            if (currentProjects.Any())
            {
                var sourceBlocks = currentProjects.Select(
                    cp => cp.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.SyncLinkOptions<IProjectValueVersions>());

                var target = new ActionBlock<Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary>>(ProjectPropertyChangedAsync);

                _evaluationSubscriptionLink = ProjectDataSources.SyncLinkTo(sourceBlocks.ToImmutableList(), target, null);
            }
        }

        private async Task ProjectPropertyChangedAsync(Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary> sources)
        {
            IVsProjectRestoreInfo projectRestoreInfo = ProjectRestoreInfoBuilder.Build(sources.Item1);

            //await _solutionRestoreService
            //    .NominateProjectAsync(_projectVsServices.Project.FullPath, projectRestoreInfo, CancellationToken.None)
            //    .ConfigureAwait(false);

            await DoNothing(projectRestoreInfo).ConfigureAwait(false);
        }

        private Task DoNothing(IVsProjectRestoreInfo projectRestoreInfo)
        {
            return Task.CompletedTask;
        }
    }

    public class ConfiguredNuGetRestorer
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private readonly IVsSolutionRestoreService _solutionRestoreService;
        private IDisposable _evaluationSubscriptionLink;

        private static ImmutableHashSet<string> _watchedRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(ConfigurationGeneral.SchemaName)
            .Add(ProjectReference.SchemaName)
            .Add(PackageReference.SchemaName);

        [ImportingConstructor]
        public ConfiguredNuGetRestorer(
            ConfiguredProject project,
            IVsSolutionRestoreService solutionRestoreService,
            IProjectSubscriptionService projectSubscriptionService)
        {
            _project = project;
            _projectSubscriptionService = projectSubscriptionService;
            _solutionRestoreService = solutionRestoreService;
        }

        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [ConfiguredProjectAutoLoad]
        public void OnProjectLoaded()
        {
            var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(ProjectPropertyChangedAsync);
            _evaluationSubscriptionLink = _projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                target,
                ruleNames: _watchedRules,
                initialDataAsNew: true,
                suppressVersionOnlyUpdates: true);
        }

        private async Task ProjectPropertyChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> obj)
        {
            IVsProjectRestoreInfo projectRestoreInfo = ProjectRestoreInfoBuilder.Build(obj);

            if (projectRestoreInfo != null)
            {
                await _solutionRestoreService
                    .NominateProjectAsync(_project.UnconfiguredProject.FullPath, projectRestoreInfo, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
