/*
 * File: SentimentDetector.cs
 * Purpose: Naive token-based sentiment detector used to slightly adapt bot tone.
 *
 * Implementation notes:
 * - Uses a small dictionary of trigger tokens mapped to Sentiment enum values.
 * - The detector is intentionally simple for demo/teaching purposes and is not
 *   intended to replace a production sentiment analysis model.
 * - To improve accuracy, consider integrating a statistical or ML model and
 *   normalizing input (stemming, stopword removal) before detection.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberAware
{
    public enum Sentiment { Neutral, Worried, Curious, Frustrated, Confident, Happy }

    public class SentimentDetector
    {
        private readonly Dictionary<Sentiment, List<string>> _triggers = new();

        public SentimentDetector()
        {
            _triggers[Sentiment.Worried] = new List<string>
            {
                "worried", "scared", "afraid", "anxious", "nervous", "unsafe", "concerned", "i don't feel safe", "dont feel safe"
            };

            _triggers[Sentiment.Curious] = new List<string>
            {
                "curious", "wonder", "interested", "want to know", "i want to know", "how does", "what is", "tell me", "tell me more"
            };

            _triggers[Sentiment.Frustrated] = new List<string>
            {
                "frustrated", "confused", "don't understand", "dont understand", "too much", "complicated", "i give up", "overwhelmed", "im overwhelmed", "it's too complicated", "its too complicated"
            };

            _triggers[Sentiment.Confident] = new List<string>
            {
                "i know", "i understand", "got it", "makes sense", "i'm good at", "i am good at", "im good at"
            };

            _triggers[Sentiment.Happy] = new List<string> { "great", "thanks", "helpful", "awesome", "love" };
        }

        public Sentiment Detect(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Sentiment.Neutral;
            var s = input.ToLowerInvariant();
            foreach (var kvp in _triggers)
            {
                foreach (var token in kvp.Value)
                {
                    if (s.Contains(token)) return kvp.Key;
                }
            }
            return Sentiment.Neutral;
        }

        public string GetSentimentOpener(Sentiment s)
        {
            return s switch
            {
                Sentiment.Worried => "I understand that can be worrying — here are some immediate steps to help:",
                Sentiment.Curious => "Great question — here's an interesting note to get you started:",
                Sentiment.Frustrated => "I hear you — let's simplify this to one small step:",
                Sentiment.Confident => "Nice — sounds like you've got a handle on this. Here's a slightly more advanced tip:",
                Sentiment.Happy => "Glad that helped — here's an extra tip:",
                _ => string.Empty,
            };
        }
    }
}