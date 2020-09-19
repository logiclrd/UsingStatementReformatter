using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using FluentAssertions;

using NUnit.Framework;

namespace UsingStatementReformatter.Tests
{
	class UsingStatementFormatterTests
	{
		public class FormatTestCase
		{
			public string FileName;

			public string ConfigurationString;
			public List<UsingStatement> Input;
			public string ExpectedOutput;

			public override string ToString() => FileName;
		}

		class DiscoverTestCases : IEnumerable
		{
			public IEnumerator GetEnumerator()
			{
				foreach (var filePath in Directory.GetFiles("TestCases"))
				{
					var testCase = new FormatTestCase();

					testCase.FileName = Path.GetFileNameWithoutExtension(filePath);

					using (var reader = new StreamReader(filePath))
					{
						testCase.ConfigurationString = reader.ReadLine();

						reader.ReadLine().Should().Be("INPUT");

						testCase.Input = new List<UsingStatement>();

						while (true)
						{
							string line = reader.ReadLine();

							if ((line == null) || (line == "OUTPUT"))
								break;

							if (UsingStatement.TryParse(line, out var statement))
								testCase.Input.Add(statement);
						}

						var outputBuffer = new StringWriter();

						while (true)
						{
							string line = reader.ReadLine();

							if (line == null)
								break;

							outputBuffer.WriteLine(line);
						}

						testCase.ExpectedOutput = outputBuffer.ToString();

						yield return testCase;
					}
				}
			}
		}

		[TestCaseSource(typeof(DiscoverTestCases))]
		public void Format_should_produce_matching_output(FormatTestCase testCase)
		{
			// Arrange
			var groupingRules = GroupingRule.Parse(testCase.ConfigurationString);

			var outputBuffer = new StringWriter();

			// Act
			UsingStatementFormatter.Format(
				outputBuffer,
				testCase.Input,
				groupingRules);

			// Assert
			outputBuffer.ToString().Should().Be(testCase.ExpectedOutput);
		}
	}
}
