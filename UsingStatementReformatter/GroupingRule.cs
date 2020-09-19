using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UsingStatementReformatter
{
	public class GroupingRule
	{
		public List<string> MatchList = new List<string>();
		public SubgroupingBehaviour SubgroupingBehaviour;

		public bool IsMatch(UsingStatement statement)
		{
			foreach (var match in MatchList)
			{
				if (match == "*")
					return true;

				if (statement.Namespace.StartsWith(match)
				 && ((statement.Namespace.Length == match.Length) || (statement.Namespace[match.Length] == '.')))
					return true;
			}

			return false;
		}

		public void Format(TextWriter output, IEnumerable<UsingStatement> statements)
		{
			switch (SubgroupingBehaviour)
			{
				case SubgroupingBehaviour.MatchRootGroup:
				{
					foreach (var statement in statements.OrderBy(statement => statement.Namespace))
						output.WriteLine(statement);

					break;
				}
				case SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock:
				case SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks:
				{
					var groups = new SortedDictionary<string, List<UsingStatement>>();

					foreach (var statement in statements)
					{
						string group = IdentifyGroup(statement);

						if (!groups.TryGetValue(group, out var groupList))
							groupList = groups[group] = new List<UsingStatement>();

						groupList.Add(statement);
					}

					switch (SubgroupingBehaviour)
					{
						case SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock:
						{
							var allStatements = groups.SelectMany(group => group.Value);

							foreach (var statement in allStatements.OrderBy(statement => statement.Namespace))
								output.WriteLine(statement);

							break;
						}
						case SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks:
						{
							bool isFirstGroup = true;

							foreach (var group in groups)
							{
								if (isFirstGroup)
									isFirstGroup = false;
								else
									output.WriteLine();

								foreach (var statement in group.Value.OrderBy(statement => statement.Namespace))
									output.WriteLine(statement);
							}

							break;
						}
					}

					break;
				}
			}
		}

		public string IdentifyGroup(UsingStatement statement)
		{
			string bestMatch = null;
			int bestMatchDepth = -1;

			switch (SubgroupingBehaviour)
			{
				case SubgroupingBehaviour.MatchRootGroup:
				case SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock:
				{
					// Direct match: If namespace name starts with the match text, then the group name is the match text.

					foreach (var match in MatchList)
					{
						if (statement.Namespace.StartsWith(match)
						 && ((statement.Namespace.Length == match.Length) || (statement.Namespace[match.Length] == '.')))
						{
							int matchDepth = match.Count(ch => ch == '.');

							if (matchDepth > bestMatchDepth)
							{
								bestMatch = match + ".";
								bestMatchDepth = matchDepth;
							}
						}
					}

					break;
				}
				case SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks:
				{
					// Subgroups match for a custom root namespace: If namespace name starts with the match text, then
					// the group name is one component more of the namespace, e.g. "A.B.C.D" matched by "A.B" produces
					// group name "A.B.C". If the namespace name starts with the match text but doesn't have another
					// component, then it isn't a match.

					foreach (var match in MatchList)
					{
						if (statement.Namespace.StartsWith(match)
						 && (statement.Namespace.Length > match.Length)
						 && (statement.Namespace[match.Length] == '.'))
						{
							int nextSeparator = statement.Namespace.IndexOf('.', match.Length + 1);

							string matchWithSeparator;

							if (nextSeparator >= 0)
								matchWithSeparator = statement.Namespace.Substring(0, nextSeparator + 1);
							else
								matchWithSeparator = statement.Namespace + '.';

							int matchDepth = matchWithSeparator.Count(ch => ch == '.');

							if (matchDepth > bestMatchDepth)
							{
								bestMatch = matchWithSeparator;
								bestMatchDepth = matchDepth;
							}
						}
					}

					break;
				}
			}

			if (bestMatch != null)
				return bestMatch;
			else
			{
				int separatorIndex = statement.Namespace.IndexOf('.');

				if (separatorIndex >= 0)
					return statement.Namespace.Substring(0, separatorIndex + 1);
				else
					return statement.Namespace + '.';
			}
		}

		const string IdentifierExpression = @"@?[_\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}][\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]*";

		static readonly string MultipartIdentifierExpression = $@"{IdentifierExpression}(\.{IdentifierExpression})*";
		static readonly string MultipartIdentifierOrWildcardExpression = $@"({MultipartIdentifierExpression}|\*)";

		static readonly Regex ParsingRegex = new Regex(
			pattern: $@"
 (?<Literal>{MultipartIdentifierExpression})
 |
 \*(?<SeparateGroups>\*)?
   (
     \s*\(\s*
       (
         (?<Match>{MultipartIdentifierOrWildcardExpression})
         \s*,\s*
       )*
       (?<Match>{MultipartIdentifierOrWildcardExpression})
     \s*\)\s*
   )?",
			RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

		public static IEnumerable<GroupingRule> Parse(string formatString)
		{
			int startAt = 0;

			while (startAt < formatString.Length)
			{
				while ((startAt < formatString.Length) && (char.IsWhiteSpace(formatString, startAt) || (formatString[startAt] == ';')))
					startAt++;

				if (startAt >= formatString.Length)
					yield break;

				var match = ParsingRegex.Match(formatString, startAt);

				if (!match.Success || (match.Index > startAt))
					throw new FormatException("Couldn't understand using statement configuration at character " + startAt);

				var rule = new GroupingRule();

				if (match.Groups.TryGetValue("Literal", out var literalIdentifier) && literalIdentifier.Success)
				{
					rule.SubgroupingBehaviour = SubgroupingBehaviour.MatchRootGroup;
					rule.MatchList.Add(literalIdentifier.Value);
				}
				else
				{
					if (match.Groups["SeparateGroups"].Success)
						rule.SubgroupingBehaviour = SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks;
					else
						rule.SubgroupingBehaviour = SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock;

					if (!match.Groups.TryGetValue("Match", out var matchGroup) || !matchGroup.Success)
						rule.MatchList.Add("*");
					else
						rule.MatchList.AddRange(matchGroup.Captures.Select(capture => capture.Value));
				}

				yield return rule;

				startAt += match.Length;
			}
		}
	}
}
