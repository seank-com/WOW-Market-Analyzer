using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WMA
{
    public enum JsonType
    {
        None,
        Object,
        Array,
        String,
        Number,
        Boolean
    }

    public class JsonValue
    {

        private object _value;

        private JsonType _type = JsonType.None;

        public JsonType Type { get { return _type; } }

        public Dictionary<string, JsonValue> Object
        {
            get
            {
                if (_type != JsonType.Object)
                {
                    throw new InvalidOperationException();
                }

                return _value as Dictionary<string, JsonValue>;
            }

            set { _value = value; _type = JsonType.Object; }
        }

        public List<JsonValue> Array
        {
            get
            {
                if (_type != JsonType.Array)
                {
                    throw new InvalidOperationException();
                }
                return _value as List<JsonValue>;
            }

            set { _value = value; _type = JsonType.Array; }
        }

        public string String
        {
            get
            {
                if (_type != JsonType.String)
                {
                    throw new InvalidOperationException();
                }
                return (string)_value;
            }

            set { _value = value; _type = JsonType.String; }
        }

        public double Double
        {
            get
            {
                if (_type != JsonType.Number)
                {
                    throw new InvalidOperationException();
                }
                return (double)_value;
            }

            set { _value = value; _type = JsonType.Number; }
        }

        public long Long
        {
            get
            {
                if (_type != JsonType.Number)
                {
                    throw new InvalidOperationException();
                }
                return (long)_value;
            }

            set { _value = value; _type = JsonType.Number; }
        }

        public bool Boolean
        {
            get
            {
                if (_type != JsonType.Boolean)
                {
                    throw new InvalidOperationException();
                }
                return (bool)_value;
            }

            set { _value = value; _type = JsonType.Boolean; }
        }

        public void Empty()
        {
            _type = JsonType.None;
        }
    }

    public class JSON
    {
        public const int TOKEN_NONE = 0;
        public const int TOKEN_CURLY_OPEN = 1;
        public const int TOKEN_CURLY_CLOSE = 2;
        public const int TOKEN_SQUARED_OPEN = 3;
        public const int TOKEN_SQUARED_CLOSE = 4;
        public const int TOKEN_COLON = 5;
        public const int TOKEN_COMMA = 6;
        public const int TOKEN_STRING = 7;
        public const int TOKEN_NUMBER = 8;
        public const int TOKEN_TRUE = 9;
        public const int TOKEN_FALSE = 10;
        public const int TOKEN_NULL = 11;

        private const int BUILDER_CAPACITY = 2000;

        public static JsonValue JsonDecode(string json)
        {
            if (json != null)
            {
                char[] charArray = json.ToCharArray();
                int index = 0;
                return ParseValue(charArray, ref index);
            }
            else
            {
                return new JsonValue();
            }
        }

        public static string JsonEncode(JsonValue json)
        {
            StringBuilder builder = new StringBuilder(BUILDER_CAPACITY);
            bool success = SerializeValue(json, builder);
            return (success ? builder.ToString() : null);
        }

        protected static JsonValue ParseObject(char[] json, ref int index)
        {
            JsonValue result = new JsonValue();
            result.Object = new Dictionary<string, JsonValue>();
            int token;

            // {
            NextToken(json, ref index);

            bool done = false;
            while (!done)
            {
                token = LookAhead(json, index);
                if (token == JSON.TOKEN_NONE)
                {
                    result.Empty();
                    return result;
                }
                else if (token == JSON.TOKEN_COMMA)
                {
                    NextToken(json, ref index);
                }
                else if (token == JSON.TOKEN_CURLY_CLOSE)
                {
                    NextToken(json, ref index);
                    break;
                }
                else
                {

                    // name
                    JsonValue name = ParseString(json, ref index);
                    if (name.Type != JsonType.String)
                    {
                        result.Empty();
                        return result;
                    }

                    // :
                    token = NextToken(json, ref index);
                    if (token != JSON.TOKEN_COLON)
                    {
                        result.Empty();
                        return result;
                    }

                    // value
                    JsonValue value = ParseValue(json, ref index);
                    if (value.Type == JsonType.None)
                    {
                        return value;
                    }

                    result.Object[name.String] = value;
                }
            }

            return result;
        }

        protected static JsonValue ParseArray(char[] json, ref int index)
        {
            JsonValue result = new JsonValue();
            result.Array = new List<JsonValue>();

            // [
            NextToken(json, ref index);

            bool done = false;
            while (!done)
            {
                int token = LookAhead(json, index);
                if (token == JSON.TOKEN_NONE)
                {
                    result.Empty();
                    return result;
                }
                else if (token == JSON.TOKEN_COMMA)
                {
                    NextToken(json, ref index);
                }
                else if (token == JSON.TOKEN_SQUARED_CLOSE)
                {
                    NextToken(json, ref index);
                    break;
                }
                else
                {
                    JsonValue value = ParseValue(json, ref index);
                    if (value.Type == JsonType.None)
                    {
                        return value;
                    }

                    result.Array.Add(value);
                }
            }

            return result;
        }

        protected static JsonValue ParseValue(char[] json, ref int index)
        {
            JsonValue result = new JsonValue();
            switch (LookAhead(json, index))
            {
                case JSON.TOKEN_STRING:
                    return ParseString(json, ref index);
                case JSON.TOKEN_NUMBER:
                    return ParseNumber(json, ref index);
                case JSON.TOKEN_CURLY_OPEN:
                    return ParseObject(json, ref index);
                case JSON.TOKEN_SQUARED_OPEN:
                    return ParseArray(json, ref index);
                case JSON.TOKEN_TRUE:
                    NextToken(json, ref index);
                    result.Boolean = true;
                    break;
                case JSON.TOKEN_FALSE:
                    NextToken(json, ref index);
                    result.Boolean = false;
                    break;
                case JSON.TOKEN_NULL:
                    NextToken(json, ref index);
                    break;
                case JSON.TOKEN_NONE:
                    break;
            }

            return result;
        }

        protected static JsonValue ParseString(char[] json, ref int index)
        {
            JsonValue result = new JsonValue();
            StringBuilder s = new StringBuilder(BUILDER_CAPACITY);
            char c;

            EatWhitespace(json, ref index);

            // "
            c = json[index++];

            bool complete = false;
            while (!complete)
            {

                if (index == json.Length)
                {
                    break;
                }

                c = json[index++];
                if (c == '"')
                {
                    complete = true;
                    break;
                }
                else if (c == '\\')
                {

                    if (index == json.Length)
                    {
                        break;
                    }
                    c = json[index++];
                    if (c == '"')
                    {
                        s.Append('"');
                    }
                    else if (c == '\\')
                    {
                        s.Append('\\');
                    }
                    else if (c == '/')
                    {
                        s.Append('/');
                    }
                    else if (c == 'b')
                    {
                        s.Append('\b');
                    }
                    else if (c == 'f')
                    {
                        s.Append('\f');
                    }
                    else if (c == 'n')
                    {
                        s.Append('\n');
                    }
                    else if (c == 'r')
                    {
                        s.Append('\r');
                    }
                    else if (c == 't')
                    {
                        s.Append('\t');
                    }
                    else if (c == 'u')
                    {
                        int remainingLength = json.Length - index;
                        if (remainingLength >= 4)
                        {
                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint;
                            if (!UInt32.TryParse(new string(json, index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint))
                            {
                                return result;
                            }
                            // convert the integer codepoint to a unicode char and add to string
                            s.Append(Char.ConvertFromUtf32((int)codePoint));
                            // skip 4 chars
                            index += 4;
                        }
                        else
                        {
                            break;
                        }
                    }

                }
                else
                {
                    s.Append(c);
                }

            }

            if (complete)
            {
                result.String = s.ToString();
            }
            return result;
        }

        protected static JsonValue ParseNumber(char[] json, ref int index)
        {
            JsonValue result = new JsonValue();

            EatWhitespace(json, ref index);

            int lastIndex = GetLastIndexOfNumber(json, index);
            int charLength = (lastIndex - index) + 1;

            long lVal;
            if (Int64.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out lVal))
            {
                index = lastIndex + 1;
                result.Long = lVal;
            }

            double lfVal;
            if (result.Type == JsonType.None && Double.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out lfVal))
            {
                index = lastIndex + 1;
                result.Double = lfVal;
            }
            return result;
        }

        protected static int GetLastIndexOfNumber(char[] json, int index)
        {
            int lastIndex;

            for (lastIndex = index; lastIndex < json.Length; lastIndex++)
            {
                if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
                {
                    break;
                }
            }
            return lastIndex - 1;
        }

        protected static void EatWhitespace(char[] json, ref int index)
        {
            for (; index < json.Length; index++)
            {
                if (" \t\n\r".IndexOf(json[index]) == -1)
                {
                    break;
                }
            }
        }

        protected static int LookAhead(char[] json, int index)
        {
            int saveIndex = index;
            return NextToken(json, ref saveIndex);
        }

        protected static int NextToken(char[] json, ref int index)
        {
            EatWhitespace(json, ref index);

            if (index == json.Length)
            {
                return JSON.TOKEN_NONE;
            }

            char c = json[index];
            index++;
            switch (c)
            {
                case '{':
                    return JSON.TOKEN_CURLY_OPEN;
                case '}':
                    return JSON.TOKEN_CURLY_CLOSE;
                case '[':
                    return JSON.TOKEN_SQUARED_OPEN;
                case ']':
                    return JSON.TOKEN_SQUARED_CLOSE;
                case ',':
                    return JSON.TOKEN_COMMA;
                case '"':
                    return JSON.TOKEN_STRING;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    return JSON.TOKEN_NUMBER;
                case ':':
                    return JSON.TOKEN_COLON;
            }
            index--;

            int remainingLength = json.Length - index;

            // false
            if (remainingLength >= 5)
            {
                if (json[index] == 'f' &&
                    json[index + 1] == 'a' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 's' &&
                    json[index + 4] == 'e')
                {
                    index += 5;
                    return JSON.TOKEN_FALSE;
                }
            }

            // true
            if (remainingLength >= 4)
            {
                if (json[index] == 't' &&
                    json[index + 1] == 'r' &&
                    json[index + 2] == 'u' &&
                    json[index + 3] == 'e')
                {
                    index += 4;
                    return JSON.TOKEN_TRUE;
                }
            }

            // null
            if (remainingLength >= 4)
            {
                if (json[index] == 'n' &&
                    json[index + 1] == 'u' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 'l')
                {
                    index += 4;
                    return JSON.TOKEN_NULL;
                }
            }

            return JSON.TOKEN_NONE;
        }

        protected static bool SerializeValue(JsonValue value, StringBuilder builder)
        {
            bool success = true;

            switch (value.Type)
            {
            case JsonType.String:
                success = SerializeString(value.String, builder);
                break;
            case JsonType.Object:
                success = SerializeObject(value.Object, builder);
                break;
            case JsonType.Array:
                success = SerializeArray(value.Array, builder);
                break;
            case JsonType.Number:
                success = SerializeNumber(value.Double, builder);
                break;
            case JsonType.Boolean:
                if (value.Boolean)
                builder.Append("true");
                else
                builder.Append("false");
                break;
            default:
                builder.Append("null");
                break;
            }
            return success;
        }

        protected static bool SerializeObject(Dictionary<string, JsonValue> anObject, StringBuilder builder)
        {
            builder.Append("{");

            bool first = true;
            foreach(KeyValuePair<string, JsonValue> pair in anObject)
            {
                string key = pair.Key;
                JsonValue value = pair.Value;

                if (!first)
                {
                    builder.Append(", ");
                }

                SerializeString(key, builder);
                builder.Append(":");
                if (!SerializeValue(value, builder))
                {
                    return false;
                }

                first = false;
            }

            builder.Append("}");
            return true;
        }

        protected static bool SerializeArray(List<JsonValue> anArray, StringBuilder builder)
        {
            builder.Append("[");

            bool first = true;
            for (int i = 0; i < anArray.Count; i++)
            {
                JsonValue value = anArray[i];

                if (!first)
                {
                    builder.Append(", ");
                }

                if (!SerializeValue(value, builder))
                {
                    return false;
                }

                first = false;
            }

            builder.Append("]");
            return true;
        }

        protected static bool SerializeString(string aString, StringBuilder builder)
        {
            builder.Append("\"");

            char[] charArray = aString.ToCharArray();
            for (int i = 0; i < charArray.Length; i++)
            {
                char c = charArray[i];
                if (c == '"')
                {
                    builder.Append("\\\"");
                }
                else if (c == '\\')
                {
                    builder.Append("\\\\");
                }
                else if (c == '\b')
                {
                    builder.Append("\\b");
                }
                else if (c == '\f')
                {
                    builder.Append("\\f");
                }
                else if (c == '\n')
                {
                    builder.Append("\\n");
                }
                else if (c == '\r')
                {
                    builder.Append("\\r");
                }
                else if (c == '\t')
                {
                    builder.Append("\\t");
                }
                else
                {
                    int codepoint = Convert.ToInt32(c);
                    if ((codepoint >= 32) && (codepoint <= 126))
                    {
                        builder.Append(c);
                    }
                    else
                    {
                        builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                    }
                }
            }

            builder.Append("\"");
            return true;
        }

        protected static bool SerializeNumber(double number, StringBuilder builder)
        {
            builder.Append(Convert.ToString(number, CultureInfo.InvariantCulture));
            return true;
        }

    }
}
