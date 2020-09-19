using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using NUnit.Framework;

using FluentAssertions;

using UsingStatementReformatter.Tests.Extensions;

namespace UsingStatementReformatter.Tests
{
	public class GroupingRuleTests
	{
		#region IsMatch
		public enum MatchingEntryIndex
		{
			First,
			Middle,
			Last,
		}

		[Test]
		public void IsMatch_should_return_false_when_no_match_list()
		{
			// Arrange
			var rule = new GroupingRule();

			var usingStatement = new UsingStatement() { Namespace = "Foo" };

			// Act
			var result = rule.IsMatch(usingStatement);

			// Assert
			result.Should().BeFalse();
		}

		[Test]
		public void IsMatch_should_return_false_when_no_matches()
		{
			// Arrange
			var rule = new GroupingRule();

			rule.MatchList.Add("Foo");
			rule.MatchList.Add("Bar");
			rule.MatchList.Add("Qux");

			var usingStatement = new UsingStatement() { Namespace = "Eenp" };

			// Act
			var result = rule.IsMatch(usingStatement);

			// Assert
			result.Should().BeFalse();
		}

		[Test]
		public void IsMatch_should_return_true_when_exact_match([Values] MatchingEntryIndex matching)
		{
			// Arrange
			var rule = new GroupingRule();

			rule.MatchList.Add("Foo");
			rule.MatchList.Add("Bar");
			rule.MatchList.Add("Qux");

			var usingStatement = new UsingStatement() { Namespace = rule.MatchList[(int)matching] };

			// Act
			var result = rule.IsMatch(usingStatement);

			// Assert
			result.Should().BeTrue();
		}

		[Test]
		public void IsMatch_should_return_true_when_prefix_match([Values] MatchingEntryIndex matching)
		{
			// Arrange
			var rule = new GroupingRule();

			rule.MatchList.Add("Foo");
			rule.MatchList.Add("Bar");
			rule.MatchList.Add("Qux");

			var usingStatement = new UsingStatement() { Namespace = rule.MatchList[(int)matching] + ".Common" };

			// Act
			var result = rule.IsMatch(usingStatement);

			// Assert
			result.Should().BeTrue();
		}
		#endregion

		#region Parse
		[TestCase("")]
		[TestCase(" ")]
		[TestCase("     ")]
		[TestCase(";")]
		[TestCase(";   ")]
		[TestCase("   ;   ")]
		[TestCase("   ;")]
		[TestCase(";   ;")]
		[TestCase("   ;   ;   ")]
		public void Parse_should_parse_empty_string_as_empty_sequence(string testInput)
		{
			// Act
			var rules = GroupingRule.Parse(testInput);

			rules = rules.ToList();

			// Assert
			rules.Should().BeEmpty();
		}

		[TestCase("Test", "Test")]
		[TestCase("   Test", "Test")]
		[TestCase("Test   ", "Test")]
		[TestCase("Test.Foo", "Test.Foo")]
		[TestCase("Test.Foo.Bar", "Test.Foo.Bar")]
		[TestCase("   Test.Foo.Bar", "Test.Foo.Bar")]
		[TestCase("Test.Foo.Bar   ", "Test.Foo.Bar")]
		public void Parse_should_parse_direct_namespace_name(string testInput, string expectedParsedNamespace)
		{
			// Act
			var rules = GroupingRule.Parse(testInput);

			rules = rules.ToList();

			// Assert
			rules.Should().HaveCount(1);

			rules.Single().MatchList.Should().HaveCount(1);
			rules.Single().MatchList.Should().HaveElementAt(index: 0, expectedParsedNamespace);

			rules.Single().SubgroupingBehaviour.Should().Be(SubgroupingBehaviour.MatchRootGroup);
		}

		[TestCase("*", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase("   *", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase("   *   ", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase("*   ", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase("**", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks)]
		[TestCase("   **", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks)]
		[TestCase("   **   ", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks)]
		[TestCase("**   ", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks)]
		public void Parse_should_parse_wildcard_with_no_matchlist(string testInput, SubgroupingBehaviour expectedSubgroupingBehaviour)
		{
			// Act
			var rules = GroupingRule.Parse(testInput);

			rules = rules.ToList();

			// Assert
			rules.Should().HaveCount(1);

			var rule = rules.Single();

			rule.MatchList.Should().HaveCount(1);
			rule.MatchList.Should().HaveElementAt(index: 0, "*");

			rule.SubgroupingBehaviour.Should().Be(expectedSubgroupingBehaviour);
		}

		[TestCase("*(*)", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "*" })]
		[TestCase(" * ( *  ) ", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "*" })]
		[TestCase("*(A)", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "A" })]
		[TestCase(" * ( A ) ", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "A" })]
		[TestCase("*(A,*)", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "A", "*" })]
		[TestCase(" * ( A , * ) ", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "A", "*" })]
		[TestCase("*(A,B)", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "A", "B" })]
		[TestCase(" * ( A , B ) ", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "A", "B" })]
		[TestCase("*(*,B)", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "*", "B" })]
		[TestCase(" * ( * , B ) ", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock, new[] { "*", "B" })]
		[TestCase("**(*)", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "*" })]
		[TestCase(" ** ( *  ) ", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "*" })]
		[TestCase("**(A)", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "A" })]
		[TestCase(" ** ( A ) ", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "A" })]
		[TestCase("**(A,*)", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "A", "*" })]
		[TestCase(" ** ( A , * ) ", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "A", "*" })]
		[TestCase("**(A,B)", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "A", "B" })]
		[TestCase(" ** ( A , B ) ", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "A", "B" })]
		[TestCase("**(*,B)", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "*", "B" })]
		[TestCase(" ** ( * , B ) ", SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks, new[] { "*", "B" })]
		public void Parse_should_parse_wildcard_with_matchlist(string testInput, SubgroupingBehaviour expectedSubgroupingBehaviour, string[] expectedMatchList)
		{
			// Act
			var rules = GroupingRule.Parse(testInput);

			rules = rules.ToList();

			// Assert
			rules.Should().HaveCount(1);

			var rule = rules.Single();

			rule.MatchList.Should().BeEquivalentTo(expectedMatchList);

			rule.SubgroupingBehaviour.Should().Be(expectedSubgroupingBehaviour);
		}

		[TestCase("System;**(Test);*(A,B);**")]
		[TestCase(" System ; ** ( Test ) ; * ( A , B ) ; ** ")]
		[TestCase("  System  ;  **  (  Test  )  ;  *  (  A  ,  B  )  ;  **  ")]
		[TestCase("\t\n\vSystem\t\n\v;\t\n\v**\t\n\v(\t\n\vTest\t\n\v)\t\n\v;\t\n\v*\t\n\v(\t\n\vA\t\n\v,\t\n\vB\t\n\v)\t\n\v;\t\n\v**\t\n\v")]
		public void Parse_should_handle_multiple_rules(string testInput)
		{
			// Act
			var rules = GroupingRule.Parse(testInput)
				.ToList();

			// Assert
			rules.Should().HaveCount(4);

			rules[0].MatchList.Should().BeEquivalentTo(new[] { "System" });
			rules[0].SubgroupingBehaviour.Should().Be(SubgroupingBehaviour.MatchRootGroup);

			rules[1].MatchList.Should().BeEquivalentTo(new[] { "Test" });
			rules[1].SubgroupingBehaviour.Should().Be(SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks);

			rules[2].MatchList.Should().BeEquivalentTo(new[] { "A", "B" });
			rules[2].SubgroupingBehaviour.Should().Be(SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock);

			rules[3].MatchList.Should().BeEquivalentTo(new[] { "*" });
			rules[3].SubgroupingBehaviour.Should().Be(SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks);
		}
		#endregion

		#region Format
		class OutputBuffer : StringWriter
		{
			public string[] GetLines()
			{
				var reader = new StringReader(this.ToString());

				List<string> lines = new List<string>();

				while (true)
				{
					string line = reader.ReadLine();

					if (line == null)
						break;

					lines.Add(line);
				}

				return lines.ToArray();
			}
		}

		[TestCase("**(Foo,*)", new[] { "Foo.A", "Foo.B", "Foo.A.A", "Foo.B.A" }, new[] { "Foo.A", "Foo.A.A", "", "Foo.B", "Foo.B.A" })]
		[TestCase("  **  (  Foo  ,  *  )  ", new[] { "Foo.A", "Foo.B", "Foo.A.A", "Foo.B.A" }, new[] { "Foo.A", "Foo.A.A", "", "Foo.B", "Foo.B.A" })]
		public void Format_should_separate_groups_properly_when_formatting_with_custom_match(string testInput, string[] inputUsingNamespaces, string[] expectedOutputNamespaces)
		{
			// Arrange
			var inputUsings = inputUsingNamespaces.Select(ns => new UsingStatement() { Namespace = ns });

			var rule = GroupingRule.Parse(testInput).Single();

			var output = new OutputBuffer();

			// Act
			rule.Format(output, inputUsings);

			// Assert
			var expectedUsings = expectedOutputNamespaces.Select(ns => (ns == "") ? "" : $"using {ns};");

			output
				.GetLines()
				.Zip(expectedUsings)
				.Apply(
					((string Actual, string Expected) pair, int index) =>
					{
						pair.Actual.Should().Be(pair.Expected, because: $"line {index} should match");
					});
		}
		#endregion

		#region IdentifyGroup
		[TestCase("Foo", SubgroupingBehaviour.MatchRootGroup)]
		[TestCase("Foo.Bar", SubgroupingBehaviour.MatchRootGroup)]
		[TestCase("Foo", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase("Foo.Bar", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		public void IdentifyGroup_should_identify_root_group(string importedNamespace, SubgroupingBehaviour subgroupingBehaviour)
		{
			// Arrange
			var rule = new GroupingRule();

			rule.SubgroupingBehaviour = subgroupingBehaviour;

			rule.MatchList.Add(importedNamespace);

			var usingStatement = new UsingStatement() { Namespace = importedNamespace };

			// Act
			var result = rule.IdentifyGroup(usingStatement);

			// Assert
			result.Should().Be(importedNamespace + ".");
		}

		[TestCase(new[] { "Foo" }, "Foo", SubgroupingBehaviour.MatchRootGroup)]
		[TestCase(new[] { "Foo", "Foo.Bar" }, "Foo.Bar", SubgroupingBehaviour.MatchRootGroup)]
		[TestCase(new[] { "Foo.Bar", "Foo" }, "Foo.Bar", SubgroupingBehaviour.MatchRootGroup)]
		[TestCase(new[] { "Foo", "Foo.Bar.Qux", "Foo.Bar" }, "Foo.Bar.Qux", SubgroupingBehaviour.MatchRootGroup)]
		[TestCase(new[] { "Foo" }, "Foo", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase(new[] { "Foo", "Foo.Bar" }, "Foo.Bar", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase(new[] { "Foo.Bar", "Foo" }, "Foo.Bar", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase(new[] { "Foo", "Foo.Bar.Qux", "Foo.Bar" }, "Foo.Bar.Qux", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		public void IdentifyGroup_should_identify_deepest_root_group(string[] matchStrings, string importedNamespace, SubgroupingBehaviour subgroupingBehaviour)
		{
			// Arrange
			var rule = new GroupingRule();

			rule.SubgroupingBehaviour = subgroupingBehaviour;

			rule.MatchList.AddRange(matchStrings);

			var usingStatement = new UsingStatement() { Namespace = importedNamespace };

			// Act
			var result = rule.IdentifyGroup(usingStatement);

			// Assert
			result.Should().Be(importedNamespace + ".");
		}

		[TestCase("Foo", SubgroupingBehaviour.MatchRootGroup)]
		[TestCase("Foo.Bar", SubgroupingBehaviour.MatchRootGroup)]
		[TestCase("Foo", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		[TestCase("Foo.Bar", SubgroupingBehaviour.MatchRootGroupAndSubgroupsAsSingleBlock)]
		public void IdentifyGroup_should_return_top_namespace_when_no_match(string importedNamespace, SubgroupingBehaviour subgroupingBehaviour)
		{
			// Arrange
			var rule = new GroupingRule();

			rule.SubgroupingBehaviour = subgroupingBehaviour;

			rule.MatchList.Add(importedNamespace);

			// Generate non-matching namespace
			var usingStatement = new UsingStatement() { Namespace = "A" + importedNamespace };

			// Act
			var result = rule.IdentifyGroup(usingStatement);

			// Assert
			var firstComponent = usingStatement.Namespace.Split('.').First();

			result.Should().Be(firstComponent + ".");
		}

		[TestCase("Foo")]
		[TestCase("Foo.Bar")]
		public void IdentifyGroup_should_identify_subgroup(string importedNamespace)
		{
			// Arrange
			var rule = new GroupingRule();

			rule.SubgroupingBehaviour = SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks;

			rule.MatchList.Add(importedNamespace);

			var usingStatement = new UsingStatement() { Namespace = importedNamespace + ".Qux" };

			// Act
			var result = rule.IdentifyGroup(usingStatement);

			// Assert
			result.Should().Be(usingStatement.Namespace + ".");
		}

		[TestCase(new[] { "Foo" }, "Foo")]
		[TestCase(new[] { "Foo", "Foo.Bar" }, "Foo.Bar")]
		[TestCase(new[] { "Foo.Bar", "Foo" }, "Foo.Bar")]
		[TestCase(new[] { "Foo.Bar", "Foo.Bar.Qux", "Foo" }, "Foo.Bar.Qux")]
		public void IdentifyGroup_should_identify_deepest_subgroup(string[] matchStrings, string importedNamespace)
		{
			// Arrange
			var rule = new GroupingRule();

			rule.SubgroupingBehaviour = SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks;

			rule.MatchList.AddRange(matchStrings);

			var usingStatement = new UsingStatement() { Namespace = importedNamespace + ".Eenp" };

			// Act
			var result = rule.IdentifyGroup(usingStatement);

			// Assert
			result.Should().Be(usingStatement.Namespace + ".");
		}

		[TestCase("Foo.Bar")]
		[TestCase("Foo.Bar.Qux")]
		public void IdentifyGroup_should_not_match_top_namespace_when_in_subgroup_mode(string importedNamespace)
		{
			// Arrange
			var rule = new GroupingRule();

			rule.SubgroupingBehaviour = SubgroupingBehaviour.MatchSubgroupsOnlyAsSeparateBlocks;

			rule.MatchList.Add(importedNamespace);

			var usingStatement = new UsingStatement() { Namespace = importedNamespace };

			// Act
			var result = rule.IdentifyGroup(usingStatement);

			// Assert
			result.Should().Be("Foo.");
		}
		#endregion
	}
}
