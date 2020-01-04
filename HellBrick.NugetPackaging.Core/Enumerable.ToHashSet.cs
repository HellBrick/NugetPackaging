using System.Collections.Generic;

namespace HellBrick.NugetPackaging
{
	internal static partial class EnumerableExtensions
	{
		public static HashSet<T> ToHashSet<T>( this IEnumerable<T> sequence ) => sequence.ToHashSet( EqualityComparer<T>.Default );
		public static HashSet<T> ToHashSet<T>( this IEnumerable<T> sequence, IEqualityComparer<T> comparer ) => new HashSet<T>( sequence, comparer );
	}
}
