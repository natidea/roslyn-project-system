// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Threading;
using NuGet.SolutionRestoreManager;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    using TIdentityDictionary = IImmutableDictionary<NamedIdentity, IComparable>;

    internal class NugetRestorer : OnceInitializedOnceDisposedAsync
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private IVsSolutionRestoreService _solutionRestoreService;
        private ImmutableHashSet<string> _watchedRules;
        private IDisposable _evaluationSubscriptionLink;        

        [ImportingConstructor]
        public NugetRestorer(
            IUnconfiguredProjectVsServices projectVsServices,
            IVsSolutionRestoreService solutionRestoreService) 
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));

            _solutionRestoreService = solutionRestoreService;

            _projectVsServices = projectVsServices;
            _watchedRules = Empty.OrdinalIgnoreCaseStringSet
                                 .Add(ConfigurationGeneral.SchemaName)
                                 .Add(ProjectReference.SchemaName)
                                 .Add(PackageReference.SchemaName);
        }

        public UnconfiguredProject Project
        {
            get
            {
                return _projectVsServices.Project;
            }
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal async Task OnProjectFactoryCompletedAsync()
        {
            await InitializeCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            ResetSubscriptions();

            await TplExtensions.CompletedTask.ConfigureAwait(false);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _evaluationSubscriptionLink?.Dispose();
            return TplExtensions.CompletedTask;
        }

        private void ResetSubscriptions()
        {
            _evaluationSubscriptionLink?.Dispose();

            var currentProjects = Project.LoadedConfiguredProjects;

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

            await _solutionRestoreService
                .NominateProjectAsync(_projectVsServices.Project.FullPath, projectRestoreInfo, CancellationToken.None)
                .ConfigureAwait(false);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
