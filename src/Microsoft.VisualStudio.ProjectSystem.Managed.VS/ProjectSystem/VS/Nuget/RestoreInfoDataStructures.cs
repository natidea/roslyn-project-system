using NuGet.SolutionRestoreManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Nuget
{
    internal abstract class ItemList<T> : List<T>
    {
        public ItemList() : base() { }

        public ItemList(IEnumerable<T> collection): base(collection) { }

        public abstract string GetName(T value);

        public T Item(Object index)
        {
            if (index is string)
            {
                return this.FirstOrDefault(v => GetName(v) == (string)index);
            }
            else if (index is int)
            {
                return this[(int)index];
            }
            else
            {
                return default(T);
            }
        }
    }

    internal class ProjectRestoreInfo : IVsProjectRestoreInfo
    {
        public String BaseIntermediatePath { get; set; }

        public IVsTargetFrameworks TargetFrameworks { get; set; }
    }

    internal class TargetFrameworks : ItemList<IVsTargetFrameworkInfo>, IVsTargetFrameworks
    {
        public TargetFrameworks(): base() { }

        public TargetFrameworks(IEnumerable<IVsTargetFrameworkInfo> collection) : base(collection) { }

        public override String GetName(IVsTargetFrameworkInfo value) => value.TargetFrameworkMoniker;
    }

    internal class TargetFrameworkInfo : IVsTargetFrameworkInfo
    {
        public IVsReferenceItems PackageReferences { get; set; }

        public IVsReferenceItems ProjectReferences { get; set; }

        public String TargetFrameworkMoniker { get; set; }
    }

    internal class ReferenceItems : ItemList<IVsReferenceItem>, IVsReferenceItems
    {
        public ReferenceItems(): base() { }

        public ReferenceItems(IEnumerable<IVsReferenceItem> collection) : base(collection) { }

        public override String GetName(IVsReferenceItem value) => value.Name;
    }

    internal class ReferenceItem : IVsReferenceItem
    {
        public String Name { get; set; }

        public IVsReferenceProperties Properties { get; set; }
    }

    internal class ReferenceProperties : ItemList<IVsReferenceProperty>, IVsReferenceProperties
    {
        public ReferenceProperties(): base() { }

        public ReferenceProperties(IEnumerable<IVsReferenceProperty> collection) : base(collection) { }

        public override String GetName(IVsReferenceProperty value) => value.Name;
    }

    internal class ReferenceProperty : IVsReferenceProperty
    {
        public String Name { get; set; }

        public String Value { get; set; }
    }
}
