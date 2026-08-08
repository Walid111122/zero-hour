using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ZeroHour.Bridge
{
    /// <summary>
    /// Wire format for the Cline ↔ Unity bridge (docs/28 §2).
    ///
    /// Requests are flat on purpose. JsonUtility cannot deserialise dictionaries, and the
    /// command set is a fixed allowlist rather than arbitrary input, so a handful of typed
    /// argument slots is both sufficient and harder to abuse than a free-form payload.
    /// </summary>
    [Serializable]
    public class BridgeRequest
    {
        public string id;
        public string command;
        public string argString;
        public int argInt;
        public bool argBool;
    }

    /// <summary>A single captured console line.</summary>
    [Serializable]
    public class BridgeLogEntry
    {
        public string severity;
        public string message;
        public string stackTrace;
        public string timestamp;
    }

    /// <summary>
    /// Minimal JSON writer.
    ///
    /// Responses embed pre-rendered JSON fragments (a compile result, a scene dump), and
    /// running those back through JsonUtility would double-escape them into something the
    /// caller has to unwrap twice. Emitting the envelope by hand keeps the output directly
    /// parseable.
    /// </summary>
    public static class Json
    {
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length + 16);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b");  break;
                    case '\f': sb.Append("\\f");  break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.ToString();
        }

        public static string Str(string value)
        {
            return value == null ? "null" : "\"" + Escape(value) + "\"";
        }

        public static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        public static string Num(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string Array(IEnumerable<string> jsonFragments)
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (string fragment in jsonFragments)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                sb.Append(fragment);
                first = false;
            }

            return sb.Append(']').ToString();
        }

        public static string StringArray(IEnumerable<string> values)
        {
            var quoted = new List<string>();
            foreach (string value in values)
            {
                quoted.Add(Str(value));
            }

            return Array(quoted);
        }
    }
}
