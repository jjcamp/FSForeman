using System;
using System.IO;
using System.Diagnostics;

namespace FSForeman {
    /// <summary>
    /// Simple logging implementation.
    /// </summary>
    public class Logger {
        public enum OutputType {
            Console,
            Debug,
            File
        }

        /// <summary>Where the logger outputs to.</summary>
        public static OutputType Output { get; set; }
        /// <summary>If logging to a file, this is the file path.</summary>
        public static string FilePath { get; set; }
        /// <summary>If true, the logger will add timestamps.</summary>
        public static bool UseTimestamp { get; set; } = false;

        /// <summary>
        /// Logs to the current <see cref="Output"/>.
        /// </summary>
        /// <param name="text">The string to log.</param>
        public static void Log(string text) {
            if (UseTimestamp)
                text = $"[{DateTime.Now}] {text}";
            switch (Output) {
                case OutputType.File:
                    LogFile(text);
                    break;
                case OutputType.Console:
                    LogConsole(text);
                    break;
                case OutputType.Debug:
                    LogDebug(text);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Logs a line to the current <see cref="Output"/>.
        /// </summary>
        /// <param name="text">The string to log.</param>
        public static void LogLine(string text) {
            Log(text + Environment.NewLine);
        }

        private static void LogConsole(string text) {
            Console.Write(text);
        }

        private static void LogDebug(string text) {
            Debug.Write(text);
        }

        private static void LogFile(string text) {
            if (!File.Exists(FilePath)) {
                lock (FilePath) {
                    using (var sw = File.CreateText(FilePath)) {
                        sw.Write(text);
                    }
                }
            }
            else if (FilePath != null) {
                lock (FilePath) {
                    using (var sw = File.AppendText(FilePath)) {
                        sw.Write(text);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the current <see cref="FilePath"/> file.
        /// </summary>
        public static void DeleteLogFile() {
            lock (FilePath) {
                if (FilePath != null && File.Exists(FilePath))
                    File.Delete(FilePath);
            }
        }
    }
}
