/*
 * File: ChatBotCore.cs
 * Purpose: UI-focused chatbot implementation used by the WPF frontend.
 *
 * Design notes:
 * - This class implements a lightweight conversational layer tailored for GUI usage.
 * - Responsibilities include: capturing the user's display name, detecting simple
 *   keywords, applying a naive sentiment opener, and tracking a small in-memory
 *   MemoryStore for per-session personalisation like favourite topic.
 * - The class is intentionally decoupled from persistence and eventing to keep it
 *   testable and straightforward to adapt. If you need persistent memory, extend
 *   MemoryStore or add a persistence adapter rather than changing this class.
 *
 * Usage guidance:
 * - Instantiate ChatBotCore inside UI code and call GetGreeting() to obtain the
 *   initial prompt. Use ProcessInput() for each user message and display the
 *   returned string in the chat UI. Access GetMemory() to read session memory.
 */
using System;

namespace CyberAware
{
    // ChatBotCore is the UI-focused chatbot used by WPF. Named to avoid clashing with existing console Chatbot.
    public class ChatBotCore
    {
        private readonly KeywordResponder _keywords = new();
        private readonly SentimentDetector _sentiment = new();
        private readonly MemoryStore _memory = new();
        private bool _awaitingName = true;
        private string? _lastTopicKey;

        public ChatBotCore()
        {
        }

        public string GetGreeting()
        {
            return "Welcome to CyberAware — I can help you with passwords, phishing, scams, privacy and more. What's your name?";
        }

        public string ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            input = input.Trim();

            if (_awaitingName)
            {
                var name = input.Split(' ', ',', '.')[0];
                _memory.UserName = name;
                _awaitingName = false;
                return $"Nice to meet you, {name}! Tell me a topic you're interested in (e.g., privacy, phishing, passwords).";
            }

            var lowered = input.ToLowerInvariant();

            if (lowered.Contains("tell me more") || lowered.Contains("explain more") || lowered == "more")
            {
                if (!string.IsNullOrWhiteSpace(_lastTopicKey))
                {
                    var resp = _keywords.GetRandomResponseForKey(_lastTopicKey) ?? "I don't have more on that right now.";
                    return resp;
                }
            }

            var sentiment = _sentiment.Detect(input);
            var opener = _sentiment.GetSentimentOpener(sentiment);

            var kw = _keywords.TryGetResponse(input);
            if (kw != null)
            {
                _lastTopicKey = kw.Value.key;
                // store favourite topic if user explicitly mentions it
                if (lowered.Contains("i like ") || lowered.Contains("my favourite") || lowered.Contains("my favorite"))
                {
                    _memory.FavouriteTopic = kw.Value.key;
                }

                return (string.IsNullOrWhiteSpace(opener) ? "" : opener + " ") + kw.Value.response;
            }

            if (lowered.Contains("how are you")) return "I'm doing well — thanks for asking!";
            if (lowered.Contains("what can you do") || lowered.Contains("what can i ask"))
            {
                return "You can ask me about phishing, passwords, malware, privacy, ransomware, ddos and more.";
            }

            var fallbacks = new[] { "I didn't recognise that topic. Try selecting one from the Topics list, or ask about phishing, malware, passwords, encryption, or privacy.", "Sorry, I couldn't understand that — try asking about passwords or phishing.", "I can help with passwords, phishing, malware, encryption, and privacy. Which would you like to discuss?" };
            var r = new Random();
            return fallbacks[r.Next(fallbacks.Length)];
        }

        public MemoryStore GetMemory() => _memory;
    }
}