using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;
using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    [ProjectSystemTrait]
    public class ProjectRestoreInfoBuilderTests
    {
        [Fact]
        public void DummyTest()
        {
            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""Something"" ]
            }
        }
    }
}");

            var projectVersionedValue = IProjectVersionedValueFactory.Implement(projectSubscriptionUpdate);

            Assert.Equal(projectSubscriptionUpdate, projectVersionedValue.Value);
        }

        [Fact]
        public void ProjectRestoreInfoBuilder_NullUpdate_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("updates", () => {
                ProjectRestoreInfoBuilder.Build(null);
            });
        }

        [Fact]
        public void ProjectRestoreInfoBuilder_NoProjectChanges_ReturnsNull()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            }
        }
    }
}");
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);
            Assert.Null(restoreInfo);
        }

        [Fact]
        public void ProjectRestoreInfoBuilder_WithAnyChanges_ReturnsFullRestoreInfo()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Properties"": {
                   ""BaseIntermediateOutputPath"": ""obj\\"",
                   ""TargetFrameworkMoniker"": "".NETCoreApp,Version=v1.0"",
                   ""TargetFrameworkIdentifier"": "".NETCoreApp"",
                   ""TargetFrameworkVersion"": ""v1.0"",
                   ""TargetFrameworks"": ""netcoreapp1.0"",
                   ""Configuration"": ""Debug"",
                   ""Platform"": ""AnyCPU"",
                   ""OutputPath"": ""bin\\Debug\\netcoreapp1.0\\"",
                   ""OutputType"": ""Exe"",
                   ""MSBuildProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                   ""IntermediateOutputPath"": ""obj\\Debug\\netcoreapp1.0\\""
                }
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": {
                    ""Microsoft.NETCore.Sdk"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj"",
                        ""Version"": ""1.0.0-alpha-20161007-5"",
                        ""TargetFramework"": """",
                        ""RuntimeIdentifier"": """"
                    },
                    ""Microsoft.NETCore.App"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj"",
                        ""Version"": ""1.0.1"",
                        ""TargetFramework"": """",
                        ""RuntimeIdentifier"": """"
                    }
                }
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""true"",
                ""AddedItems"": [ ""..\\TestLib\\TestLib.csproj"" ]
            },
            ""After"": {
                ""Items"": {
                    ""..\\TestLib\\TestLib.csproj"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj""
                    }
                }
            }
        }
    }
}");
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);

            Assert.NotNull(restoreInfo);
            Assert.Equal(@"obj\", restoreInfo.BaseIntermediatePath);

            // just one target framework
            Assert.Equal(1, restoreInfo.TargetFrameworks.Count);
            restoreInfo.TargetFrameworks.Item(0);
            var tfm = restoreInfo.TargetFrameworks.Item(".NETCoreApp,Version=v1.0"); // equals above
            restoreInfo.TargetFrameworks.Item("InvalidFrameworkMoniker"); // returns null

            Assert.Equal(".NETCoreApp,Version=v1.0", tfm.TargetFrameworkMoniker);
            Assert.Equal(2, tfm.PackageReferences.Count);
            Assert.Equal(1, tfm.ProjectReferences.Count);
        }

        private ImmutableList<IProjectVersionedValue<IProjectSubscriptionUpdate>> GetVersionedUpdatesFromJson(
            params string[] jsonStrings) =>
                jsonStrings
                    .Select(s => IProjectSubscriptionUpdateFactory.FromJson(s))
                    .Select(u => IProjectVersionedValueFactory.Implement(u))
                    .ToImmutableList();
    }
}
