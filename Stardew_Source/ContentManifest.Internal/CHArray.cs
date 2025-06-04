using System;
using System.Collections.Generic;

namespace ContentManifest.Internal;

internal class CHArray : CHParsable
{
	private static readonly List<object> ElementList = new List<object>();

	public object[] Elements;

	public void Parse(CHJsonParserContext context)
	{
		if (context.JsonText[context.ReadHead] != '[')
		{
			throw new InvalidOperationException();
		}
		context.ReadHead++;
		bool needsElement = false;
		ElementList.Clear();
		while (true)
		{
			context.SkipWhitespace();
			context.AssertReadHeadIsValid();
			if (context.JsonText[context.ReadHead] == ']')
			{
				break;
			}
			CHElement element = new CHElement();
			element.Parse(context);
			ElementList.Add(element.Value.GetManagedObject());
			needsElement = false;
			context.SkipWhitespace();
			context.AssertReadHeadIsValid();
			if (context.JsonText[context.ReadHead] == ',')
			{
				context.ReadHead++;
				needsElement = true;
			}
		}
		if (needsElement)
		{
			throw new InvalidOperationException();
		}
		Elements = ElementList.ToArray();
		context.ReadHead++;
	}
}
