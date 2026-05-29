/*
 * File: KeywordResponder.cs
 * Purpose: Lightweight keyword-to-randomized-response mapping used by UI chatbot logic.
 *
 * Notes:
 * - This class contains a small dictionary mapping topic keys (e.g., "phishing") to
 *   a list of sample responses. TryGetResponse scans the user input and returns a
 *   random response for the first matching key.
 * - Because matching is substring-based, ensure keys are chosen to avoid unintended
 *   collisions (e.g., "pass" matching "password" would be ambiguous). Keys here
 *   are full words or phrases to reduce accidental matches.
 * - To extend behavior: add more keys/responses or replace the matching strategy
 *   with regex/word-boundary checks for higher precision.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberAware
{
    public class KeywordResponder
    {
        private readonly Dictionary<string, List<string>> _responses = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random _random = new();

        public KeywordResponder()
        {
            _responses["phishing"] = new List<string>
            {
                "Phishing is when attackers impersonate trusted sources to steal credentials. Tip: hover over links before clicking.",
                "Phishing emails often use urgent language. Tip: verify the sender and don't enter credentials via links."
            };
            _responses["password"] = new List<string>
            {
                "Use a long, unique passphrase for each account and consider a reputable password manager.",
                "Avoid reusing passwords; enable two-factor authentication where available."
            };
            _responses["malware"] = new List<string>
            {
                "Malware includes viruses, ransomware, and trojans. Tip: keep software updated and scan attachments.",
                "Prevent malware by avoiding unknown downloads and using endpoint protection."
            };
            _responses["encryption"] = new List<string>
            {
                "Encryption protects data in transit and at rest. Use TLS and strong algorithms like AES-256.",
                "Encrypt sensitive backups and use secure key management to protect encrypted data."
            };
            _responses["privacy"] = new List<string>
            {
                "Limit app permissions and review privacy settings on social platforms regularly.",
                "Be cautious about sharing personal details online; attackers can use them for identity theft."
            };
            _responses["ransomware"] = new List<string>
            {
                "Ransomware encrypts files and demands payment. Tip: maintain offline backups and patch systems.",
                "Don't pay ransoms; restore from backups and involve incident response."
            };
            _responses["scam"] = new List<string>
            {
                "Scams often ask for money or gift cards. Verify requests independently before acting.",
                "Report scams to the platform and never share financial info with unknown parties."
            };
            _responses["two factor"] = new List<string>
            {
                "Two-factor authentication adds a second verification step; prefer authenticator apps over SMS.",
                "Use hardware keys for strong 2FA where supported."
            };
            _responses["ddos"] = new List<string>
            {
                "DDoS overwhelms services; mitigation includes rate limiting and CDNs.",
                "Use upstream DDoS protection and redundant infrastructure to withstand attacks."
            };
            _responses["safe browsing"] = new List<string>
            {
                "Prefer HTTPS and avoid downloading from untrusted sites. Use privacy extensions if needed.",
                "Keep your browser updated and limit extensions to reduce attack surface."
            };
        }

        public (string key, string response)? TryGetResponse(string input)
        {
            // Try to match a known keyword in the input and return a random response for that key.
            //
            // Notes on behavior:
            // - Matching is substring-based and case-insensitive. The first matching key
            //   encountered in the dictionary order will be used. This is intentionally simple
            //   for pedagogical reasons but may be improved with word-boundary checks or regexes.
            // - The method returns a tuple (key, response) so callers can record the canonical
            //   topic key for follow-ups (e.g., "tell me more").
            if (string.IsNullOrWhiteSpace(input)) return null;
            foreach (var key in _responses.Keys)
            {
                if (input.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    var list = _responses[key];
                    var resp = list[_random.Next(list.Count)];
                    return (key, resp);
                }
            }
            return null;
        }

        public string? GetRandomResponseForKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            if (!_responses.ContainsKey(key)) return null;
            var list = _responses[key];
            return list[_random.Next(list.Count)];
        }

        public IEnumerable<string> GetAllKeywords() => _responses.Keys.OrderBy(k => k);
    }
}