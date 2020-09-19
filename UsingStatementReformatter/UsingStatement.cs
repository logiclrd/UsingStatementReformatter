namespace UsingStatementReformatter
{
	public class UsingStatement
	{
		public bool IsGlobal;
		public string Namespace;

		public override string ToString()
			=> $"using {(IsGlobal ? "global::" : "")}{Namespace};";

		public static bool TryParse(string line, out UsingStatement parsed)
		{
			parsed = default;

			if (line == null)
				return false;

			line = line.Trim();

			if (!line.StartsWith("using "))
				return false;

			line = line.Substring(6);

			parsed = new UsingStatement();

			parsed.IsGlobal = line.StartsWith("global::");

			if (parsed.IsGlobal)
				line = line.Substring(8);

			if (line.EndsWith(";"))
				line = line.Substring(0, line.Length - 1);

			parsed.Namespace = line;

			return true;
		}
	}
}
