using System;
using System.Globalization;
using System.Text;

namespace ContentManifest.Internal;

internal class CHNumber : CHParsable
{
	private static StringBuilder DoubleSb;

	public double RawDouble;

	public static bool IsValidPrefix(char prefixChar)
	{
		if (prefixChar != '-')
		{
			if ('0' <= prefixChar)
			{
				return prefixChar <= '9';
			}
			return false;
		}
		return true;
	}

	public void Parse(CHJsonParserContext context)
	{
		EnsureStringBuilderInitialized();
		DoubleSb.Clear();
		if (context.JsonText[context.ReadHead] == '-')
		{
			context.ReadHead++;
			DoubleSb.Append('-');
		}
		context.AssertReadHeadIsValid();
		char firstDigit = context.JsonText[context.ReadHead];
		if (firstDigit == '0')
		{
			context.ReadHead++;
			if (context.ReadHead < context.JsonText.Length)
			{
				char oneNineChar = context.JsonText[context.ReadHead];
				if ('1' <= oneNineChar && oneNineChar <= '9')
				{
					throw new InvalidOperationException();
				}
			}
			DoubleSb.Append('0');
		}
		else
		{
			if ('1' > firstDigit || firstDigit > '9')
			{
				throw new InvalidOperationException();
			}
			context.ReadHead++;
			DoubleSb.Append(firstDigit);
		}
		ParseDigits(context);
		if (context.ReadHead < context.JsonText.Length && context.JsonText[context.ReadHead] == '.')
		{
			context.ReadHead++;
			context.AssertReadHeadIsValid();
			DoubleSb.Append('.');
			ParseDigits(context);
		}
		if (context.ReadHead < context.JsonText.Length)
		{
			char expChar = context.JsonText[context.ReadHead];
			if (expChar == 'e' || expChar == 'E')
			{
				context.ReadHead++;
				context.AssertReadHeadIsValid();
				DoubleSb.Append('E');
				char signChar = context.JsonText[context.ReadHead];
				if (signChar == '-' || signChar == '+')
				{
					context.ReadHead++;
					context.AssertReadHeadIsValid();
					DoubleSb.Append(signChar);
				}
				ParseDigits(context);
			}
		}
		RawDouble = double.Parse(DoubleSb.ToString(), CultureInfo.InvariantCulture);
	}

	private void ParseDigits(CHJsonParserContext context)
	{
		string jsonText = context.JsonText;
		int readHead;
		for (readHead = context.ReadHead; readHead < jsonText.Length; readHead++)
		{
			char c = jsonText[readHead];
			if (c < '0' || c > '9')
			{
				break;
			}
			DoubleSb.Append(c);
		}
		context.ReadHead = readHead;
	}

	private static void EnsureStringBuilderInitialized()
	{
		string maxLongString = Convert.ToString(long.MaxValue);
		DoubleSb = new StringBuilder("-".Length + maxLongString.Length + ".".Length + maxLongString.Length + "E".Length + "+".Length + maxLongString.Length);
	}
}
