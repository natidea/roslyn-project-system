using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    using TVersionedUpdate = IProjectVersionedValue<IProjectSubscriptionUpdate>;
    using TVersionedUpdateList = ImmutableList<IProjectVersionedValue<IProjectSubscriptionUpdate>>;
    using TIdentityDictionary = IImmutableDictionary<NamedIdentity, IComparable>;
    using System.Threading;

    public class NugetRestorer
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        private ImmutableHashSet<string> _watchedRules;
        private IDisposable _evaluationSubscriptionLink;

        [ImportingConstructor]
        public NugetRestorer(
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

    public class UnconfiguredNugetRestorer
    {
        private readonly UnconfiguredProject _project;
        private readonly IVsSolutionRestoreService _solutionRestoreService;
        private ImmutableHashSet<string> _watchedRules;
        private IDisposable _evaluationSubscriptionLink;        

        [ImportingConstructor]
        public UnconfiguredNugetRestorer(UnconfiguredProject project)
        {
            // TODO MEF import IVsSolutionRestoreService
            _solutionRestoreService = null;

            _project = project;
            _watchedRules = Empty.OrdinalIgnoreCaseStringSet
                                 .Add(ConfigurationGeneral.SchemaName)
                                 .Add(ProjectReference.SchemaName)
                                 .Add(PackageReference.SchemaName);
        }

        /// <summary>
        /// Each time a configured project is loaded, update subcriptions
        /// </summary>
        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        public async Task OnProjectLoaded()
        {
            TraceSubscriptions();

            _project.Services.ProjectConfigurationsService.Added += ProjectConfigurationsService_Added;

            await Task.CompletedTask.ConfigureAwait(false);
        }

        private Task ProjectConfigurationsService_Added(Object sender, ProjectConfigurationChangeEventArgs args)
        {
            TraceUtilities.TraceVerbose($"added {args.NewProjectConfiguration}");
            return Task.CompletedTask;
        }

        //[AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        //[ConfiguredProjectAutoLoad]
        //public async Task OnConfiguredProjectLoaded()
        //{
        //    if (isLoaded)
        //    {
        //        TraceSubscriptions();
        //    }

        //    await Task.CompletedTask.ConfigureAwait(false);
        //}

        private void TraceSubscriptions()
        {
            var currentProjects = _project.LoadedConfiguredProjects;
            foreach (var project in currentProjects)
            {
                TraceUtilities.TraceVerbose($"project seen {project.ProjectConfiguration.Name}");
            }
            ResetSubscriptions();
        }

        private void ResetSubscriptions()
        {
            if (_evaluationSubscriptionLink != null)
            {
                _evaluationSubscriptionLink.Dispose();
            }

            var currentProjects = _project.LoadedConfiguredProjects;

            var sourceBlocks = currentProjects.Select(
                cp => cp.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.SyncLinkOptions<IProjectValueVersions>());

            var target = new ActionBlock<Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary>>(ProjectPropertyChangedAsync);

            _evaluationSubscriptionLink = ProjectDataSources.SyncLinkTo(sourceBlocks.ToImmutableList(), target, null);
        }

        private async Task ProjectPropertyChangedAsync(Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary> sources)
        {
            foreach (TVersionedUpdate item in sources.Item1)
            {
                var configName = item.Value.ProjectConfiguration.Name;
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

                    TraceUtilities.TraceVerbose($"tfm seen {targetFrameworkMoniker} in {intermediateOutputPath}");
                }

            }

            IVsProjectRestoreInfo projectRestoreInfo = ProjectRestoreInfoBuilder.Build(sources.Item1);
            /*await*/
            _solutionRestoreService
      ?.NominateProjectAsync(_project.FullPath, projectRestoreInfo, CancellationToken.None)
      ?.ConfigureAwait(false);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    internal static class ProjectRestoreInfoBuilder
    {
        internal static IVsProjectRestoreInfo Build(ImmutableList<IProjectValueVersions> updates)
        {
            string baseIntermediatePath = null;
            var targetFrameworks = new TargetFrameworks();
            
            foreach (TVersionedUpdate update in updates)
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
