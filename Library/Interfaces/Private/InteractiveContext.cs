/*
 * InteractiveContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

#if HISTORY
using Eagle._Interfaces.Public;
#endif

namespace Eagle._Interfaces.Private
{
    [ObjectId("690d1c76-7901-4a6d-b710-602a33968669")]
    internal interface IInteractiveContext : IThreadContext
    {
        bool Interactive { get; set; }
        string InteractiveInput { get; set; }
        string PreviousInteractiveInput { get; set; }
        string InteractiveMode { get; set; }
        int ActiveInteractiveLoops { get; set; }
        int TotalInteractiveLoops { get; set; }

        InteractiveLoopData InteractiveLoopData { get; set; }
        StringTransformCallback InteractiveCommandCallback { get; set; }

#if HISTORY
        IHistoryData HistoryLoadData { get; set; }
        IHistoryData HistorySaveData { get; set; }

        IHistoryFilter HistoryInfoFilter { get; set; }
        IHistoryFilter HistoryLoadFilter { get; set; }
        IHistoryFilter HistorySaveFilter { get; set; }

        string HistoryFileName { get; set; }
#endif
    }
}
