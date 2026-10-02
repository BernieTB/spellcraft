using System.Globalization;
using System.Text;

namespace Game.Core.Combat.Log
{
    /// <summary>
    /// Minimal JSON helpers for <see cref="CombatLog.ToJson"/>. Hand-written because <c>System.Text.Json</c> is not
    /// part of Unity's .NET profile, and the log must not depend on <c>UnityEngine.JsonUtility</c> (Core is
    /// engine-free). Output is deterministic: invariant culture, fixed field order.
    /// </summary>
    internal static class JsonWriter
    {
        /// <summary>Appends <paramref name="value"/> as a quoted JSON string, escaping as RFC 8259 requires.</summary>
        public static void AppendString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < ' ' || c == (char)0x2028 || c == (char)0x2029)
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
        }

        /// <summary>Appends <c>"name":</c>.</summary>
        public static void AppendName(StringBuilder builder, string name)
        {
            AppendString(builder, name);
            builder.Append(':');
        }

        /// <summary>Appends <c>"name":value</c> for an integer.</summary>
        public static void AppendField(StringBuilder builder, string name, int value)
        {
            AppendName(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Appends <c>"name":"value"</c> for a string.</summary>
        public static void AppendField(StringBuilder builder, string name, string value)
        {
            AppendName(builder, name);
            AppendString(builder, value);
        }
    }
}
