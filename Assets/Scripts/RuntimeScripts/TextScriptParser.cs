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
        private static readonly Regex actionRegex = new Regex(@"(?<name>\w+)\((?<args>[^)]*)\)");

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

            var m = actionRegex.Match(rest);
            if (!m.Success)
                return null;
            var name = m.Groups["name"].Value;
            var args = new List<string>();
            if (m.Groups["args"].Length > 0)
            {
                foreach (var a in m.Groups["args"].Value.Split(','))
                    args.Add(a.Trim());
            }

            var optionsPart = rest.Substring(m.Length).Trim();
            var options = new Dictionary<string, string>();
            foreach (Match om in optionRegex.Matches(optionsPart))
            {
                options[om.Groups[1].Value] = om.Groups[2].Value;
            }

            var pa = new ParsedAction
            {
                ActionType = ParseActionType(name),
                Condition = condition
            };
            pa.Args.AddRange(args);
            if (options.TryGetValue("interval", out var iv))
                if (float.TryParse(iv, out var intervalValue))
                    pa.Interval = intervalValue;
            if (options.TryGetValue("period", out var pd))
                if (float.TryParse(pd, out var periodValue))
                    pa.Period = periodValue;
            if (options.TryGetValue("canExecute", out var canExecuteValue))
                pa.CanExecuteRaw = canExecuteValue;
            if (options.TryGetValue("intervalFunc", out var intervalFuncValue))
                pa.IntervalFuncRaw = intervalFuncValue;
            return pa;
        }

        private static ActionType ParseActionType(string name)
        {
            return Enum.TryParse<ActionType>(name, out var at) ? at : ActionType.Attack;
        }
    }
}