using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    public class AltNugetRestorer
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        private ImmutableHashSet<string> _watchedRules;
        private IDisposable _evaluationSubscriptionLink;

        //[ImportingConstructor]
        public AltNugetRestorer(
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

        //[AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        //[ConfiguredProjectAutoLoad]
        public void OnProjectLoaded()
        {
            var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(ProjectPropertyChangedAsync);
            _evaluationSubscriptionLink = this._projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                target,
                ruleNames: _watchedRules,
                initialDataAsNew: true,
                suppressVersionOnlyUpdates: true);
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
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
