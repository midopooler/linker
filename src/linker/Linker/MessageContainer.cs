﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{
	public readonly struct MessageContainer
	{
		internal static readonly MessageContainer Empty = new MessageContainer ();

		/// <summary>
		/// Optional data with a filename, line and column that triggered the
		/// linker to output an error (or warning) message.
		/// </summary>
		public MessageOrigin? Origin { get; }

		public MessageCategory Category { get; }

		/// <summary>
		/// Further categorize the message.
		/// </summary>
		public string SubCategory { get; }

		/// <summary>
		/// Code identifier for errors and warnings reported by the IL linker.
		/// </summary>
		public int? Code { get; }

		/// <summary>
		/// User friendly text describing the error or warning.
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Create an error message.
		/// </summary>
		/// <param name="text">Humanly readable message describing the error</param>
		/// <param name="code">Unique error ID. Please see https://github.com/mono/linker/blob/master/doc/error-codes.md
		/// for the list of errors and possibly add a new one</param>
		/// <param name="subcategory">Optionally, further categorize this error</param>
		/// <param name="origin">Filename, line, and column where the error was found</param>
		/// <returns>New MessageContainer of 'Error' category</returns>
		public static MessageContainer CreateErrorMessage (string text, int code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
		{
			if (!(code >= 1000 && code <= 2000))
				throw new ArgumentException ($"The provided code '{code}' does not fall into the error category, which is in the range of 1000 to 2000 (inclusive).");

			return new MessageContainer (MessageCategory.Error, text, code, subcategory, origin);
		}

		/// <summary>
		/// Create a warning message.
		/// </summary>
		/// <param name="context">Context with the relevant warning suppression info.</param>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/mono/linker/blob/master/doc/error-codes.md
		/// for the list of warnings and possibly add a new one</param>
		/// /// <param name="origin">Filename or member where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		/// <returns>New MessageContainer of 'Warning' category</returns>
		public static MessageContainer CreateWarningMessage (LinkContext context, string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
		{
			if (!(code > 2000 && code <= 6000))
				throw new ArgumentException ($"The provided code '{code}' does not fall into the warning category, which is in the range of 2001 to 6000 (inclusive).");

			if (context.IsWarningSuppressed (code, origin))
				return Empty;

			return new MessageContainer (MessageCategory.Warning, text, code, subcategory, origin);
		}

		/// <summary>
		/// Create a warning message.
		/// </summary>
		/// <param name="context">Context with the relevant warning suppression info.</param>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/mono/linker/blob/master/doc/error-codes.md
		/// for the list of warnings and possibly add a new one</param>
		/// /// <param name="origin">Filename where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		/// <returns>New MessageContainer of 'Warning' category</returns>
		internal static MessageContainer CreateWarningMessage (LinkContext context, string text, int code, string origin, string subcategory = MessageSubCategory.None)
		{
			MessageOrigin _origin = new MessageOrigin (origin);
			return CreateWarningMessage (context, text, code, _origin, subcategory);
		}

		/// <summary>
		/// Create a warning message.
		/// </summary>
		/// <param name="context">Context with the relevant warning suppression info.</param>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/mono/linker/blob/master/doc/error-codes.md
		/// for the list of warnings and possibly add a new one</param>
		/// /// <param name="origin">Type or member where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		/// <returns>New MessageContainer of 'Warning' category</returns>
		internal static MessageContainer CreateWarningMessage (LinkContext context, string text, int code, IMemberDefinition origin, string subcategory = MessageSubCategory.None)
		{
			MessageOrigin _origin = new MessageOrigin (origin);
			return CreateWarningMessage (context, text, code, _origin, subcategory);
		}

		/// <summary>
		/// Create a info message.
		/// </summary>
		/// <param name="text">Humanly readable message</param>
		/// <returns>New MessageContainer of 'Info' category</returns>
		public static MessageContainer CreateInfoMessage (string text)
		{
			return new MessageContainer (MessageCategory.Info, text, null);
		}

		/// <summary>
		/// Create a diagnostics message.
		/// </summary>
		/// <param name="text">Humanly readable message</param>
		/// <returns>New MessageContainer of 'Diagnostic' category</returns>
		public static MessageContainer CreateDiagnosticMessage (string text)
		{
			return new MessageContainer (MessageCategory.Diagnostic, text, null);
		}

		private MessageContainer (MessageCategory category, string text, int? code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
		{
			Code = code;
			Category = category;
			Origin = origin;
			SubCategory = subcategory;
			Text = text;
		}

		public override string ToString () => ToMSBuildString ();

		public string ToMSBuildString ()
		{
			const string originApp = "ILlinker";
			string origin = Origin?.ToString () ?? originApp;

			StringBuilder sb = new StringBuilder ();
			sb.Append (origin).Append (":");

			if (!string.IsNullOrEmpty (SubCategory))
				sb.Append (" ").Append (SubCategory);

			string cat;
			switch (Category) {
			case MessageCategory.Error:
				cat = "error";
				break;
			case MessageCategory.Warning:
				cat = "warning";
				break;
			default:
				cat = "";
				break;
			}

			if (!string.IsNullOrEmpty (cat)) {
				sb.Append (" ")
					.Append (cat)
					.Append (" IL")
					.Append (Code.Value.ToString ("D4"));

				if (!string.IsNullOrEmpty (Text))
					sb.Append (": ").Append (Text);
			} else {
				sb.Append (" ").Append (Text);
			}

			// Expected output $"{Origin}: {SubCategory}{Category} IL{Code}: {Text}");
			return sb.ToString ();
		}

		public bool Equals (MessageContainer other) =>
			(Category, Text, Code, SubCategory, Origin) == (other.Category, other.Text, other.Code, other.SubCategory, other.Origin);

		public override bool Equals (object obj) => obj is MessageContainer messageContainer && Equals (messageContainer);
		public override int GetHashCode () => (Category, Text, Code, SubCategory, Origin).GetHashCode ();
		public static bool operator == (MessageContainer lhs, MessageContainer rhs) => lhs.Equals (rhs);
		public static bool operator != (MessageContainer lhs, MessageContainer rhs) => !lhs.Equals (rhs);
	}
}
