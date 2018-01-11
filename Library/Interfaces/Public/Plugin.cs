/*
 * Plugin.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Globalization;
using System.IO;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("3b770696-81da-481f-b9ef-aba55f81d004")]
    public interface IPlugin : IPluginData, IState, IExecuteRequest
#if NOTIFY || NOTIFY_OBJECT
        , INotify
#endif
    {
        [Throw(true)]
        void PostInitialize(Interpreter interpreter, IClientData clientData);

        Stream GetStream(
            Interpreter interpreter, string name, ref Result error);

        string GetString(
            Interpreter interpreter, string name, CultureInfo cultureInfo,
            ref Result error);

        ReturnCode Banner(Interpreter interpreter, ref Result result);
        ReturnCode About(Interpreter interpreter, ref Result result);
        ReturnCode Options(Interpreter interpreter, ref Result result);
    }
}
