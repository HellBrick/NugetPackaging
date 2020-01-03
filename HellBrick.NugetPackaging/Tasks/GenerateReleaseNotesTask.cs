using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace HellBrick.NugetPackaging.Tasks
{
	public class GenerateReleaseNotesTask : Task
	{
		public string? RepositoryFolderPath { get; set; }
		public string? GithubApiToken { get; set; }
		public string? GithubApiProduct { get; set; }

		[Output]
		public string? ChangeLog { get; set; }

		public override bool Execute()
		{
			if ( RepositoryFolderPath is null )
			{
				Log.LogError( $"{nameof( RepositoryFolderPath )} must be specified." );
				return false;
			}
			if ( GithubApiToken is null )
			{
				Log.LogError( $"{nameof( GithubApiToken )} must be specified." );
				return false;
			}
			if ( GithubApiProduct is null )
			{
				Log.LogError( $"{nameof( GithubApiProduct )} must be specified." );
				return false;
			}

			string changeLog
				= ChangeLogBuilder
				.BuildAsync
				(
					RepositoryFolderPath,
					GithubApiToken,
					GithubApiProduct,
					issue => $"{String.Join( " ", issue.Labels.Select( lbl => $"[{lbl}]" ) )} {issue.Title}"
				)
				.GetAwaiter()
				.GetResult();

			return true;
		}
	}
}
