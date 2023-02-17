using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Utility {
    public static class Log {
        enum LogLevel { NONE, INFO, WARNING, ERROR }
        enum LogType { AI, GAME, GENERATION }

        static Dictionary<LogType, Logger> _loggers = new Dictionary<LogType, Logger> {
            { LogType.AI, new Logger(LogLevel.ERROR) },
            { LogType.GAME, new Logger(LogLevel.ERROR) },
            { LogType.GENERATION, new Logger(LogLevel.ERROR) },
        };

        public static void AI_INFO(object message) {
            _loggers[LogType.AI].INFO(message);
        }
        public static void AI_WARNING(object message) {
            _loggers[LogType.AI].WARNING(message);
        }
        public static void AI_ERROR(object message) {
            _loggers[LogType.AI].ERROR(message);
        }

        internal static void GAME_INFO(object message) {
            _loggers[LogType.GAME].INFO(message);
        }
        internal static void GAME_WARNING(object message) {
            _loggers[LogType.GAME].WARNING(message);
        }
        internal static void GAME_ERROR(object message) {
            _loggers[LogType.GAME].ERROR(message);
        }
        internal static void GENERATION_INFO(object message) {
            _loggers[LogType.GENERATION].INFO(message);
        }
        internal static void GENERATION_WARNING(object message) {
            _loggers[LogType.GENERATION].WARNING(message);
        }
        internal static void GENERATION_ERROR(object message) {
            _loggers[LogType.GENERATION].ERROR(message);
        }
        class Logger {
            public LogLevel LEVEL = LogLevel.ERROR;
            public Logger(LogLevel level) {
                this.LEVEL = level;
            }
            internal void INFO(object message) {
                if (LEVEL == LogLevel.INFO) Debug.Log(message);
            }
            internal void WARNING(object message) {
                if (LEVEL == LogLevel.WARNING) Debug.LogWarning(message);
            }
            internal void ERROR(object message) {
                if (LEVEL == LogLevel.ERROR) Debug.LogError(message);
            }
        }
    }

}
