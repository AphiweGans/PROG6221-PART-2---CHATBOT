/*
 * File: Logger.cs
 * Purpose: Simple thread-safe logging helper used across the application.
 *
 * Behavior and guarantees:
 * - Writes INFO messages to the log file only and writes WARN/ERROR to both
 *   console and file with colored console output for visibility during development.
 * - File logging is best-effort: any file I/O errors are caught and ignored so
 *   logging never throws and never blocks application logic.
 * - The static constructor attempts to create the log directory; if that fails
 *   the logger simply continues in memory-only mode.
 *
 * Usage:
 * - Use Logger.Info for non-displayable informational logs, Logger.Warn for
 *   recoverable issues, and Logger.Error for serious failures. Keep log messages
 *   concise and include context if possible.
 */
using System;
using System.IO;
using System.Threading;

namespace CyberAware
{
    internal static class Logger
    {
        private static readonly object FileLock = new();
        private static readonly string LogFilePath;

        static Logger()
        {
            try
            {
                LogFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "assets", "log.txt");
                var dir = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch
            {
                // ignore errors - logging to file is best-effort
                LogFilePath = null!;
            }
        }

        // Write INFO messages to the log file only (no green console output)
        public static void Info(string message) => WriteToFileOnly("INFO", message);
        public static void Warn(string message) => Write(ConsoleColor.DarkYellow, "WARN", message);
        public static void Error(string message) => Write(ConsoleColor.DarkRed, "ERROR", message);

        private static void Write(ConsoleColor color, string level, string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            var timestamp = DateTime.UtcNow.ToString("o");
            var line = $"[{timestamp}] [{level}] {message}";
            Console.WriteLine(line);
            Console.ForegroundColor = prev;

            if (string.IsNullOrEmpty(LogFilePath))
                return;

            try
            {
                lock (FileLock)
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // ignore file logging failures
            }
        }

        private static void WriteToFileOnly(string level, string message)
        {
            if (string.IsNullOrEmpty(LogFilePath))
                return;

            try
            {
                var timestamp = DateTime.UtcNow.ToString("o");
                var line = $"[{timestamp}] [{level}] {message}";
                lock (FileLock)
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // ignore file logging failures
            }
        }
    }
}