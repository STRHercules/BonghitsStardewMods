namespace ContentManifest.Internal;

internal class CHJson : CHParsable
{
	public CHElement Element;

	public void Parse(CHJsonParserContext context)
	{
		Element = new CHElement();
		Element.Parse(context);
	}
}
