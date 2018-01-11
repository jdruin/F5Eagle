/*
 * Channel.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;

#if NETWORK
using System.Net.Sockets;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("a35ad515-f878-426c-8073-bfc5aee4658e")]
    internal sealed class Channel : IDisposable
    {
        #region Private Constants
#if NET_40 && CONSOLE
        private static readonly Type ConsoleStreamType = Type.GetType("System.IO.__ConsoleStream");
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly string StdIn = "stdin";
        public static readonly string StdOut = "stdout";
        public static readonly string StdErr = "stderr";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly int EndOfFile = -1;

        public static readonly int DefaultBufferSize = 4096; // 4KB
        public static readonly int MaximumBufferSize = 4194304; // 4MB

        public static readonly bool StrictGetStream = false;

        public static readonly char NewLine = Characters.NewLine;

        public static readonly CharList EndOfLine = ChannelStream.CarriageReturnLineFeedCharList;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private ChannelStream stream;
        private Encoding encoding;
        private IClientData clientData;
        private StringBuilder virtualOutput; // are we capturing output to a string (non-null)?

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool blockingMode;
        private bool appendMode; // are we always in append mode for output to this file?
        private bool autoFlush; // always flush after a [puts]?
        private bool hitEndOfStream; // did we hit the end of the stream while doing a read?

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private BinaryReader binaryReader;
        private BinaryWriter binaryWriter;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40 && CONSOLE
        private static Stream GetInnerStream(
            Stream stream
            )
        {
            ChannelStream channelStream = stream as ChannelStream;

            if (channelStream == null)
                return null;

            return channelStream.GetStream();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool NeedConsoleStreamHack(
            Stream stream
            )
        {
            if (stream == null)
                return false;

            Stream innerStream = GetInnerStream(stream);

            if (innerStream == null)
                return false;

            if (ConsoleStreamType == null)
                return false;

            Type streamType = innerStream.GetType();

            if (streamType == null)
                return false;

            return Object.ReferenceEquals(streamType, ConsoleStreamType);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int ReadByte(
            Stream stream
            )
        {
#if NET_40 && CONSOLE
            if (NeedConsoleStreamHack(stream))
                return Console.Read();
#endif

            if (stream == null)
                return EndOfFile;

            //
            // BUGBUG: This seems to intermittently produce garbage
            //         (i.e. for the first character) when reading
            //         from the console standard input channel when
            //         running on the .NET Framework 4.0 or higher.
            //         Initial research reveals that this may be
            //         caused by the WaitForAvailableConsoleInput
            //         method.
            //
            // HACK: Hopefully, the NeedConsoleStreamHack() handling
            //       above should work around this issue.
            //
            return stream.ReadByte();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void RemoveEndOfLine(
            char[] buffer,            /* in */
            CharList endOfLine,       /* in */
            bool useAnyEndOfLineChar, /* in */
            ref int bufferLength      /* in, out */
            )
        {
            if ((buffer != null) && (endOfLine != null) && (bufferLength > 0))
            {
                if (useAnyEndOfLineChar)
                {
                    int bufferIndex = bufferLength - 1;

                    while (bufferIndex >= 0)
                    {
                        if (endOfLine.Contains(buffer[bufferIndex]))
                            bufferIndex--;
                        else
                            break;
                    }

                    bufferLength = bufferIndex + 1;
                }
                else
                {
                    int eolLength = endOfLine.Count;

                    if (bufferLength >= eolLength)
                    {
                        bool match = true;
                        int bufferIndex = bufferLength - eolLength;
                        int eolIndex = 0;

                        while ((bufferIndex < bufferLength) &&
                               (eolIndex < eolLength))
                        {
                            if (buffer[bufferIndex] != endOfLine[eolIndex])
                            {
                                match = false;
                                break;
                            }

                            bufferIndex++;
                            eolIndex++;
                        }

                        if (match)
                            bufferLength -= eolLength;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void RemoveEndOfLine(
            byte[] buffer,            /* in */
            CharList endOfLine,       /* in */
            bool useAnyEndOfLineChar, /* in */
            ref int bufferLength      /* in, out */
            )
        {
            if ((buffer != null) && (endOfLine != null) && (bufferLength > 0))
            {
                if (useAnyEndOfLineChar)
                {
                    int bufferIndex = bufferLength - 1;

                    while (bufferIndex >= 0)
                    {
                        if (endOfLine.Contains(ConversionOps.ToChar(buffer[bufferIndex])))
                            bufferIndex--;
                        else
                            break;
                    }

                    bufferLength = bufferIndex + 1;
                }
                else
                {
                    int eolLength = endOfLine.Count;

                    if (bufferLength >= eolLength)
                    {
                        bool match = true;
                        int bufferIndex = bufferLength - eolLength;
                        int eolIndex = 0;

                        while ((bufferIndex < bufferLength) &&
                               (eolIndex < eolLength))
                        {
                            if (buffer[bufferIndex] != ConversionOps.ToByte(endOfLine[eolIndex]))
                            {
                                match = false;
                                break;
                            }

                            bufferIndex++;
                            eolIndex++;
                        }

                        if (match)
                            bufferLength -= eolLength;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode Read( /* throw */
            ref ByteList list,
            ref Result error
            )
        {
            CheckDisposed();

            return Read(Count.Invalid, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode Read( /* throw */
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            ref ByteList list,
            ref Result error
            )
        {
            CheckDisposed();

            return Read(Count.Invalid, endOfLine, useAnyEndOfLineChar, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode Read( /* throw */
            int count,
            ref ByteList list,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Error;

            if (stream != null)
            {
                CharList endOfLine;

                if (stream.InputTranslation != StreamTranslation.binary)
                    endOfLine = stream.InputEndOfLine;
                else
                    endOfLine = ChannelStream.LineFeedCharList;

                bool useAnyEndOfLineChar = stream.UseAnyEndOfLineChar;

                return Read(count, endOfLine, useAnyEndOfLineChar, ref list, ref error);
            }
            else
            {
                error = "invalid stream";
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private void TranslateEndOfLine(
            StreamDirection direction,
            ByteList inputList,
            ref ByteList outputList
            )
        {
            //
            // NOTE: We require the underlying stream to be valid because we use it to
            //       perform the configured end-of-line transformations.
            //
            if (stream != null)
            {
                //
                // NOTE: Is the input list valid?
                //
                if (inputList != null)
                {
                    //
                    // NOTE: How many bytes are in the list?
                    //
                    int inputCount = inputList.Count;

                    if (inputCount > 0)
                    {
                        //
                        // NOTE: Copy the input list to the input buffer because the underlying
                        //       stream transform works on buffers, not lists.
                        //
                        byte[] inputBuffer = inputList.ToArray();

                        //
                        // NOTE: Allocate an output buffer of equal length to the input buffer
                        //       because the underlying stream transform works on buffers, not
                        //       lists.
                        //
                        byte[] outputBuffer = new byte[inputCount];

                        //
                        // NOTE: Use the underlying stream to perform the actual end-of-line
                        //       transformations via the buffers we have prepared.  If the stream
                        //       direction is neither Input only nor Output only, we do nothing.
                        //
                        int outputCount = Count.Invalid;

                        if (direction == StreamDirection.Output)
                        {
                            outputCount = stream.TranslateOutputEndOfLine(
                                inputBuffer, outputBuffer, 0, inputCount);
                        }
                        else if (direction == StreamDirection.Input)
                        {
                            outputCount = stream.TranslateInputEndOfLine(
                                inputBuffer, outputBuffer, 0, inputCount);
                        }

                        //
                        // NOTE: Did we transform anything?
                        //
                        if (outputCount != Count.Invalid)
                        {
                            //
                            // NOTE: Finally, set the caller's output list to the contents of the
                            //       resulting [transformed] output buffer.  We have to manually
                            //       copy the bytes into the resulting output list because we may
                            //       not need all the bytes in the output buffer.
                            //
                            outputList = new ByteList(outputCount);

                            for (int outputIndex = 0; outputIndex < outputCount; outputIndex++)
                                outputList.Add(outputBuffer[outputIndex]);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Garbage in, garbage out.  Empty list to empty list.
                        //
                        outputList = new ByteList();
                    }
                }
                else
                {
                    //
                    // NOTE: Garbage in, garbage out.  Null list to null list.
                    //
                    outputList = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode Read( /* throw */
            int count,
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            ref ByteList list,
            ref Result error
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Error;

            if (stream != null)
            {
                //
                // NOTE: Allocate enough for the whole file.
                //
                if (list == null)
                {
                    long length = 0;

                    //
                    // NOTE: Only attempt to query the length of
                    //       seekable streams.
                    //
                    if (stream.CanSeek)
                        length = stream.Length;

                    if (length > 0)
                        list = new ByteList((int)Math.Min(length, MaximumBufferSize));
                    else
                        list = new ByteList();
                }

                //
                // NOTE: Read from the stream in a loop until we hit a terminator
                //       (typically "end of line" or "end of file").
                //
                int readCount = 0;
                bool eolFound = false;
                int eolLength = (endOfLine != null) ? endOfLine.Count : 0;
                int eolIndex = 0;

                do
                {
                    int value = ReadByte(stream);

                    //
                    // NOTE: Did we hit the end of the stream?
                    //
                    if (value != EndOfFile)
                    {
                        byte byteValue = ConversionOps.ToByte(value);

                        //
                        // NOTE: Did they supply a valid end-of-line sequence to check
                        //       against?
                        //
                        if ((endOfLine != null) && (eolLength > 0))
                        {
                            //
                            // NOTE: Does the caller want to stop reading as soon as any of
                            //       the supplied end-of-line characters are detected?
                            //
                            if (useAnyEndOfLineChar)
                            {
                                //
                                // NOTE: Does the byte match any of the supplied end-of-line
                                //       characters?
                                //
                                if (endOfLine.Contains(ConversionOps.ToChar(byteValue)))
                                    eolFound = true;
                            }
                            else
                            {
                                //
                                // NOTE: Does the byte we just read match the next character in
                                //       the end-of-line sequence we were expecting to see?
                                //
                                if (byteValue == endOfLine[eolIndex])
                                {
                                    //
                                    // NOTE: Have we just match the last character of the end-of-line
                                    //       sequence?  If so, we have found the end-of-line and we
                                    //       are done.
                                    //
                                    if (++eolIndex == eolLength)
                                        eolFound = true; /* NOTE: Hit end-of-line sequence. */
                                }
                                else if (eolIndex > 0)
                                {
                                    //
                                    // NOTE: Any bytes previously matched against end-of-line sequence
                                    //       characters no longer count because the end-of-line sequence
                                    //       characters must appear consecutively.
                                    //
                                    eolIndex = 0;
                                }
                            }
                        }

                        //
                        // NOTE: Add the byte (which could potentially be part of an end-of-line
                        //       sequence) to the buffer.
                        //
                        list.Add(byteValue);

                        //
                        // NOTE: We just read another byte, keep track.
                        //
                        readCount++;

                        //
                        // NOTE: Now that we have added the byte to the buffer, check to see if we
                        //       hit the end-of-line (above).  If so, remove the end-of-line seuqnece
                        //       from the end of the buffer and bail out.
                        //
                        if (eolFound)
                        {
                            int bufferLength = list.Count;

                            RemoveEndOfLine(list.ToArray(), endOfLine, useAnyEndOfLineChar, ref bufferLength);

                            while (list.Count > bufferLength)
                                list.RemoveAt(list.Count - 1);

                            break;
                        }
                    }
                    else
                    {
                        hitEndOfStream = true; /* NOTE: No more data. */
                        break;
                    }
                }
                while ((count == Count.Invalid) || (readCount < count));

                TranslateEndOfLine(StreamDirection.Input, list, ref list); // TEST: Test this.

                code = ReturnCode.Ok;
            }
            else
            {
                error = "invalid stream";
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void RemoveTrailingEndOfLine(
            ByteList buffer,     /* in */
            CharList endOfLine   /* in */
            )
        {
            CheckDisposed();

            if ((buffer != null) && (buffer.Count > 0))
            {
                //
                // HACK: We only remove the trailing end-of-line character if it
                //       is a line-feed (i.e. Unix end-of-line, COMPAT: Tcl).
                //
                if (buffer[buffer.Count - 1] == Characters.LineFeed)
                    //
                    // NOTE: Remove the final character.
                    //
                    buffer.RemoveAt(buffer.Count - 1);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Channel()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK
        public Channel(
            TcpListener listener,
            ChannelType channelType,
            OptionDictionary options,
            StreamFlags flags,
            IClientData clientData
            )
            : this()
        {
            this.stream = new ChannelStream(
                listener, channelType, options, flags);

            this.encoding = null;
            this.appendMode = false;
            this.autoFlush = false;
            this.clientData = clientData;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Channel(
            Stream stream,
            ChannelType channelType,
            OptionDictionary options,
            StreamFlags flags,
            StreamTranslation inTranslation,
            StreamTranslation outTranslation,
            Encoding encoding,
            bool appendMode,
            bool autoFlush,
            IClientData clientData
            )
            : this()
        {
            this.stream = new ChannelStream(
                stream, channelType, options, flags, inTranslation,
                outTranslation);

            this.encoding = encoding;
            this.appendMode = appendMode;
            this.autoFlush = autoFlush;
            this.clientData = clientData;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Channel(
            ChannelStream stream,
            Encoding encoding,
            bool appendMode,
            bool autoFlush,
            IClientData clientData
            )
            : this()
        {
            this.stream = stream;
            this.encoding = encoding;
            this.appendMode = appendMode;
            this.autoFlush = autoFlush;
            this.clientData = clientData;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void CheckAppend()
        {
            CheckDisposed();

            if ((stream != null) && stream.CanSeek && appendMode)
                stream.Seek(0, SeekOrigin.End);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool CheckAutoFlush()
        {
            CheckDisposed();

            return autoFlush && Flush();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Stream GetInnerStream()
        {
            CheckDisposed();

            return (stream != null) ? stream.GetStream() : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ChannelStream GetStream()
        {
            CheckDisposed();

            return stream;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private void CloseReadersAndWriters(bool preventClose)
        {
            if (stream != null)
            {
                //
                // NOTE: Here we workaround a "design flaw" in the .NET Framework by
                //       preventing the stream itself from being closed merely by closing
                //       any readers and writers that we may have open.
                //
                stream.PreventClose = preventClose;
            }

            if (streamWriter != null)
            {
                streamWriter.Close();
                streamWriter = null;
            }

            if (streamReader != null)
            {
                streamReader.Close();
                streamReader = null;
            }

            if (binaryWriter != null)
            {
                binaryWriter.Close();
                binaryWriter = null;
            }

            if (binaryReader != null)
            {
                binaryReader.Close();
                binaryReader = null;
            }

            if (preventClose && (stream != null))
            {
                //
                // NOTE: Allow the stream itself to actually be closed.  This is part of
                //       the workaround mentioned above and is necessary only because the .NET
                //       Framework is fundamentally broken with regard to Stream objects.
                //
                stream.PreventClose = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void Close()
        {
            CheckDisposed();

            CloseReadersAndWriters(true);

            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Flush() /* throw */
        {
            CheckDisposed();

            bool flushed = false;

            if ((stream != null) && stream.CanWrite)
            {
                if (binaryWriter != null)
                {
                    binaryWriter.Flush();
                    flushed = true;
                }

                if (streamWriter != null)
                {
                    streamWriter.Flush();
                    flushed = true;
                }

                //
                // NOTE: Finally, flush the stream itself.
                //
                stream.Flush();
            }

            return flushed;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public BinaryReader GetBinaryReader()
        {
            CheckDisposed();

            if ((stream != null) && (binaryReader == null))
            {
                try
                {
                    if (encoding != null)
                        binaryReader = new BinaryReader(stream, encoding);
                    else
                        binaryReader = new BinaryReader(stream);
                }
                catch
                {
                    // do nothing.
                }
            }

            return binaryReader;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public BinaryWriter GetBinaryWriter()
        {
            CheckDisposed();

            if ((stream != null) && (binaryWriter == null))
            {
                try
                {
                    if (encoding != null)
                        binaryWriter = new BinaryWriter(stream, encoding);
                    else
                        binaryWriter = new BinaryWriter(stream);
                }
                catch
                {
                    // do nothing.
                }
            }

            return binaryWriter;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StreamReader GetStreamReader()
        {
            CheckDisposed();

            if ((stream != null) && (streamReader == null))
            {
                try
                {
                    if (encoding != null)
                        streamReader = new StreamReader(stream, encoding);
                    else
                        streamReader = new StreamReader(stream);

                    //
                    // BUGBUG: Why does the .NET Framework reset the position to be the end of the
                    //         stream upon creating a stream reader or writer on the stream?
                    //
                    //if (!seekBegin && streamReader.BaseStream.CanSeek)
                    //{
                    //    streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    //    seekBegin = true;
                    //}
                }
                catch
                {
                    // do nothing.
                }
            }

            return streamReader;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StreamWriter GetStreamWriter()
        {
            CheckDisposed();

            if ((stream != null) && (streamWriter == null))
            {
                try
                {
                    if (encoding != null)
                        streamWriter = new StreamWriter(stream, encoding);
                    else
                        streamWriter = new StreamWriter(stream);

                    //
                    // BUGBUG: Why does the .NET Framework reset the position to be the end of the
                    //         stream upon creating a stream reader or writer on the stream?
                    //
                    //if (!seekBegin && streamWriter.BaseStream.CanSeek)
                    //{
                    //    streamWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                    //    seekBegin = true;
                    //}
                }
                catch
                {
                    // do nothing.
                }
            }

            return streamWriter;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StreamTranslation GetInputTranslation()
        {
            CheckDisposed();

            return (stream != null) ? stream.InputTranslation : StreamTranslation.auto;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StreamTranslation GetOutputTranslation()
        {
            CheckDisposed();

            return (stream != null) ? stream.OutputTranslation : StreamTranslation.crlf;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void SetTranslation(StreamTranslationList translation)
        {
            CheckDisposed();

            if ((stream != null) && (translation != null) && (translation.Count > 0))
            {
                if (translation.Count >= 2)
                {
                    stream.InputTranslation = translation[0];
                    stream.OutputTranslation = translation[1];
                }
                else
                {
                    stream.InputTranslation = translation[0];
                    stream.OutputTranslation = translation[0];
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StreamTranslationList GetTranslation()
        {
            CheckDisposed();

            StreamTranslationList translation = new StreamTranslationList();

            if (stream != null)
            {
                if (stream.CanRead)
                    translation.Add(stream.InputTranslation);

                if (stream.CanWrite)
                    translation.Add(stream.OutputTranslation);
            }

            return translation;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CharList GetInputEndOfLine()
        {
            CheckDisposed();

            return (stream != null) ? stream.InputEndOfLine : EndOfLine;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CharList GetOutputEndOfLine()
        {
            CheckDisposed();

            return (stream != null) ? stream.OutputEndOfLine : EndOfLine;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void SetBlockingMode(bool blockingMode)
        {
            CheckDisposed();

            this.blockingMode = blockingMode;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool GetBlockingMode()
        {
            CheckDisposed();

            return blockingMode;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Encoding GetEncoding()
        {
            CheckDisposed();

            return encoding;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void SetEncoding(Encoding encoding)
        {
            CheckDisposed();

            CloseReadersAndWriters(true);

            this.encoding = encoding;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool IsVirtualOutput
        {
            get { CheckDisposed(); return (virtualOutput != null); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool AppendVirtualOutput(char value)
        {
            CheckDisposed();

            return AppendVirtualOutput(value.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool AppendVirtualOutput(string value)
        {
            CheckDisposed();

            if (virtualOutput != null)
            {
                virtualOutput.Append(value);

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringBuilder VirtualOutput /* NOTE: For use by Interpreter class only. */
        {
            get { CheckDisposed(); return virtualOutput; }
            set { CheckDisposed(); virtualOutput = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool IsConsoleStream
        {
            get
            {
                CheckDisposed();

#if CONSOLE
                if (stream != null)
                    return stream.IsConsole();
#endif

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool IsNetworkStream
        {
            get
            {
                CheckDisposed();

#if NETWORK
                if (stream != null)
                {
                    Stream innerStream = stream.GetStream();

                    if (innerStream != null)
                    {
                        NetworkStream networkStream = innerStream as NetworkStream;

                        if (networkStream != null)
                            return true;
                    }
                }
#endif

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object Socket
        {
            get
            {
                CheckDisposed();

#if NETWORK
                if (stream != null)
                {
                    Stream innerStream = stream.GetStream();

                    if (innerStream != null)
                    {
                        NetworkStream networkStream = innerStream as NetworkStream;

                        if (networkStream != null)
                            return SocketOps.GetSocket(networkStream);
                    }
                }
#endif

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Connected
        {
            get
            {
                CheckDisposed();

#if NETWORK
                if (stream != null)
                {
                    Stream innerStream = stream.GetStream();

                    if (innerStream != null)
                    {
                        NetworkStream networkStream = innerStream as NetworkStream;

                        if (networkStream != null)
                        {
                            Socket socket = SocketOps.GetSocket(networkStream);

                            if (socket != null)
                                return socket.Connected;
                        }

                    }
                }
#endif

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool DataAvailable
        {
            get
            {
                CheckDisposed();

#if NETWORK
                if (stream != null)
                {
                    Stream innerStream = stream.GetStream();

                    if (innerStream != null)
                    {
                        NetworkStream networkStream = innerStream as NetworkStream;

                        if (networkStream != null)
                            return networkStream.DataAvailable;
                    }
                }
#endif

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool HitEndOfStream
        {
            get { CheckDisposed(); return hitEndOfStream; }
            set { CheckDisposed(); hitEndOfStream = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool EndOfStream /* throw */
        {
            get { CheckDisposed(); return (stream != null) ? (stream.Position >= stream.Length) : false; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public long Length /* throw */
        {
            get { CheckDisposed(); return (stream != null) ? stream.Length : _Constants.Length.Invalid; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public long Position /* throw */
        {
            get { CheckDisposed(); return (stream != null) ? stream.Position : Index.Invalid; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool CanWrite
        {
            get { CheckDisposed(); return (stream != null) ? stream.CanWrite : false; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool CanRead
        {
            get { CheckDisposed(); return (stream != null) ? stream.CanRead : false; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool CanSeek
        {
            get { CheckDisposed(); return (stream != null) ? stream.CanSeek : false; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool HasReader
        {
            get { CheckDisposed(); return (streamReader != null) || (binaryReader != null); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool HasWriter
        {
            get { CheckDisposed(); return (streamWriter != null) || (binaryWriter != null); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void SetLength(long value) /* throw */
        {
            CheckDisposed();

            if (stream != null)
                stream.SetLength(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public long Seek(long offset, SeekOrigin origin) /* throw */
        {
            CheckDisposed();

            return (stream != null) ? stream.Seek(offset, origin) : Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        ~Channel()
        {
            Dispose(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(Channel).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    CloseReadersAndWriters(true);

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (stream != null)
                    {
                        stream.Dispose();
                        stream = null;
                    }
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
