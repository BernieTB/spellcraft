using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Core.Meta
{
    /// <summary>
    /// Minimal strict JSON parser for save files. Hand-written for the same reason as the combat log's writer:
    /// <c>System.Text.Json</c> is not part of Unity's .NET profile and Core cannot use <c>UnityEngine.JsonUtility</c>.
    /// </summary>
    /// <remarks>
    /// Produces <see cref="Dictionary{TKey,TValue}"/> (objects, read by key only, never iterated), <see cref="List{T}"/>
    /// (arrays), <see cref="string"/>, <see cref="long"/> (integers only), <see cref="bool"/> and <c>null</c>.
    /// Throws <see cref="FormatException"/> on anything else.
    /// </remarks>
    internal sealed class JsonReader
    {
        private const int MaxDepth = 32;

        private readonly string _text;
        private int _index;

        private JsonReader(string text)
        {
            _text = text;
        }

        /// <summary>Parses a whole JSON document.</summary>
        /// <exception cref="FormatException">The text is not valid JSON of the supported subset.</exception>
        public static object Parse(string text)
        {
            if (text == null)
            {
                throw new FormatException("The JSON text is null.");
            }

            var reader = new JsonReader(text);
            var value = reader.ReadValue(0);
            reader.SkipWhitespace();
            if (reader._index != text.Length)
            {
                throw reader.Error("Unexpected content after the JSON value");
            }

            return value;
        }

        private object ReadValue(int depth)
        {
            if (depth > MaxDepth)
            {
                throw Error("JSON nested too deeply");
            }

            SkipWhitespace();
            if (_index >= _text.Length)
            {
                throw Error("Unexpected end of JSON");
            }

            var c = _text[_index];
            switch (c)
            {
                case '{':
                    return ReadObject(depth);
                case '[':
                    return ReadArray(depth);
                case '"':
                    return ReadString();
                case 't':
                    ReadLiteral("true");
                    return true;
                case 'f':
                    ReadLiteral("false");
                    return false;
                case 'n':
                    ReadLiteral("null");
                    return null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        return ReadInteger();
                    }

                    throw Error("Unexpected character '" + c + "'");
            }
        }

        private Dictionary<string, object> ReadObject(int depth)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            _index++;
            SkipWhitespace();
            if (TryConsume('}'))
            {
                return result;
            }

            while (true)
            {
                SkipWhitespace();
                if (_index >= _text.Length || _text[_index] != '"')
                {
                    throw Error("Expected a property name");
                }

                var name = ReadString();
                SkipWhitespace();
                Expect(':');
                if (result.ContainsKey(name))
                {
                    throw Error("Duplicate property '" + name + "'");
                }

                result[name] = ReadValue(depth + 1);
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return result;
                }

                Expect(',');
            }
        }

        private List<object> ReadArray(int depth)
        {
            var result = new List<object>();
            _index++;
            SkipWhitespace();
            if (TryConsume(']'))
            {
                return result;
            }

            while (true)
            {
                result.Add(ReadValue(depth + 1));
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return result;
                }

                Expect(',');
            }
        }

        private string ReadString()
        {
            _index++;
            var builder = new StringBuilder();
            while (_index < _text.Length)
            {
                var c = _text[_index++];
                if (c == '"')
                {
                    return builder.ToString();
                }

                if (c < ' ')
                {
                    throw Error("Control character in a string");
                }

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (_index >= _text.Length)
                {
                    break;
                }

                var escape = _text[_index++];
                switch (escape)
                {
                    case '"':
                        builder.Append('"');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    case '/':
                        builder.Append('/');
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'u':
                        if (_index + 4 > _text.Length
                            || !int.TryParse(_text.Substring(_index, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                        {
                            throw Error("Invalid unicode escape");
                        }

                        builder.Append((char)code);
                        _index += 4;
                        break;
                    default:
                        throw Error("Invalid escape '\\" + escape + "'");
                }
            }

            throw Error("Unterminated string");
        }

        private long ReadInteger()
        {
            var start = _index;
            if (_text[_index] == '-')
            {
                _index++;
            }

            while (_index < _text.Length && _text[_index] >= '0' && _text[_index] <= '9')
            {
                _index++;
            }

            if (_index < _text.Length && (_text[_index] == '.' || _text[_index] == 'e' || _text[_index] == 'E'))
            {
                throw Error("Only integer numbers are supported");
            }

            var token = _text.Substring(start, _index - start);
            if (!long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            {
                throw Error("Invalid number '" + token + "'");
            }

            return value;
        }

        private void ReadLiteral(string literal)
        {
            if (string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0)
            {
                throw Error("Invalid literal");
            }

            _index += literal.Length;
        }

        private void SkipWhitespace()
        {
            while (_index < _text.Length && (_text[_index] == ' ' || _text[_index] == '\t' || _text[_index] == '\n' || _text[_index] == '\r'))
            {
                _index++;
            }
        }

        private bool TryConsume(char c)
        {
            if (_index < _text.Length && _text[_index] == c)
            {
                _index++;
                return true;
            }

            return false;
        }

        private void Expect(char c)
        {
            if (!TryConsume(c))
            {
                throw Error("Expected '" + c + "'");
            }
        }

        private FormatException Error(string message)
        {
            return new FormatException(message + " at character " + _index.ToString(CultureInfo.InvariantCulture) + ".");
        }
    }
}
