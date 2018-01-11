/*
 * SubstituteFile.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Tasks
{
    [ObjectId("2cd9b48d-a031-4928-a463-89cd800aef25")]
    public sealed class SubstituteFile : Script
    {
        #region Microsoft.Build.Utilities.Task Overrides
        public override bool Execute()
        {
            Result localResult = null;

            try
            {
                using (Interpreter interpreter = CreateInterpreter(ref localResult))
                {
                    if (interpreter != null)
                    {
                        code = PostCreateInterpreter(interpreter, ref localResult);

                        if (code == ReturnCode.Ok)
                            code = SubstituteFile(interpreter, ref localResult);
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }

                    if (!IsSuccess(code))
                        LogError(code, localResult);
                }
            }
            catch (Exception e)
            {
                localResult = e;
                code = ReturnCode.Error;

                if (e.InnerException != null)
                    LogErrorFromException(e.InnerException);

                LogErrorFromException(e);
            }

            result = localResult;
            return IsSuccess(code) && !Log.HasLoggedErrors;
        }
        #endregion
    }
}
