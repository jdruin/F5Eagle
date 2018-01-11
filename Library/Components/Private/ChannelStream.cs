/*
 * ChannelStream.cs --
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

using System.Runtime.Remoting;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("b9b9bfc0-b902-4476-afb9-116ddec7a779")]
    internal class ChannelStream : Stream /* BASE CLASS NOT USED */
    {
        #region End-of-Line Static Data
        internal static readonly CharList LineFeedCharList =
            new CharList(new char[] { Characters.LineFeed });

        internal static readonly CharList CarriageReturnCharList =
            new CharList(new char[] { Characters.CarriageReturn });

        internal static readonly CharList CarriageReturnLineFeedCharList =
            new CharList(new char[] { Characters.CarriageReturn, Characters.LineFeed });
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private ChannelType channelType;
        private OptionDictionary options; // ORIGINAL options used when opening the stream.
        private Stream stream;
        private StreamFlags flags;
        private StreamTranslation inTranslation;
        private StreamTranslation outTranslation;

#if NETWORK
        private TcpListener listener;
        private Socket socket;
        private int timeout;
#endif

        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ChannelStream()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Internal Constructors
#if NETWORK
        internal ChannelStream(
            TcpListener listener,
            ChannelType channelType,
            OptionDictionary options,
            StreamFlags flags
            )
            : this()
        {
            this.listener = listener;
            this.channelType = channelType;
            this.options = options;
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal ChannelStream(
            Socket socket,
            int timeout,
            ChannelType channelType,
            OptionDictionary options,
            StreamFlags flags,
            StreamTranslation inTranslation,
            StreamTranslation outTranslation
            )
            : this()
        {
            this.socket = socket;
            this.timeout = timeout;
            this.channelType = channelType;
            this.options = options;
            this.flags = flags;
            this.inTranslation = inTranslation;
            this.outTranslation = outTranslation;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ChannelStream(
            Stream stream,
            ChannelType channelType,
            OptionDictionary options,
            StreamFlags flags,
            StreamTranslation inTranslation,
            StreamTranslation outTranslation
            )
            : this()
        {
            this.stream = stream;
            this.channelType = channelType;
            this.options = options;
            this.flags = flags;
            this.inTranslation = inTranslation;
            this.outTranslation = outTranslation;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data Accessor Members
#if NETWORK
        public virtual TcpListener GetListener()
        {
            CheckDisposed();

            return listener;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual Socket GetSocket()
        {
            CheckDisposed();

            return socket;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual OptionDictionary GetOptions()
        {
            CheckDisposed();

            return options;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual Stream GetStream()
        {
            CheckDisposed();

            return stream;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Channel Type Members
#if CONSOLE
        public virtual bool IsConsole()
        {
            CheckDisposed();

            return FlagOps.HasFlags(channelType, ChannelType.Console, true);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Stream Flags Members
        public virtual bool HasFlags(StreamFlags flags, bool all)
        {
            CheckDisposed();

            if (all)
                return ((this.flags & flags) == flags);
            else
                return ((this.flags & flags) != StreamFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual StreamFlags SetFlags(StreamFlags flags, bool set)
        {
            CheckDisposed();

            if (set)
                return (this.flags |= flags);
            else
                return (this.flags &= ~flags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool PreventClose
        {
            get { CheckDisposed(); return HasFlags(StreamFlags.PreventClose, true); }
            set { CheckDisposed(); SetFlags(StreamFlags.PreventClose, value); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region End-of-Line Translation Members
        public virtual StreamTranslation InputTranslation
        {
            get { CheckDisposed(); return inTranslation; }
            set { CheckDisposed(); inTranslation = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation OutputTranslation
        {
            get { CheckDisposed(); return outTranslation; }
            set { CheckDisposed(); outTranslation = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation GetEnvironmentInputTranslation()
        {
            CheckDisposed();

            if (PlatformOps.IsWindowsOperatingSystem())
                return StreamTranslation.crlf; /* NOTE: Always assume cr/lf on windows. */
            else
                return StreamTranslation.lf; /* FIXME: Assumes Unix. */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation GetEnvironmentInputTranslation(StreamTranslation translation)
        {
            CheckDisposed();

            return (translation == StreamTranslation.environment) ?
                GetEnvironmentInputTranslation() : translation;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation GetEnvironmentOutputTranslation()
        {
            CheckDisposed();

            if (PlatformOps.IsWindowsOperatingSystem())
                return StreamTranslation.protocol; /* NOTE: Always use cr/lf on windows. */
            else
                return StreamTranslation.lf; /* FIXME: Assumes Unix. */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation GetEnvironmentOutputTranslation(StreamTranslation translation)
        {
            CheckDisposed();

            return (translation == StreamTranslation.environment) ?
                GetEnvironmentOutputTranslation() : translation;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual CharList InputEndOfLine
        {
            get
            {
                CheckDisposed();

                switch (GetEnvironmentInputTranslation(inTranslation))
                {
                    case StreamTranslation.lf:
                        return LineFeedCharList;
                    case StreamTranslation.cr:
                        return CarriageReturnCharList;
                    case StreamTranslation.crlf:
                    case StreamTranslation.platform:
                    case StreamTranslation.auto:
                        return CarriageReturnLineFeedCharList;
                    default:
                        return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool UseAnyEndOfLineChar
        {
            get { CheckDisposed(); return HasFlags(StreamFlags.UseAnyEndOfLineChar, true); }
            set { CheckDisposed(); SetFlags(StreamFlags.UseAnyEndOfLineChar, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual CharList OutputEndOfLine
        {
            get
            {
                CheckDisposed();

                switch (GetEnvironmentOutputTranslation(outTranslation))
                {
                    case StreamTranslation.lf:
                        return LineFeedCharList;
                    case StreamTranslation.cr:
                        return CarriageReturnCharList;
                    case StreamTranslation.crlf:
                    case StreamTranslation.platform:
                    case StreamTranslation.auto:
                        return CarriageReturnLineFeedCharList;
                    default:
                        return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual int TranslateInputEndOfLine(
            byte[] inputBuffer,
            byte[] outputBuffer,
            int offset,
            int count
            )
        {
            switch (GetEnvironmentInputTranslation(inTranslation))
            {
                case StreamTranslation.binary:
                case StreamTranslation.lf:
                case StreamTranslation.protocol:
                    {
                        Array.Copy(inputBuffer, 0, outputBuffer, offset, count);

                        return count;
                    }
                case StreamTranslation.cr:
                    {
                        int newCount = offset + count;

                        Array.Copy(inputBuffer, 0, outputBuffer, offset, count);

                        for (int outIndex = offset; outIndex < newCount; outIndex++)
                            if (outputBuffer[outIndex] == (byte)Characters.CarriageReturn)
                                outputBuffer[outIndex] = (byte)Channel.NewLine;

                        return count;
                    }
                case StreamTranslation.crlf:
                case StreamTranslation.platform:
                    {
                        int newCount = offset + count;
                        int inIndex = offset;
                        int outIndex = 0;

                        if (HasFlags(StreamFlags.NeedLineFeed, true) && (inIndex < newCount))
                        {
                            if (inputBuffer[inIndex] == (byte)Characters.LineFeed)
                                outputBuffer[outIndex++] = (byte)Channel.NewLine;
                            else
                                outputBuffer[outIndex++] = inputBuffer[inIndex++];

                            SetFlags(StreamFlags.NeedLineFeed, false);
                        }

                        for (; inIndex < newCount; )
                        {
                            if (inputBuffer[inIndex] == (byte)Characters.CarriageReturn)
                            {
                                if (++inIndex >= newCount)
                                    SetFlags(StreamFlags.NeedLineFeed, true);
                                else if (inputBuffer[inIndex] == (byte)Characters.LineFeed)
                                    outputBuffer[outIndex++] = inputBuffer[inIndex++];
                                else
                                    outputBuffer[outIndex++] = inputBuffer[inIndex - 1]; // carriage-return
                            }
                            else
                            {
                                outputBuffer[outIndex++] = inputBuffer[inIndex++];
                            }
                        }

                        return outIndex;
                    }
                case StreamTranslation.auto:
                    {
                        int newCount = offset + count;
                        int inIndex = offset;
                        int outIndex = 0;

                        if (HasFlags(StreamFlags.SawCarriageReturn, true) && (inIndex < newCount))
                        {
                            if (inputBuffer[inIndex] == (byte)Characters.LineFeed)
                                inIndex++;

                            SetFlags(StreamFlags.SawCarriageReturn, false);
                        }

                        for (; inIndex < newCount; )
                        {
                            if (inputBuffer[inIndex] == (byte)Characters.CarriageReturn)
                            {
                                if (++inIndex >= newCount)
                                    SetFlags(StreamFlags.SawCarriageReturn, true);
                                else if (inputBuffer[inIndex] == (byte)Characters.LineFeed)
                                    inIndex++;

                                outputBuffer[outIndex++] = (byte)Channel.NewLine;
                            }
                            else
                            {
                                outputBuffer[outIndex++] = inputBuffer[inIndex++];
                            }
                        }

                        return outIndex;
                    }
                default:
                    {
                        return 0;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual int TranslateOutputEndOfLine(
            byte[] inputBuffer,
            byte[] outputBuffer,
            int offset,
            int count
            )
        {
            switch (GetEnvironmentOutputTranslation(outTranslation))
            {
                case StreamTranslation.binary:
                case StreamTranslation.lf:
                    {
                        Array.Copy(inputBuffer, 0, outputBuffer, offset, count);

                        return count;
                    }
                case StreamTranslation.cr:
                    {
                        int newCount = offset + count;

                        Array.Copy(inputBuffer, 0, outputBuffer, offset, count);

                        for (int outIndex = offset; outIndex < newCount; outIndex++)
                            if (outputBuffer[outIndex] == (byte)Characters.LineFeed)
                                outputBuffer[outIndex] = (byte)Characters.CarriageReturn;

                        return count;
                    }
                case StreamTranslation.crlf:
                case StreamTranslation.platform:
                case StreamTranslation.auto:
                    {
                        int newCount = offset + count;
                        int inIndex = offset;
                        int outIndex = 0;

                        for (; inIndex < newCount; )
                        {
                            if (inputBuffer[inIndex] == (byte)Characters.LineFeed)
                                outputBuffer[outIndex++] = (byte)Characters.CarriageReturn;

                            outputBuffer[outIndex++] = inputBuffer[inIndex++];
                        }

                        return outIndex;
                    }
                case StreamTranslation.protocol: /* NOTE: Enforce CR/LF always. */
                    {
                        int newCount = offset + count;
                        int inIndex = offset;
                        int outIndex = 0;

                        for (; inIndex < newCount; )
                        {
                            //
                            // NOTE: Have we seen an unpaired carriage-return?
                            //
                            bool sawCarriageReturn = HasFlags(StreamFlags.SawCarriageReturn, true);

                            //
                            // NOTE: Is the current character a carriage-return?
                            //
                            if (inputBuffer[inIndex] == (byte)Characters.CarriageReturn)
                            {
                                //
                                // NOTE: If we have already seen an unpaired
                                //       carriage-return we need to add a line-feed
                                //       now before doing anything else to complete
                                //       the pairing.
                                //
                                if (sawCarriageReturn)
                                    outputBuffer[outIndex++] = (byte)Characters.LineFeed;

                                //
                                // NOTE: Emit the input character (which is a
                                //       carriage-return).
                                //
                                outputBuffer[outIndex++] = inputBuffer[inIndex++];

                                //
                                // NOTE: We just emitted an unpaired carriage-return.
                                //       If there are more characters to process, we
                                //       can just set the flag to indicate an unpaired
                                //       carriage-return; otherwise, we must emit the
                                //       line-feed now to complete the pairing because
                                //       there are no more characters.
                                //
                                if (inIndex >= newCount)
                                    outputBuffer[outIndex++] = (byte)Characters.LineFeed;
                                else
                                    SetFlags(StreamFlags.SawCarriageReturn, true);
                            }
                            //
                            // NOTE: Otherwise, is the current character a line-feed?
                            //
                            else if (inputBuffer[inIndex] == (byte)Characters.LineFeed)
                            {
                                //
                                // NOTE: If we have not seen an unpaired carriage-return
                                //       yet, we need to add one now for the pairing to
                                //       be complete when we emit the line-feed below.
                                //
                                if (!sawCarriageReturn)
                                    outputBuffer[outIndex++] = (byte)Characters.CarriageReturn;

                                //
                                // NOTE: Emit the input character (which is a line-feed)
                                //       to complete the pairing.
                                //
                                outputBuffer[outIndex++] = inputBuffer[inIndex++];

                                //
                                // NOTE: Now, if we had previously seen an unpaired
                                //       carriage-return, reset the flag now because
                                //       we just completed the pairing.
                                //
                                if (sawCarriageReturn)
                                    SetFlags(StreamFlags.SawCarriageReturn, false);
                            }
                            else
                            {
                                //
                                // NOTE: If we have seen an unpaired carriage-return
                                //       we need to add a line-feed now before doing
                                //       anything else to complete the pairing.
                                //
                                if (sawCarriageReturn)
                                    outputBuffer[outIndex++] = (byte)Characters.LineFeed;

                                //
                                // NOTE: Emit the input character.
                                //
                                outputBuffer[outIndex++] = inputBuffer[inIndex++];

                                //
                                // NOTE: Now, if we had previously seen an unpaired
                                //       carriage-return, reset the flag now because
                                //       we completed the pairing above.
                                //
                                if (sawCarriageReturn)
                                    SetFlags(StreamFlags.SawCarriageReturn, false);
                            }
                        }

                        return outIndex;
                    }
                default:
                    {
                        return 0;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.IO.Stream Overrides
        public override IAsyncResult BeginRead(
            byte[] buffer,
            int offset,
            int count,
            AsyncCallback callback,
            object state
            )
        {
            CheckDisposed();

            return stream.BeginRead(buffer, offset, count, callback, state);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override IAsyncResult BeginWrite(
            byte[] buffer,
            int offset,
            int count,
            AsyncCallback callback,
            object state
            )
        {
            CheckDisposed();

            return stream.BeginWrite(buffer, offset, count, callback, state);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool CanRead
        {
            get { CheckDisposed(); return stream.CanRead; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool CanSeek
        {
            get { CheckDisposed(); return stream.CanSeek; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool CanTimeout
        {
            get { CheckDisposed(); return stream.CanTimeout; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool CanWrite
        {
            get { CheckDisposed(); return stream.CanWrite; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override void Close()
        {
            CheckDisposed();

            if (!PreventClose)
            {
#if NETWORK
                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }

                if (socket != null)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close(timeout);
                    socket = null;
                }
#endif

                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Obsolete()]
        protected override WaitHandle CreateWaitHandle()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int EndRead(IAsyncResult asyncResult)
        {
            CheckDisposed();

            return stream.EndRead(asyncResult);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override void EndWrite(IAsyncResult asyncResult)
        {
            CheckDisposed();

            stream.EndWrite(asyncResult);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override void Flush()
        {
            CheckDisposed();

            stream.Flush();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override long Length
        {
            get { CheckDisposed(); return stream.Length; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override long Position
        {
            get { CheckDisposed(); return stream.Position; }
            set { CheckDisposed(); stream.Position = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            if (inTranslation != StreamTranslation.binary)
            {
                byte[] input = new byte[count];

                int newCount = stream.Read(input, 0, count);

                return TranslateInputEndOfLine(input, buffer, offset, newCount);
            }
            else
            {
                return stream.Read(buffer, offset, count);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int ReadByte()
        {
            CheckDisposed();

            return stream.ReadByte();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int ReadTimeout
        {
            get { CheckDisposed(); return stream.ReadTimeout; }
            set { CheckDisposed(); stream.ReadTimeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            return stream.Seek(offset, origin);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override void SetLength(long value)
        {
            CheckDisposed();

            stream.SetLength(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            if (outTranslation != StreamTranslation.binary)
            {
                int oldCount = offset + count;
                int newCount = count;

                for (int inIndex = offset; inIndex < oldCount; inIndex++)
                {
                    char character = (char)buffer[inIndex];

                    if ((character == Characters.CarriageReturn) ||
                        (character == Characters.LineFeed))
                    {
                        newCount += 2; // NOTE: Every line terminator may double.
                    }
                }

                byte[] output = new byte[newCount];

                newCount = TranslateOutputEndOfLine(buffer, output, offset, count);

                stream.Write(output, 0, newCount);
            }
            else
            {
                stream.Write(buffer, offset, count);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override void WriteByte(byte value)
        {
            CheckDisposed();

            stream.WriteByte(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int WriteTimeout
        {
            get { CheckDisposed(); return stream.WriteTimeout; }
            set { CheckDisposed(); stream.WriteTimeout = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Net.Sockets.NetworkStream Members
        public virtual bool DataAvailable
        {
            get
            {
                CheckDisposed();

#if NETWORK
                return ((NetworkStream)stream).DataAvailable;
#else
                return false;
#endif
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override bool Equals(object obj)
        {
            CheckDisposed();

            return stream.Equals(obj);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            CheckDisposed();

            return stream.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            CheckDisposed();

            return stream.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.MarshalByRefObject Overrides
        public override ObjRef CreateObjRef(Type requestedType)
        {
            CheckDisposed();

            return stream.CreateObjRef(requestedType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override object InitializeLifetimeService()
        {
            CheckDisposed();

            return stream.InitializeLifetimeService();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(ChannelStream).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        Close();
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                //
                // NOTE: This is not necessary because
                //       we do not use our base class.
                //
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~ChannelStream()
        {
            Dispose(false);
        }
        #endregion
    }
}
