namespace ContentManifest.Internal;

internal class CHElement : CHParsable
{
	public CHValue Value;

	public void Parse(CHJsonParserContext context)
	{
		context.SkipWhitespace();
		Value = new CHValue();
		Value.Parse(context);
		context.SkipWhitespace();
	}
}
