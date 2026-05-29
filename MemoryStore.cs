/*
 * File: MemoryStore.cs
 * Purpose: Small in-memory key/value store used by the UI chatbot to persist simple
 * session state such as UserName and FavouriteTopic.
 *
 * Notes:
 * - This class is intentionally minimal and stores values only for the lifetime
 *   of the MemoryStore instance. Persistence to disk is handled by ResponseHandler
 *   (which uses LoadMemory/SaveMemory helpers) when required.
 * - To extend persistence, inject a persistence adapter rather than adding file
 *   I/O directly to this class; this keeps responsibilities separate and testable.
 */
using System;
using System.Collections.Generic;

namespace CyberAware
{
    public class MemoryStore
    {
        private readonly Dictionary<string, string> _store = new(StringComparer.OrdinalIgnoreCase);

        public string? UserName
        {
            get => Get("UserName");
            set => Store("UserName", value);
        }

        public string? FavouriteTopic
        {
            get => Get("FavouriteTopic");
            set => Store("FavouriteTopic", value);
        }

        public void Store(string key, string? value)
        {
            if (value == null) { if (_store.ContainsKey(key)) _store.Remove(key); return; }
            _store[key] = value;
        }

        public string? Get(string key)
        {
            return _store.TryGetValue(key, out var v) ? v : null;
        }

        public string GetPersonalisedOpener()
        {
            var name = UserName ?? "";
            var topic = FavouriteTopic ?? "";
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(topic))
                return $"Nice to see you back, {name}. As someone interested in {topic}, here's a quick tip:";
            if (!string.IsNullOrWhiteSpace(name))
                return $"Welcome back, {name}. How can I help today?";
            return "Hello — tell me your name and favourite topic so I can personalise advice.";
        }
    }
}