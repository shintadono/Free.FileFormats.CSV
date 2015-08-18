using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Free.FileFormats.CSV
{
	/// <summary>
	/// Contains methods for reading and writing CSV (character separated values) files and streams.
	/// </summary>
	public static class CSVFile
	{
		#region Read
		/// <summary>
		/// Parses a file of character separated values.
		/// Uses field separator ',', record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="filename">File to parse.</param>
		/// <returns>List of records of values as string.</returns>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static List<List<string>> Read(string filename)
		{
			return Read(filename, ",", '"');
		}

		/// <summary>
		/// Parses a file of character separated values.
		/// Uses record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="filename">File to parse.</param>
		/// <param name="separators">One or more separator characters for field separation.</param>
		/// <returns>List of records of values as string.</returns>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static List<List<string>> Read(string filename, string separators)
		{
			return Read(filename, separators, '"');
		}

		/// <summary>
		/// Parses a file of character separated values.
		/// Uses record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="filename">File to parse.</param>
		/// <param name="separators">One or more separator characters for field separation.</param>
		/// <returns>List of records of values as string.</returns>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static List<List<string>> Read(string filename, params char[] separators)
		{
			if (separators == null) throw new ArgumentNullException("separators");
			return Read(filename, new string(separators), '"');
		}

		/// <summary>
		/// Parses a file of character separated values.
		/// Uses record separator CR/LF, CR or LF as specified in RFC 4180.
		/// </summary>
		/// <param name="filename">File to parse.</param>
		/// <param name="separators">One or more separator characters for field separation.</param>
		/// <param name="encloser">
		/// Character, which encloses values that (may or may not) contain separator or enclosing characters.
		/// If '\0' no encloser is used/parsed.
		/// </param>
		/// <returns>List of records of values as string.</returns>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static List<List<string>> Read(string filename, string separators, char encloser)
		{
			if (filename == null) throw new ArgumentNullException("filename");
			if (separators == null) throw new ArgumentNullException("separators");
			using (StreamReader sr = new StreamReader(new BufferedStream(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))))
			{
				return Read(sr, separators, encloser);
			}
		}

		/// <summary>
		/// Parses a (file)stream of character separated values.
		/// Uses field separator ',', record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="stream">(File)Stream to parse.</param>
		/// <returns>List of records of values as string.</returns>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static List<List<string>> Read(StreamReader stream)
		{
			return Read(stream, ",", '"');
		}

		/// <summary>
		/// Parses a (file)stream of character separated values.
		/// Uses record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="stream">(File)Stream to parse.</param>
		/// <param name="separators">One or more separator characters for field separation.</param>
		/// <returns>List of records of values as string.</returns>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static List<List<string>> Read(StreamReader stream, string separators)
		{
			return Read(stream, separators, '"');
		}

		/// <summary>
		/// Parses a (file)stream of character separated values.
		/// Uses record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="stream">(File)Stream to parse.</param>
		/// <param name="separators">One or more separator characters for field separation.</param>
		/// <returns>List of records of values as string.</returns>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static List<List<string>> Read(StreamReader stream, params char[] separators)
		{
			if (separators == null) throw new ArgumentNullException("separators");
			return Read(stream, new string(separators), '"');
		}

		/// <summary>
		/// Parses a (file)stream of character separated values.
		/// Uses record separator CR/LF, CR or LF as specified in RFC 4180.
		/// </summary>
		/// <param name="stream">(File)Stream to parse.</param>
		/// <param name="separators">One or more separator characters for field separation.</param>
		/// <param name="encloser">
		/// Character, which encloses values that (may or may not) contain separator or enclosing characters.
		/// If '\0' no encloser is used/parsed.
		/// </param>
		/// <returns>List of records of values as string.</returns>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static List<List<string>> Read(StreamReader stream, string separators, char encloser)
		{
			if (separators == null) throw new ArgumentNullException("separators");
			if (separators.Length == 0) throw new ArgumentException("Must not be empty.", "separators");

			List<List<string>> ret = new List<List<string>>();
			List<string> currentrecord = new List<string>();
			StringBuilder currentfield = new StringBuilder();

			bool enclose = encloser != '\0';
			while (!stream.EndOfStream)
			{
				int ic = stream.Read();
				if (ic < 0) throw new EndOfStreamException();

				char c = (char)ic;

				if (enclose && c == encloser)
				{
					if (currentfield.Length != 0) throw new InvalidDataException("Enclosing character found after first character in value.");
					currentfield.Append(ReadEnclosed(stream, encloser));

					ic = stream.Peek();
					if (ic != -1)
					{
						c = (char)ic;
						if (separators.IndexOf(c) == -1 && !(c == '\n' || c == '\r'))
							throw new InvalidDataException("Non-separator character found after closing enclosing character in value.");
					}
				}
				else if (separators.IndexOf(c) != -1)
				{
					currentrecord.Add(currentfield.ToString());
					currentfield = new StringBuilder();
				}
				else if (c >= 32 || c == '\t')
				{
					currentfield.Append(c);
				}
				else if (c == '\n' || c == '\r')
				{
					if (currentfield.Length != 0)
					{
						currentrecord.Add(currentfield.ToString());
						currentfield = new StringBuilder();
					}

					if (currentrecord.Count != 0)
					{
						ret.Add(currentrecord);
						currentrecord = new List<string>();
					}
				}
				else throw new InvalidDataException("Invalid character in stream.");
			}

			if (currentfield.Length != 0) // In case there is no final new line.
			{
				currentrecord.Add(currentfield.ToString());
			}

			if (currentrecord.Count != 0) // In case there is no final new line.
			{
				ret.Add(currentrecord);
			}

			return ret;
		}

		static string ReadEnclosed(StreamReader stream, char encloser)
		{
			StringBuilder currentfield = new StringBuilder();

			for (; ; )
			{
				int ic = stream.Read();
				if (ic < 0) throw new EndOfStreamException();

				char c = (char)ic;

				if (c == encloser)
				{
					ic = stream.Peek();
					if (ic == -1) break;

					c = (char)ic;
					if (c != encloser) break;

					currentfield.Append(encloser);
					stream.Read();
				}
				else if (c >= 32 || c == '\t' || c == '\n' || c == '\r')
				{
					currentfield.Append(c);
				}
				else throw new InvalidDataException("Invalid character in stream.");
			}

			return currentfield.ToString();
		}
		#endregion

		#region Write
		/// <summary>
		/// Writes a file with character separated values.
		/// Uses field separator ',', record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="data">Data to write.</param>
		/// <param name="filename">File to write.</param>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static void Write(List<List<string>> data, string filename)
		{
			Write(data, filename, ',', '"');
		}

		/// <summary>
		/// Writes a file with character separated values.
		/// Uses record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="data">Data to write.</param>
		/// <param name="filename">File to write.</param>
		/// <param name="separator">Separator character for field separation.</param>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static void Write(List<List<string>> data, string filename, char separator)
		{
			Write(data, filename, separator, '"');
		}

		/// <summary>
		/// Writes a file with character separated values.
		/// Uses record separator CR/LF, CR or LF as specified in RFC 4180.
		/// </summary>
		/// <param name="data">Data to write.</param>
		/// <param name="filename">File to write.</param>
		/// <param name="separator">Separator character for field separation.</param>
		/// <param name="encloser">
		/// Character, which encloses values that contain separator or enclosing characters.
		/// If '\0' no encloser is used/written.
		/// </param>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static void Write(List<List<string>> data, string filename, char separator, char encloser)
		{
			using (StreamWriter sw = new StreamWriter(new BufferedStream(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))))
			{
				Write(data, sw, separator, encloser);
			}
		}

		/// <summary>
		/// Writes a file with character separated values.
		/// Uses field separator ',', record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="data">Data to write.</param>
		/// <param name="stream">Stream to be filled.</param>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static void Write(List<List<string>> data, StreamWriter stream)
		{
			Write(data, stream, ',', '"');
		}

		/// <summary>
		/// Writes a file with character separated values.
		/// Uses record separator CR/LF, CR or LF and value encloser '"' as specified in RFC 4180.
		/// </summary>
		/// <param name="data">Data to write.</param>
		/// <param name="stream">Stream to be filled.</param>
		/// <param name="separator">Separator character for field separation.</param>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static void Write(List<List<string>> data, StreamWriter stream, char separator)
		{
			Write(data, stream, separator, '"');
		}

		/// <summary>
		/// Fills a stream with character separated values.
		/// Uses record separator CR/LF, CR or LF as specified in RFC 4180.
		/// </summary>
		/// <param name="data">Data to write.</param>
		/// <param name="stream">Stream to be filled.</param>
		/// <param name="separator">Separator character for field separation.</param>
		/// <param name="encloser">
		/// Character, which encloses values that contain separator or enclosing characters.
		/// If '\0' no encloser is used/written.
		/// </param>
		/// <exception cref="System.Exception">On any errors.</exception>
		public static void Write(List<List<string>> data, StreamWriter stream, char separator, char encloser)
		{
			bool enclose = encloser != '\0';

			foreach (List<string> record in data)
			{
				bool first = true;
				foreach (string value in record)
				{
					if (first) first = false;
					else stream.Write(separator);

					if (value.IndexOf(separator) != -1)
					{
						if (!enclose) throw new Exception("Need enclosing character to write the data.");
						WriteEnclosed(value, stream, encloser);
					}
					else if (enclose && value.IndexOf(encloser) != -1)
					{
						WriteEnclosed(value, stream, encloser);
					}
					else stream.Write(value);
				}

				stream.WriteLine();
			}
		}

		static void WriteEnclosed(string value, StreamWriter stream, char encloser)
		{
			stream.Write(encloser);
			foreach (char c in value)
			{
				if (c == encloser) stream.Write(c);
				stream.Write(c);
			}
			stream.Write(encloser);
		}
		#endregion
	}
}
