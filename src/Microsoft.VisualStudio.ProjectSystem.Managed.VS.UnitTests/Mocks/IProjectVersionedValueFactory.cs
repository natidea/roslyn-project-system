using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectVersionedValueFactory
    {
        public static IProjectVersionedValue<T> Create<T>()
        {
            return Mock.Of<IProjectVersionedValue<T>>();
        }

        public static IProjectVersionedValue<T> Implement<T>(
                        T value = default(T),
                        MockBehavior mockBehavior = MockBehavior.Default)
        {
            var mock = new Mock<IProjectVersionedValue<T>>(mockBehavior);

            mock.Setup(x => x.Value).Returns(value);

            return mock.Object;
        }
    }
}
