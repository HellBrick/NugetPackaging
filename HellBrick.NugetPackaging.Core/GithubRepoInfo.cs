using System;
using System.Linq;
using LibGit2Sharp;

namespace HellBrick.NugetPackaging
{
	public readonly struct GithubRepoInfo : IEquatable<GithubRepoInfo>
	{
		public static GithubRepoInfo FromRepositoryFolderPath( string repositoryFolderPath )
		{
			using Repository localRepo = new Repository( repositoryFolderPath );
			return
				localRepo
				.Network
				.Remotes
				.Select( remote => new Uri( remote.Url ) )
				.Where( url => url.Host == "github.com" )
				.Select( url => FromRemoteUrl( url ) )
				.Single();
		}

		public static GithubRepoInfo FromRemoteUrl( Uri githubRemoteUrl )
			=> new GithubRepoInfo
			(
				owner: githubRemoteUrl.Segments[ 1 ].Trim( '/' ),
				repository: githubRemoteUrl.Segments[ 2 ].TrimSuffix( ".git" )
			);

		public GithubRepoInfo( string owner, string repository )
		{
			Owner = owner;
			Repository = repository;
		}

		public string Owner { get; }
		public string Repository { get; }

		public override string ToString() => $"{Owner}/{Repository}";

		public override int GetHashCode() => (Owner, Repository).GetHashCode();
		public bool Equals( GithubRepoInfo other ) => (Owner, Repository) == (other.Owner, other.Repository);
		public override bool Equals( object obj ) => obj is GithubRepoInfo other && Equals( other );

		public static bool operator ==( GithubRepoInfo x, GithubRepoInfo y ) => x.Equals( y );
		public static bool operator !=( GithubRepoInfo x, GithubRepoInfo y ) => !x.Equals( y );
	}
}
