using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HellBrick.NugetPackaging
{
	public static class ChangeLogBuilder
	{
		private static readonly Regex _issueIdRegex = new Regex( @"(?<=#)\d+", RegexOptions.Singleline );

		public static async Task<string> BuildAsync
		(
			string repositoryFolderPath,
			string githubApiToken,
			string githubApiProduct,
			Func<Octokit.Issue, string> issueFormatter
		)
		{
			GithubRepoInfo repoInfo = GithubRepoInfo.FromRepositoryFolderPath( repositoryFolderPath );
			IReadOnlyCollection<string> commitMessages = GetCommitMessagesSinceLastTag( repositoryFolderPath );

			IReadOnlyList<Octokit.Issue> allIssues = await GetIssuesAsync( githubApiToken, githubApiProduct, repoInfo ).ConfigureAwait( false );
			Dictionary<int, Octokit.Issue> issuesByNumber = allIssues.ToDictionary( i => i.Number );

			IReadOnlyList<Octokit.PullRequest> allPullRequests = await GetPullRequestsAsync( githubApiToken, githubApiProduct, repoInfo ).ConfigureAwait( false );
			Dictionary<int, Octokit.PullRequest> pullRequestsByNumber = allPullRequests.ToDictionary( pr => pr.Number );

			return commitMessages
				.SelectMany( commitMessage => ParseIssueOrPrNumbers( commitMessage ) )
				.SelectMany( issueOrPrNumber => EnumerateIssueNumbers( issueOrPrNumber ) )
				.Distinct()
				.Select( issueNumber => issuesByNumber[ issueNumber ] )
				.Select( issueFormatter )
				.Aggregate
				(
					new StringBuilder(),
					( builder, issueText ) => builder.AppendLine( issueText ),
					builder => builder.ToString()
				);

			IEnumerable<int> EnumerateIssueNumbers( int issueOrPrNumber )
				=> pullRequestsByNumber.TryGetValue( issueOrPrNumber, out Octokit.PullRequest pr ) ? ParseIssueOrPrNumbers( pr.Body )
				: issuesByNumber.TryGetValue( issueOrPrNumber, out Octokit.Issue issue ) ? new[] { issue.Number }
				: Enumerable.Empty<int>();
		}

		private static IReadOnlyCollection<string> GetCommitMessagesSinceLastTag( string repositoryFolderPath )
		{
			using LibGit2Sharp.Repository localRepo = new LibGit2Sharp.Repository( repositoryFolderPath );

			return
				EnumerateCommitsUntilPreviousTagIsReached( localRepo.Head.Tip )
				.Reverse()
				.Select( commit => commit.Message )
				.ToList();

			IEnumerable<LibGit2Sharp.Commit> EnumerateCommitsUntilPreviousTagIsReached( LibGit2Sharp.Commit startingCommit )
			{
				HashSet<string> previousTaggedCommitShas
					= localRepo
					.Tags
					.Select( tag => tag.Target.Sha )
					.Except( new[] { startingCommit.Sha } )
					.ToHashSet();

				LibGit2Sharp.Commit? currentCommit = startingCommit;

				while ( currentCommit is object )
				{
					if ( previousTaggedCommitShas.Contains( currentCommit.Sha ) )
						break;

					yield return currentCommit;

					List<LibGit2Sharp.Commit> parents = currentCommit.Parents.ToList();
					currentCommit = parents.Count switch
					{
						0 => null,
						1 => parents[ 0 ],
						_ => parents[ 1 ],
					};
				}
			}
		}

		private static Task<IReadOnlyList<Octokit.Issue>> GetIssuesAsync( string githubApiToken, string githubApiProduct, GithubRepoInfo repoInfo )
			=> CreateGithubClient( githubApiToken, githubApiProduct )
			.Issue
			.GetAllForRepository
			(
				repoInfo.Owner,
				repoInfo.Repository,
				new Octokit.RepositoryIssueRequest() { State = Octokit.ItemStateFilter.All }
			);

		private static Task<IReadOnlyList<Octokit.PullRequest>> GetPullRequestsAsync( string githubApiToken, string githubApiProduct, GithubRepoInfo repoInfo )
			=> CreateGithubClient( githubApiToken, githubApiProduct )
			.PullRequest
			.GetAllForRepository
			(
				repoInfo.Owner,
				repoInfo.Repository,
				new Octokit.PullRequestRequest() { State = Octokit.ItemStateFilter.All }
			);

		private static Octokit.GitHubClient CreateGithubClient( string githubApiToken, string githubApiProduct )
			=> new Octokit.GitHubClient( new Octokit.ProductHeaderValue( githubApiProduct ) )
			{
				Credentials = new Octokit.Credentials( githubApiToken )
			};

		private static IEnumerable<int> ParseIssueOrPrNumbers( string text )
			=> _issueIdRegex
			.Matches( text )
			.OfType<Match>()
			.Select( match => Int32.Parse( match.Value ) );
	}
}
