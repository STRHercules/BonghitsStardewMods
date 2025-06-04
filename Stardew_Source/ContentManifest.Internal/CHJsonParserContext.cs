using System;

namespace ContentManifest.Internal;

internal class CHJsonParserContext
{
	public int ReadHead;

	public string JsonText = "";

	public CHJsonParserContext(string jsonText)
	{
		JsonText = jsonText;
	}

	public void SkipWhitespace()
	{
		while (ReadHead < JsonText.Length)
		{
			switch (JsonText[ReadHead])
			{
			case '\t':
			case '\n':
			case '\r':
			case ' ':
				break;
			default:
				return;
			}
			ReadHead++;
		}
	}

	public void AssertReadHeadIsValid()
	{
		if (ReadHead < 0 || ReadHead >= JsonText.Length)
		{
			throw new InvalidOperationException();
		}
	}
}
