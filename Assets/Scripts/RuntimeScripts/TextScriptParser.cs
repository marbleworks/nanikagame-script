using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RuntimeScripting
{
    /// <summary>
    /// Parses script files located under the Resources folder.
    /// </summary>
    public static class TextScriptParser
    {
        private static readonly Regex optionRegex = new Regex(@"(\w+)=([^\s]+)");

        /// <summary>
        /// Loads all script files and returns a dictionary of <see cref="ParsedEvent"/> objects.
        /// </summary>
        public static Dictionary<string, ParsedEvent> LoadScripts(string folder)
        {
            var result = new Dictionary<string, ParsedEvent>();
            var assets = Resources.LoadAll<TextAsset>(folder);
            foreach (var asset in assets)
            {
                ParseText(asset.text, result);
            }
            return result;
        }

        /// <summary>
        /// Loads a single script file and returns its events.
        /// </summary>
        /// <param name="path">Path to the script file.</param>
        public static Dictionary<string, ParsedEvent> LoadFile(string path)
        {
            var result = new Dictionary<string, ParsedEvent>();
            ParseFile(path, result);
            return result;
        }

        /// <summary>
        /// Parses a single script text and returns its events.
        /// </summary>
        /// <param name="script">The script contents.</param>
        public static Dictionary<string, ParsedEvent> ParseString(string script)
        {
            var result = new Dictionary<string, ParsedEvent>();
            var lines = script.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            ParseLines(lines, result);
            return result;
        }

        private static void ParseFile(string path, Dictionary<string, ParsedEvent> events)
        {
            if (path.EndsWith(".txt"))
                path = path.Substring(0, path.Length - 4);
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogWarning($"Script file not found: {path}");
                return;
            }
            ParseText(asset.text, events);
        }

        private static void ParseText(string text, Dictionary<string, ParsedEvent> events)
        {
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            ParseLines(lines, events);
        }

        private static void ParseLines(IEnumerable<string> lines, Dictionary<string, ParsedEvent> events)
        {
            string currentEvent = null;
            ParsedEvent parsedEvent = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentEvent = line.Trim('[', ']');
                    if (!events.TryGetValue(currentEvent, out parsedEvent))
                    {
                        parsedEvent = new ParsedEvent { EventName = currentEvent };
                        events.Add(currentEvent, parsedEvent);
                    }
                    continue;
                }

                if (parsedEvent == null)
                    continue;

                var parsedAction = ParseLineToAction(line);
                if (parsedAction != null)
                {
                    parsedEvent.Actions.Add(parsedAction);
                }
            }
        }

        private static ParsedAction ParseLineToAction(string line)
        {
            string condition = null;
            string rest = line;
            int colon = line.IndexOf(':');
            if (colon >= 0)
            {
                condition = line.Substring(0, colon).Trim();
                rest = line.Substring(colon + 1).Trim();
            }

            if (!TryExtractAction(rest, out var name, out var argString, out var optionsPart))
                return null;
            var args = new List<string>();
            if (!string.IsNullOrEmpty(argString))
            {
                foreach (var a in argString.Split(','))
                    args.Add(a.Trim());
            }

            var options = ParseOptions(optionsPart);

            var pa = new ParsedAction
            {
                ActionType = ParseActionType(name),
                Condition = condition
            };
            pa.Args.AddRange(args);
            if (options.TryGetValue("interval", out var iv))
            {
                if (float.TryParse(iv, out var intervalValue))
                    pa.Interval = intervalValue;
                else
                    pa.IntervalFuncRaw = iv;
            }
            if (options.TryGetValue("period", out var pd))
            {
                if (float.TryParse(pd, out var periodValue))
                    pa.Period = periodValue;
                else
                    pa.PeriodFuncRaw = pd;
            }
            if (options.TryGetValue("canExecute", out var canExecuteValue))
                pa.CanExecuteRaw = canExecuteValue;
            if (options.TryGetValue("intervalFunc", out var intervalFuncValue))
                pa.IntervalFuncRaw = intervalFuncValue;
            return pa;
        }

        private static bool TryExtractAction(string text, out string name, out string args, out string optionsPart)
        {
            name = null;
            args = null;
            optionsPart = string.Empty;

            int open = text.IndexOf('(');
            if (open <= 0)
                return false;

            name = text.Substring(0, open).Trim();
            int depth = 0;
            int i = open;
            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '(')
                    depth++;
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        args = text.Substring(open + 1, i - open - 1);
                        optionsPart = text.Substring(i + 1).Trim();
                        return true;
                    }
                }
            }
            return false;
        }

        private static ActionType ParseActionType(string name)
        {
            return Enum.TryParse<ActionType>(name, out var at) ? at : ActionType.Attack;
        }

        private static Dictionary<string, string> ParseOptions(string text)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(text))
                return result;

            int i = 0;
            while (i < text.Length)
            {
                // skip whitespace
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                if (i >= text.Length) break;

                int keyStart = i;
                while (i < text.Length && text[i] != '=') i++;
                if (i >= text.Length) break;
                string key = text.Substring(keyStart, i - keyStart).Trim();
                i++; // skip '='

                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                int valueStart = i;
                int depth = 0;
                bool inQuote = false;
                char quoteChar = '\0';
                while (i < text.Length)
                {
                    char c = text[i];
                    if (inQuote)
                    {
                        if (c == quoteChar)
                            inQuote = false;
                    }
                    else
                    {
                        if (c == '"' || c == '\'')
                        {
                            inQuote = true;
                            quoteChar = c;
                        }
                        else if (c == '(')
                            depth++;
                        else if (c == ')')
                        {
                            if (depth > 0)
                                depth--;
                        }
                        else if (char.IsWhiteSpace(c) && depth == 0)
                        {
                            break;
                        }
                    }
                    i++;
                }

                string value = text.Substring(valueStart, i - valueStart).Trim();
                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[value.Length - 1] == '"') ||
                     (value[0] == '\'' && value[value.Length - 1] == '\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                result[key] = value;
            }

            return result;
        }
    }
}
