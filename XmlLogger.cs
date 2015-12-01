#region XmlLogger for MSBuild - Copyright (C) 2005 Szymon Kobalczyk

// XmlLogger for MSBuild
// Copyright (C) 2005-2006 Szymon Kobalczyk
//
// This software is provided 'as-is', without any express or implied warranty. 
// In no event will the authors be held liable for any damages arising from 
// the use of this software.
//
// Permission is granted to anyone to use this software for any purpose, 
// including commercial applications, and to alter it and redistribute it freely, 
// subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not claim 
//    that you wrote the original software. If you use this software in a product, 
//    an acknowledgment in the product documentation would be appreciated 
//    but is not required.
// 
// 2. Altered source versions must be plainly marked as such, and must not be 
//    misrepresented as being the original software.
//
// 3. This notice may not be removed or altered from any source distribution.
//
// Szymon Kobalczyk (http://www.geekswithblogs.com/kobush)

#endregion

#region Using directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
#endregion

namespace Kobush.Build.Logging
{
    /// <summary>
    /// Implements an XML logger for MSBuild.
    /// </summary>
    public class XmlLogger : Logger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlLogger"/> class.
        /// </summary>
        public XmlLogger()
        {
            codeRegex = new Regex(@"^(?<code>\w{2}\d{4})\s*:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        private readonly Regex codeRegex;
        private string outputPath;
        private XmlTextWriter xmlWriter;

        /// <summary>
        /// Initializes the logger by attaching events and parsing command line.
        /// </summary>
        /// <param name="eventSource">The event source.</param>
        public override void Initialize(IEventSource eventSource)
        {
            outputPath = this.Parameters;

            InitializeLogFile();

            AttachToRequiredEvents(eventSource);
        }

        private void AttachToRequiredEvents(IEventSource eventSource)
        {
            eventSource.ErrorRaised += eventSource_ErrorRaised;
            eventSource.WarningRaised += eventSource_WarningRaised;

            eventSource.BuildStarted += eventSource_BuildStartedHandler;
            eventSource.BuildFinished += eventSource_BuildFinishedHandler;

            if (Verbosity == LoggerVerbosity.Quiet) return;

            eventSource.MessageRaised += eventSource_MessageHandler;
            eventSource.CustomEventRaised += eventSource_CustomBuildEventHandler;

            eventSource.ProjectStarted += eventSource_ProjectStartedHandler;
            eventSource.ProjectFinished += eventSource_ProjectFinishedHandler;

            if (Verbosity == LoggerVerbosity.Minimal) return;

            eventSource.TargetStarted += eventSource_TargetStartedHandler;
            eventSource.TargetFinished += eventSource_TargetFinishedHandler;

            if (Verbosity == LoggerVerbosity.Normal) return;

            eventSource.TaskStarted += eventSource_TaskStartedHandler;
            eventSource.TaskFinished += eventSource_TaskFinishedHandler;
        }

        public override void Shutdown()
        {
            FinalizeLogFile();
        }

        public void InitializeLogFile()
        {
            if (!string.IsNullOrEmpty(outputPath))
            {
                try
                {
                    xmlWriter = new XmlTextWriter(outputPath, System.Text.Encoding.UTF8);
                }
                catch (IOException e)
                {
                    const string message = "XmlLogger: An exception was thrown while creating output file. Did you specify a valid filename?";
                    Console.WriteLine(message);
                    throw new InvalidOperationException(message, e);
                }
            }
            else
            {
                xmlWriter = new XmlTextWriter(Console.Out);
            }

            xmlWriter.Formatting = Formatting.Indented;

            // Write the start tag for the root node
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement(XmlLoggerElements.Build);
            xmlWriter.Flush();
        }

        public void FinalizeLogFile()
        {
            // this should close the corresponding WriteStartElement in InitializeLogger
            // before closing the writer down itself
            xmlWriter.Close();
        }

        #region Event Handlers

        private void eventSource_BuildStartedHandler(object sender, BuildStartedEventArgs e)
        {
            LogStageStarted(XmlLoggerElements.Build, string.Empty, string.Empty, e.Timestamp);
        }

        private void eventSource_BuildFinishedHandler(object sender, BuildFinishedEventArgs e)
        {
            LogStageFinished(e.Timestamp);
        }

        private void eventSource_ProjectStartedHandler(object sender, ProjectStartedEventArgs e)
        {
            LogStageStarted(XmlLoggerElements.Project, e.TargetNames, e.ProjectFile, e.Timestamp);
        }

        private void eventSource_ProjectFinishedHandler(object sender, ProjectFinishedEventArgs e)
        {
            LogStageFinished(e.Timestamp);
        }

        private void eventSource_TargetStartedHandler(object sender, TargetStartedEventArgs e)
        {
            LogStageStarted(XmlLoggerElements.Target, e.TargetName, string.Empty, e.Timestamp);
        }

        private void eventSource_TargetFinishedHandler(object sender, TargetFinishedEventArgs e)
        {
            LogStageFinished(e.Timestamp);
        }

        private void eventSource_TaskStartedHandler(object sender, TaskStartedEventArgs e)
        {
            LogStageStarted(XmlLoggerElements.Task, e.TaskName, e.ProjectFile, e.Timestamp);
        }

        private void eventSource_TaskFinishedHandler(object sender, TaskFinishedEventArgs e)
        {
            LogStageFinished(e.Timestamp);
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            LogErrorOrWarning(XmlLoggerElements.Warning, e.Message, e.Code, e.File, e.LineNumber, e.ColumnNumber, e.Timestamp);
        }

        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            LogErrorOrWarning(XmlLoggerElements.Error, e.Message, e.Code, e.File, e.LineNumber, e.ColumnNumber, e.Timestamp);
        }

        private void eventSource_MessageHandler(object sender, BuildMessageEventArgs e)
        {
            LogMessage(XmlLoggerElements.Message, e.Message, e.Importance, e.Timestamp);
        }

        private void eventSource_CustomBuildEventHandler(object sender, CustomBuildEventArgs e)
        {
            LogMessage(XmlLoggerElements.Custom, e.Message, MessageImportance.Normal, e.Timestamp);
        }

        #endregion

        #region Logging

        // stores stage start times used to calculate stage duration
        readonly Stack<DateTime> stageDurationStack = new Stack<DateTime>();

        private void LogStageStarted(string elementName, string stageName, string file, DateTime timeStamp)
        {
            // use the default root for the build element; otherwise start new element
            if (elementName != XmlLoggerElements.Build)
            {
                xmlWriter.WriteStartElement(elementName);
            }

            SetAttribute(XmlLoggerAttributes.Name, stageName);
            SetAttribute(XmlLoggerAttributes.File, String.IsNullOrEmpty(file) ? "" : Path.GetFullPath(file));

            if (elementName == XmlLoggerElements.Build || Verbosity == LoggerVerbosity.Diagnostic)
            {
                SetAttribute(XmlLoggerAttributes.StartTime, timeStamp);
            }
            stageDurationStack.Push(timeStamp);

            xmlWriter.Flush();
        }

        private void LogStageFinished(DateTime timeStamp)
        {
            // log duration of current stage
            var startTime = stageDurationStack.Pop();
            if (stageDurationStack.Count == 0 || Verbosity == LoggerVerbosity.Diagnostic)
            {
                var duration = timeStamp - startTime;
                xmlWriter.WriteStartElement(XmlLoggerElements.Duration);
                xmlWriter.WriteValue(duration.TotalSeconds.ToString("g2",CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.Flush();
        }

        private void LogErrorOrWarning(string messageType, string message, string code, string file, int lineNumber, int columnNumber, DateTime timestamp)
        {
            var messageCode = code;

            if(string.IsNullOrWhiteSpace(code))
            {
                message = codeRegex.Replace(
                    message,
                    m =>
                    {
                        messageCode = m.Groups["code"].Value;
                        return string.Empty;
                    });
            }

            xmlWriter.WriteStartElement(messageType);
            SetAttribute(XmlLoggerAttributes.Code, messageCode);

            SetAttribute(XmlLoggerAttributes.File, string.IsNullOrEmpty(file) ? "" : Path.GetFullPath(file));
            SetAttribute(XmlLoggerAttributes.LineNumber, lineNumber);
            SetAttribute(XmlLoggerAttributes.ColumnNumber, columnNumber);

            if (Verbosity == LoggerVerbosity.Diagnostic)
                SetAttribute(XmlLoggerAttributes.TimeStamp, timestamp);

             // Escape < and > if this is not a "Properties" message.  This is because in a Properties
             // message, we want the ability to insert legal XML, but otherwise we can get malformed
             // XML that will cause the parser to fail.
            WriteMessage(message, code != "Properties");

            xmlWriter.WriteEndElement();
        }

        private void LogMessage(string messageType, string message, MessageImportance importance, DateTime timestamp)
        {
            if (importance == MessageImportance.Low
                && Verbosity != LoggerVerbosity.Detailed
                && Verbosity != LoggerVerbosity.Diagnostic)
                return;

            if (importance == MessageImportance.Normal
                && (Verbosity == LoggerVerbosity.Minimal
                    || Verbosity == LoggerVerbosity.Quiet))
                return;

            xmlWriter.WriteStartElement(messageType);

            SetAttribute(XmlLoggerAttributes.Importance, importance);

            if (Verbosity == LoggerVerbosity.Diagnostic)
            {
                SetAttribute(XmlLoggerAttributes.TimeStamp, timestamp);
            }

            WriteMessage(message, false);

            xmlWriter.WriteEndElement();
        }

        private void WriteMessage(string message, bool escapeLtGt)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            
            // replace xml entities
            var text = message.Replace("&", "&amp;");

            if (escapeLtGt)
            {
                text = text.Replace("<", "&lt;");
                text = text.Replace(">", "&gt;");
            }
            xmlWriter.WriteCData(text);
        }

        private void SetAttribute(string name, object value)
        {
            if (value == null)
                return;

            var t = value.GetType();
            if (t == typeof(int))
            {
                if (Int32.Parse(value.ToString()) > 0)    //????
                {
                    xmlWriter.WriteAttributeString(name, value.ToString());
                }
            }
            else if (t == typeof(DateTime))
            {
                var dateTime = (DateTime)value;
                xmlWriter.WriteAttributeString(name, dateTime.ToString("G", DateTimeFormatInfo.InvariantInfo));
            }
            else if (t == typeof(TimeSpan))
            {
                // format TimeSpan to show only integral seconds
                var seconds = ((TimeSpan)value).TotalSeconds;
                var whole = TimeSpan.FromSeconds(Math.Truncate(seconds));
                xmlWriter.WriteAttributeString(name, whole.ToString());
            }
            else if (t == typeof(bool))
            {
                xmlWriter.WriteAttributeString(name, value.ToString().ToLower());
            }
            else if (t == typeof(MessageImportance))
            {
                var importance = (MessageImportance)value;
                xmlWriter.WriteAttributeString(name, importance.ToString().ToLower());
            }
            else
            {
                var text = value.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    xmlWriter.WriteAttributeString(name, text);
                }
            }
        }

        #endregion
    }
}
