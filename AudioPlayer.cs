/*
 * File: AudioPlayer.cs
 * Purpose: Cross-platform helper to play short audio greetings (e.g., greeting.wav).
 * Detailed notes:
 * - Attempts to use System.Media.SoundPlayer on Windows for synchronous playback.
 * - On Linux/macOS, it tries common CLI players (aplay, ffplay, afplay) as fallbacks.
 * - Playback is best-effort: failures are caught and logged via Logger; the app
 *   continues gracefully if audio cannot be played on the user's platform.
 * - Keep audio files small and WAV-encoded for maximum compatibility with SoundPlayer.
 * - This file contains only helper logic; do not perform heavy audio processing here.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;

namespace CyberAware
{
    public class AudioPlayer
    {
        public void Play(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Logger.Warn($"Audio file not found at '{path}'. Skipping voice greeting. Place your greeting.wav in assets/greeting.wav to enable audio.");
                    return;
                }

                Logger.Info($"Attempting to play audio: {path}");

                // ✅ Windows playback using SoundPlayer directly
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        using var player = new SoundPlayer(path);
                        player.Load();
                        player.PlaySync();
                        Logger.Info("Played audio using System.Media.SoundPlayer.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"SoundPlayer failed: {ex.Message}");
                    }
                }

                // ✅ Linux fallback
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (TryRunProcess("aplay", $"\"{path}\""))
                    {
                        Logger.Info("Played audio using aplay.");
                        return;
                    }
                    if (TryRunProcess("ffplay", $"-nodisp -autoexit \"{path}\""))
                    {
                        Logger.Info("Played audio using ffplay.");
                        return;
                    }
                }

                // ✅ macOS fallback
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (TryRunProcess("afplay", $"\"{path}\""))
                    {
                        Logger.Info("Played audio using afplay.");
                        return;
                    }
                }

                Logger.Warn("No supported audio player was available on this platform. Skipping audio playback.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error playing audio: {ex.Message}");
            }
        }

        private static bool TryRunProcess(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                    return false;

                proc.WaitForExit(5000);
                return proc.ExitCode == 0 || proc.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}
