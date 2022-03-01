using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNetCore.Pack;
using CodeCake.Abstractions;
using CSemVer;
using SimpleGitVersion;
using System.Collections.Generic;
using System.Linq;
using static CodeCake.Build;

namespace CodeCake
{

    public partial class DotnetSolution : ICIPublishWorkflow
    {
        private ArtifactType? _artifactType;

        public ArtifactType ArtifactType
        {
            get
            {
                if( _artifactType == null ) _artifactType = new NuGetArtifactType( _globalInfo, this );
                return _artifactType;
            }
        }

        public void Pack()
        {
            var nugetInfo = _globalInfo.ArtifactTypes.OfType<NuGetArtifactType>().Single();
            var settings = new DotNetCorePackSettings().AddVersionArguments( _globalInfo.BuildInfo, c =>
            {
                c.NoBuild = true;
                c.IncludeSymbols = true;
                c.Configuration = _globalInfo.BuildInfo.BuildConfiguration;
                c.OutputDirectory = _globalInfo.ReleasesFolder.Path;
            } );
            foreach( var p in nugetInfo.GetNuGetArtifacts() )
            {
                _globalInfo.Cake.Information( p.ArtifactInstance );
                _globalInfo.Cake.DotNetPack( p.Project.Path.FullPath, settings );
            }
        }
    }




    public partial class Build
    {
        /// <summary>
        /// Implements NuGet package handling.
        /// </summary>
        public class NuGetArtifactType : ArtifactType
        {
            readonly DotnetSolution _solution;

            public class NuGetArtifact : ILocalArtifact
            {
                public NuGetArtifact( SolutionProject p, SVersion v )
                {
                    Project = p;
                    ArtifactInstance = new ArtifactInstance( "NuGet", p.Name, v );
                }

                public ArtifactInstance ArtifactInstance { get; }

                public SolutionProject Project { get; }
            }


            public NuGetArtifactType( StandardGlobalInfo globalInfo, DotnetSolution solution )
                : base( globalInfo, "NuGet" )
            {
                _solution = solution;
            }

            /// <summary>
            /// Downcasts the mutable list <see cref="ILocalArtifact"/> as a set of <see cref="NuGetArtifact"/>.
            /// </summary>
            /// <returns>The set of NuGet artifacts.</returns>
            public IEnumerable<NuGetArtifact> GetNuGetArtifacts() => GetArtifacts().Cast<NuGetArtifact>();

            /// <summary>
            /// Gets the remote target feeds.
            /// </summary>
            /// <returns>The set of remote NuGet feeds (in practice at most one).</returns>
            protected override IEnumerable<ArtifactFeed> GetRemoteFeeds()
            {if( GlobalInfo.BuildInfo.Version.PackageQuality <= CSemVer.PackageQuality.Stable ) yield return new SignatureVSTSFeed( this, "Signature-Code","Default", null);
}

            /// <summary>
            /// Gets the local target feeds.
            /// </summary>
            /// <returns>The set of remote NuGet feeds (in practice at most one).</returns>
            protected override IEnumerable<ArtifactFeed> GetLocalFeeds()
            {
                return new NuGetHelper.NuGetFeed[] {
                    new NugetLocalFeed( this, GlobalInfo.LocalFeedPath )
                };
            }

            protected override IEnumerable<ILocalArtifact> GetLocalArtifacts()
            {
                return _solution.ProjectsToPublish.Select( p => new NuGetArtifact( p, GlobalInfo.BuildInfo.Version ) );
            }
        }
    }
}
