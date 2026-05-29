/*
 * File: ResponsiveHandler.cs
 * Purpose: Core bot logic that maps user input to helpful topic responses, handles sentiment-aware replies,
 * session memory and persistence, and exposes events for UI integration.
 *
 * Responsibilities and design choices:
 * - topics: a dictionary of canonical topic keys to full-length explanations.
 * - Tip pools: small lists of tips per topic to provide randomized, short-form guidance.
 * - Sessions: an in-memory per-user session cache that tracks LastTopic, FavoriteTopic and sentiment.
 * - Persistence: Load/Save helpers read and write lightweight JSON blobs to the assets folder to remember
 *   the last user and favourite topic between runs. Persistence failures are non-fatal and are silently ignored.
 * - Sentiment handling: Simple keyword-based detection that maps to a small set of empathy-prefixed replies
 *   and notifies the UI through the SentimentDetected event so it can display supportive UI affordances.
 *
 * Notes for maintainers:
 * - This file contains substantial logic. For larger projects consider splitting concerns into dedicated classes
 *   (e.g., TopicRepository, SessionStore, SentimentService) to improve testability and separation.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text;

namespace CyberAware
{
    public class ResponseHandler
    {
        // Core topics dictionary (kept as before, extended with a few additional keys)
        private readonly Dictionary<string, string> topics = new()
        {
            { "password", "Strong passwords are your first line of defense. Use a long, unique passphrase for each account, combine upper and lower case letters, numbers and symbols, and avoid dictionary words or obvious substitutions. Consider using a reputable password manager to generate and store complex passwords safely, and enable two-factor authentication where available to add an extra layer of protection." },
            { "phishing", "Phishing is a cyberattack where criminals trick you into revealing sensitive information by impersonating trusted organizations. Phishing can arrive by email, text, or spoofed websites. Look for signs like unexpected requests for credentials, poor spelling or grammar, mismatched URLs, and urgent language. Always verify senders, avoid clicking links in unsolicited messages, and use multi-factor authentication to limit the damage of credential theft." },
            { "safe browsing", "Safe browsing means being cautious online to reduce exposure to threats. Use up-to-date browsers, keep extensions to a minimum, and avoid downloading files from unknown sources. Check site certificates on sensitive pages, prefer HTTPS, and be careful when entering personal information. Use privacy settings, ad-blockers if appropriate, and enable security features like site isolation to reduce risk." },
            { "malware", "Malware is malicious software designed to damage, steal, or control systems. It includes viruses, worms, trojans, ransomware, spyware, and more. Prevent malware by keeping software updated, running reputable antivirus or endpoint protection, avoiding dubious downloads and attachments, and applying the principle of least privilege. Back up important data regularly to recover from attacks like ransomware." },
            { "identity theft", "Identity theft occurs when criminals steal personal information to impersonate you, access accounts, or commit fraud. Protect yourself by monitoring credit reports, using strong unique passwords, enabling two-factor authentication, shredding sensitive documents, and being cautious about sharing personal data online. If you suspect theft, contact banks and credit agencies immediately and consider a fraud alert or freeze on your credit." },
            { "social media", "Social media can expose you to risks if you overshare personal information, accept unknown friend requests, or click on suspicious links. Adjust privacy settings to limit who can see your posts, avoid posting sensitive details like vacation dates or addresses, and be mindful of phishing attempts that come through messages. Use strong account security and review connected apps regularly." },
            { "two factor", "Two-factor authentication (2FA) adds an extra layer of security by requiring a second verification step beyond a password, such as a code from an authenticator app or a hardware token. 2FA greatly reduces the risk of account takeover even if passwords are compromised. Prefer authenticator apps or hardware keys over SMS where possible, because SMS can be intercepted." },
            { "updates", "Software updates are critical for security because they often include patches for vulnerabilities that attackers can exploit. Keep your operating system, applications, and firmware up to date. Enable automatic updates when practical, and test updates in critical environments to avoid disruptions. Regular patching is one of the most effective ways to reduce risk." },
            { "wifi", "Public Wi-Fi networks are convenient but often insecure; attackers may intercept traffic on open hotspots. Avoid accessing sensitive sites on public Wi-Fi, or use a trusted VPN to encrypt your connection. Ensure your home Wi-Fi uses a strong WPA2/WPA3 password and consider hiding the SSID or using a guest network for visitors." },
            { "privacy", "Privacy means controlling how your personal information is collected and shared online. Limit data sharing, review app and social media permissions, use strong account controls, and prefer services with clear privacy practices. Consider using privacy-enhancing tools like tracker blockers and private browsing modes." },
            { "virus", "A computer virus is a type of malware that attaches to legitimate programs and propagates when those programs run. Modern threats are diverse, including ransomware and trojans that don’t behave like traditional viruses. Use layered defenses: keep software patched, run endpoint protection, avoid suspicious attachments, and back up data so you can recover from infection." },
            { "purpose", "My purpose is to increase people’s knowledge and understanding about cybersecurity, helping you stay safe online. I provide practical tips, explain common threats, and suggest steps you can take to reduce risk. If you have specific scenarios or concerns, ask and I’ll give actionable guidance tailored to your situation." },
            { "how are you", "I’m doing well — thank you for asking! I’m here to help you learn about cybersecurity and answer questions. Tell me a topic you’d like to explore and I’ll provide guidance, examples, and resources to help you stay safe online." },
            { "what can i ask", "You can ask me about cybersecurity topics such as passwords, phishing, malware, safe browsing, scams, identity theft, social media safety, two-factor authentication, software updates, public Wi-Fi, computer viruses, and also practical steps for securing personal devices and accounts. If you have a real-world scenario, describe it and I’ll help you analyze risks and protections." }
            ,
            { "scam", "Scams are fraudulent schemes designed to trick people into sending money, revealing personal information, or installing malicious software. Common signs include unexpected requests for payment or gift cards, pressure to act quickly, unsolicited contact, and requests for personal or financial information. Verify requests via independent channels, never send money to unknown parties, and report scams to the platform or authorities." },
            { "ransomware", "Definition: Ransomware is malicious software that encrypts data or locks systems and demands a ransom to restore access. Explanation: It spreads via phishing, exploit kits, or unsecured remote access and can target individuals and organizations, often encrypting backups to maximize impact. Solution: Maintain offline, tested backups; keep systems patched; use endpoint protection; employ least-privilege access; segment networks; and train users to recognise phishing attempts." },
            { "ddos", "Definition: A DDoS (Distributed Denial of Service) attack overwhelms a target service with excessive traffic to make it unavailable. Explanation: Attackers use botnets or reflected amplification to flood bandwidth or exhaust resources, causing outages and degraded performance. Solution: Use rate-limiting, upstream DDoS protection, CDN services, traffic filtering, redundant infrastructure, and incident response plans to mitigate and recover from attacks." },
            { "zero-day", "Definition: A zero-day vulnerability is a software flaw unknown to the vendor and exploitable by attackers before a patch is available. Explanation: Because no fix exists initially, attackers can weaponize zero-days for high-impact breaches or espionage. Solution: Employ defense-in-depth: timely threat monitoring, behavior-based detection, application whitelisting, network segmentation, rapid patching once advisories publish, and vendor mitigation workarounds when available." },
            { "encryption", "Definition: Encryption converts data into a coded form to prevent unauthorized access. Explanation: Proper encryption protects data at rest and in transit from interception or theft; key management and algorithm choice are critical. Solution: Use strong, modern algorithms (AES-256, TLS 1.2+/TLS 1.3), protect keys with hardware security modules or secure vaults, enforce encryption for backups and communications, and rotate keys per policy." },
            { "insider threat", "Definition: An insider threat occurs when a current or former employee, contractor, or partner intentionally or accidentally causes harm or data loss. Explanation: Risks include data exfiltration, careless credential handling, or abuse of privileges. Solution: Implement least-privilege access, monitor user activity, enforce separation of duties, use data loss prevention (DLP), conduct background checks, and provide regular security awareness training." },
            { "supply chain", "Definition: A supply chain attack targets software or hardware vendors to compromise downstream customers. Explanation: Attackers compromise updates, third-party libraries, or vendor systems to distribute malware or backdoors at scale. Solution: Vet vendors, apply integrity checks (signatures/hashes), monitor for unusual behavior, use dependency scanning, and keep critical systems isolated and monitored." },
            { "iot security", "Definition: IoT security covers protecting internet-connected devices like cameras, sensors, and appliances. Explanation: Many IoT devices are resource constrained and shipped with weak defaults, making them attractive targets for compromise and botnet recruitment. Solution: Change default credentials, keep firmware updated, segment IoT networks, disable unnecessary services, and choose devices from vendors with security practices and update policies." },
            { "cloud security", "Definition: Cloud security involves protecting data, applications, and services hosted in cloud environments. Explanation: Misconfigurations, excessive permissions, and insecure APIs are common cloud risks that can lead to data exposure. Solution: Use strong identity and access management (IAM), enable logging and monitoring, encrypt data, apply the principle of least privilege, automate configuration checks, and follow provider security best practices." }
        };

        // Randomized tip pools to keep responses varied and engaging
        private readonly List<string> phishingTips = new()
        {
            "Tip: Always verify the sender's email address and hover over links to see the real destination before clicking.",
            "Tip: Be wary of urgent or threatening language asking you to act immediately—scammers use pressure to force mistakes.",
            "Tip: Never provide credentials or sensitive data in response to unsolicited messages; use official channels to verify requests.",
            "Tip: Check for mismatched domain names or subtle typos in URLs—attackers often create convincing look-alikes."
        };

        private readonly List<string> passwordTips = new()
        {
            "Use a long passphrase made of multiple unrelated words rather than a single word.",
            "Consider a reputable password manager to create and store unique passwords for every account.",
            "Enable two-factor authentication (2FA) wherever possible to add an extra layer of protection.",
            "Avoid reusing passwords across important accounts; compromise on one site can cascade elsewhere."
        };

        // Privacy-focused tips to respond when the user asks about privacy
        private readonly List<string> privacyTips = new()
        {
            "Tip: Review and tighten privacy settings on social platforms; limit who can see your posts and profile details.",
            "Tip: Be cautious sharing personal details online (birthdate, address, phone); attackers can use this for identity theft.",
            "Tip: Use browser privacy features (block third-party cookies, enable tracking protection) and consider privacy-focused extensions.",
            "Tip: Regularly review app permissions on your devices and revoke access that isn’t necessary."
        };

        private readonly List<string> scamTips = new()
        {
            "If someone asks for money or gift cards unexpectedly, pause and verify via an independent channel.",
            "Report scams to the appropriate platform or authority; reporting helps protect others.",
            "Don't trust messages asking for personal or financial information—contact the organisation directly using known contact details."
        };

        // Random instance used to pick random tips from tip pools.
        // The System.Random class provides pseudo-random numbers; here we use
        // rng.Next(pool.Count) to choose an index into a List<string> of tips.
        // This keeps responses varied so users don't always get the same tip.
        private readonly Random rng = new();

        // General tips to use when the user expresses emotion but no specific topic is mentioned
        private readonly List<string> generalTips = new()
        {
            "Quick tip: If you're unsure about messages, don't click links or download attachments until you verify the sender.",
            "Quick tip: Use unique passwords and enable two-factor authentication to reduce the chance of account takeover.",
            "Quick tip: Keep backups of important files and ensure your system and applications are up to date.",
            "Quick tip: If you feel overwhelmed, take a short break and tackle one security step at a time (e.g., enable 2FA on your main email)."
        };

        // Simple per-user session memory to support follow-ups and personalization
        private class UserSession
        {
            public string? LastTopic { get; set; }
            public int TipIndex { get; set; }
            public string? FavoriteTopic { get; set; }
            public string? Sentiment { get; set; }
        }

        // Simple memory DTO used by LoadMemory/SaveMemory to persist the active user and their favourite topic
        public class UserMemory
        {
            public string? UserName { get; set; }
            public string? FavoriteTopic { get; set; }
        }

        private static readonly Dictionary<string, UserSession> sessions = new(StringComparer.OrdinalIgnoreCase);

        // Sentiment detection delegate and event so UI can react (empathetic messages / UI changes)
        public delegate void SentimentDelegate(string userName, string sentiment, string originalInput);
        public event SentimentDelegate? SentimentDetected;

        // Favorite topic delegate and event so UI can react when a user sets a favourite
        public delegate void FavoriteDelegate(string userName, string favouriteTopic);
        public event FavoriteDelegate? FavoriteUpdated;

        // Expose some session info for the UI to query (read-only)
        public string? GetFavoriteTopic(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;
            if (sessions.TryGetValue(userName, out var s)) return s.FavoriteTopic;
            return null;
        }

        public string? GetLastTopic(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;
            if (sessions.TryGetValue(userName, out var s)) return s.LastTopic;
            return null;
        }

        public string? GetSentiment(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;
            if (sessions.TryGetValue(userName, out var s)) return s.Sentiment;
            return null;
        }

        public string GetResponse(string input, string userName)
        {
            input = input.ToLower();

            // If caller has not supplied a user name yet, try to load last-known memory
            if (string.IsNullOrWhiteSpace(userName))
            {
                var mem = LoadMemory();
                if (!string.IsNullOrWhiteSpace(mem?.UserName))
                {
                    userName = mem.UserName!;
                }
            }

            // If still no user name, check the input for an explicit name declaration
            if (string.IsNullOrWhiteSpace(userName))
            {
                var extractedName = TryExtractUserName(input);
                if (!string.IsNullOrWhiteSpace(extractedName))
                {
                    userName = extractedName;
                    // create session for this new name and persist as current
                    lock (sessions)
                    {
                        if (!sessions.ContainsKey(userName)) sessions[userName] = new UserSession();
                    }
                    SaveMemory(new UserMemory { UserName = userName, FavoriteTopic = sessions[userName].FavoriteTopic });
                }
            }

            // If still missing userName, prompt startup sequence (name is required to continue)
            if (string.IsNullOrWhiteSpace(userName))
            {
                return GetStartupPrompt();
            }

            // Ensure a session exists (try loading from disk first)
            lock (sessions)
            {
                if (!sessions.ContainsKey(userName))
                {
                    var loaded = LoadSessionFromDisk(userName);
                    sessions[userName] = loaded ?? new UserSession();
                    // If no favourite topic is set yet, default to a friendly starter topic
                    // so the UI has something to display. "how are you" is a lightweight
                    // conversational starter present in the topics dictionary.
                    try
                    {
                        var sess = sessions[userName];
                        if (string.IsNullOrWhiteSpace(sess.FavoriteTopic))
                        {
                            sess.FavoriteTopic = "how are you";
                            // persist defaults so UI can read the favourite immediately
                            try { SaveSessionToDisk(userName); } catch { }
                            try { SaveMemory(new UserMemory { UserName = userName, FavoriteTopic = sess.FavoriteTopic }); } catch { }
                            try { FavoriteUpdated?.Invoke(userName, sess.FavoriteTopic); } catch { }
                        }
                    }
                    catch { }
                }
            }

            var session = sessions[userName];

            // If we have a persisted memory that includes a favourite topic, ensure session reflects it
            try
            {
                var mem = LoadMemory();
                if (mem != null && string.Equals(mem.UserName, userName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(mem.FavoriteTopic) && string.IsNullOrWhiteSpace(session.FavoriteTopic))
                        session.FavoriteTopic = mem.FavoriteTopic;
                }
            }
            catch { }

            // Basic sentiment detection: map keywords to simple sentiment labels
            var sentiment = DetectSentiment(input);
            if (sentiment != null)
            {
                session.Sentiment = sentiment;
                // persist sentiment change
                try { SaveSessionToDisk(userName); } catch { }
                // Notify any subscribers (UI) about the sentiment so they can adjust tone/appearance
                try { SentimentDetected?.Invoke(userName, sentiment, input); } catch { }

                // Determine a relevant topic to tie the tip to: prefer explicit topic in input, else favorite topic
                var matchedTopic = topics.Keys.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k) && input.Contains(k, StringComparison.OrdinalIgnoreCase));
                var relevantTopic = matchedTopic ?? session.FavoriteTopic;

                // Choose a short, focused tip based on the relevant topic or general tips
                string chosenTip;
                if (!string.IsNullOrWhiteSpace(relevantTopic) && topics.ContainsKey(relevantTopic))
                {
                    if (string.Equals(relevantTopic, "phishing", StringComparison.OrdinalIgnoreCase)) chosenTip = GetRandomFromPool(phishingTips, userName);
                    else if (string.Equals(relevantTopic, "password", StringComparison.OrdinalIgnoreCase)) chosenTip = GetRandomFromPool(passwordTips, userName);
                    else if (string.Equals(relevantTopic, "privacy", StringComparison.OrdinalIgnoreCase)) chosenTip = GetRandomFromPool(privacyTips, userName);
                    else if (string.Equals(relevantTopic, "scam", StringComparison.OrdinalIgnoreCase)) chosenTip = GetRandomFromPool(scamTips, userName);
                    else chosenTip = ShortenToSentences(topics[relevantTopic], 1);
                }
                else
                {
                    chosenTip = generalTips[rng.Next(generalTips.Count)];
                }

                // Map sentiment to an empathy prefix and adjust tone
                string empathyPrefix;
                switch (sentiment)
                {
                    case "worried":
                        empathyPrefix = "It's completely understandable to feel worried — that can be unsettling.";
                        break;
                    case "frustrated":
                        empathyPrefix = "I hear you — that can be frustrating. Let's make this simple.";
                        break;
                    case "curious":
                        empathyPrefix = "Great question — curiosity is how you learn more.";
                        break;
                    case "confident":
                        empathyPrefix = "Nice — sounds like you're comfortable with this. Here's a slightly more advanced tip.";
                        break;
                    default:
                        empathyPrefix = "Thanks for sharing how you feel — here are a few practical steps.";
                        break;
                }

                // Compose a concise response: acknowledge emotion then give the tip
                var composed = empathyPrefix + " " + ShortenToSentences(chosenTip, 1);

                // If a matched topic was present and it's not already in the tip, append a one-sentence explanation
                if (!string.IsNullOrWhiteSpace(matchedTopic) && !composed.Contains(matchedTopic, StringComparison.OrdinalIgnoreCase) && topics.ContainsKey(matchedTopic))
                {
                    composed += " " + ShortenToSentences(topics[matchedTopic], 1);
                }

                return Personalize(userName, composed);
            }

            // If a sentiment word and a topic both appear in the same sentence, prefer to
            // provide the topic explanation while still notifying the UI about sentiment.
            // This explicitly checks for topic keywords (word-boundary tolerant) early so
            // inputs like "I'm worried about scams." trigger both behaviors.
            // Before we match topics, check whether the user is explicitly stating a favourite topic
            // such as "my favourite topic is phishing" or "I like passwords". If so, record it
            // and set LastTopic so the UI will display it the same way as other topic selections.
            try
            {
                var favEarly = TryExtractFavoriteTopic(input);
                if (!string.IsNullOrEmpty(favEarly))
                {
                    session.FavoriteTopic = favEarly;
                    session.LastTopic = favEarly;
                    try { FavoriteUpdated?.Invoke(userName, favEarly); } catch { }
                    try { SaveSessionToDisk(userName); } catch { }
                    try { SaveMemory(new UserMemory { UserName = userName, FavoriteTopic = favEarly }); } catch { }
                    return Personalize(userName, $"Thanks — I’ll remember that your favourite topic is '{favEarly}'. I can share more about it anytime.");
                }
            }
            catch { }

            try
            {
                var matched = topics.Keys.Where(k => !string.IsNullOrWhiteSpace(k) && input.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matched.Count > 0)
                {
                    var topicKey = matched.First();
                    session.LastTopic = topicKey;
                    if (string.Equals(topicKey, "phishing", StringComparison.OrdinalIgnoreCase))
                        return Personalize(userName, GetRandomFromPool(phishingTips, userName));
                    if (string.Equals(topicKey, "password", StringComparison.OrdinalIgnoreCase))
                        return Personalize(userName, GetRandomFromPool(passwordTips, userName));
                    if (string.Equals(topicKey, "privacy", StringComparison.OrdinalIgnoreCase))
                        return Personalize(userName, GetRandomFromPool(privacyTips, userName));
                    if (string.Equals(topicKey, "scam", StringComparison.OrdinalIgnoreCase))
                        return Personalize(userName, GetRandomFromPool(scamTips, userName));

                    return Personalize(userName, topics[topicKey]);
                }
            }
            catch { }

            // If the user entered a number (e.g. from the help menu), map it to the topic by index.
            var trimmed = input.Trim();
            if (int.TryParse(trimmed, out var idx))
            {
                if (idx >= 1 && idx <= topics.Count)
                {
                    var topicKey = topics.Keys.ElementAt(idx - 1);
                    session.LastTopic = topicKey;
                    return Personalize(userName, topics[topicKey]);
                }
            }

            var synonyms = new Dictionary<string, string>
            {
                // removed mapping to "scam" so fraud/con won't resolve to a removed topic
                { "hackers", "malware" }, { "hacker", "malware" },
                { "spyware", "malware" }, { "trojan", "malware" },
                { "identity", "identity theft" }, { "social", "social media" },
                { "2fa", "two factor" }, { "authentication", "two factor" },
                { "update", "updates" }, { "patch", "updates" },
                { "internet", "wifi" }, { "network", "wifi" }
            };

            foreach (var kvp in synonyms)
            {
                if (input.Contains(kvp.Key))
                {
                    session.LastTopic = kvp.Value;
                    return Personalize(userName, topics[kvp.Value]);
                }
            }

            // Handle follow-up requests (e.g., "give me another tip", "tell me more", "explain more")
            if (IsFollowUpRequest(input) && !string.IsNullOrWhiteSpace(session.LastTopic))
            {
                return Personalize(userName, GetFollowUpForTopic(session.LastTopic, userName));
            }

            foreach (var topic in topics.Keys)
            {
                int distance = LevenshteinDistance(input, topic);
                if (distance > 0 && distance <= 2)
                {
                    // In GUI we avoid console prompts; assume suggestion and provide helpful response
                    session.LastTopic = topic;
                    return Personalize(userName, $"I think you meant '{topic}'. {topics[topic]}");
                }

                foreach (var word in input.Split(' '))
                {
                    int wordDistance = LevenshteinDistance(word, topic);
                    if (wordDistance > 0 && wordDistance <= 2)
                    {
                        session.LastTopic = topic;
                        return Personalize(userName, $"I think you meant '{topic}'. {topics[topic]}");
                    }
                }
            }

            foreach (var kvp in topics)
            {
                if (input.Contains(kvp.Key))
                {
                    // remember the active topic for follow-ups
                    session.LastTopic = kvp.Key;
                    // Special handling for certain topics to provide randomised tips
                    if (kvp.Key == "phishing")
                        return Personalize(userName, GetRandomFromPool(phishingTips, userName));
                    if (kvp.Key == "password")
                        return Personalize(userName, GetRandomFromPool(passwordTips, userName));
                    if (kvp.Key == "privacy")
                        return Personalize(userName, GetRandomFromPool(privacyTips, userName));
                    if (kvp.Key == "scam")
                        return Personalize(userName, GetRandomFromPool(scamTips, userName));

                    return Personalize(userName, kvp.Value);
                }
            }

            if (input.Contains("help"))
            {
                // Creative, cyan-colored menu with perfectly aligned frame
                Console.ForegroundColor = ConsoleColor.Cyan;
                int innerWidth = 58;
                Console.WriteLine("╔" + new string('═', innerWidth) + "╗");
                string header = "📚 Topics you can ask me about (type a topic or number)";
                // center the header within the inner width
                string headerCentered = header.PadLeft((innerWidth + header.Length) / 2).PadRight(innerWidth);
                Console.WriteLine("║" + headerCentered + "║");
                Console.WriteLine("╠" + new string('═', innerWidth) + "╣");
                int i = 1;
                foreach (var topic in topics.Keys)
                {
                    Console.WriteLine($"║  {i.ToString().PadLeft(2)}. {topic.PadRight(52)}║");
                    i++;
                }
                Console.WriteLine("╚" + new string('═', innerWidth) + "╝");
                Console.ResetColor();
                return Personalize(userName, "Type one of these topics to learn more!");
            }

            // Capturing simple user-stated preferences, e.g. "my favourite topic is phishing" or "i like passwords"
            var fav = TryExtractFavoriteTopic(input);
            if (!string.IsNullOrEmpty(fav))
            {
                session.FavoriteTopic = fav;
                try { SaveSessionToDisk(userName); } catch { }
                try { SaveMemory(new UserMemory { UserName = userName, FavoriteTopic = fav }); } catch { }
                return Personalize(userName, $"Thanks — I’ll remember that your favourite topic is '{fav}'. I can share more about it anytime.");
            }

            // If favourite topic is missing, ask for it before proceeding to general answers
            if (string.IsNullOrWhiteSpace(session.FavoriteTopic))
            {
                return Personalize(userName, "Thanks — could you tell me which cybersecurity topic interests you most (e.g., privacy, phishing, passwords, encryption)?");
            }

            // Support simple commands: change name / forget me
            var lowered = input.ToLowerInvariant();
            if (lowered.Contains("change name to") || lowered.Contains("change my name to") || lowered.StartsWith("my name is ") || lowered.StartsWith("i am ") || lowered.StartsWith("i'm "))
            {
                var newName = TryExtractUserName(input);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    // move session to new name
                    lock (sessions)
                    {
                        sessions.Remove(userName);
                        if (!sessions.ContainsKey(newName)) sessions[newName] = new UserSession();
                        sessions[newName].FavoriteTopic = session.FavoriteTopic;
                    }
                    SaveMemory(new UserMemory { UserName = newName, FavoriteTopic = sessions[newName].FavoriteTopic });
                    return Personalize(newName, $"Nice to meet you, {newName}. I updated your name.");
                }
            }

            if (lowered.Contains("forget me") || lowered.Contains("clear memory") || lowered.Contains("clear my data"))
            {
                // delete persisted files and session
                try { File.Delete(GetSessionFilePath(userName)); } catch { }
                try { File.Delete(GetCurrentMemoryPath()); } catch { }
                lock (sessions) { sessions.Remove(userName); }
                return "I have cleared your saved name and favourite topic. If you'd like, tell me your name and favourite topic to get started again.";
            }

            return Personalize(userName, "I didn’t quite understand that. Could you rephrase or try asking about password, scam, phishing or privacy?");
        }

        private string? DetectSentiment(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.ToLowerInvariant();
            if (input.Contains("worri") || input.Contains("scared") || input.Contains("afraid") || input.Contains("concern") || input.Contains("anxious") || input.Contains("nerv") || input.Contains("i don't feel safe")) return "worried";
            if (input.Contains("curious") || input.Contains("interested") || input.Contains("i want to know") || input.Contains("how does") || input.Contains("what is") || input.Contains("tell me more")) return "curious";
            if (input.Contains("frustrat") || input.Contains("confus") || input.Contains("i don't understand") || input.Contains("dont understand") || input.Contains("this is too much") || input.Contains("i give up") || input.Contains("it's too complicated") || input.Contains("im overwhelmed") ) return "frustrated";
            if (input.Contains("i know") || input.Contains("i understand") || input.Contains("got it") || input.Contains("makes sense") || input.Contains("i'm good at") || input.Contains("i am good at")) return "confident";
            return null;
        }

        private bool IsFollowUpRequest(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.ToLowerInvariant();
            return input.Contains("another tip") || input.Contains("give me another tip") || input.Contains("tell me more") || input.Contains("explain more") || input.Equals("more") || input.Contains("explain");
        }

        private string GetFollowUpForTopic(string topic, string userName)
        {
            var session = sessions[userName];
            if (string.Equals(topic, "phishing", StringComparison.OrdinalIgnoreCase))
            {
                // cycle through tips to avoid repetition
                session.TipIndex = (session.TipIndex + 1) % Math.Max(1, phishingTips.Count);
                return GetRandomFromPool(phishingTips, userName);
            }
            if (string.Equals(topic, "password", StringComparison.OrdinalIgnoreCase))
            {
                session.TipIndex = (session.TipIndex + 1) % Math.Max(1, passwordTips.Count);
                return GetRandomFromPool(passwordTips, userName);
            }
            if (string.Equals(topic, "privacy", StringComparison.OrdinalIgnoreCase))
            {
                session.TipIndex = (session.TipIndex + 1) % Math.Max(1, privacyTips.Count);
                return GetRandomFromPool(privacyTips, userName);
            }
            if (string.Equals(topic, "scam", StringComparison.OrdinalIgnoreCase))
            {
                session.TipIndex = (session.TipIndex + 1) % Math.Max(1, scamTips.Count);
                return GetRandomFromPool(scamTips, userName);
            }
            // default: return the long-form topic explanation again
            return topics.ContainsKey(topic) ? topics[topic] : "I can share more, tell me what you'd like to know about this topic.";
        }

        private string GetRandomFromPool(List<string> pool, string userName)
        {
            if (pool == null || pool.Count == 0) return string.Empty;
            // small attempt to reduce exact repetition using session index
            var session = sessions[userName];
            int idx = rng.Next(pool.Count);
            // if TipIndex set use that to select predictable next element
            if (session.TipIndex >= 0 && session.TipIndex < pool.Count)
            {
                idx = session.TipIndex;
            }
            // rotate index for next time
            session.TipIndex = (idx + 1) % pool.Count;
            // persist tip index rotation
            try { SaveSessionToDisk(userName); } catch { }
            return pool[idx];
        }

        // Persist and load simple per-user session state to AppData so preferences survive restarts
        private static string GetSessionFilePath(string userName)
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberAware", "sessions");
            try { Directory.CreateDirectory(baseDir); } catch { }
            // simple sanitization
            var fileName = string.Concat(userName.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "unknown";
            return Path.Combine(baseDir, fileName + ".json");
        }

        private UserSession? LoadSessionFromDisk(string userName)
        {
            try
            {
                var path = GetSessionFilePath(userName);
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var s = JsonSerializer.Deserialize<UserSession>(json, opts);
                return s;
            }
            catch {
                return null;
            }
        }

        private void SaveSessionToDisk(string userName)
        {
            try
            {
                var path = GetSessionFilePath(userName);
                var opts = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(sessions[userName], opts);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        // Startup greeting prompt required by the UI flow
        public string GetStartupPrompt()
        {
            return "Hi! I'm CyberAware, your personal cybersecurity assistant. Before we dive in, could you tell me your name and which cybersecurity topic interests you most — such as privacy, phishing, passwords, or encryption? That way I can make all my advice personal to you!";
        }

        private string GetCurrentMemoryPath()
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberAware");
            try { Directory.CreateDirectory(baseDir); } catch { }
            return Path.Combine(baseDir, "current.json");
        }

        public void SaveMemory(UserMemory m)
        {
            try
            {
                var path = GetCurrentMemoryPath();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(m, opts);
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch { }
        }

        public UserMemory? LoadMemory()
        {
            try
            {
                var path = GetCurrentMemoryPath();
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path, Encoding.UTF8);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<UserMemory>(json, opts);
            }
            catch { return null; }
        }

        private string? TryExtractUserName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var s = input.Trim();
            var lowered = s.ToLowerInvariant();
            string[] patterns = new[] { "my name is ", "i am ", "i'm ", "im ", "call me " };
            foreach (var p in patterns)
            {
                int idx = lowered.IndexOf(p, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var start = idx + p.Length;
                    if (start >= s.Length) continue;
                    var rest = s.Substring(start).Trim();
                    // take first token as name
                    var nameToken = rest.Split(new[] { ' ', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(nameToken))
                    {
                        // Capitalize first letter
                        return char.ToUpperInvariant(nameToken[0]) + (nameToken.Length > 1 ? nameToken.Substring(1) : string.Empty);
                    }
                }
            }
            return null;
        }

        private string? TryExtractFavoriteTopic(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            // look for patterns like "my favourite topic is phishing" or "i like passwords"
            // If the user is asking a question, do not treat it as setting a favourite topic
            var trimmedInput = input.Trim();
            if (trimmedInput.EndsWith("?")) return null;
            var firstWord = trimmedInput.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            var interrogatives = new[] { "what", "how", "why", "is", "are", "do", "does", "did", "can", "could", "should", "will", "would", "where", "when" };
            if (interrogatives.Contains(firstWord, StringComparer.OrdinalIgnoreCase)) return null;

            input = input.ToLowerInvariant();

            // Only treat input as setting a favourite when the user explicitly expresses preference
            string[] patterns = new[] { "my favourite is ", "my favorite is ", "my favourite topic is ", "my favorite topic is ", "i like ", "i'm into ", "i am into ", "i am interested in ", "i'm interested in ", "favorite: ", "favourite: " };
            foreach (var p in patterns)
            {
                var idx = input.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var start = idx + p.Length;
                    if (start >= input.Length) continue;
                    var rest = input[start..].Trim();
                    // If rest contains a known topic, prefer that
                    foreach (var t in topics.Keys)
                    {
                        if (rest.Contains(t, StringComparison.OrdinalIgnoreCase)) return t;
                    }
                    // otherwise return first token as free-text topic
                    var token = rest.Split(new[] { ' ', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(token)) return token.ToLowerInvariant();
                }
            }

            return null; // no explicit favourite phrase found
        }

        private int LevenshteinDistance(string a, string b)
        {
            int[,] dp = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                                        dp[i - 1, j - 1] + cost);
                }
            }
            return dp[a.Length, b.Length];
        }

        private string Personalize(string userName, string content)
        {
            // Use the provided userName as the persona prefix so the bot speaks using that name.
            // Ensure responses are concise (prefer 1-3 sentences) and personalize using favourite topic when possible.
            string trimmed = ShortenToSentences(content, 2);

            if (string.IsNullOrWhiteSpace(userName))
                return trimmed;

            // build base reply with user name
            var reply = new StringBuilder();
            reply.Append(char.ToUpperInvariant(userName[0]) + userName.Substring(1));
            reply.Append(", ");
            // ensure first character of trimmed is capitalised
            if (trimmed.Length > 0)
            {
                reply.Append(char.ToUpperInvariant(trimmed[0]));
                if (trimmed.Length > 1) reply.Append(trimmed.Substring(1));
            }

            // If we have a stored favourite topic and the content doesn't already reference it,
            // append a short, focused tip tied to that topic (keeps overall reply to 2-4 sentences).
            try
            {
                if (sessions.TryGetValue(userName, out var s) && !string.IsNullOrWhiteSpace(s.FavoriteTopic))
                {
                    var fav = s.FavoriteTopic!;
                    if (!trimmed.Contains(fav, StringComparison.OrdinalIgnoreCase))
                    {
                        // pick a short tip based on fav
                        string tip = null;
                        if (string.Equals(fav, "phishing", StringComparison.OrdinalIgnoreCase) && phishingTips.Count > 0) tip = phishingTips[rng.Next(phishingTips.Count)];
                        else if (string.Equals(fav, "password", StringComparison.OrdinalIgnoreCase) && passwordTips.Count > 0) tip = passwordTips[rng.Next(passwordTips.Count)];
                        else if (string.Equals(fav, "privacy", StringComparison.OrdinalIgnoreCase) && privacyTips.Count > 0) tip = privacyTips[rng.Next(privacyTips.Count)];
                        else if (string.Equals(fav, "scam", StringComparison.OrdinalIgnoreCase) && scamTips.Count > 0) tip = scamTips[rng.Next(scamTips.Count)];
                        else if (topics.ContainsKey(fav)) tip = ShortenToSentences(topics[fav], 1);
                        if (!string.IsNullOrWhiteSpace(tip))
                        {
                            reply.Append(" As someone interested in ");
                            reply.Append(fav);
                            reply.Append(", here's a short tip: ");
                            reply.Append(ShortenToSentences(tip, 1));
                        }
                    }
                }
            }
            catch { }

            return reply.ToString();
        }

        // Return the UI startup sequence stages so the UI can enforce blank -> audio -> name prompt -> main UI
        public string[] GetStartupSequence()
        {
            return new[] { "blank", "audio", "namePrompt", "mainUI" };
        }

        private string ShortenToSentences(string text, int maxSentences)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            // naive sentence splitting
            var splits = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .Where(s => !string.IsNullOrWhiteSpace(s))
                             .ToArray();
            if (splits.Length == 0) return text.Trim();
            var take = Math.Min(maxSentences, splits.Length);
            var result = string.Join(". ", splits.Take(take)) + ".";
            return result;
        }
    }
}

