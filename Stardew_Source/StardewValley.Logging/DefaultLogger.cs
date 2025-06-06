using System;
using System.IO;
using System.Text;

namespace StardewValley.Logging;

/// <summary>A logger which writes to the console window in debug mode.</summary>
internal class DefaultLogger : IGameLogger
{
	/// <summary>The message builder used to format messages.</summary>
	private readonly StringBuilder MessageBuilder = new StringBuilder();

	/// <summary>The cached absolute path to the debug log file.</summary>
	private string _LogPath;

	/// <summary>Whether we have started the log file.</summary>
	private bool StartedLogFile;

	/// <summary>The absolute path to the debug log file.</summary>
	private string LogPath
	{
		get
		{
			if (_LogPath == null)
			{
				_LogPath = Program.GetDebugLogPath();
			}
			return _LogPath;
		}
	}

	/// <summary>Whether to log messages to the console window.</summary>
	public bool ShouldWriteToConsole { get; }

	/// <summary>Whether to log messages to the debug log file.</summary>
	public bool ShouldWriteToLogFile { get; }

	/// <summary>Construct an instance.</summary>
	/// <param name="shouldWriteToConsole">Whether to log messages to the console window.</param>
	/// <param name="shouldWriteToLogFile">Whether to log messages to the debug log file.</param>
	public DefaultLogger(bool shouldWriteToConsole, bool shouldWriteToLogFile)
	{
		ShouldWriteToConsole = shouldWriteToConsole;
		ShouldWriteToLogFile = shouldWriteToLogFile;
		if (shouldWriteToLogFile)
		{
			WriteMessageToFile("");
		}
	}

	/// <inheritdoc />
	public void Verbose(string message)
	{
		LogImpl("Verbose", message);
	}

	/// <inheritdoc />
	public void Debug(string message)
	{
		LogImpl("Debug", message);
	}

	/// <inheritdoc />
	public void Info(string message)
	{
		LogImpl("Info", message);
	}

	/// <inheritdoc />
	public void Warn(string message)
	{
		LogImpl("Warn", message);
	}

	/// <inheritdoc />
	public void Error(string error, Exception exception)
	{
		LogImpl("Error", error, exception);
	}

	private void WriteMessageToFile(string message)
	{
		if (LogPath == null)
		{
			return;
		}
		if (!StartedLogFile)
		{
			File.WriteAllText(LogPath, message);
			StartedLogFile = true;
			Game1.log.Verbose($"Starting log file at {DateTime.Now:yyyy-MM-dd HH:mm:ii}.");
			return;
		}
		try
		{
			File.AppendAllText(LogPath, message);
		}
		catch (Exception value)
		{
			if (ShouldWriteToConsole)
			{
				Console.WriteLine($"Failed writing to log file:\n{value}");
			}
		}
	}

	/// <summary>Log a message to the console and/or log file.</summary>
	/// <param name="level">The log level.</param>
	/// <param name="message">The message to log.</param>
	/// <param name="exception">The exception to logged, if applicable.</param>
	private void LogImpl(string level, string message, Exception exception = null)
	{
		bool logToConsole = ShouldWriteToConsole;
		bool logToFile = ShouldWriteToLogFile;
		if (logToConsole || logToFile)
		{
			message = FormatLog(level, message, exception);
			if (logToConsole)
			{
				Console.WriteLine(message);
			}
			if (logToFile)
			{
				WriteMessageToFile(message);
			}
		}
	}

	/// <summary>Format a log message with the date and level for display.</summary>
	/// <param name="level">The log level.</param>
	/// <param name="text">The message to log.</param>
	/// <param name="exception">The exception to logged, if applicable.</param>
	private string FormatLog(string level, string text, Exception exception = null)
	{
		StringBuilder message = MessageBuilder;
		try
		{
			int screenId = Game1.game1?.instanceId ?? 0;
			StringBuilder stringBuilder = message.Append('[');
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, stringBuilder);
			handler.AppendFormatted(DateTime.Now, "HH:mm:ss");
			handler.AppendLiteral(" ");
			stringBuilder.Append(ref handler).Append(level).Append(' ')
				.Append((screenId == 0) ? "game" : $"screen{screenId}")
				.Append("] ")
				.Append(text)
				.AppendLine();
			if (exception != null)
			{
				message.Append(exception).AppendLine();
			}
			return message.ToString();
		}
		finally
		{
			message.Clear();
		}
	}
}
