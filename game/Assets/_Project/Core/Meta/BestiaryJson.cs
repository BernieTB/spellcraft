using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Core.Combat.Log;

namespace Game.Core.Meta
{
    /// <summary>
    /// Converts a <see cref="Bestiary"/> to and from its save format: versioned JSON, written deterministically
    /// (professors by id, cards by position).
    /// </summary>
    /// <remarks>
    /// Format, version 1:
    /// <code>
    /// {
    ///   "version": 1,
    ///   "professors": [
    ///     { "id": "...", "healthKnown": true, "shieldKnown": false, "cards": [ { "position": 0, "cardId": "..." } ] }
    ///   ]
    /// }
    /// </code>
    /// A new format gets a new version number; <see cref="Deserialize"/> rejects versions it does not know.
    /// </remarks>
    public static class BestiaryJson
    {
        /// <summary>The save format version written by <see cref="Serialize"/>.</summary>
        public const int CurrentVersion = 1;

        /// <summary>Writes <paramref name="bestiary"/> as JSON.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="bestiary"/> is null.</exception>
        public static string Serialize(Bestiary bestiary)
        {
            if (bestiary == null)
            {
                throw new ArgumentNullException(nameof(bestiary));
            }

            var builder = new StringBuilder();
            builder.Append("{\n  ");
            JsonWriter.AppendField(builder, "version", CurrentVersion);
            builder.Append(",\n  ");
            JsonWriter.AppendName(builder, "professors");
            builder.Append('[');
            for (var i = 0; i < bestiary.Entries.Count; i++)
            {
                var entry = bestiary.Entries[i];
                builder.Append(i == 0 ? "\n    {" : ",\n    {");
                JsonWriter.AppendField(builder, "id", entry.ProfessorId);
                builder.Append(',');
                AppendBool(builder, "healthKnown", entry.HealthKnown);
                builder.Append(',');
                AppendBool(builder, "shieldKnown", entry.ShieldKnown);
                builder.Append(',');
                JsonWriter.AppendName(builder, "cards");
                builder.Append('[');
                for (var j = 0; j < entry.Cards.Count; j++)
                {
                    if (j > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append('{');
                    JsonWriter.AppendField(builder, "position", entry.Cards[j].Position);
                    builder.Append(',');
                    JsonWriter.AppendField(builder, "cardId", entry.Cards[j].CardId);
                    builder.Append('}');
                }

                builder.Append("]}");
            }

            builder.Append(bestiary.Entries.Count > 0 ? "\n  ]\n}\n" : "]\n}\n");
            return builder.ToString();
        }

        /// <summary>Reads a bestiary written by <see cref="Serialize"/>.</summary>
        /// <exception cref="FormatException">
        /// The text is not valid JSON, its version is missing or unknown, a field is missing or invalid, a professor
        /// appears twice or a professor has two cards at the same position.
        /// </exception>
        public static Bestiary Deserialize(string json)
        {
            var root = JsonReader.Parse(json) as Dictionary<string, object>;
            if (root == null)
            {
                throw new FormatException("The bestiary save is not a JSON object.");
            }

            var version = ReadInt(root, "version", "the save");
            if (version != CurrentVersion)
            {
                throw new FormatException(
                    "Unknown bestiary save version " + version.ToString(CultureInfo.InvariantCulture) + " (expected "
                        + CurrentVersion.ToString(CultureInfo.InvariantCulture) + ").");
            }

            var bestiary = new Bestiary();
            var seenIds = new List<string>();
            var professors = ReadArray(root, "professors", "the save");
            for (var i = 0; i < professors.Count; i++)
            {
                var where = "professor " + i.ToString(CultureInfo.InvariantCulture);
                var professor = professors[i] as Dictionary<string, object>
                    ?? throw new FormatException("The " + where + " is not a JSON object.");

                var id = ReadString(professor, "id", where);
                if (seenIds.Exists(seen => string.Equals(seen, id, StringComparison.Ordinal)))
                {
                    throw new FormatException("Professor '" + id + "' appears twice in the save.");
                }

                seenIds.Add(id);
                var cards = new List<RevealedCard>();
                var cardValues = ReadArray(professor, "cards", where);
                for (var j = 0; j < cardValues.Count; j++)
                {
                    var cardWhere = where + ", card " + j.ToString(CultureInfo.InvariantCulture);
                    var card = cardValues[j] as Dictionary<string, object>
                        ?? throw new FormatException("The " + cardWhere + " is not a JSON object.");
                    var position = ReadInt(card, "position", cardWhere);
                    if (position < 0)
                    {
                        throw new FormatException("The " + cardWhere + " has a negative position.");
                    }

                    if (cards.Exists(existing => existing.Position == position))
                    {
                        throw new FormatException("The " + where + " has two cards at position " + position.ToString(CultureInfo.InvariantCulture) + ".");
                    }

                    cards.Add(new RevealedCard(position, ReadString(card, "cardId", cardWhere)));
                }

                bestiary.Restore(id, ReadBool(professor, "healthKnown", where), ReadBool(professor, "shieldKnown", where), cards);
            }

            return bestiary;
        }

        private static void AppendBool(StringBuilder builder, string name, bool value)
        {
            JsonWriter.AppendName(builder, name);
            builder.Append(value ? "true" : "false");
        }

        private static object Read(Dictionary<string, object> obj, string name, string where)
        {
            if (!obj.TryGetValue(name, out var value))
            {
                throw new FormatException("Missing \"" + name + "\" in " + where + ".");
            }

            return value;
        }

        private static int ReadInt(Dictionary<string, object> obj, string name, string where)
        {
            if (Read(obj, name, where) is long value && value >= int.MinValue && value <= int.MaxValue)
            {
                return (int)value;
            }

            throw new FormatException("\"" + name + "\" in " + where + " is not an integer.");
        }

        private static bool ReadBool(Dictionary<string, object> obj, string name, string where)
        {
            if (Read(obj, name, where) is bool value)
            {
                return value;
            }

            throw new FormatException("\"" + name + "\" in " + where + " is not true or false.");
        }

        private static string ReadString(Dictionary<string, object> obj, string name, string where)
        {
            if (Read(obj, name, where) is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            throw new FormatException("\"" + name + "\" in " + where + " is not a non-empty string.");
        }

        private static List<object> ReadArray(Dictionary<string, object> obj, string name, string where)
        {
            if (Read(obj, name, where) is List<object> value)
            {
                return value;
            }

            throw new FormatException("\"" + name + "\" in " + where + " is not an array.");
        }
    }
}
