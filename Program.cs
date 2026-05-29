/*
 * File: Program.cs
 * Purpose: Reference console entrypoint preserved for Part 1 compatibility.
 *
 * Notes:
 * - The console Main method is commented out because the project currently
 *   targets a WPF application startup. If you need to run the console app,
 *   change the project OutputType to Exe and adjust App.xaml accordingly.
 * - Keep the original console flow intact as a reference for instructors or
 *   graders who may exercise the console scenario.
 */
using System;

namespace CyberAware
{
    class Program
    {
        // Console entrypoint from Part 1 is preserved here as a reference but is not used by the WPF application.
        // To run the original console version, change the project OutputType back to Exe and remove the WPF App.xaml startup.
        /*
        static void Main(string[] args)
        {
            GreetingManager greetingManager = new GreetingManager();
            // Show greeting (includes playing audio) before asking for the user's name
            greetingManager.ShowGreeting();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Please enter your name: ");
            Console.ResetColor();
            string userName = Console.ReadLine() ?? "User";

            Chatbot chatbot = new Chatbot(userName);
            chatbot.Start();
        }
        */
    }
}
