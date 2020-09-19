using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UsingStatementReformatter
{
	public class UsingStatementFormatter
	{
		public static void Format(TextWriter output, IEnumerable<UsingStatement> usingStatements, IEnumerable<GroupingRule> groupingRules)
		{
			var usingStatementList = usingStatements.ToList();
			var matchingStatements = new List<UsingStatement>();

			// Allow grouping rules to claim using statements in order.
			foreach (var groupingRule in groupingRules)
			{
				matchingStatements.Clear();

				var remainder = new List<UsingStatement>();

				foreach (var statement in usingStatementList)
				{
					if (groupingRule.IsMatch(statement))
						matchingStatements.Add(statement);
					else
						remainder.Add(statement);
				}

				usingStatementList = remainder;

				if (matchingStatements.Any())
					groupingRule.Format(output, matchingStatements);

				if (!usingStatementList.Any())
					break;

				output.WriteLine();
			}

			// Anything not claimed by a rule gets output in a single final block.
			foreach (var straggler in usingStatementList.OrderBy(s => s.Namespace))
				output.WriteLine(straggler);
		}
	}
}
