/*
 * File: Chatbot.cs
 * Purpose: Original console-based chatbot implementation kept for compatibility and
 * reference. This class implements a simple interactive console loop that prompts
 * for user input, forwards queries to ResponseHandler and types responses with
 * a small delay to simulate typing.
 *
 * Important notes:
 * - The WPF UI uses a separate frontend and richer interaction model; the console
 *   Chatbot is preserved so the original program behavior is still available.
 * - Avoid adding UI-specific logic to this file. For UI integration, use the
 *   ResponseHandler or create a dedicated UI adaptor class to bridge behavior.
 */
using System;
using System.IO;
using System.Threading;

namespace CyberAware
{
    public class Chatbot
    {
        private readonly ResponseHandler responseHandler = new();
        private readonly string userName;

        public Chatbot(string userName)
        {
            this.userName = userName;
        }

        public void Start()
        {
            // Check if this user has used the chatbot before.
            bool isReturning = false;
            try
            {
                var usersFile = Path.Combine(AppContext.BaseDirectory, "assets", "users.txt");
                if (File.Exists(usersFile))
                {
                    var users = File.ReadAllLines(usersFile);
                    foreach (var u in users)
                    {
                        if (string.Equals(u?.Trim(), userName, StringComparison.OrdinalIgnoreCase))
                        {
                            isReturning = true;
                            break;
                        }
                    }
                }
                else
                {
                    var dir = Path.GetDirectoryName(usersFile);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                }

                if (!isReturning)
                {
                    File.AppendAllText(usersFile, userName + Environment.NewLine);
                }
            }
            catch
            {
                // ignore persistence errors - non-critical
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            if (isReturning)
                Console.WriteLine($"Welcome back {userName}! Type 'help' to see available topics. Type 'exit' to leave the available topics.");
            else
                Console.WriteLine($"Hello {userName}, type 'help' to see available topics.");
            Console.ResetColor();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\nYou: ");
                Console.ResetColor();

                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.ToLower() == "exit")
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Goodbye it was nice having, you can come back anytime you want and you can ask about anything revolving around cybersecurity");
                    Console.WriteLine("Remember, awareness is your firewall. Stay safe online!");
                    Console.WriteLine("Made by Aphiwe Gans");
                    Console.ResetColor();
                    break;
                }

                // Allow multiple questions in one input by splitting on common separators
                var normalized = input.Replace(" and ", ",", StringComparison.OrdinalIgnoreCase)
                                      .Replace(" then ", ",", StringComparison.OrdinalIgnoreCase);
                var parts = normalized.Split(new[] { '?', ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    var question = part.Trim();
                    if (string.IsNullOrWhiteSpace(question))
                        continue;

                    string response = responseHandler.GetResponse(question, userName);
                    TypeResponse(response);
                    // small pause between multiple responses
                    Thread.Sleep(250);
                }
            }
        }

        private void TypeResponse(string response, int charDelay = 15)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Bot: ");
            foreach (var ch in response)
            {
                Console.Write(ch);
                Thread.Sleep(charDelay);
            }
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}
