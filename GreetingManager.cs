/*
 * File: GreetingManager.cs
 * Purpose: Orchestrates the startup greeting sequence for the application.
 *
 * Behavior:
 * - Plays greeting audio (assets/greeting.wav) using AudioPlayer. Playback is
 *   best-effort; if audio playback fails the app proceeds without blocking.
 * - After audio playback the manager prints a stylized ASCII banner to the
 *   console. The ASCII banner is intended for the console version and for
 *   accessibility/debug scenarios; in the WPF UI the banner is shown in a
 *   top header area instead.
 *
 * Notes:
 * - Keep the ASCII art reasonably sized to avoid overwhelming small consoles.
 * - To enable audio place a WAV file at assets/greeting.wav and ensure the
 *   file is copied to the application output directory (csproj settings).
 */
using System;
using System.IO;

namespace CyberAware
{
    public class GreetingManager
    {
        private readonly AudioPlayer audioPlayer = new();

        public void ShowGreeting()
        {
            // Resolve path to the assets folder inside the app's output directory.
            // Ensure the greeting.wav is copied to output (see csproj change) and then load from there.
            string audioPath = Path.Combine(AppContext.BaseDirectory, "assets", "greeting.wav");
            // Play voice greeting first, then show ASCII art banner
            audioPlayer.Play(audioPath);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================================================================");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
    ██████╗██╗   ██╗██████╗ ███████╗██████╗  █████╗ ██╗    ██╗ █████╗ ██████╗ ███████╗
  ██╔════╝╚██╗ ██╔╝██╔══██╗██╔════╝██╔══██╗██╔══██╗██║    ██║██╔══██╗██╔══██╗██╔════╝
  ██║      ╚████╔╝ ██████╔╝█████╗  ██████╔╝███████║██║ █╗ ██║███████║██████╔╝█████╗  
  ██║       ╚██╔╝  ██╔══██╗██╔══╝  ██╔══██╗██╔══██║██║███╗██║██╔══██║██╔══██╗██╔══╝  
  ╚██████╗   ██║   ██████╔╝███████╗██║  ██║██║  ██║╚███╔███╔╝██║  ██║██║  ██║███████╗
   ╚═════╝   ╚═╝   ╚═════╝ ╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝ ╚══╝╚══╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝
                                                                                   
        ░▒▓█ SHADOW LAYER  █▓▒░
         ░▒▓████████████████████████▓▒░
            ░▒▓ CYBERAWARE ▓▒░

        Awareness is Your Firewall. Protect What Matters. Stay Alert. Stay Protected.
");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================================================================");
            Console.ResetColor();


        }
    }
}
