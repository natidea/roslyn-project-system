using Microsoft.VisualStudio.ProjectSystem.Properties;
using NuGet.SolutionRestoreManager;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class DummySubscriber : OnceInitializedOnceDisposedAsync
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private IDisposable _acpssLink;

        private static ImmutableHashSet<string> _watchedRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(ConfigurationGeneral.SchemaName)
            .Add(ProjectReference.SchemaName)
            .Add(PackageReference.SchemaName);

        [ImportingConstructor]
        public DummySubscriber(
            IUnconfiguredProjectVsServices projectVsServices,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal Task OnProjectFactoryCompletedAsync()
        {
            var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedAsync(e));
            _acpssLink = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                target: target,
                ruleNames: _watchedRules,
                //initialDataAsNew: false, // only reset on subsequent changes
                suppressVersionOnlyUpdates: true);

            return Task.CompletedTask;
        }

        private async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            if (IsDisposing || IsDisposed)
                return;

            await InitializeAsync().ConfigureAwait(false);
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _acpssLink?.Dispose();
            return Task.CompletedTask;
        }
    }
}
