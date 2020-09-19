using System;
using System.Collections.Generic;

namespace UsingStatementReformatter.Tests.Extensions
{
	static class EnumerableExtensions
	{
		public static void Apply<T>(this IEnumerable<T> items, Action<T, int> action)
		{
			int index = 0;

			foreach (var item in items)
				action(item, index++);
		}
	}
}
