using System;
using System.Collections.Generic;
using System.IO;

namespace Adle.Benchmark
{
    /// <summary>One activity instance: its label and the ordered sensor trajectory.</summary>
    public sealed class CasasSession
    {
        public string Activity { get; set; }
        public List<string> Tokens { get; set; } = new List<string>();
    }

    /// <summary>
    /// Parses a CASAS event-log CSV (date,time,sensor,state[,Activity="begin"|"end"])
    /// into per-activity sensor sequences. Each labeled activity span becomes one
    /// session whose tokens are the sensors that fired (ON events), with consecutive
    /// duplicates collapsed into a movement/activity trajectory.
    /// </summary>
    public static class CasasReader
    {
        public static List<CasasSession> ReadSessions(
            string path,
            int minLength = 3,
            int maxLength = 40,
            int maxSessions = int.MaxValue)
        {
            var sessions = new List<CasasSession>();

            string activeActivity = null;
            CasasSession current = null;
            string lastToken = null;

            foreach (var raw in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var parts = raw.Split(',');
                if (parts.Length < 4) continue;

                string sensor = parts[2].Trim();
                string state = parts[3].Trim();
                string activity = null;
                bool isBegin = false, isEnd = false;

                if (parts.Length >= 5)
                {
                    string label = string.Join(",", parts, 4, parts.Length - 4);
                    int eq = label.IndexOf('=');
                    if (eq > 0)
                    {
                        activity = label.Substring(0, eq).Trim();
                        string marker = label.Substring(eq + 1).Trim().Trim('"').ToLowerInvariant();
                        isBegin = marker == "begin";
                        isEnd = marker == "end";
                    }
                }

                if (isBegin && activeActivity == null)
                {
                    activeActivity = activity;
                    current = new CasasSession { Activity = activity };
                    lastToken = null;
                }

                if (activeActivity != null && IsOn(state))
                {
                    if (sensor != lastToken && current.Tokens.Count < maxLength)
                    {
                        current.Tokens.Add(sensor);
                        lastToken = sensor;
                    }
                }

                if (isEnd && activity == activeActivity)
                {
                    if (current.Tokens.Count >= minLength)
                        sessions.Add(current);
                    activeActivity = null;
                    current = null;
                    lastToken = null;
                    if (sessions.Count >= maxSessions) break;
                }
            }

            return sessions;
        }

        private static bool IsOn(string state)
        {
            // Motion/door sensors: ON/OPEN mark a trigger; numeric analog values also count as activity.
            string s = state.ToUpperInvariant();
            return s == "ON" || s == "OPEN" || s == "PRESENT" || s.StartsWith("ON");
        }
    }
}
