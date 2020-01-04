namespace HellBrick.NugetPackaging
{
	internal static partial class StringExtensions
	{
		public static string TrimSuffix( this string text, string suffix )
			=> text.EndsWith( suffix )
				? text.Substring( 0, text.Length - suffix.Length )
				: text;
	}
}
