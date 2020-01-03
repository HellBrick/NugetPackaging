using System.Linq;
using LibGit2Sharp;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace HellBrick.NugetPackaging.Tasks
{
	public class GetCurrentTagTask : Task
	{
		public string? RepositoryFolderPath { get; set; }

		[Output]
		public string? CurrentTag { get; private set; }

		public override bool Execute()
		{
			if ( RepositoryFolderPath is null )
			{
				Log.LogError( $"{nameof( RepositoryFolderPath )} must be specified." );
				return false;
			}

			using Repository repo = new Repository( RepositoryFolderPath );
			CurrentTag = repo.Tags.FirstOrDefault( t => t.Target.Sha == repo.Head.Tip.Sha )?.FriendlyName;
			return true;
		}
	}
}
