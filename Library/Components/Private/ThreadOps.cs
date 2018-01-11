/*
 * ThreadOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
    [ObjectId("b81d425a-8049-4404-92c7-d106402b6bba")]
    internal static class ThreadOps
    {
        #region Public Constants
        public static readonly int DefaultJoinTimeout =
#if MONO || MONO_HACKS
            CommonOps.Runtime.IsMono() ? 6000 :
#endif
            3000;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static IntPtr GetHandle(
            EventWaitHandle @event
            )
        {
            if (@event != null)
            {
                try
                {
                    return @event.Handle;
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, e);
                }
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsStaThread()
        {
            Thread thread = Thread.CurrentThread;

            if (thread == null)
                return false;

            return (thread.GetApartmentState() == ApartmentState.STA);
        }

        ///////////////////////////////////////////////////////////////////////

        public static EventWaitHandle CreateEvent(
            bool automatic
            )
        {
            try
            {
                return new EventWaitHandle(false, automatic ?
                    EventResetMode.AutoReset : EventResetMode.ManualReset);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);

                throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static EventWaitHandle CreateEvent(
            string name
            )
        {
            return CreateEvent(name, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static EventWaitHandle CreateEvent(
            string name,
            bool automatic
            )
        {
            try
            {
                return new EventWaitHandle(false, automatic ?
                    EventResetMode.AutoReset : EventResetMode.ManualReset,
                    name);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);

                throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static EventWaitHandle OpenEvent(
            string name
            )
        {
            EventWaitHandle @event = null;

            try
            {
                @event = EventWaitHandle.OpenExisting(name);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

#if DEBUG && VERBOSE
            TraceOps.DebugTrace(String.Format(
                "OpenEvent: {0}, name = {1}",
                (@event != null) ? "success" : "failure",
                FormatOps.WrapOrNull(name)), typeof(ThreadOps).Name,
                TracePriority.EventDebug);
#endif

            return @event;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CloseEvent(
            ref EventWaitHandle @event
            )
        {
            try
            {
                if (@event != null)
                {
                    @event.Close();
                    @event = null;
                }
                else
                {
                    TraceOps.DebugTrace(
                        "CloseEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ResetEvent(
            EventWaitHandle @event
            )
        {
            try
            {
                if (@event != null)
                {
                    return @event.Reset();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "ResetEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetEvent(
            EventWaitHandle @event
            )
        {
            try
            {
                if (@event != null)
                {
                    return @event.Set();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "SetEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WaitEvent(
            EventWaitHandle @event
            )
        {
            try
            {
                if (@event != null)
                {
                    return @event.WaitOne();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WaitEvent(
            EventWaitHandle @event,
            int milliseconds
            )
        {
            try
            {
                if (@event != null)
                {
#if !MONO && !MONO_HACKS && (NET_20_SP2 || NET_40)
                    return @event.WaitOne(milliseconds);
#else
                    return @event.WaitOne(milliseconds, false);
#endif
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitEvent: invalid events",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int WaitAnyEvent(
            EventWaitHandle[] events,
            int milliseconds
            )
        {
            try
            {
                if (events != null)
                {
#if !MONO && !MONO_HACKS && (NET_20_SP2 || NET_40)
                    return EventWaitHandle.WaitAny(events, milliseconds);
#else
                    return EventWaitHandle.WaitAny(events, milliseconds, false);
#endif
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitAnyEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return WaitHandle.WaitTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WasAnyWaitFailed(
            int index
            )
        {
            if (index == WaitResult.Failed)
                return true;

#if MONO || MONO_HACKS
            if (index == WaitResult.MonoFailed)
                return true;
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WasAnyEventSignaled(
            int index
            )
        {
            if ((index != EventWaitHandle.WaitTimeout) &&
#if MONO || MONO_HACKS
                //
                // HACK: Mono can return WAIT_IO_COMPLETION as the index and
                //       we cannot handle that, see:
                //
                //       https://bugzilla.novell.com/show_bug.cgi?id=549807
                //
                (index != WaitResult.IoCompletion) &&
                //
                // HACK: Mono can return the value 0x7FFFFFFF for WAIT_FAILED
                //       and we cannot handle that.
                //
                (index != WaitResult.MonoFailed)
#else
                true
#endif
                )
            {
                return true;
            }

            return false;
        }
    }
}
