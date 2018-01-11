/*
 * WebOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("47133ca0-868a-4403-8788-530721d2f302")]
    internal static class WebOps
    {
        #region Private Event Helper Methods
        private static StringList GetAsyncCompletedArguments(
            Uri uri,
            string method,
            byte[] rawData,
            NameValueCollection collection,
            string fileName,
            AsyncCompletedEventArgs eventArgs
            )
        {
            StringList result = new StringList();

            if (uri != null)
            {
                result.Add("uri");
                result.Add(uri.ToString());
            }

            if (method != null)
            {
                result.Add("method");
                result.Add(method);
            }

            if (rawData != null)
            {
                result.Add("rawData");
                result.Add(ArrayOps.ToHexadecimalString(rawData));
            }

            if (collection != null)
            {
                result.Add("collection");
                result.Add(ListOps.FromNameValueCollection(
                    collection, new StringList()).ToString());
            }

            if (fileName != null)
            {
                result.Add("fileName");
                result.Add(fileName);
            }

            if (eventArgs != null)
            {
                bool canceled = eventArgs.Cancelled;

                result.Add("canceled");
                result.Add(canceled.ToString());

                Exception exception = eventArgs.Error;

                if (exception != null)
                {
                    result.Add("exception");
                    result.Add(exception.GetType().ToString());
                    result.Add("error");
                    result.Add(exception.ToString());
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Download Event Handlers
        #region Download Data Event Handlers
        private static void DownloadDataAsyncCompleted(
            object sender,
            DownloadDataCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyPair<WebClient, Uri> anyPair =
                        clientData.Data as IAnyPair<WebClient, Uri>;

                    if (anyPair != null)
                    {
                        WebClient webClient = anyPair.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyPair.Y;
                    }

                    clientData.Data = null;
                }

                /* NO RESULT */
                callback.FireEventHandler(sender, e,
                    GetAsyncCompletedArguments(
                        uri, null, null, null, null, e));
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download File Event Handlers
        private static void DownloadFileAsyncCompleted(
            object sender,
            AsyncCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                string method = null;
                string fileName = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyTriplet<WebClient, Uri, string> anyTriplet =
                        clientData.Data as IAnyTriplet<WebClient, Uri, string>;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;
                        fileName = anyTriplet.Z;
                    }

                    clientData.Data = null;
                }

                ReturnCode code;
                Result result = null;

                code = callback.Invoke(
                    GetAsyncCompletedArguments(
                        uri, method, null, null, fileName, e),
                    ref result);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(code, result);
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Web Download Methods
        #region WebClient Support Methods
        public static WebClient CreateClient()
        {
            return new WebClient();
        }

        ///////////////////////////////////////////////////////////////////////

        public static WebClient CreateClient(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                NewWebClientCallback callback = interpreter.NewWebClientCallback;

                if (callback != null)
                {
                    return callback(
                        interpreter, argument, clientData, ref error);
                }
            }

            return CreateClient();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download Data Methods
        public static ReturnCode DownloadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            bool wasTrusted = UpdateOps.IsTrusted();

            TraceOps.DebugTrace(String.Format(
                "DownloadData: interpreter = {0}, clientData = {1}, " +
                "uri = {2}, trusted = {3}, wasTrusted = {4}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(uri),
                trusted, wasTrusted),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = trusted ? wasTrusted ? ReturnCode.Ok :
                UpdateOps.SetTrusted(trusted, ref error) : ReturnCode.Ok;

            if (code == ReturnCode.Ok)
            {
                try
                {
                    try
                    {
                        Result localError = null;

                        using (WebClient webClient = CreateClient(
                                interpreter, "DownloadData", clientData,
                                ref localError))
                        {
                            if (webClient != null)
                            {
                                bytes = webClient.DownloadData(uri);
                                return ReturnCode.Ok;
                            }
                            else if (localError != null)
                            {
                                error = localError;
                            }
                            else
                            {
                                error = "could not create web client";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                finally
                {
                    if (trusted && !wasTrusted)
                    {
                        ReturnCode untrustCode;
                        Result untrustError = null;

                        untrustCode = UpdateOps.SetTrusted(
                            false, ref untrustError);

                        if (untrustCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, untrustCode, untrustError);
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadDataAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "DownloadDataAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri)), typeof(WebOps).Name,
                TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "DownloadDataAsync", clientData,
                            ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyPair<WebClient, Uri>(
                                    webClient, uri));

                            webClient.DownloadDataCompleted +=
                                new DownloadDataCompletedEventHandler(
                                    DownloadDataAsyncCompleted);

                            /* NO RESULT */
                            webClient.DownloadDataAsync(uri, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (webClient != null))
                {
                    ReturnCode disposeCode;
                    Result disposeError = null;

                    disposeCode = ObjectOps.TryDispose(
                        webClient, ref disposeError);

                    if (disposeCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, disposeCode, disposeError);
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download File Methods
        public static ReturnCode DownloadFile(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string fileName,
            bool trusted,
            ref Result error
            )
        {
            bool wasTrusted = UpdateOps.IsTrusted();

            TraceOps.DebugTrace(String.Format(
                "DownloadFile: interpreter = {0}, clientData = {1}, " +
                "uri = {2}, fileName = {3}, trusted = {4}, " +
                "wasTrusted = {5}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(fileName),
                trusted, wasTrusted),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = trusted ? wasTrusted ? ReturnCode.Ok :
                UpdateOps.SetTrusted(trusted, ref error) : ReturnCode.Ok;

            if (code == ReturnCode.Ok)
            {
                try
                {
                    try
                    {
                        Result localError = null;

                        using (WebClient webClient = CreateClient(
                                interpreter, "DownloadFile", clientData,
                                ref localError))
                        {
                            if (webClient != null)
                            {
                                /* NO RESULT */
                                webClient.DownloadFile(uri, fileName);

                                return ReturnCode.Ok;
                            }
                            else if (localError != null)
                            {
                                error = localError;
                            }
                            else
                            {
                                error = "could not create web client";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                finally
                {
                    if (trusted && !wasTrusted)
                    {
                        ReturnCode untrustCode;
                        Result untrustError = null;

                        untrustCode = UpdateOps.SetTrusted(
                            false, ref untrustError);

                        if (untrustCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, untrustCode, untrustError);
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadFileAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            string fileName,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "DownloadFileAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "fileName = {5}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(fileName)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "DownloadFileAsync", clientData,
                            ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyTriplet<WebClient, Uri, string>(
                                    webClient, uri, fileName));

                            webClient.DownloadFileCompleted +=
                                new AsyncCompletedEventHandler(
                                    DownloadFileAsyncCompleted);

                            /* NO RESULT */
                            webClient.DownloadFileAsync(
                                uri, fileName, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (webClient != null))
                {
                    ReturnCode disposeCode;
                    Result disposeError = null;

                    disposeCode = ObjectOps.TryDispose(
                        webClient, ref disposeError);

                    if (disposeCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, disposeCode, disposeError);
                    }
                }
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Upload Event Handlers
        #region Upload Data Event Handlers
        private static void UploadDataAsyncCompleted(
            object sender,
            UploadDataCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                string method = null;
                byte[] rawData = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, byte[]>>
                        anyTriplet = clientData.Data as
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, byte[]>>;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        IAnyPair<string, byte[]> anyPair = anyTriplet.Z;

                        if (anyPair != null)
                        {
                            method = anyPair.X;
                            rawData = anyPair.Y;
                        }
                    }

                    clientData.Data = null;
                }

                /* NO RESULT */
                callback.FireEventHandler(sender, e,
                    GetAsyncCompletedArguments(
                        uri, method, rawData, null, null, e));
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload Values Event Handlers
        private static void UploadValuesAsyncCompleted(
            object sender,
            UploadValuesCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                string method = null;
                NameValueCollection collection = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, NameValueCollection>>
                        anyTriplet = clientData.Data as
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, NameValueCollection>>;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        IAnyPair<string, NameValueCollection>
                            anyPair = anyTriplet.Z;

                        if (anyPair != null)
                        {
                            method = anyPair.X;
                            collection = anyPair.Y;
                        }
                    }

                    clientData.Data = null;
                }

                /* NO RESULT */
                callback.FireEventHandler(sender, e,
                    GetAsyncCompletedArguments(
                        uri, method, null, collection, null, e));
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload File Event Handlers
        private static void UploadFileAsyncCompleted(
            object sender,
            UploadFileCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                string method = null;
                string fileName = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, string>>
                        anyTriplet = clientData.Data as
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, string>>;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        IAnyPair<string, string> anyPair = anyTriplet.Z;

                        if (anyPair != null)
                        {
                            method = anyPair.X;
                            fileName = anyPair.Y;
                        }
                    }

                    clientData.Data = null;
                }

                ReturnCode code;
                Result result = null;

                code = callback.Invoke(
                    GetAsyncCompletedArguments(
                        uri, method, null, null, fileName, e),
                    ref result);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(code, result);
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Web Upload Methods
        #region Upload Data Methods
        public static ReturnCode UploadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            byte[] rawData,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            bool wasTrusted = UpdateOps.IsTrusted();

            TraceOps.DebugTrace(String.Format(
                "UploadData: interpreter = {0}, clientData = {1}, " +
                "uri = {2}, method = {3}, rawData = {4}, trusted = {5}, " +
                "wasTrusted = {6}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                FormatOps.WrapOrNull(rawData),
                trusted, wasTrusted),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = trusted ? wasTrusted ? ReturnCode.Ok :
                UpdateOps.SetTrusted(trusted, ref error) : ReturnCode.Ok;

            if (code == ReturnCode.Ok)
            {
                try
                {
                    try
                    {
                        Result localError = null;

                        using (WebClient webClient = CreateClient(
                                interpreter, "UploadData", clientData,
                                ref localError))
                        {
                            if (webClient != null)
                            {
                                bytes = webClient.UploadData(
                                    uri, method, rawData);

                                return ReturnCode.Ok;
                            }
                            else if (localError != null)
                            {
                                error = localError;
                            }
                            else
                            {
                                error = "could not create web client";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                finally
                {
                    if (trusted && !wasTrusted)
                    {
                        ReturnCode untrustCode;
                        Result untrustError = null;

                        untrustCode = UpdateOps.SetTrusted(
                            false, ref untrustError);

                        if (untrustCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, untrustCode, untrustError);
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadDataAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            string method,
            byte[] rawData,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "UploadDataAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "method = {5}, rawData = {6}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                FormatOps.WrapOrNull(rawData)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "UploadDataAsync", clientData,
                            ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyTriplet<WebClient, Uri,
                                    IAnyPair<string, byte[]>>(
                                        webClient, uri, new AnyPair<string,
                                            byte[]>(method, rawData)));

                            webClient.UploadDataCompleted +=
                                new UploadDataCompletedEventHandler(
                                    UploadDataAsyncCompleted);

                            /* NO RESULT */
                            webClient.UploadDataAsync(
                                uri, method, rawData, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (webClient != null))
                {
                    ReturnCode disposeCode;
                    Result disposeError = null;

                    disposeCode = ObjectOps.TryDispose(
                        webClient, ref disposeError);

                    if (disposeCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, disposeCode, disposeError);
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload Values Methods
        public static ReturnCode UploadValues(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            NameValueCollection collection,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            bool wasTrusted = UpdateOps.IsTrusted();

            TraceOps.DebugTrace(String.Format(
                "UploadValues: interpreter = {0}, clientData = {1}, " +
                "uri = {2}, method = {3}, collection = {4}, trusted = {5}, " +
                "wasTrusted = {6}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                FormatOps.NameValueCollection(collection, true),
                trusted, wasTrusted),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = trusted ? wasTrusted ? ReturnCode.Ok :
                UpdateOps.SetTrusted(trusted, ref error) : ReturnCode.Ok;

            if (code == ReturnCode.Ok)
            {
                try
                {
                    try
                    {
                        Result localError = null;

                        using (WebClient webClient = CreateClient(
                                interpreter, "UploadValues", clientData,
                                ref localError))
                        {
                            if (webClient != null)
                            {
                                bytes = webClient.UploadValues(
                                    uri, method, collection);

                                return ReturnCode.Ok;
                            }
                            else if (localError != null)
                            {
                                error = localError;
                            }
                            else
                            {
                                error = "could not create web client";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                finally
                {
                    if (trusted && !wasTrusted)
                    {
                        ReturnCode untrustCode;
                        Result untrustError = null;

                        untrustCode = UpdateOps.SetTrusted(
                            false, ref untrustError);

                        if (untrustCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, untrustCode, untrustError);
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadValuesAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            string method,
            NameValueCollection collection,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "UploadValuesAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "method = {5}, collection = {6}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                FormatOps.NameValueCollection(collection, true)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "UploadValuesAsync", clientData,
                            ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyTriplet<WebClient, Uri,
                                    IAnyPair<string, NameValueCollection>>(
                                        webClient, uri, new AnyPair<string,
                                            NameValueCollection>(method,
                                                collection)));

                            webClient.UploadValuesCompleted +=
                                new UploadValuesCompletedEventHandler(
                                    UploadValuesAsyncCompleted);

                            /* NO RESULT */
                            webClient.UploadValuesAsync(
                                uri, method, collection, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (webClient != null))
                {
                    ReturnCode disposeCode;
                    Result disposeError = null;

                    disposeCode = ObjectOps.TryDispose(
                        webClient, ref disposeError);

                    if (disposeCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, disposeCode, disposeError);
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload File Methods
        public static ReturnCode UploadFile(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            string fileName,
            bool trusted,
            ref Result error
            )
        {
            bool wasTrusted = UpdateOps.IsTrusted();

            TraceOps.DebugTrace(String.Format(
                "UploadFile: interpreter = {0}, clientData = {1}, " +
                "uri = {2}, method = {3}, fileName = {4}, " +
                "trusted = {5}, wasTrusted = {6}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                FormatOps.WrapOrNull(fileName),
                trusted, wasTrusted),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = trusted ? wasTrusted ? ReturnCode.Ok :
                UpdateOps.SetTrusted(trusted, ref error) : ReturnCode.Ok;

            if (code == ReturnCode.Ok)
            {
                try
                {
                    try
                    {
                        Result localError = null;

                        using (WebClient webClient = CreateClient(
                                interpreter, "UploadFile", clientData,
                                ref localError))
                        {
                            if (webClient != null)
                            {
                                /* NO RESULT */
                                webClient.UploadFile(
                                    uri, method, fileName);

                                return ReturnCode.Ok;
                            }
                            else if (localError != null)
                            {
                                error = localError;
                            }
                            else
                            {
                                error = "could not create web client";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                finally
                {
                    if (trusted && !wasTrusted)
                    {
                        ReturnCode untrustCode;
                        Result untrustError = null;

                        untrustCode = UpdateOps.SetTrusted(
                            false, ref untrustError);

                        if (untrustCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, untrustCode, untrustError);
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadFileAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            string method,
            string fileName,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "UploadFileAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "method = {5}, fileName = {6}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                FormatOps.WrapOrNull(fileName)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "UploadFileAsync", clientData,
                            ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyTriplet<WebClient, Uri,
                                    IAnyPair<string, string>>(
                                        webClient, uri, new AnyPair<string,
                                            string>(method, fileName)));

                            webClient.UploadFileCompleted +=
                                new UploadFileCompletedEventHandler(
                                    UploadFileAsyncCompleted);

                            /* NO RESULT */
                            webClient.UploadFileAsync(
                                uri, method, fileName, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (webClient != null))
                {
                    ReturnCode disposeCode;
                    Result disposeError = null;

                    disposeCode = ObjectOps.TryDispose(
                        webClient, ref disposeError);

                    if (disposeCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, disposeCode, disposeError);
                    }
                }
            }

            return code;
        }
        #endregion
        #endregion
    }
}
