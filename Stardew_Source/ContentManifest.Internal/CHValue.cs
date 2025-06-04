using System;

namespace ContentManifest.Internal;

internal class CHValue : CHParsable
{
	public CHValueUnion RawValue;

	public CHValueEnum ValueType = CHValueEnum.Unknown;

	public CHValue()
	{
		RawValue.ValueNull = null;
	}

	public void Parse(CHJsonParserContext context)
	{
		if (context.ReadHead >= context.JsonText.Length)
		{
			throw new InvalidOperationException();
		}
		CHParsable parsable = null;
		char prefixChar = context.JsonText[context.ReadHead];
		switch (prefixChar)
		{
		case '{':
			parsable = (RawValue.ValueObject = new CHObject());
			ValueType = CHValueEnum.Object;
			break;
		case '[':
			parsable = (RawValue.ValueArray = new CHArray());
			ValueType = CHValueEnum.Array;
			break;
		case '"':
			parsable = (RawValue.ValueString = new CHString());
			ValueType = CHValueEnum.String;
			break;
		case 'f':
		case 't':
			parsable = (RawValue.ValueBoolean = new CHBoolean());
			ValueType = CHValueEnum.Boolean;
			break;
		case 'n':
			if (context.ReadHead + 3 >= context.JsonText.Length)
			{
				throw new InvalidOperationException();
			}
			if (context.JsonText[context.ReadHead + 1] != 'u' || context.JsonText[context.ReadHead + 2] != 'l' || context.JsonText[context.ReadHead + 3] != 'l')
			{
				throw new InvalidOperationException();
			}
			parsable = null;
			ValueType = CHValueEnum.Null;
			break;
		default:
			if (CHNumber.IsValidPrefix(prefixChar))
			{
				parsable = (RawValue.ValueNumber = new CHNumber());
				ValueType = CHValueEnum.Number;
				break;
			}
			throw new InvalidOperationException();
		}
		parsable?.Parse(context);
	}

	public object GetManagedObject()
	{
		return ValueType switch
		{
			CHValueEnum.Object => RawValue.ValueObject.Members, 
			CHValueEnum.Array => RawValue.ValueArray.Elements, 
			CHValueEnum.String => RawValue.ValueString.RawString, 
			CHValueEnum.Number => RawValue.ValueNumber.RawDouble, 
			CHValueEnum.Boolean => RawValue.ValueBoolean.RawBoolean, 
			CHValueEnum.Null => null, 
			_ => throw new InvalidOperationException(), 
		};
	}
}
