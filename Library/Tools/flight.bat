@ECHO OFF

::
:: flight.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Release Preparation & Build Tool
::
:: Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
::
:: See the file "license.terms" for information on usage and redistribution of
:: this file, and for a DISCLAIMER OF ALL WARRANTIES.
::
:: RCS: @(#) $Id: $
::

REM ****************************************************************************
REM ******************** Prologue / Command Line Processing ********************
REM ****************************************************************************

SETLOCAL
ECHO FLIGHT STARTED ON %DATE% AT %TIME% BY %USERDOMAIN%\%USERNAME%

REM SET __ECHO=ECHO
REM SET __ECHO2=ECHO
REM SET __ECHO3=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

SET APPEND=^>^>
SET _CAPPEND=^^^>^^^>
IF DEFINED __ECHO SET APPEND=^^^>^^^>

SET PIPE=^|
SET _CPIPE=^^^|
IF DEFINED __ECHO SET PIPE=^^^|

%_AECHO% Running %0 %*

REM SET DFLAGS=/L

%_VECHO% DFlags = '%DFLAGS%'

SET FFLAGS=/V /F /G /H /I /R /Y /Z

%_VECHO% FFlags = '%FFLAGS%'

SET CONFIGURATION=%1

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

%_VECHO% Configuration = '%CONFIGURATION%'

REM ****************************************************************************
REM ****************** Set Miscellaneous Environment, Phase 1 ******************
REM ****************************************************************************

IF NOT DEFINED PLATFORMS (
  %_AECHO% No platforms specified, using default...
  SET PLATFORMS=Win32 x86 x64
)

%_VECHO% Platforms = '%PLATFORMS%'

IF NOT DEFINED NATIVE_CONFIGURATION (
  %_AECHO% No native configuration specified, using default...
  SET NATIVE_CONFIGURATION=%CONFIGURATION%Dll
)

%_VECHO% NativeConfiguration = '%NATIVE_CONFIGURATION%'

IF NOT DEFINED DEFAULT_PLATFORM (
  SET DEFAULT_PLATFORM=Win32
)

%_VECHO% DefaultPlatform = '%DEFAULT_PLATFORM%'

IF NOT DEFINED PACKAGE_PLATFORM (
  %_AECHO% No package platform specified, using default...
  SET PACKAGE_PLATFORM=%DEFAULT_PLATFORM%
)

%_VECHO% PackagePlatform = '%PACKAGE_PLATFORM%'

REM ****************************************************************************
REM *********************** Minimum Build Configuration? ***********************
REM ****************************************************************************

REM
REM NOTE: This is setup to do only the necessary steps for releases marked as
REM       "latest" ^(i.e. in development^).  This should run quickly.  As of
REM       December 2015, only the following build configurations are produced
REM       and tested in this mode of operation:
REM
REM       1. NetFx20
REM       2. NetFx462
REM
REM       This build configuration list is subject to change at any time.
REM
IF DEFINED MINBUILD (
  %_AECHO% Skipping build configurations for "minimal" release...
  REM SET NONETFX20=1
  SET NONETFX35=1
  SET NONETFX40=1
  SET NONETFX45=1
  SET NONETFX451=1
  SET NONETFX452=1
  SET NONETFX46=1
  SET NONETFX461=1
  REM SET NONETFX462=1
  SET NONETFX47=1
  SET NONETFX471=1
  SET NOBARE=1
  SET NOLEAN=1
  SET NODATABASE=1
  SET NOUNIX=1
)

REM ****************************************************************************
REM ****************** Set Miscellaneous Environment, Phase 2 ******************
REM ****************************************************************************

REM
REM HACK: If we are not building any NetFx4x build configurations then Garuda
REM       will not be built; therefore, disable other steps that require it.
REM
IF DEFINED NONETFX40 (
  IF DEFINED NONETFX45 (
    IF DEFINED NONETFX451 (
      IF DEFINED NONETFX452 (
        IF DEFINED NONETFX46 (
          IF DEFINED NONETFX461 (
            IF DEFINED NONETFX462 (
              IF DEFINED NONETFX47 (
                IF DEFINED NONETFX471 (
                  SET NOPACKAGE=1
                )
              )
            )
          )
        )
      )
    )
  )
)

IF NOT DEFINED HASHBASES (
  SET HASHBASES=EagleSource EagleSetup EagleBinary EagleRuntime EagleCore

  IF NOT DEFINED NOPACKAGE (
    CALL :fn_AppendVariable HASHBASES " GarudaSetup GarudaBinary"
    CALL :fn_AppendVariable HASHBASES " GarudaRuntime GarudaCore"
  )
)

%_VECHO% HashBases = '%HASHBASES%'

IF NOT DEFINED HASHEXTS (
  SET HASHEXTS=asc exe exe.asc nupkg rar rar.asc txt zip
)

%_VECHO% HashExts = '%HASHEXTS%'

IF DEFINED MANAGEDIMAGEFILES GOTO skip_managedImageFiles

SET MANAGEDIMAGEFILES=Eagle.dll EagleCmdlets.dll EagleExtensions.dll
SET MANAGEDIMAGEFILES=%MANAGEDIMAGEFILES% EagleTasks.dll EagleShell.exe
SET MANAGEDIMAGEFILES=%MANAGEDIMAGEFILES% EagleShell32.exe Hippogriff.exe

IF NOT DEFINED NOEXTRA (
  SET MANAGEDIMAGEFILES=%MANAGEDIMAGEFILES% Harpy.Basic.dll Badge.Basic.dll
)

:skip_managedImageFiles

%_VECHO% ManagedImageFiles = '%MANAGEDIMAGEFILES%'

IF DEFINED NATIVEIMAGEFILES GOTO skip_nativeImageFiles

SET NATIVEIMAGEFILES=Garuda.dll Spilornis.dll

:skip_nativeImageFiles

%_VECHO% NativeImageFiles = '%NATIVEIMAGEFILES%'

IF NOT DEFINED PATCHLEVELFILES (
  SET PATCHLEVELFILES=BuildInfo.cs PatchLevel.cs
)

%_VECHO% PatchLevelFiles = '%PATCHLEVELFILES%'

IF NOT DEFINED TAGFILES (
  SET TAGFILES=BuildInfo.cs PatchLevel.cs
)

%_VECHO% TagFiles = '%TAGFILES%'

IF NOT DEFINED PACKAGE_TAGFILES (
  SET PACKAGE_TAGFILES=pkgVersion.h
)

%_VECHO% PackageTagFiles = '%PACKAGE_TAGFILES%'

SET ARGS=%*

%_VECHO% Args = '%ARGS%'

REM ****************************************************************************
REM **************************** Set Build Suffixes ****************************
REM ****************************************************************************

SET NETFX20_SUFFIX=NetFx20
SET NETFX35_SUFFIX=NetFx35
SET NETFX40_SUFFIX=NetFx40
SET NETFX45_SUFFIX=NetFx45
SET NETFX451_SUFFIX=NetFx451
SET NETFX452_SUFFIX=NetFx452
SET NETFX46_SUFFIX=NetFx46
SET NETFX461_SUFFIX=NetFx461
SET NETFX462_SUFFIX=NetFx462
SET NETFX47_SUFFIX=NetFx47
SET NETFX471_SUFFIX=NetFx471
SET BARE_SUFFIX=Bare
SET LEAN_SUFFIX=LeanAndMean
SET DATABASE_SUFFIX=Database
SET UNIX_SUFFIX=MonoOnUnix

%_VECHO% NetFx20Suffix = '%NETFX20_SUFFIX%'
%_VECHO% NetFx35Suffix = '%NETFX35_SUFFIX%'
%_VECHO% NetFx40Suffix = '%NETFX40_SUFFIX%'
%_VECHO% NetFx45Suffix = '%NETFX45_SUFFIX%'
%_VECHO% NetFx451Suffix = '%NETFX451_SUFFIX%'
%_VECHO% NetFx452Suffix = '%NETFX452_SUFFIX%'
%_VECHO% NetFx46Suffix = '%NETFX46_SUFFIX%'
%_VECHO% NetFx461Suffix = '%NETFX461_SUFFIX%'
%_VECHO% NetFx462Suffix = '%NETFX462_SUFFIX%'
%_VECHO% NetFx47Suffix = '%NETFX47_SUFFIX%'
%_VECHO% NetFx471Suffix = '%NETFX471_SUFFIX%'
%_VECHO% BareSuffix = '%BARE_SUFFIX%'
%_VECHO% LeanSuffix = '%LEAN_SUFFIX%'
%_VECHO% DatabaseSuffix = '%DATABASE_SUFFIX%'
%_VECHO% UnixSuffix = '%UNIX_SUFFIX%'

REM ****************************************************************************
REM ************************* Set Build Configurations *************************
REM ****************************************************************************

IF NOT DEFINED NETFX20_CONFIGURATION (
  %_AECHO% No "%NETFX20_SUFFIX%" configuration specified, using default...
  SET NETFX20_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx20Configuration = '%NETFX20_CONFIGURATION%'

IF NOT DEFINED NETFX35_CONFIGURATION (
  %_AECHO% No "%NETFX35_SUFFIX%" configuration specified, using default...
  SET NETFX35_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx35Configuration = '%NETFX35_CONFIGURATION%'

IF NOT DEFINED NETFX40_CONFIGURATION (
  %_AECHO% No "%NETFX40_SUFFIX%" configuration specified, using default...
  SET NETFX40_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx40Configuration = '%NETFX40_CONFIGURATION%'

IF NOT DEFINED NETFX45_CONFIGURATION (
  %_AECHO% No "%NETFX45_SUFFIX%" configuration specified, using default...
  SET NETFX45_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx45Configuration = '%NETFX45_CONFIGURATION%'

IF NOT DEFINED NETFX451_CONFIGURATION (
  %_AECHO% No "%NETFX451_SUFFIX%" configuration specified, using default...
  SET NETFX451_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx451Configuration = '%NETFX451_CONFIGURATION%'

IF NOT DEFINED NETFX452_CONFIGURATION (
  %_AECHO% No "%NETFX452_SUFFIX%" configuration specified, using default...
  SET NETFX452_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx452Configuration = '%NETFX452_CONFIGURATION%'

IF NOT DEFINED NETFX46_CONFIGURATION (
  %_AECHO% No "%NETFX46_SUFFIX%" configuration specified, using default...
  SET NETFX46_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx46Configuration = '%NETFX46_CONFIGURATION%'

IF NOT DEFINED NETFX461_CONFIGURATION (
  %_AECHO% No "%NETFX461_SUFFIX%" configuration specified, using default...
  SET NETFX461_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx461Configuration = '%NETFX461_CONFIGURATION%'

IF NOT DEFINED NETFX462_CONFIGURATION (
  %_AECHO% No "%NETFX462_SUFFIX%" configuration specified, using default...
  SET NETFX462_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx462Configuration = '%NETFX462_CONFIGURATION%'

IF NOT DEFINED NETFX47_CONFIGURATION (
  %_AECHO% No "%NETFX47_SUFFIX%" configuration specified, using default...
  SET NETFX47_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx47Configuration = '%NETFX47_CONFIGURATION%'

IF NOT DEFINED NETFX471_CONFIGURATION (
  %_AECHO% No "%NETFX471_SUFFIX%" configuration specified, using default...
  SET NETFX471_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% NetFx471Configuration = '%NETFX471_CONFIGURATION%'

IF NOT DEFINED NETFX40_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX40_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX40_BUILD_CONFIGURATION=%NETFX40_CONFIGURATION%All
  ) ELSE (
    SET NETFX40_BUILD_CONFIGURATION=%NETFX40_CONFIGURATION%
  )
)

%_VECHO% NetFx40BuildConfiguration = '%NETFX40_BUILD_CONFIGURATION%'

IF NOT DEFINED NETFX45_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX45_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX45_BUILD_CONFIGURATION=%NETFX45_CONFIGURATION%All
  ) ELSE (
    SET NETFX45_BUILD_CONFIGURATION=%NETFX45_CONFIGURATION%
  )
)

%_VECHO% NetFx45BuildConfiguration = '%NETFX45_BUILD_CONFIGURATION%'

IF NOT DEFINED NETFX451_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX451_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX451_BUILD_CONFIGURATION=%NETFX451_CONFIGURATION%All
  ) ELSE (
    SET NETFX451_BUILD_CONFIGURATION=%NETFX451_CONFIGURATION%
  )
)

%_VECHO% NetFx451BuildConfiguration = '%NETFX451_BUILD_CONFIGURATION%'

IF NOT DEFINED NETFX452_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX452_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX452_BUILD_CONFIGURATION=%NETFX452_CONFIGURATION%All
  ) ELSE (
    SET NETFX452_BUILD_CONFIGURATION=%NETFX452_CONFIGURATION%
  )
)

%_VECHO% NetFx452BuildConfiguration = '%NETFX452_BUILD_CONFIGURATION%'

IF NOT DEFINED NETFX46_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX46_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX46_BUILD_CONFIGURATION=%NETFX46_CONFIGURATION%All
  ) ELSE (
    SET NETFX46_BUILD_CONFIGURATION=%NETFX46_CONFIGURATION%
  )
)

%_VECHO% NetFx46BuildConfiguration = '%NETFX46_BUILD_CONFIGURATION%'

IF NOT DEFINED NETFX461_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX461_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX461_BUILD_CONFIGURATION=%NETFX461_CONFIGURATION%All
  ) ELSE (
    SET NETFX461_BUILD_CONFIGURATION=%NETFX461_CONFIGURATION%
  )
)

%_VECHO% NetFx461BuildConfiguration = '%NETFX461_BUILD_CONFIGURATION%'

IF NOT DEFINED NETFX462_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX462_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX462_BUILD_CONFIGURATION=%NETFX462_CONFIGURATION%All
  ) ELSE (
    SET NETFX462_BUILD_CONFIGURATION=%NETFX462_CONFIGURATION%
  )
)

%_VECHO% NetFx462BuildConfiguration = '%NETFX462_BUILD_CONFIGURATION%'

IF NOT DEFINED NETFX47_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX47_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX47_BUILD_CONFIGURATION=%NETFX47_CONFIGURATION%All
  ) ELSE (
    SET NETFX47_BUILD_CONFIGURATION=%NETFX47_CONFIGURATION%
  )
)

%_VECHO% NetFx47BuildConfiguration = '%NETFX47_BUILD_CONFIGURATION%'

IF NOT DEFINED NETFX471_BUILD_CONFIGURATION (
  %_AECHO% No "%NETFX471_SUFFIX%" build configuration specified, using default...

  IF NOT DEFINED NOPACKAGE (
    SET NETFX471_BUILD_CONFIGURATION=%NETFX471_CONFIGURATION%All
  ) ELSE (
    SET NETFX471_BUILD_CONFIGURATION=%NETFX471_CONFIGURATION%
  )
)

%_VECHO% NetFx471BuildConfiguration = '%NETFX471_BUILD_CONFIGURATION%'

IF NOT DEFINED BARE_CONFIGURATION (
  %_AECHO% No "%BARE_SUFFIX%" configuration specified, using default...
  SET BARE_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% BareConfiguration = '%BARE_CONFIGURATION%'

IF NOT DEFINED LEAN_CONFIGURATION (
  %_AECHO% No "%LEAN_SUFFIX%" configuration specified, using default...
  SET LEAN_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% LeanConfiguration = '%LEAN_CONFIGURATION%'

IF NOT DEFINED DATABASE_CONFIGURATION (
  %_AECHO% No "%DATABASE_SUFFIX%" configuration specified, using default...
  SET DATABASE_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% DatabaseConfiguration = '%DATABASE_CONFIGURATION%'

IF NOT DEFINED UNIX_CONFIGURATION (
  %_AECHO% No "%UNIX_SUFFIX%" configuration specified, using default...
  SET UNIX_CONFIGURATION=%CONFIGURATION%
)

%_VECHO% UnixConfiguration = '%UNIX_CONFIGURATION%'

REM ****************************************************************************
REM ************************ Verify Temporary Directory ************************
REM ****************************************************************************

IF NOT DEFINED TEMP (
  ECHO The TEMP environment variable must be set first.
  GOTO errors
)

%_VECHO% Temp = '%TEMP%'

REM ****************************************************************************
REM ***************************** Set Build Paths ******************************
REM ****************************************************************************

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

SET ROOT=%~dp0\..\..
SET ROOT=%ROOT:\\=\%

%_VECHO% Root = '%ROOT%'

SET RELEASES=%~dp0\..\..\Releases
SET RELEASES=%RELEASES:\\=\%

%_VECHO% Releases = '%RELEASES%'

SET PACKAGE_TOOLS=%~dp0\..\..\Native\Package\Tools
SET PACKAGE_TOOLS=%PACKAGE_TOOLS:\\=\%

%_VECHO% PackageTools = '%PACKAGE_TOOLS%'

SET LOGDIR=%RELEASES%
SET LOGPREFIX=EagleFlight

%_VECHO% LogDir = '%LOGDIR%'
%_VECHO% LogPrefix = '%LOGPREFIX%'

SET NUGETBASEPATH=%ROOT%\bin
SET NUGETOUTPUTDIR=%RELEASES%\Release

%_VECHO% NuGetBasePath = '%NUGETBASEPATH%'
%_VECHO% NuGetOutputDir = '%NUGETOUTPUTDIR%'

SET NETFX20_NUGETBASEPATH=%TEMP%\Eagle_NuGet_%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%
SET NETFX20_NUGETOUTPUTDIR=%RELEASES%\%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%

%_VECHO% NetFx20NuGetBasePath = '%NETFX20_NUGETBASEPATH%'
%_VECHO% NetFx20NuGetOutputDir = '%NETFX20_NUGETOUTPUTDIR%'

SET NETFX40_NUGETBASEPATH=%TEMP%\Eagle_NuGet_%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%
SET NETFX40_NUGETOUTPUTDIR=%RELEASES%\%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%

%_VECHO% NetFx40NuGetBasePath = '%NETFX40_NUGETBASEPATH%'
%_VECHO% NetFx40NuGetOutputDir = '%NETFX40_NUGETOUTPUTDIR%'

SET UNIX_NUGETBASEPATH=%TEMP%\Eagle_NuGet_%UNIX_CONFIGURATION%%UNIX_SUFFIX%
SET UNIX_NUGETOUTPUTDIR=%RELEASES%\%UNIX_CONFIGURATION%%UNIX_SUFFIX%

%_VECHO% UnixNuGetBasePath = '%UNIX_NUGETBASEPATH%'
%_VECHO% UnixNuGetOutputDir = '%UNIX_NUGETOUTPUTDIR%'

REM
REM NOTE: When performing the release process for just the core library, only
REM       look for files to tag inside of the "Library" sub-directory.
REM
IF DEFINED COREONLY (
  SET TAGPATTERN=\Library
) ELSE (
  CALL :fn_UnsetVariable TAGPATTERN
)

%_VECHO% TagPattern = '%TAGPATTERN%'

IF NOT DEFINED NUGET_URL (
  SET NUGET_URL=https://www.nuget.org/
)

IF NOT DEFINED SYMBOLSOURCE_URL (
  SET SYMBOLSOURCE_URL=https://nuget.smbsrc.net
)

%_VECHO% SymbolSourceUrl = '%SYMBOLSOURCE_URL%'

REM ****************************************************************************
REM *********************** Set Primary Garuda File Name ***********************
REM ****************************************************************************

IF NOT DEFINED NONETFX40 (
  SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%\Garuda.dll
) ELSE (
  IF NOT DEFINED NONETFX45 (
    SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%\Garuda.dll
  ) ELSE (
    IF NOT DEFINED NONETFX451 (
      SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%\Garuda.dll
    ) ELSE (
      IF NOT DEFINED NONETFX452 (
        SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%\Garuda.dll
      ) ELSE (
        IF NOT DEFINED NONETFX46 (
          SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%\Garuda.dll
        ) ELSE (
          IF NOT DEFINED NONETFX461 (
            SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%\Garuda.dll
          ) ELSE (
            IF NOT DEFINED NONETFX462 (
              SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%\Garuda.dll
            ) ELSE (
              IF NOT DEFINED NONETFX47 (
                SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%\Garuda.dll
              ) ELSE (
                IF NOT DEFINED NONETFX471 (
                  SET GARUDA_DLL=%ROOT%\bin\%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%\Garuda.dll
                ) ELSE (
                  CALL :fn_UnsetVariable GARUDA_DLL
                )
              )
            )
          )
        )
      )
    )
  )
)

%_VECHO% GarudaDll = '%GARUDA_DLL%'

REM ****************************************************************************
REM ************************ Reset Initial Error Level *************************
REM ****************************************************************************
REM
REM NOTE: Reset the error level now so that we can easily check it against zero
REM       later.
REM
CALL :fn_ResetErrorLevel

REM ****************************************************************************
REM ************************ Verify External Utilities *************************
REM ****************************************************************************

%_VECHO% EagleJunctionDir = '%EagleJunctionDir%'
%_VECHO% EagleSnDir = '%EagleSnDir%'
%_VECHO% EagleSigCheckDir = '%EagleSigCheckDir%'
%_VECHO% EagleNuGetDir = '%EagleNuGetDir%'

IF NOT DEFINED EagleJunctionDir (
  SET JUNCTION_EXE=junction.exe
  GOTO :no_eagleJunctionDir
)

SET JUNCTION_EXE=%EagleJunctionDir%\junction.exe
SET JUNCTION_EXE=%JUNCTION_EXE:\\=\%

:no_eagleJunctionDir

IF NOT DEFINED EagleSnDir (
  SET SN_EXE=sn.exe
  GOTO :no_eagleSnDir
)

SET SN_EXE=%EagleSnDir%\sn.exe
SET SN_EXE=%SN_EXE:\\=\%

:no_eagleSnDir

IF NOT DEFINED EagleSigCheckDir (
  SET SIGCHECK_EXE=SigCheck.exe
  GOTO :no_eagleSigCheckDir
)

SET SIGCHECK_EXE=%EagleSigCheckDir%\SigCheck.exe
SET SIGCHECK_EXE=%SIGCHECK_EXE:\\=\%

:no_eagleSigCheckDir

IF NOT DEFINED EagleNuGetDir (
  SET NUGET_EXE=NuGet4.exe
  GOTO :no_eagleNuGetDir
)

SET NUGET_EXE=%EagleNuGetDir%\NuGet4.exe
SET NUGET_EXE=%NUGET_EXE:\\=\%

:no_eagleNuGetDir

%_VECHO% JunctionExe = '%JUNCTION_EXE%'
%_VECHO% SnExe = '%SN_EXE%'
%_VECHO% SigCheckExe = '%SIGCHECK_EXE%'
%_VECHO% NuGetExe = '%NUGET_EXE%'

IF DEFINED EagleJunctionDir (
  IF EXIST "%JUNCTION_EXE%" (
    %_AECHO% The file "%JUNCTION_EXE%" does exist.
  ) ELSE (
    ECHO The file "%JUNCTION_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %JUNCTION_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

IF DEFINED EagleSnDir (
  IF EXIST "%SN_EXE%" (
    %_AECHO% The file "%SN_EXE%" does exist.
  ) ELSE (
    ECHO The file "%SN_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %SN_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

IF DEFINED EagleSigCheckDir (
  IF EXIST "%SIGCHECK_EXE%" (
    %_AECHO% The file "%SIGCHECK_EXE%" does exist.
  ) ELSE (
    ECHO The file "%SIGCHECK_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %SIGCHECK_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

IF DEFINED EagleNuGetDir (
  IF EXIST "%NUGET_EXE%" (
    %_AECHO% The file "%NUGET_EXE%" does exist.
  ) ELSE (
    ECHO The file "%NUGET_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %NUGET_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

REM ****************************************************************************
REM ********************** Verify Eagle Shell ^(in LKG^) ***********************
REM ****************************************************************************
REM
REM NOTE: Attempt to use the last known good build of Eagle to set the source
REM       identifier for the current build.  If the NOLKG environment variable
REM       is defined, we assume that the last known good build of Eagle will
REM       already be in the path; otherwise, the build will fail ^(below^) when
REM       we try to update the source identifier.
REM
IF DEFINED NOLKG GOTO skip_lastKnownGood
IF NOT DEFINED LKG GOTO skip_lastKnownGood

SET SRCLKGDIR=%LKG%\Eagle\bin
SET SRCLKGDIR=%SRCLKGDIR:\\=\%

%_VECHO% SrcLkgDir = '%SRCLKGDIR%'

IF NOT EXIST "%SRCLKGDIR%\EagleShell.exe" (
  ECHO The file "%SRCLKGDIR%\EagleShell.exe" does not exist.
  GOTO usage
)

%__ECHO% "%SRCLKGDIR%\EagleShell.exe" /? > NUL 2>&1

IF ERRORLEVEL 1 (
  ECHO The "%SRCLKGDIR%\EagleShell.exe" tool appears to be missing.
  GOTO usage
)

%_AECHO% Using LKG "EagleShell.exe" tool from "%SRCLKGDIR%"...

CALL :fn_AppendToPath SRCLKGDIR

%_VECHO% Path = '%PATH%'

REM
REM NOTE: Skip checking for the "EagleShell.exe" tool along the PATH because we
REM       just found it in the LKG location.
REM
GOTO skip_eagleShell

:skip_lastKnownGood

REM ****************************************************************************
REM ********************** Verify Eagle Shell ^(in PATH^) **********************
REM ****************************************************************************

%__ECHO% EagleShell.exe /? > NUL 2>&1

IF ERRORLEVEL 1 (
  ECHO The "EagleShell.exe" tool appears to be missing.
  GOTO usage
)

%_AECHO% Using "EagleShell.exe" tool from the PATH...

:skip_eagleShell

REM ****************************************************************************
REM *********************** Set Build Output Directories ***********************
REM ****************************************************************************

SET SRCBINDIR20=%~dp0\..\..\bin\%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%\bin
SET SRCBINDIR20=%SRCBINDIR20:\\=\%

%_VECHO% SrcBinDir20 = '%SRCBINDIR20%'

SET SRCBINDIR35=%~dp0\..\..\bin\%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%\bin
SET SRCBINDIR35=%SRCBINDIR35:\\=\%

%_VECHO% SrcBinDir35 = '%SRCBINDIR35%'

SET SRCBINDIR40=%~dp0\..\..\bin\%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%\bin
SET SRCBINDIR40=%SRCBINDIR40:\\=\%

%_VECHO% SrcBinDir40 = '%SRCBINDIR40%'

SET SRCBINDIR45=%~dp0\..\..\bin\%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%\bin
SET SRCBINDIR45=%SRCBINDIR45:\\=\%

%_VECHO% SrcBinDir45 = '%SRCBINDIR45%'

SET SRCBINDIR451=%~dp0\..\..\bin\%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%\bin
SET SRCBINDIR451=%SRCBINDIR451:\\=\%

%_VECHO% SrcBinDir451 = '%SRCBINDIR451%'

SET SRCBINDIR452=%~dp0\..\..\bin\%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%\bin
SET SRCBINDIR452=%SRCBINDIR452:\\=\%

%_VECHO% SrcBinDir452 = '%SRCBINDIR452%'

SET SRCBINDIR46=%~dp0\..\..\bin\%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%\bin
SET SRCBINDIR46=%SRCBINDIR46:\\=\%

%_VECHO% SrcBinDir46 = '%SRCBINDIR46%'

SET SRCBINDIR461=%~dp0\..\..\bin\%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%\bin
SET SRCBINDIR461=%SRCBINDIR461:\\=\%

%_VECHO% SrcBinDir461 = '%SRCBINDIR461%'

SET SRCBINDIR462=%~dp0\..\..\bin\%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%\bin
SET SRCBINDIR462=%SRCBINDIR462:\\=\%

%_VECHO% SrcBinDir462 = '%SRCBINDIR462%'

SET SRCBINDIR47=%~dp0\..\..\bin\%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%\bin
SET SRCBINDIR47=%SRCBINDIR47:\\=\%

%_VECHO% SrcBinDir47 = '%SRCBINDIR47%'

SET SRCBINDIR471=%~dp0\..\..\bin\%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%\bin
SET SRCBINDIR471=%SRCBINDIR471:\\=\%

%_VECHO% SrcBinDir471 = '%SRCBINDIR471%'

SET BARESRCBINDIR=%~dp0\..\..\bin\%BARE_CONFIGURATION%%BARE_SUFFIX%\bin
SET BARESRCBINDIR=%BARESRCBINDIR:\\=\%

%_VECHO% BareSrcBinDir = '%BARESRCBINDIR%'

SET LEANSRCBINDIR=%~dp0\..\..\bin\%LEAN_CONFIGURATION%%LEAN_SUFFIX%\bin
SET LEANSRCBINDIR=%LEANSRCBINDIR:\\=\%

%_VECHO% LeanSrcBinDir = '%LEANSRCBINDIR%'

SET DATABASESRCBINDIR=%~dp0\..\..\bin\%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%\bin
SET DATABASESRCBINDIR=%DATABASESRCBINDIR:\\=\%

%_VECHO% DatabaseSrcBinDir = '%DATABASESRCBINDIR%'

SET UNIXSRCBINDIR=%~dp0\..\..\bin\%UNIX_CONFIGURATION%%UNIX_SUFFIX%\bin
SET UNIXSRCBINDIR=%UNIXSRCBINDIR:\\=\%

%_VECHO% UnixSrcBinDir = '%UNIXSRCBINDIR%'

REM ****************************************************************************
REM ******************** Add Build Output Directory to PATH ********************
REM ****************************************************************************
REM
REM NOTE: Always add the build output directory to the PATH because when we
REM       later execute the "EagleShell.exe" tool, we expect to get the patch
REM       level for the current release, not the last known good release.  Also,
REM       to make absolutely sure that we always get the current release version
REM       of the "EagleShell.exe" tool, we must prepend it to the PATH instead
REM       of appending it.
REM
IF DEFINED PATHSRCBINDIR (
  %_AECHO% Using pre-existing build output directory for PATH...
  GOTO skip_OutputDirPath16
)

IF DEFINED NONETFX20 GOTO skip_OutputDirPath2
%_AECHO% Using "%NETFX20_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR20%
GOTO skip_OutputDirPath16

:skip_OutputDirPath2

IF DEFINED NONETFX35 GOTO skip_OutputDirPath3
%_AECHO% Using "%NETFX35_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR35%
GOTO skip_OutputDirPath16

:skip_OutputDirPath3

IF DEFINED NONETFX40 GOTO skip_OutputDirPath4
%_AECHO% Using "%NETFX40_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR40%
GOTO skip_OutputDirPath16

:skip_OutputDirPath4

IF DEFINED NONETFX45 GOTO skip_OutputDirPath5
%_AECHO% Using "%NETFX45_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR45%
GOTO skip_OutputDirPath16

:skip_OutputDirPath5

IF DEFINED NONETFX451 GOTO skip_OutputDirPath6
%_AECHO% Using "%NETFX451_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR451%
GOTO skip_OutputDirPath16

:skip_OutputDirPath6

IF DEFINED NONETFX452 GOTO skip_OutputDirPath7
%_AECHO% Using "%NETFX452_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR452%
GOTO skip_OutputDirPath16

:skip_OutputDirPath7

IF DEFINED NONETFX46 GOTO skip_OutputDirPath8
%_AECHO% Using "%NETFX46_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR46%
GOTO skip_OutputDirPath16

:skip_OutputDirPath8

IF DEFINED NONETFX461 GOTO skip_OutputDirPath9
%_AECHO% Using "%NETFX461_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR461%
GOTO skip_OutputDirPath16

:skip_OutputDirPath9

IF DEFINED NONETFX462 GOTO skip_OutputDirPath10
%_AECHO% Using "%NETFX462_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR462%
GOTO skip_OutputDirPath16

:skip_OutputDirPath10

IF DEFINED NONETFX47 GOTO skip_OutputDirPath11
%_AECHO% Using "%NETFX47_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR47%
GOTO skip_OutputDirPath16

:skip_OutputDirPath11

IF DEFINED NONETFX471 GOTO skip_OutputDirPath12
%_AECHO% Using "%NETFX471_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%SRCBINDIR471%
GOTO skip_OutputDirPath16

:skip_OutputDirPath12

REM
REM NOTE: The "Bare" build configuration is unsuitable for use during the
REM       release process; therefore, its use is manually disabled here.
REM
REM IF DEFINED NOBARE GOTO skip_OutputDirPath13
REM %_AECHO% Using "%BARE_SUFFIX%" build output directory for PATH...
REM SET PATHSRCBINDIR=%BARESRCBINDIR%
REM GOTO skip_OutputDirPath16
REM
REM :skip_OutputDirPath13

IF DEFINED NOLEAN GOTO skip_OutputDirPath14
%_AECHO% Using "%LEAN_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%LEANSRCBINDIR%
GOTO skip_OutputDirPath16

:skip_OutputDirPath14

IF DEFINED NODATABASE GOTO skip_OutputDirPath15
%_AECHO% Using "%DATABASE_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%DATABASESRCBINDIR%
GOTO skip_OutputDirPath16

:skip_OutputDirPath15

IF DEFINED NOUNIX GOTO skip_OutputDirPath16
%_AECHO% Using "%UNIX_SUFFIX%" build output directory for PATH...
SET PATHSRCBINDIR=%UNIXSRCBINDIR%
GOTO skip_OutputDirPath16

ECHO.
ECHO WARNING: No suitable build output directory could be added to the PATH.
ECHO.

:skip_OutputDirPath16

%_VECHO% PathSrcBinDir = '%PATHSRCBINDIR%'

IF DEFINED PATHSRCBINDIR (
  CALL :fn_PrependToPath PATHSRCBINDIR
) ELSE (
  ECHO.
  ECHO WARNING: No suitable build output directory was added to the PATH.
  ECHO.
)

%_VECHO% Path = '%PATH%'

REM ****************************************************************************
REM ***************** Verify Elevated Administrator Privileges *****************
REM ****************************************************************************
REM
REM NOTE: Using Eagle, make sure we have [elevated] administrator privileges
REM       before continuing unless we have been forbidden from doing so.
REM
IF DEFINED NOADMINISTRATOR GOTO skip_isAdministrator

SET IS_ADMINISTRATOR_CMD=EagleShell.exe -eval "puts stdout [isAdministrator]"

IF DEFINED __ECHO (
  %__ECHO% %IS_ADMINISTRATOR_CMD%
  SET ADMINISTRATOR=True
) ELSE (
  FOR /F %%T IN ('%IS_ADMINISTRATOR_CMD%') DO (SET ADMINISTRATOR=%%T)
)

IF NOT DEFINED ADMINISTRATOR (
  ECHO The ADMINISTRATOR environment variable could not be set.
  GOTO usage
)

%_VECHO% Administrator = '%ADMINISTRATOR%'

IF "%ADMINISTRATOR%" == "False" (
  ECHO This tool requires [elevated] administrator privileges.
  GOTO usage
)

:skip_isAdministrator

REM ****************************************************************************
REM ************************ Release Preparation HotFix ************************
REM ****************************************************************************

REM
REM NOTE: Normally, nothing should be done in this section; however, if most
REM       of the release process works perfectly and then fails on something
REM       minor near the very end, this section can be used to insert a GOTO
REM       batch command that skips the previously successful sections, thus
REM       potentially saving several hours of processing time.
REM
REM GOTO skip_successfulSections
REM

REM ****************************************************************************
REM ************************* Verify Release Directory *************************
REM ****************************************************************************

IF NOT EXIST "%RELEASES%" (
  %__ECHO% MKDIR "%RELEASES%"

  IF ERRORLEVEL 1 (
    ECHO Could not create directory "%RELEASES%".
    GOTO errors
  )
)

REM ****************************************************************************
REM ************************ Clean Build/Release Output ************************
REM ****************************************************************************

IF NOT DEFINED NOCLEAN (
  REM
  REM HACK: The clean tool is being run from this tool; therefore, we must
  REM       have it skip trying to delete the log file that we are currently
  REM       using, if any.  The clean tool assumes that this tool uses log
  REM       file names matching the pattern "%TEMP%\EagleFlight*.log".  This
  REM       logging is typically accomplished by redirecting the output of
  REM       this tool on the command line.
  REM
  SET NOFLIGHT=1

  %__ECHO3% CALL "%TOOLS%\clean.bat"

  IF ERRORLEVEL 1 (
    ECHO Cleaning failed.
    GOTO errors
  )

  CALL :fn_UnsetVariable NOFLIGHT
)

REM ****************************************************************************
REM **************************** Skip Build Steps? *****************************
REM ****************************************************************************

IF DEFINED NOBUILD (
  %_AECHO% Skipping build steps...
  GOTO skip_build
)

IF DEFINED BUILD_RELEASE (
  %_AECHO% Going directly to the release phase...
  GOTO build_Release
)

IF DEFINED VERIFY_RELEASE (
  %_AECHO% Going directly to the verify phase...
  GOTO verify_Release
)

REM ****************************************************************************
REM *********************** Minimum Test Configuration? ************************
REM ****************************************************************************

REM
REM NOTE: This is setup to only test the "normal" build configurations for the
REM       lowest and highest supported versions of the .NET Framework.  Tests
REM       are still run for the more esoteric build configurations ^(e.g. Mono,
REM       bare-bones, lean-and-mean, etc)^.  This assumes the machine running
REM       the tests has all released versions of the .NET Framework installed.
REM       Therefore, testing any 4.x version actually results in testing with
REM       the latest in-place update of 4.x ^(e.g. 4.7.1^).  Furthermore, 3.5
REM       is more-or-less the same as 2.0, for our testing purposes.
REM
IF DEFINED MINTEST (
  %_AECHO% Skipping tests for "extra" build configurations...
  REM SET NETFX20_NOTEST=1
  SET NETFX35_NOTEST=1
  SET NETFX40_NOTEST=1
  SET NETFX45_NOTEST=1
  SET NETFX451_NOTEST=1
  SET NETFX452_NOTEST=1
  SET NETFX46_NOTEST=1
  SET NETFX461_NOTEST=1
  REM SET NETFX462_NOTEST=1
  SET NETFX47_NOTEST=1
  SET NETFX471_NOTEST=1
  REM SET BARE_NOTEST=1
  REM SET LEAN_NOTEST=1
  REM SET DATABASE_NOTEST=1
  REM SET UNIX_NOTEST=1
)

REM ****************************************************************************
REM ***************** Default ^(NetFx20^) Build Configuration ******************
REM ****************************************************************************

SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleBuildType=%NETFX20_SUFFIX%
SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleTestType=%NETFX20_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED PATCHLEVEL (
  REM
  REM NOTE: If the patch level has been manually set, add the associated
  REM       build property.
  REM
  SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EaglePatchLevel=true
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleStable=true
)

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX20_NOTEST (
    SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX20_NOTEST (
    IF NOT DEFINED NOTESTALL (
      SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleTestAll=true
    ) ELSE (
      SET NETFX20_FLAGS=%NETFX20_FLAGS% /property:EagleTestAll=false
    )
  )
)

%_VECHO% NetFx20Flags = '%NETFX20_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx35 Build Configuration ************************
REM ****************************************************************************

SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleBuildType=%NETFX35_SUFFIX%
SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleTestType=%NETFX35_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleStable=true
)

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX35_NOTEST (
    SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX35_NOTEST (
    IF NOT DEFINED NOTESTALL (
      SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleTestAll=true
    ) ELSE (
      SET NETFX35_FLAGS=%NETFX35_FLAGS% /property:EagleTestAll=false
    )
  )
)

%_VECHO% NetFx35Flags = '%NETFX35_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx40 Build Configuration ************************
REM ****************************************************************************

SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleBuildType=%NETFX40_SUFFIX%
SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleTestType=%NETFX40_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleStable=true
)

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX40_NOTEST (
    SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX40_NOTEST (
    SET NETFX40_FLAGS=%NETFX40_FLAGS% /property:EagleTestAll=false
  )
)

%_VECHO% NetFx40Flags = '%NETFX40_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx45 Build Configuration ************************
REM ****************************************************************************

SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleBuildType=%NETFX45_SUFFIX%
SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleTestType=%NETFX45_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleStable=true
)

REM
REM HACK: Evidently, installing Visual Studio 2013 breaks using MSBuild to
REM       build native projects that specify a platform toolset of "v110"
REM       ^(e.g. the Garuda and Spilornis projects for Visual Studio 2012^).
REM
SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:VisualStudioVersion=11.0

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX45_NOTEST (
    SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX45_NOTEST (
    SET NETFX45_FLAGS=%NETFX45_FLAGS% /property:EagleTestAll=false
  )
)

%_VECHO% NetFx45Flags = '%NETFX45_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx451 Build Configuration ***********************
REM ****************************************************************************

SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleBuildType=%NETFX451_SUFFIX%
SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleTestType=%NETFX451_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleStable=true
)

REM
REM HACK: Evidently, using MSBuild with Visual Studio 2013 requires some
REM       extra magic to make it recognize the "v120" platform toolset.
REM       ^(e.g. the Garuda and Spilornis projects for Visual Studio 2013^).
REM
SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:VisualStudioVersion=12.0

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX451_NOTEST (
    SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX451_NOTEST (
    SET NETFX451_FLAGS=%NETFX451_FLAGS% /property:EagleTestAll=false
  )
)

%_VECHO% NetFx451Flags = '%NETFX451_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx452 Build Configuration ***********************
REM ****************************************************************************

SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleBuildType=%NETFX452_SUFFIX%
SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleTestType=%NETFX452_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleStable=true
)

REM
REM HACK: Evidently, using MSBuild with Visual Studio 2013 requires some
REM       extra magic to make it recognize the "v120" platform toolset.
REM       ^(e.g. the Garuda and Spilornis projects for Visual Studio 2013^).
REM
SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:VisualStudioVersion=12.0

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX452_NOTEST (
    SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX452_NOTEST (
    SET NETFX452_FLAGS=%NETFX452_FLAGS% /property:EagleTestAll=false
  )
)

%_VECHO% NetFx452Flags = '%NETFX452_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx46 Build Configuration ************************
REM ****************************************************************************

SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleBuildType=%NETFX46_SUFFIX%
SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleTestType=%NETFX46_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleStable=true
)

REM
REM HACK: Evidently, using MSBuild with Visual Studio 2015 requires some
REM       extra magic to make it recognize the "v140" platform toolset.
REM       ^(e.g. the Garuda and Spilornis projects for Visual Studio 2015^).
REM
SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:VisualStudioVersion=14.0

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX46_NOTEST (
    SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX46_NOTEST (
    SET NETFX46_FLAGS=%NETFX46_FLAGS% /property:EagleTestAll=false
  )
)

%_VECHO% NetFx46Flags = '%NETFX46_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx461 Build Configuration ***********************
REM ****************************************************************************

SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleBuildType=%NETFX461_SUFFIX%
SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleTestType=%NETFX461_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleStable=true
)

REM
REM HACK: Evidently, using MSBuild with Visual Studio 2015 requires some
REM       extra magic to make it recognize the "v140" platform toolset.
REM       ^(e.g. the Garuda and Spilornis projects for Visual Studio 2015^).
REM
SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:VisualStudioVersion=14.0

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX461_NOTEST (
    SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX461_NOTEST (
    IF NOT DEFINED NOTESTALL (
      SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleTestAll=true
    ) ELSE (
      SET NETFX461_FLAGS=%NETFX461_FLAGS% /property:EagleTestAll=false
    )
  )
)

%_VECHO% NetFx461Flags = '%NETFX461_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx462 Build Configuration ***********************
REM ****************************************************************************

SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleBuildType=%NETFX462_SUFFIX%
SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleTestType=%NETFX462_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleStable=true
)

REM
REM HACK: Evidently, using MSBuild with Visual Studio 2015 requires some
REM       extra magic to make it recognize the "v140" platform toolset.
REM       ^(e.g. the Garuda and Spilornis projects for Visual Studio 2015^).
REM
SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:VisualStudioVersion=14.0

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX462_NOTEST (
    SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX462_NOTEST (
    IF NOT DEFINED NOTESTALL (
      SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleTestAll=true
    ) ELSE (
      SET NETFX462_FLAGS=%NETFX462_FLAGS% /property:EagleTestAll=false
    )
  )
)

%_VECHO% NetFx462Flags = '%NETFX462_FLAGS%'

REM ****************************************************************************
REM *********************** NetFx47 Build Configuration ************************
REM ****************************************************************************

SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleBuildType=%NETFX47_SUFFIX%
SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleTestType=%NETFX47_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleStable=true
)

REM
REM HACK: Evidently, using MSBuild with Visual Studio 2017 requires some
REM       extra magic to make it recognize the "v141" platform toolset.
REM       ^(e.g. the Garuda and Spilornis projects for Visual Studio 2017^).
REM
SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:VisualStudioVersion=15.0

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX47_NOTEST (
    SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX47_NOTEST (
    IF NOT DEFINED NOTESTALL (
      SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleTestAll=true
    ) ELSE (
      SET NETFX47_FLAGS=%NETFX47_FLAGS% /property:EagleTestAll=false
    )
  )
)

%_VECHO% NetFx47Flags = '%NETFX47_FLAGS%'

REM ****************************************************************************
REM ********************** NetFx471 Build Configuration ************************
REM ****************************************************************************

SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleBuildType=%NETFX471_SUFFIX%
SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleTestType=%NETFX471_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleStable=true
)

REM
REM HACK: Evidently, using MSBuild with Visual Studio 2017 requires some
REM       extra magic to make it recognize the "v141" platform toolset.
REM       ^(e.g. the Garuda and Spilornis projects for Visual Studio 2017^).
REM
SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:VisualStudioVersion=15.0

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX471_NOTEST (
    SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED NETFX471_NOTEST (
    IF NOT DEFINED NOTESTALL (
      SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleTestAll=true
    ) ELSE (
      SET NETFX471_FLAGS=%NETFX471_FLAGS% /property:EagleTestAll=false
    )
  )
)

%_VECHO% NetFx471Flags = '%NETFX471_FLAGS%'

REM ****************************************************************************
REM ************************* Bare Build Configuration *************************
REM ****************************************************************************

SET BARE_FLAGS=%BARE_FLAGS% /property:EagleBuildType=%BARE_SUFFIX%
SET BARE_FLAGS=%BARE_FLAGS% /property:EagleTestType=%BARE_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET BARE_FLAGS=%BARE_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET BARE_FLAGS=%BARE_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET BARE_FLAGS=%BARE_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET BARE_FLAGS=%BARE_FLAGS% /property:EagleStable=true
)

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET BARE_FLAGS=%BARE_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED BARE_NOTEST (
    SET BARE_FLAGS=%BARE_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED BARE_NOTEST (
    SET BARE_FLAGS=%BARE_FLAGS% /property:EagleTestAll=false
  )
)

%_VECHO% BareFlags = '%BARE_FLAGS%'

REM ****************************************************************************
REM ********************* LeanAndMean Build Configuration **********************
REM ****************************************************************************

SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleBuildType=%LEAN_SUFFIX%
SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleTestType=%LEAN_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleStable=true
)

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED LEAN_NOTEST (
    SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED LEAN_NOTEST (
    IF NOT DEFINED NOTESTALL (
      SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleTestAll=true
    ) ELSE (
      SET LEAN_FLAGS=%LEAN_FLAGS% /property:EagleTestAll=false
    )
  )
)

%_VECHO% LeanFlags = '%LEAN_FLAGS%'

REM ****************************************************************************
REM *********************** Database Build Configuration ***********************
REM ****************************************************************************

SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleBuildType=%DATABASE_SUFFIX%
SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleTestType=%DATABASE_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleStable=true
)

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED DATABASE_NOTEST (
    SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED DATABASE_NOTEST (
    SET DATABASE_FLAGS=%DATABASE_FLAGS% /property:EagleTestAll=false
  )
)

%_VECHO% DatabaseFlags = '%DATABASE_FLAGS%'

REM ****************************************************************************
REM ********************** MonoOnUnix Build Configuration **********************
REM ****************************************************************************

SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleBuildType=%UNIX_SUFFIX%
SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleTestType=%UNIX_SUFFIX%

IF DEFINED NOSIGN (
  REM
  REM NOTE: When signing is totally disabled, we need to skip all SignCode
  REM       and SignTool related command execution in the targets file.
  REM
  SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleAuthenticodeSign=false /property:EagleAuthenticodeSign32BitOnly=false
)

IF DEFINED ASSEMBLY_DATETIME (
  REM
  REM NOTE: If the assembly date/time has been manually set, add the
  REM       associated build property.
  REM
  SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleAssemblyDateTime=true
)

IF DEFINED OFFICIAL (
  REM
  REM NOTE: For an official release build, set the necessary build property.
  REM
  SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleOfficial=true
)

IF DEFINED STABLE (
  REM
  REM NOTE: For a stable release build, set the necessary build property.
  REM
  SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleStable=true
)

IF DEFINED STRONGNAMETAG_ATTRIBUTE (
  REM
  REM NOTE: Technically, the EagleStrongNamePrefix MSBuild property and the
  REM       STRONGNAMETAG_ATTRIBUTE environment variable are related purely
  REM       as a matter of convention and need not contain the same value.
  REM
  SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleStrongNamePrefix=%STRONGNAMETAG_ATTRIBUTE%
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED UNIX_NOTEST (
    SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleRunTests=true
  )
)

IF NOT DEFINED NOTEST (
  IF NOT DEFINED UNIX_NOTEST (
    SET UNIX_FLAGS=%UNIX_FLAGS% /property:EagleTestAll=false
  )
)

%_VECHO% UnixFlags = '%UNIX_FLAGS%'

REM ****************************************************************************
REM ************************** Query Eagle PatchLevel **************************
REM ****************************************************************************

REM
REM NOTE: This must be done now so that invocations of the "versionTag.eagle"
REM       tool can pickup the PATCHLEVEL environment variable, if necessary
REM       ^(e.g. if the NONETFX20 environment variable has been set for this
REM       release^).
REM
IF DEFINED PATCHLEVEL GOTO skip_patchLevel1
IF NOT DEFINED NONETFX20 GOTO skip_patchLevel1

SET GET_PATCHLEVEL_CMD=EagleShell.exe -eval "puts stdout [appendArgs [info engine Version] . [join [clock build] .]]"

IF DEFINED __ECHO (
  %__ECHO% %GET_PATCHLEVEL_CMD%
  SET PATCHLEVEL=1.0.X.X
) ELSE (
  FOR /F %%T IN ('%GET_PATCHLEVEL_CMD%') DO (SET PATCHLEVEL=%%T)
)

IF NOT DEFINED PATCHLEVEL (
  ECHO The PATCHLEVEL environment variable could not be set.
  GOTO errors
)

:skip_patchLevel1

%_VECHO% PatchLevel = '%PATCHLEVEL%'

REM ****************************************************************************
REM ************************** Set Build Release Info **************************
REM ****************************************************************************

IF NOT DEFINED NOATTRIBUTE (
  IF DEFINED RELEASE_ATTRIBUTE (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %RELEASE_ATTRIBUTE% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyReleaseMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  ) ELSE (
    ECHO.
    ECHO WARNING: The RELEASE_ATTRIBUTE environment variable is not set.
    ECHO.
  )

  IF DEFINED STRONGNAMETAG_ATTRIBUTE (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %STRONGNAMETAG_ATTRIBUTE% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyStrongNameTagMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  ) ELSE (
    ECHO.
    ECHO WARNING: The STRONGNAMETAG_ATTRIBUTE environment variable is not set.
    ECHO.
  )
)

REM ****************************************************************************
REM ************************** Set Build Source Info ***************************
REM ****************************************************************************

IF NOT DEFINED NOSOURCEID (
  FOR %%T IN (%TAGFILES%) DO (
    REM
    REM NOTE: The TAGPATTERN environment variable may not be set and that
    REM       is fine.
    REM
    FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" SourceIdMode "%%F"

      IF ERRORLEVEL 1 (
        ECHO Updating "%%F" failed.
        GOTO errors
      )
    )
  )

  IF NOT DEFINED NOPACKAGE (
    FOR %%T IN (%PACKAGE_TAGFILES%) DO (
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\Native\Package\%%T" 2^> NUL') DO (
        %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" SourceIdMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )
)

REM ****************************************************************************
REM *********************** Skip To Specific Build Step? ***********************
REM ****************************************************************************

IF DEFINED BUILD_NETFX20 (
  GOTO build_NetFx20
)

IF DEFINED BUILD_NETFX35 (
  GOTO build_NetFx35
)

IF DEFINED BUILD_NETFX40 (
  GOTO build_NetFx40
)

IF DEFINED BUILD_NETFX45 (
  GOTO build_NetFx45
)

IF DEFINED BUILD_NETFX451 (
  GOTO build_NetFx451
)

IF DEFINED BUILD_NETFX452 (
  GOTO build_NetFx452
)

IF DEFINED BUILD_NETFX46 (
  GOTO build_NetFx46
)

IF DEFINED BUILD_NETFX461 (
  GOTO build_NetFx461
)

IF DEFINED BUILD_NETFX462 (
  GOTO build_NetFx462
)

IF DEFINED BUILD_NETFX47 (
  GOTO build_NetFx47
)

IF DEFINED BUILD_NETFX471 (
  GOTO build_NetFx471
)

IF DEFINED BUILD_BARE (
  GOTO build_Bare
)

IF DEFINED BUILD_LEAN (
  GOTO build_Lean
)

IF DEFINED BUILD_DATABASE (
  GOTO build_Database
)

IF DEFINED BUILD_UNIX (
  GOTO build_Unix
)

REM ****************************************************************************
REM ************************ Manual PatchLevel Override ************************
REM ****************************************************************************

CALL :fn_UnsetVariable WROTE_PATCHLEVEL

IF DEFINED PATCHLEVEL (
  CALL :fn_writePatchLevel

  IF ERRORLEVEL 1 (
    GOTO errors
  )

  SET WROTE_PATCHLEVEL=1
)

REM ****************************************************************************
REM ************************* Manual DateTime Override *************************
REM ****************************************************************************

IF DEFINED ASSEMBLY_DATETIME (
  CALL :fn_writeDateTime

  IF ERRORLEVEL 1 (
    GOTO errors
  )
)

REM ****************************************************************************
REM ************************ Build Default ^(NetFx20^) *************************
REM ****************************************************************************

:build_NetFx20

IF NOT DEFINED NONETFX20 (
  SET LOGSUFFIX=%NETFX20_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX20ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX20_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET EagleConfigurationSuffix=%NETFX20_SUFFIX%
    SET UTILITYONLY=1
    SET PLATFORM=Win32

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX20_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX20_SUFFIX%" native utility binaries for x86 failed.
      GOTO errors
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX20_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX20_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
    CALL :fn_UnsetVariable EagleConfigurationSuffix
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX20_CONFIGURATION%" %NETFX20_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%" binaries failed.
    GOTO errors
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX20ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM ********************* Check Default ^(NetFx20^) Files **********************
REM ****************************************************************************

IF NOT DEFINED NONETFX20 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX20_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM *********************** Synchronize Build PatchLevel ***********************
REM ****************************************************************************

REM
REM NOTE: There is no need to do this if the patch level was already written
REM       due to a manual override ^(i.e. for the default build^).
REM
IF NOT DEFINED NOPATCHLEVEL IF NOT DEFINED WROTE_PATCHLEVEL (
  CALL :fn_writePatchLevel

  IF ERRORLEVEL 1 (
    GOTO errors
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx35 *******************************
REM ****************************************************************************

:build_NetFx35

IF NOT DEFINED NONETFX35 (
  SET LOGSUFFIX=%NETFX35_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX35ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED EXTRA (
    SET NOEXTRA=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX35_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET EagleConfigurationSuffix=%NETFX35_SUFFIX%
    SET UTILITYONLY=1
    SET PLATFORM=Win32

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX35_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX35_SUFFIX%" native utility binaries for x86 failed.
      GOTO errors
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX35_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX35_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
    CALL :fn_UnsetVariable EagleConfigurationSuffix
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX35_CONFIGURATION%" %NETFX35_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%" binaries failed.
    GOTO errors
  )

  IF NOT DEFINED EXTRA (
    CALL :fn_UnsetVariable NOEXTRA
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX35ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx35 Files ****************************
REM ****************************************************************************

IF NOT DEFINED NONETFX35 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX35_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx40 *******************************
REM ****************************************************************************

:build_NetFx40

IF NOT DEFINED NONETFX40 (
  SET LOGSUFFIX=%NETFX40_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX40ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX40_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX40_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX40_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX40_BUILD_CONFIGURATION%" %NETFX40_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX40_BUILD_CONFIGURATION%%NETFX40_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX40_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX40ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx40 Files ****************************
REM ****************************************************************************

IF NOT DEFINED NONETFX40 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx45 *******************************
REM ****************************************************************************

:build_NetFx45

IF NOT DEFINED NONETFX45 (
  SET LOGSUFFIX=%NETFX45_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX45ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX45_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX45_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX45_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX45_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" native utility binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX45_BUILD_CONFIGURATION%" %NETFX45_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX45_BUILD_CONFIGURATION%%NETFX45_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX45_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX45_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" native package binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX45ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx45 Files ****************************
REM ****************************************************************************

IF NOT DEFINED NONETFX45 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx451 ******************************
REM ****************************************************************************

:build_NetFx451

IF NOT DEFINED NONETFX451 (
  SET LOGSUFFIX=%NETFX451_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX451ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX451_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX451_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX451_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX451_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" native utility binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX451_BUILD_CONFIGURATION%" %NETFX451_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX451_BUILD_CONFIGURATION%%NETFX451_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX451_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX451_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" native package binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX451ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx451 Files ***************************
REM ****************************************************************************

IF NOT DEFINED NONETFX451 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx452 ******************************
REM ****************************************************************************

:build_NetFx452

IF NOT DEFINED NONETFX452 (
  SET LOGSUFFIX=%NETFX452_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX452ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX452_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX452_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX452_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX452_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" native utility binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX452_BUILD_CONFIGURATION%" %NETFX452_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX452_BUILD_CONFIGURATION%%NETFX452_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX452_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX452_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" native package binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX452ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx452 Files ***************************
REM ****************************************************************************

IF NOT DEFINED NONETFX452 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx46 *******************************
REM ****************************************************************************

:build_NetFx46

IF NOT DEFINED NONETFX46 (
  SET LOGSUFFIX=%NETFX46_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX46ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX46_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX46_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX46_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX46_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" native utility binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX46_BUILD_CONFIGURATION%" %NETFX46_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX46_BUILD_CONFIGURATION%%NETFX46_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX46_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX46_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" native package binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX46ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx46 Files ****************************
REM ****************************************************************************

IF NOT DEFINED NONETFX46 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx461 ******************************
REM ****************************************************************************

:build_NetFx461

IF NOT DEFINED NONETFX461 (
  SET LOGSUFFIX=%NETFX461_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX461ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX461_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX461_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX461_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX461_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" native utility binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX461_BUILD_CONFIGURATION%" %NETFX461_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX461_BUILD_CONFIGURATION%%NETFX461_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX461_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX461_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" native package binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX461ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx461 Files ***************************
REM ****************************************************************************

IF NOT DEFINED NONETFX461 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx462 ******************************
REM ****************************************************************************

:build_NetFx462

IF NOT DEFINED NONETFX462 (
  SET LOGSUFFIX=%NETFX462_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX462ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX462_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX462_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX462_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX462_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" native utility binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX462_BUILD_CONFIGURATION%" %NETFX462_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX462_BUILD_CONFIGURATION%%NETFX462_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX462_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX462_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" native package binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX462ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx462 Files ***************************
REM ****************************************************************************

IF NOT DEFINED NONETFX462 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build NetFx47 *******************************
REM ****************************************************************************

:build_NetFx47

IF NOT DEFINED NONETFX47 (
  SET LOGSUFFIX=%NETFX47_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX47ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX47_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX47_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX47_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX47_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" native utility binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX47_BUILD_CONFIGURATION%" %NETFX47_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX47_BUILD_CONFIGURATION%%NETFX47_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX47_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX47_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" native package binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX47ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check NetFx47 Files ****************************
REM ****************************************************************************

IF NOT DEFINED NONETFX47 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ***************************** Build NetFx471 *******************************
REM ****************************************************************************

:build_NetFx471

IF NOT DEFINED NONETFX471 (
  SET LOGSUFFIX=%NETFX471_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX471ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %NETFX471_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET UTILITYONLY=1

    REM
    REM NOTE: If we are going to build the Eagle Package for Tcl, then the
    REM       Win32 platform binaries for the Eagle Native Utility Library
    REM       will also be built; therefore, skip building it separately in
    REM       that case.
    REM
    IF DEFINED NOPACKAGE (
      SET PLATFORM=Win32

      %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX471_FLAGS% /property:FlightType=%ARGS%

      IF ERRORLEVEL 1 (
        ECHO Building "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" native utility binaries for x86 failed.
        GOTO errors
      )
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX471_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX471_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" native utility binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%NETFX471_BUILD_CONFIGURATION%" %NETFX471_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%NETFX471_BUILD_CONFIGURATION%%NETFX471_SUFFIX%" binaries failed.
    GOTO errors
  )

  REM
  REM NOTE: Unless explicitly disabled, the Eagle Native Package for Tcl
  REM       binaries will be built as part of this build configuration;
  REM       however, they will only be built for the default platform,
  REM       which is typically "Win32".  We need to make sure the other
  REM       platforms get built as well.
  REM
  IF NOT DEFINED NOPACKAGE (
    SET PACKAGEONLY=1
    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX471_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" native package binaries for x64 failed.
      GOTO errors
    )

    SET PLATFORM=ARM

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %NETFX471_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" native package binaries for ARM failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable PACKAGEONLY
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX471ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM ************************** Check NetFx471 Files ****************************
REM ****************************************************************************

IF NOT DEFINED NONETFX471 (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ******************************** Build Bare ********************************
REM ****************************************************************************

:build_Bare

IF NOT DEFINED NOBARE (
  SET LOGSUFFIX=%BARE_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX20ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED EXTRA (
    SET NOEXTRA=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %BARE_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%BARE_CONFIGURATION%" %BARE_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%BARE_CONFIGURATION%%BARE_SUFFIX%" binaries failed.
    GOTO errors
  )

  IF NOT DEFINED EXTRA (
    CALL :fn_UnsetVariable NOEXTRA
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX20ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM ***************************** Check Bare Files *****************************
REM ****************************************************************************

IF NOT DEFINED NOBARE (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%BARE_CONFIGURATION%%BARE_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%BARE_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM **************************** Build LeanAndMean *****************************
REM ****************************************************************************

:build_Lean

IF NOT DEFINED NOLEAN (
  SET LOGSUFFIX=%LEAN_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX40ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %LEAN_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  IF NOT DEFINED NONATIVE (
    SET EagleConfigurationSuffix=%LEAN_SUFFIX%
    SET UTILITYONLY=1
    SET PLATFORM=Win32

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %LEAN_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%LEAN_SUFFIX%" native utility binaries for x86 failed.
      GOTO errors
    )

    SET PLATFORM=x64

    %__ECHO3% CALL "%TOOLS%\build.bat" "%NATIVE_CONFIGURATION%" %LEAN_FLAGS% /property:FlightType=%ARGS%

    IF ERRORLEVEL 1 (
      ECHO Building "%NATIVE_CONFIGURATION%%LEAN_SUFFIX%" native utility binaries for x64 failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable PLATFORM
    CALL :fn_UnsetVariable UTILITYONLY
    CALL :fn_UnsetVariable EagleConfigurationSuffix
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%LEAN_CONFIGURATION%" %LEAN_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%LEAN_CONFIGURATION%%LEAN_SUFFIX%" binaries failed.
    GOTO errors
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX40ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM ***************************** Check Lean Files *****************************
REM ****************************************************************************

IF NOT DEFINED NOLEAN (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%LEAN_CONFIGURATION%%LEAN_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%LEAN_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ****************************** Build Database ******************************
REM ****************************************************************************

:build_Database

IF NOT DEFINED NODATABASE (
  SET LOGSUFFIX=%DATABASE_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX20ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED EXTRA (
    SET NOEXTRA=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %DATABASE_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%DATABASE_CONFIGURATION%" %DATABASE_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%" binaries failed.
    GOTO errors
  )

  IF NOT DEFINED EXTRA (
    CALL :fn_UnsetVariable NOEXTRA
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX20ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

REM ****************************************************************************
REM *************************** Check Database Files ***************************
REM ****************************************************************************

IF NOT DEFINED NODATABASE (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%DATABASE_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

REM ****************************************************************************
REM ***************************** Build MonoOnUnix *****************************
REM ****************************************************************************

:build_Unix

IF NOT DEFINED NOUNIX (
  SET LOGSUFFIX=%UNIX_SUFFIX%
  CALL :fn_ShowVariable LogSuffix LOGSUFFIX

  IF NOT DEFINED NOFRAMEWORK (
    SET NETFX40ONLY=1
  )

  IF NOT DEFINED COMMERCIAL (
    SET NOCOMMERCIAL=1
  )

  IF NOT DEFINED ENTERPRISE (
    SET NOENTERPRISE=1
  )

  IF NOT DEFINED EXTRA (
    SET NOEXTRA=1
  )

  IF NOT DEFINED NOBUILDINFO (
    FOR %%T IN (%TAGFILES%) DO (
      REM
      REM NOTE: The TAGPATTERN environment variable may not be set and that
      REM       is fine.
      REM
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
        %__ECHO% ECHO %UNIX_SUFFIX% %PIPE% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyTextMode "%%F"

        IF ERRORLEVEL 1 (
          ECHO Updating "%%F" failed.
          GOTO errors
        )
      )
    )
  )

  %__ECHO3% CALL "%TOOLS%\build.bat" "%UNIX_CONFIGURATION%" %UNIX_FLAGS% /property:FlightType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Building "%UNIX_CONFIGURATION%%UNIX_SUFFIX%" binaries failed.
    GOTO errors
  )

  IF NOT DEFINED EXTRA (
    CALL :fn_UnsetVariable NOEXTRA
  )

  IF NOT DEFINED ENTERPRISE (
    CALL :fn_UnsetVariable NOENTERPRISE
  )

  IF NOT DEFINED COMMERCIAL (
    CALL :fn_UnsetVariable NOCOMMERCIAL
  )

  IF NOT DEFINED NOFRAMEWORK (
    CALL :fn_UnsetVariable NETFX40ONLY
  )

  CALL :fn_UnsetVariable LOGSUFFIX
)

IF DEFINED BUILDONLY (
  %_AECHO% Build phase complete in build-only mode, exiting...
  GOTO no_errors
)

REM ****************************************************************************
REM ************************** Check MonoOnUnix Files **************************
REM ****************************************************************************

IF NOT DEFINED NOUNIX (
  IF NOT DEFINED NOSTRONGNAME (
    FOR %%I IN (%MANAGEDIMAGEFILES%) DO (
      FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%UNIX_CONFIGURATION%%UNIX_SUFFIX%\%%I" ^| FIND /V /I "Original" ^| FIND /V /I "Obfuscated" 2^> NUL') DO (
        %_AECHO% Checking strong name on file "%%F"...
        %_CECHO% "%SN_EXE%" -vf "%%F"
        %__ECHO% "%SN_EXE%" -vf "%%F"

        IF ERRORLEVEL 1 (
          ECHO Checking strong name on file "%%F" failed.
          GOTO errors
        )

        IF NOT DEFINED NOSIGN (
          IF NOT DEFINED NOSIGCHECK (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )

  IF NOT DEFINED NOSIGN (
    IF NOT DEFINED NOSIGCHECK (
      FOR %%P IN (%PLATFORMS%) DO (
        FOR %%I IN (%NATIVEIMAGEFILES%) DO (
          FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%\bin\%%P\%NATIVE_CONFIGURATION%%UNIX_SUFFIX%\%%I" 2^> NUL') DO (
            %_AECHO% Checking signatures on file "%%F"...
            %_CECHO% "%SIGCHECK_EXE%" "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
            %__ECHO% "%SIGCHECK_EXE%" "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

            IF ERRORLEVEL 1 (
              ECHO Checking signatures on file "%%F" failed.
              GOTO errors
            )
          )
        )
      )
    )
  )
)

:skip_build

REM ****************************************************************************
REM ************************** Create Setup Packages ***************************
REM ****************************************************************************

IF NOT DEFINED NOBAKE (
  SET SKIP_SIGN_UNINSTALLER=1

  IF NOT DEFINED NONETFX20 (
    REM SET SUFFIX=%NETFX20_SUFFIX%_
    CALL :fn_UnsetVariable SUFFIX
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 2.0
    REM       setup package require the Visual C++ 2005 Runtime and we also
    REM       know that it is the default runtime used by the setup package
    REM       baking tool.
    REM
    SET VCRUNTIME=2005_SP1_MFC

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable VCRUNTIME
    REM CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX35 (
    SET SUFFIX=%NETFX35_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 3.5
    REM       setup package require the Visual C++ 2008 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2008_SP1_MFC

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX40 (
    SET SUFFIX=%NETFX40_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.0
    REM       setup package require the Visual C++ 2010 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2010_SP1_MFC

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX45 (
    SET SUFFIX=%NETFX45_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.5
    REM       setup package require the Visual C++ 2012 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2012_VSU4

    REM
    REM HACK: We know that the native components for the .NET Framework 4.5
    REM       setup package should include the ARM binaries.
    REM
    SET INCLUDEARM=1

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable INCLUDEARM
    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX451 (
    SET SUFFIX=%NETFX451_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.5.1
    REM       setup package require the Visual C++ 2013 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2013_VSU2

    REM
    REM HACK: We know that the native components for the .NET Framework 4.5.1
    REM       setup package should include the ARM binaries.
    REM
    SET INCLUDEARM=1

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable INCLUDEARM
    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX452 (
    SET SUFFIX=%NETFX452_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.5.2
    REM       setup package require the Visual C++ 2013 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2013_VSU2

    REM
    REM HACK: We know that the native components for the .NET Framework 4.5.2
    REM       setup package should include the ARM binaries.
    REM
    SET INCLUDEARM=1

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable INCLUDEARM
    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX46 (
    SET SUFFIX=%NETFX46_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.6
    REM       setup package require the Visual C++ 2015 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2015_VSU3

    REM
    REM HACK: We know that the native components for the .NET Framework 4.6
    REM       setup package should include the ARM binaries.
    REM
    SET INCLUDEARM=1

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable INCLUDEARM
    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX461 (
    SET SUFFIX=%NETFX461_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.6.1
    REM       setup package require the Visual C++ 2015 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2015_VSU3

    REM
    REM HACK: We know that the native components for the .NET Framework 4.6.1
    REM       setup package should include the ARM binaries.
    REM
    SET INCLUDEARM=1

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable INCLUDEARM
    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX462 (
    SET SUFFIX=%NETFX462_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.6.2
    REM       setup package require the Visual C++ 2015 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2015_VSU3

    REM
    REM HACK: We know that the native components for the .NET Framework 4.6.2
    REM       setup package should include the ARM binaries.
    REM
    SET INCLUDEARM=1

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable INCLUDEARM
    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX47 (
    SET SUFFIX=%NETFX47_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.7
    REM       setup package require the Visual C++ 2017 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2017_VCU3

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX471 (
    SET SUFFIX=%NETFX471_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    REM
    REM HACK: We know that the native components in the .NET Framework 4.7.1
    REM       setup package require the Visual C++ 2017 Runtime and we also
    REM       know that it must be set explicitly.
    REM
    SET VCRUNTIME=2017_VCU3

    %__ECHO3% CALL "%TOOLS%\bake.bat" "%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%"

    IF ERRORLEVEL 1 (
      ECHO Baking "%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%" setup failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable VCRUNTIME
    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NOPACKAGE (
    IF NOT DEFINED NONETFX40 (
      SET SUFFIX=%NETFX40_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      REM
      REM HACK: We know that the native components in the Eagle Package for Tcl
      REM       setup package require the Visual C++ 2010 Runtime and we also
      REM       know that it is the default runtime used by the native setup
      REM       package baking tool.
      REM
      %__ECHO3% CALL "%PACKAGE_TOOLS%\bake.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Baking "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" setup failed.
        GOTO errors
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX46 (
      SET SUFFIX=%NETFX46_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      REM
      REM HACK: We know that the native components in the Eagle Package for Tcl
      REM       setup package require the Visual C++ 2015 Runtime and we also
      REM       know that it must be set explicitly for use by the native setup
      REM       package baking tool.
      REM
      SET VCRUNTIME=2015_VSU3

      %__ECHO3% CALL "%PACKAGE_TOOLS%\bake.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Baking "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" setup failed.
        GOTO errors
      )

      CALL :fn_UnsetVariable VCRUNTIME
      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX461 (
      SET SUFFIX=%NETFX461_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      REM
      REM HACK: We know that the native components in the Eagle Package for Tcl
      REM       setup package require the Visual C++ 2015 Runtime and we also
      REM       know that it must be set explicitly for use by the native setup
      REM       package baking tool.
      REM
      SET VCRUNTIME=2015_VSU3

      %__ECHO3% CALL "%PACKAGE_TOOLS%\bake.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Baking "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" setup failed.
        GOTO errors
      )

      CALL :fn_UnsetVariable VCRUNTIME
      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX462 (
      SET SUFFIX=%NETFX462_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      REM
      REM HACK: We know that the native components in the Eagle Package for Tcl
      REM       setup package require the Visual C++ 2015 Runtime and we also
      REM       know that it must be set explicitly for use by the native setup
      REM       package baking tool.
      REM
      SET VCRUNTIME=2015_VSU3

      %__ECHO3% CALL "%PACKAGE_TOOLS%\bake.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Baking "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" setup failed.
        GOTO errors
      )

      CALL :fn_UnsetVariable VCRUNTIME
      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX47 (
      SET SUFFIX=%NETFX47_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      REM
      REM HACK: We know that the native components in the Eagle Package for Tcl
      REM       setup package require the Visual C++ 2017 Runtime and we also
      REM       know that it must be set explicitly for use by the native setup
      REM       package baking tool.
      REM
      SET VCRUNTIME=2017_VCU3

      %__ECHO3% CALL "%PACKAGE_TOOLS%\bake.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Baking "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" setup failed.
        GOTO errors
      )

      CALL :fn_UnsetVariable VCRUNTIME
      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX471 (
      SET SUFFIX=%NETFX471_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      REM
      REM HACK: We know that the native components in the Eagle Package for Tcl
      REM       setup package require the Visual C++ 2017 Runtime and we also
      REM       know that it must be set explicitly for use by the native setup
      REM       package baking tool.
      REM
      SET VCRUNTIME=2017_VCU3

      %__ECHO3% CALL "%PACKAGE_TOOLS%\bake.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Baking "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" setup failed.
        GOTO errors
      )

      CALL :fn_UnsetVariable VCRUNTIME
      CALL :fn_UnsetVariable SUFFIX
    )
  )

  CALL :fn_UnsetVariable SKIP_SIGN_UNINSTALLER
)

REM ****************************************************************************
REM ********************* Tag ChangeLog/Update/NuGet Files *********************
REM ****************************************************************************

IF NOT DEFINED NOTAG (
  IF EXIST "%TOOLS%\..\..\ChangeLog" (
    %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" ChangeLogMode "%TOOLS%\..\..\ChangeLog"

    IF ERRORLEVEL 1 (
      ECHO Updating "%TOOLS%\..\..\ChangeLog" failed.
      GOTO errors
    )
  )

  IF DEFINED STABLE (
    IF EXIST "%TOOLS%\..\..\web\stable.txt" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" UpdateMode "%TOOLS%\..\..\web\stable.txt"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\web\stable.txt" failed.
        GOTO errors
      )
    )
  ) ELSE (
    IF EXIST "%TOOLS%\..\..\web\latest.txt" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" UpdateMode "%TOOLS%\..\..\web\latest.txt"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\web\latest.txt" failed.
        GOTO errors
      )
    )
  )

  IF DEFINED STABLE (
    IF EXIST "%TOOLS%\..\..\NuGet\Eagle.nuspec" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" NuSpecMode "%TOOLS%\..\..\NuGet\Eagle.nuspec"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\NuGet\Eagle.nuspec" failed.
        GOTO errors
      )
    )

    IF EXIST "%TOOLS%\..\..\NuGet\Eagle.src.nuspec" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" NuSpecMode "%TOOLS%\..\..\NuGet\Eagle.src.nuspec"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\NuGet\Eagle.src.nuspec" failed.
        GOTO errors
      )
    )

    IF EXIST "%TOOLS%\..\..\NuGet\Eagle.CLRv2.nuspec" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" NuSpecMode "%TOOLS%\..\..\NuGet\Eagle.CLRv2.nuspec"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\NuGet\Eagle.CLRv2.nuspec" failed.
        GOTO errors
      )
    )

    IF EXIST "%TOOLS%\..\..\NuGet\Eagle.CLRv4.nuspec" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" NuSpecMode "%TOOLS%\..\..\NuGet\Eagle.CLRv4.nuspec"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\NuGet\Eagle.CLRv4.nuspec" failed.
        GOTO errors
      )
    )

    IF EXIST "%TOOLS%\..\..\NuGet\Eagle.Mono.nuspec" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" NuSpecMode "%TOOLS%\..\..\NuGet\Eagle.Mono.nuspec"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\NuGet\Eagle.Mono.nuspec" failed.
        GOTO errors
      )
    )

    IF EXIST "%TOOLS%\..\..\NuGet\Eagle.Tools.nuspec" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" NuSpecMode "%TOOLS%\..\..\NuGet\Eagle.Tools.nuspec"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\NuGet\Eagle.Tools.nuspec" failed.
        GOTO errors
      )
    )
  ) ELSE (
    IF EXIST "%TOOLS%\..\..\NuGet\Eagle.Test.nuspec" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" NuSpecMode "%TOOLS%\..\..\NuGet\Eagle.Test.nuspec"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\NuGet\Eagle.Test.nuspec" failed.
        GOTO errors
      )
    )

    IF EXIST "%TOOLS%\..\..\NuGet\Eagle.Beta.nuspec" (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" NuSpecMode "%TOOLS%\..\..\NuGet\Eagle.Beta.nuspec"

      IF ERRORLEVEL 1 (
        ECHO Updating "%TOOLS%\..\..\NuGet\Eagle.Beta.nuspec" failed.
        GOTO errors
      )
    )
  )
)

REM ****************************************************************************
REM ************************** Query Eagle PatchLevel **************************
REM ****************************************************************************

:build_Release

IF DEFINED PATCHLEVEL GOTO skip_patchLevel2
IF NOT DEFINED PATHSRCBINDIR GOTO skip_patchLevel2

SET GET_PATCHLEVEL_CMD=EagleShell.exe -eval "puts stdout [info engine PatchLevel]"

IF DEFINED __ECHO (
  %__ECHO% %GET_PATCHLEVEL_CMD%
  SET PATCHLEVEL=1.0.X.X
) ELSE (
  FOR /F %%T IN ('%GET_PATCHLEVEL_CMD%') DO (SET PATCHLEVEL=%%T)
)

IF NOT DEFINED PATCHLEVEL (
  ECHO The PATCHLEVEL environment variable could not be set.
  GOTO errors
)

:skip_patchLevel2

%_VECHO% PatchLevel = '%PATCHLEVEL%'

REM ****************************************************************************
REM ************************* Query Garuda PatchLevel **************************
REM ****************************************************************************

IF DEFINED PACKAGE_PATCHLEVEL GOTO skip_patchLevel3
IF DEFINED NOPACKAGE GOTO skip_patchLevel3

IF NOT DEFINED GARUDA_DLL (
  ECHO.
  ECHO WARNING: The GARUDA_DLL environment variable is not set.
  ECHO.
  SET PACKAGE_PATCHLEVEL=1.0.0.0
  GOTO skip_patchLevel3
)

SET GET_PACKAGE_PATCHLEVEL_CMD=EagleShell.exe -eval "puts stdout [file version {%GARUDA_DLL%}]"

IF DEFINED __ECHO (
  %__ECHO% %GET_PACKAGE_PATCHLEVEL_CMD%
  SET PACKAGE_PATCHLEVEL=1.0.X.X
) ELSE (
  FOR /F %%T IN ('%GET_PACKAGE_PATCHLEVEL_CMD%') DO (SET PACKAGE_PATCHLEVEL=%%T)
)

IF NOT DEFINED PACKAGE_PATCHLEVEL (
  ECHO The PACKAGE_PATCHLEVEL environment variable could not be set.
  GOTO errors
)

:skip_patchLevel3

%_VECHO% PackagePatchLevel = '%PACKAGE_PATCHLEVEL%'

REM ****************************************************************************
REM ************************** Create Source Packages **************************
REM ****************************************************************************

IF NOT DEFINED NOARCHIVE (
  IF NOT DEFINED NOSOURCE (
    %__ECHO3% CALL "%TOOLS%\archive.bat"

    IF ERRORLEVEL 1 (
      ECHO Creating source archives failed.
      GOTO errors
    )
  )

  IF NOT DEFINED NOSOURCEONLY (
    SET NOTOOL=1

    %__ECHO3% CALL "%TOOLS%\archive.bat"

    IF ERRORLEVEL 1 (
      ECHO Creating source-only archives failed.
      GOTO errors
    )

    CALL :fn_UnsetVariable NOTOOL
  )
)

REM ****************************************************************************
REM *************************** Skip Release Steps? ****************************
REM ****************************************************************************

IF DEFINED BUILD_SOURCE_ONLY (
  %_AECHO% Skipping binary portion of the release phase...
  GOTO verify_Release
)

REM ****************************************************************************
REM ************************** Create Binary Packages **************************
REM ****************************************************************************

IF NOT DEFINED NORELEASE (
  IF NOT DEFINED NONETFX20 (
    REM SET SUFFIX=%NETFX20_SUFFIX%_
    CALL :fn_UnsetVariable SUFFIX
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED EXTRA (
      SET NOEXTRA=1
    )

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX20_CONFIGURATION%%NETFX20_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED EXTRA (
      CALL :fn_UnsetVariable NOEXTRA
    )

    REM CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX35 (
    SET SUFFIX=%NETFX35_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED EXTRA (
      SET NOEXTRA=1
    )

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX35_CONFIGURATION%%NETFX35_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED EXTRA (
      CALL :fn_UnsetVariable NOEXTRA
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX40 (
    SET SUFFIX=%NETFX40_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX40_CONFIGURATION%%NETFX40_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX45 (
    SET SUFFIX=%NETFX45_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX45_CONFIGURATION%%NETFX45_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX451 (
    SET SUFFIX=%NETFX451_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX451_CONFIGURATION%%NETFX451_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX452 (
    SET SUFFIX=%NETFX452_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX452_CONFIGURATION%%NETFX452_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX46 (
    SET SUFFIX=%NETFX46_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX46_CONFIGURATION%%NETFX46_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX461 (
    SET SUFFIX=%NETFX461_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX461_CONFIGURATION%%NETFX461_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX462 (
    SET SUFFIX=%NETFX462_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX462_CONFIGURATION%%NETFX462_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX47 (
    SET SUFFIX=%NETFX47_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX47_CONFIGURATION%%NETFX47_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NONETFX471 (
    SET SUFFIX=%NETFX471_SUFFIX%_
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%NETFX471_CONFIGURATION%%NETFX471_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NOBARE (
    SET SUFFIX=%BARE_SUFFIX%
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED EXTRA (
      SET NOEXTRA=1
    )

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%BARE_CONFIGURATION%%BARE_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%BARE_CONFIGURATION%%BARE_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%BARE_CONFIGURATION%%BARE_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%BARE_CONFIGURATION%%BARE_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%BARE_CONFIGURATION%%BARE_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%BARE_CONFIGURATION%%BARE_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED EXTRA (
      CALL :fn_UnsetVariable NOEXTRA
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  SET NOLIB=1

  IF NOT DEFINED NOLEAN (
    SET SUFFIX=%LEAN_SUFFIX%
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%LEAN_CONFIGURATION%%LEAN_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%LEAN_CONFIGURATION%%LEAN_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%LEAN_CONFIGURATION%%LEAN_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%LEAN_CONFIGURATION%%LEAN_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%LEAN_CONFIGURATION%%LEAN_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%LEAN_CONFIGURATION%%LEAN_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NODATABASE (
    SET SUFFIX=%DATABASE_SUFFIX%
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED EXTRA (
      SET NOEXTRA=1
    )

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%DATABASE_CONFIGURATION%%DATABASE_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED EXTRA (
      CALL :fn_UnsetVariable NOEXTRA
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  CALL :fn_UnsetVariable NOLIB

  IF NOT DEFINED NOUNIX (
    SET SUFFIX=%UNIX_SUFFIX%
    CALL :fn_ShowVariable Suffix SUFFIX

    IF NOT DEFINED EXTRA (
      SET NOEXTRA=1
    )

    IF NOT DEFINED NOBINARY (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%UNIX_CONFIGURATION%%UNIX_SUFFIX%"

      IF ERRORLEVEL 1 (
        ECHO Creating "%UNIX_CONFIGURATION%%UNIX_SUFFIX%" binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NORUNTIME (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%UNIX_CONFIGURATION%%UNIX_SUFFIX%" Runtime data\exclude_runtime.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%UNIX_CONFIGURATION%%UNIX_SUFFIX%" runtime binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED NOCORE (
      %__ECHO3% CALL "%TOOLS%\release.bat" "%UNIX_CONFIGURATION%%UNIX_SUFFIX%" Core data\exclude_core.txt

      IF ERRORLEVEL 1 (
        ECHO Creating "%UNIX_CONFIGURATION%%UNIX_SUFFIX%" core binaries archive failed.
        GOTO errors
      )
    )

    IF NOT DEFINED EXTRA (
      CALL :fn_UnsetVariable NOEXTRA
    )

    CALL :fn_UnsetVariable SUFFIX
  )

  IF NOT DEFINED NOPACKAGE (
    IF NOT DEFINED NONETFX40 (
      SET SUFFIX=%NETFX40_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX40_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX45 (
      SET SUFFIX=%NETFX45_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX45_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX451 (
      SET SUFFIX=%NETFX451_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX451_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX452 (
      SET SUFFIX=%NETFX452_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX452_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX46 (
      SET SUFFIX=%NETFX46_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX46_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX461 (
      SET SUFFIX=%NETFX461_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX461_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX462 (
      SET SUFFIX=%NETFX462_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX462_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX47 (
      SET SUFFIX=%NETFX47_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX47_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )

    IF NOT DEFINED NONETFX471 (
      SET SUFFIX=%NETFX471_SUFFIX%_
      CALL :fn_ShowVariable Suffix SUFFIX

      IF NOT DEFINED NOBINARY (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%"

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NORUNTIME (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" Runtime data\exclude_runtime.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" runtime binaries archive failed.
          GOTO errors
        )
      )

      IF NOT DEFINED NOCORE (
        %__ECHO3% CALL "%PACKAGE_TOOLS%\release.bat" "%PACKAGE_PLATFORM%" "%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" Core data\exclude_core.txt

        IF ERRORLEVEL 1 (
          ECHO Creating "%PACKAGE_PLATFORM%\%NATIVE_CONFIGURATION%%NETFX471_SUFFIX%" core binaries archive failed.
          GOTO errors
        )
      )

      CALL :fn_UnsetVariable SUFFIX
    )
  )
)

REM ****************************************************************************
REM ************************ Copy Build/Test Log Files *************************
REM ****************************************************************************

IF NOT DEFINED NOLOGS (
  IF EXIST "%TEMP%\EagleShell.exe.test.*.log" (
    %__ECHO% XCOPY "%TEMP%\EagleShell.exe.test.*.log" "%RELEASES%" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%TEMP%\EagleShell.exe.test.*.log" to "%RELEASES%".
      GOTO errors
    )
  )

  IF NOT DEFINED NONATIVE (
    IF EXIST "%TEMP%\Spilornis*Build.*" (
      %__ECHO% XCOPY "%TEMP%\Spilornis*Build.*" "%RELEASES%" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%TEMP%\Spilornis*Build.*" to "%RELEASES%".
        GOTO errors
      )
    )

    IF NOT DEFINED NOPACKAGE (
      IF EXIST "%TEMP%\Garuda*Build.*" (
        %__ECHO% XCOPY "%TEMP%\Garuda*Build.*" "%RELEASES%" %FFLAGS% %DFLAGS%

        IF ERRORLEVEL 1 (
          ECHO Failed to copy "%TEMP%\Garuda*Build.*" to "%RELEASES%".
          GOTO errors
        )
      )
    )
  )

  %__ECHO% XCOPY "%ROOT%\bin\EagleShell.exe.test.*.log" "%RELEASES%" %FFLAGS% %DFLAGS% /S

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%ROOT%\bin\EagleShell.exe.test.*.log" to "%RELEASES%".
    CALL :fn_ResetErrorLevel
  )

  IF NOT DEFINED NONATIVE (
    %__ECHO% XCOPY "%ROOT%\bin\Spilornis*Build.*" "%RELEASES%" %FFLAGS% %DFLAGS% /S

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%ROOT%\bin\Spilornis*Build.*" to "%RELEASES%".
      CALL :fn_ResetErrorLevel
    )

    IF NOT DEFINED NOPACKAGE (
      %__ECHO% XCOPY "%ROOT%\bin\Garuda*Build.*" "%RELEASES%" %FFLAGS% %DFLAGS% /S

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%ROOT%\bin\Garuda*Build.*" to "%RELEASES%".
        CALL :fn_ResetErrorLevel
      )
    )
  )
)

REM ****************************************************************************
REM ************************** Create NuGet Packages ***************************
REM ****************************************************************************

IF NOT DEFINED NONUGET (
  IF DEFINED NONUGETPACK (
    %_AECHO% Skipping creation of NuGet and SymbolSource packages...
    GOTO skip_nuGetPack
  )

  IF NOT DEFINED NONETFX20 (
    IF NOT DEFINED NONETFX40 (
      IF NOT EXIST "%NUGETBASEPATH%" (
        %__ECHO% MKDIR "%NUGETBASEPATH%"

        IF ERRORLEVEL 1 (
          ECHO Could not create directory "%NUGETBASEPATH%".
          GOTO errors
        )
      )

      IF NOT EXIST "%NUGETOUTPUTDIR%\NuGet" (
        %__ECHO% MKDIR "%NUGETOUTPUTDIR%\NuGet"

        IF ERRORLEVEL 1 (
          ECHO Could not create directory "%NUGETOUTPUTDIR%\NuGet".
          GOTO errors
        )
      )

      IF NOT EXIST "%NUGETOUTPUTDIR%\SymbolSource" (
        %__ECHO% MKDIR "%NUGETOUTPUTDIR%\SymbolSource"

        IF ERRORLEVEL 1 (
          ECHO Could not create directory "%NUGETOUTPUTDIR%\SymbolSource".
          GOTO errors
        )
      )
    )

    IF NOT EXIST "%NETFX20_NUGETBASEPATH%" (
      %__ECHO% MKDIR "%NETFX20_NUGETBASEPATH%"

      IF ERRORLEVEL 1 (
        ECHO Could not create directory "%NETFX20_NUGETBASEPATH%".
        GOTO errors
      )
    )

    IF NOT EXIST "%NETFX20_NUGETOUTPUTDIR%\NuGet" (
      %__ECHO% MKDIR "%NETFX20_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Could not create directory "%NETFX20_NUGETOUTPUTDIR%\NuGet".
        GOTO errors
      )
    )

    %__ECHO% XCOPY "%ROOT%\NuGet\build\Eagle.NuGet.targets" "%NETFX20_NUGETBASEPATH%\build\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%ROOT%\NuGet\build\Eagle.NuGet.targets" to "%NETFX20_NUGETBASEPATH%\build\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\Eagle.dll" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\Eagle.dll" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\Eagle.pdb" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\Eagle.pdb" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\EagleShell.exe" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\EagleShell.exe" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\EagleShell.pdb" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\EagleShell.pdb" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\EagleShell32.exe" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\EagleShell32.exe" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\EagleCmdlets.dll" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\EagleCmdlets.dll" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\EagleCmdlets.pdb" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\EagleCmdlets.pdb" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\EagleCmdlets.ps1" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\EagleCmdlets.ps1" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\EagleTasks.dll" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\EagleTasks.dll" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\EagleTasks.pdb" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\EagleTasks.pdb" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR20%\Eagle.tasks" "%NETFX20_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR20%\Eagle.tasks" to "%NETFX20_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    IF NOT DEFINED NONATIVE (
      %__ECHO% XCOPY "%SRCBINDIR20%\x86\Spilornis.dll" "%NETFX20_NUGETBASEPATH%\bin\x86\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SRCBINDIR20%\x86\Spilornis.dll" to "%NETFX20_NUGETBASEPATH%\bin\x86\".
        GOTO errors
      )

      %__ECHO% XCOPY "%SRCBINDIR20%\x86\Spilornis.pdb" "%NETFX20_NUGETBASEPATH%\bin\x86\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SRCBINDIR20%\x86\Spilornis.pdb" to "%NETFX20_NUGETBASEPATH%\bin\x86\".
        GOTO errors
      )

      %__ECHO% XCOPY "%SRCBINDIR20%\x64\Spilornis.dll" "%NETFX20_NUGETBASEPATH%\bin\x64\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SRCBINDIR20%\x64\Spilornis.dll" to "%NETFX20_NUGETBASEPATH%\bin\x64\".
        GOTO errors
      )

      %__ECHO% XCOPY "%SRCBINDIR20%\x64\Spilornis.pdb" "%NETFX20_NUGETBASEPATH%\bin\x64\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SRCBINDIR20%\x64\Spilornis.pdb" to "%NETFX20_NUGETBASEPATH%\bin\x64\".
        GOTO errors
      )
    )

    REM
    REM HACK: We must change the current directory because the EXCLUDE argument
    REM       to the XCOPY command is very picky.  The first problem is that it
    REM       apparently cannot include surrounding double quotes, even though
    REM       this fact is not documented anywhere, thus making it impossible to
    REM       use any path that contains spaces.  The second problem is that all
    REM       the files we want to copy would match the exclusion rule "\Tools\"
    REM       because we are running from the "Tools" directory and we would
    REM       have to use our directory as the basis for the library source
    REM       directory ^(unless we could find a reasonable way of resolving it
    REM       to an absolute path beforehand^).
    REM
    %__ECHO2% PUSHD "%ROOT%"

    IF ERRORLEVEL 1 (
      ECHO Could not change directory to "%ROOT%".
      GOTO errors
    )

    REM
    REM HACK: Must use relative path for source files and EXCLUDE file here, see
    REM       above comment.
    REM
    %__ECHO% XCOPY "Build\*.cs" "%NETFX20_NUGETBASEPATH%\src\Build\" %FFLAGS% %DFLAGS% /S /EXCLUDE:data\exclude_nuget.txt

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "Build\*.cs" to "%NETFX20_NUGETBASEPATH%\src\Build\".
      GOTO errors
    )

    %__ECHO% XCOPY "Library\*.cs" "%NETFX20_NUGETBASEPATH%\src\Library\" %FFLAGS% %DFLAGS% /S /EXCLUDE:data\exclude_nuget.txt

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "Library\*.cs" to "%NETFX20_NUGETBASEPATH%\src\Library\".
      GOTO errors
    )

    %__ECHO% XCOPY "Management\*.cs" "%NETFX20_NUGETBASEPATH%\src\Management\" %FFLAGS% %DFLAGS% /S /EXCLUDE:data\exclude_nuget.txt

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "Management\*.cs" to "%NETFX20_NUGETBASEPATH%\src\Management\".
      GOTO errors
    )

    %__ECHO% XCOPY "Shell\*.cs" "%NETFX20_NUGETBASEPATH%\src\Shell\" %FFLAGS% %DFLAGS% /S /EXCLUDE:data\exclude_nuget.txt

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "Shell\*.cs" to "%NETFX20_NUGETBASEPATH%\src\Shell\".
      GOTO errors
    )

    %__ECHO% XCOPY "Native\Utility\*.c" "%NETFX20_NUGETBASEPATH%\src\Native\Utility\" %FFLAGS% %DFLAGS% /S /EXCLUDE:data\exclude_nuget.txt

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "Native\Utility\*.c" to "%NETFX20_NUGETBASEPATH%\src\Native\Utility\".
      GOTO errors
    )

    %__ECHO% XCOPY "Native\Utility\*.h" "%NETFX20_NUGETBASEPATH%\src\Native\Utility\" %FFLAGS% %DFLAGS% /S /EXCLUDE:data\exclude_nuget.txt

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "Native\Utility\*.h" to "%NETFX20_NUGETBASEPATH%\src\Native\Utility\".
      GOTO errors
    )

    %__ECHO2% POPD

    IF ERRORLEVEL 1 (
      ECHO Could not restore directory.
      GOTO errors
    )

    IF DEFINED STABLE (
      REM
      REM HACK: The primary NuGet packages now require both the .NET Framework
      REM       2.0 and .NET Framework 4.0 builds to be enabled.  This may be
      REM       changed in the future.
      REM
      IF NOT DEFINED NONETFX40 (
        %__ECHO% XCOPY "%ROOT%\NuGet\build\Eagle.NuGet.targets" "%SRCBINDIR20%\..\build\" %FFLAGS% %DFLAGS%

        IF ERRORLEVEL 1 (
          ECHO Failed to copy "%ROOT%\NuGet\build\Eagle.NuGet.targets" to "%SRCBINDIR20%\..\build\".
          GOTO errors
        )

        %__ECHO% XCOPY "%ROOT%\NuGet\build\Eagle.NuGet.targets" "%SRCBINDIR40%\..\build\" %FFLAGS% %DFLAGS%

        IF ERRORLEVEL 1 (
          ECHO Failed to copy "%ROOT%\NuGet\build\Eagle.NuGet.targets" to "%SRCBINDIR40%\..\build\".
          GOTO errors
        )

        %_CECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.nuspec" /OutputDirectory "%NUGETOUTPUTDIR%\NuGet"
        %__ECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.nuspec" /OutputDirectory "%NUGETOUTPUTDIR%\NuGet"

        IF ERRORLEVEL 1 (
          ECHO Failed to create NuGet package from "%ROOT%\NuGet\Eagle.nuspec".
          GOTO errors
        )
      )

      %_CECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.CLRv2.nuspec" /BasePath "%NETFX20_NUGETBASEPATH%" /OutputDirectory "%NETFX20_NUGETOUTPUTDIR%\NuGet"
      %__ECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.CLRv2.nuspec" /BasePath "%NETFX20_NUGETBASEPATH%" /OutputDirectory "%NETFX20_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Failed to create NuGet package from "%ROOT%\NuGet\Eagle.CLRv2.nuspec".
        GOTO errors
      )

      REM
      REM HACK: The primary NuGet packages now require both the .NET Framework
      REM       2.0 and .NET Framework 4.0 builds to be enabled.  This may be
      REM       changed in the future.
      REM
      IF NOT DEFINED NONETFX40 (
        %_CECHO% "%JUNCTION_EXE%" "%NUGETBASEPATH%\src" "%ROOT%"
        %__ECHO% "%JUNCTION_EXE%" "%NUGETBASEPATH%\src" "%ROOT%"

        IF ERRORLEVEL 1 (
          ECHO Failed to create junction "%NUGETBASEPATH%\src" pointing to "%ROOT%".
          GOTO errors
        )

        %_CECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.src.nuspec" /BasePath "%NUGETBASEPATH%" /OutputDirectory "%NUGETOUTPUTDIR%\SymbolSource"
        %__ECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.src.nuspec" /BasePath "%NUGETBASEPATH%" /OutputDirectory "%NUGETOUTPUTDIR%\SymbolSource"

        IF ERRORLEVEL 1 (
          ECHO Failed to create SymbolSource package from "%ROOT%\NuGet\Eagle.src.nuspec".
          GOTO errors
        )

        %_CECHO% "%JUNCTION_EXE%" -d "%NUGETBASEPATH%\src"
        %__ECHO% "%JUNCTION_EXE%" -d "%NUGETBASEPATH%\src"

        IF ERRORLEVEL 1 (
          ECHO Failed to delete junction "%NUGETBASEPATH%\src".
          GOTO errors
        )
      )

      %_CECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.Tools.nuspec" /BasePath "%NETFX20_NUGETBASEPATH%" /OutputDirectory "%NETFX20_NUGETOUTPUTDIR%\NuGet"
      %__ECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.Tools.nuspec" /BasePath "%NETFX20_NUGETBASEPATH%" /OutputDirectory "%NETFX20_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Failed to create NuGet package from "%ROOT%\NuGet\Eagle.Tools.nuspec".
        GOTO errors
      )
    ) ELSE (
      REM
      REM HACK: This assumes that the "Eagle.Test" package should contain the
      REM       binaries for the .NET Framework 2.0.  In the future, this may
      REM       change.
      REM
      %_CECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.Test.nuspec" /BasePath "%NETFX20_NUGETBASEPATH%" /OutputDirectory "%NETFX20_NUGETOUTPUTDIR%\NuGet"
      %__ECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.Test.nuspec" /BasePath "%NETFX20_NUGETBASEPATH%" /OutputDirectory "%NETFX20_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Failed to create NuGet package from "%ROOT%\NuGet\Eagle.Test.nuspec".
        GOTO errors
      )
    )
  )

  REM
  REM NOTE: For now, there is only a "core" CLRv4 NuGet package, so there
  REM       is no need to copy the shell, library source code, etc.
  REM
  IF NOT DEFINED NONETFX40 (
    IF NOT EXIST "%NETFX40_NUGETBASEPATH%" (
      %__ECHO% MKDIR "%NETFX40_NUGETBASEPATH%"

      IF ERRORLEVEL 1 (
        ECHO Could not create directory "%NETFX40_NUGETBASEPATH%".
        GOTO errors
      )
    )

    IF NOT EXIST "%NETFX40_NUGETOUTPUTDIR%\NuGet" (
      %__ECHO% MKDIR "%NETFX40_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Could not create directory "%NETFX40_NUGETOUTPUTDIR%\NuGet".
        GOTO errors
      )
    )

    %__ECHO% XCOPY "%ROOT%\NuGet\build\Eagle.NuGet.targets" "%NETFX40_NUGETBASEPATH%\build\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%ROOT%\NuGet\build\Eagle.NuGet.targets" to "%NETFX40_NUGETBASEPATH%\build\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR40%\Eagle.dll" "%NETFX40_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR40%\Eagle.dll" to "%NETFX40_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    %__ECHO% XCOPY "%SRCBINDIR40%\Eagle.pdb" "%NETFX40_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SRCBINDIR40%\Eagle.pdb" to "%NETFX40_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    IF NOT DEFINED NONATIVE (
      %__ECHO% XCOPY "%SRCBINDIR40%\x86\Spilornis.dll" "%NETFX40_NUGETBASEPATH%\bin\x86\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SRCBINDIR40%\x86\Spilornis.dll" to "%NETFX40_NUGETBASEPATH%\bin\x86\".
        GOTO errors
      )

      %__ECHO% XCOPY "%SRCBINDIR40%\x86\Spilornis.pdb" "%NETFX40_NUGETBASEPATH%\bin\x86\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SRCBINDIR40%\x86\Spilornis.pdb" to "%NETFX40_NUGETBASEPATH%\bin\x86\".
        GOTO errors
      )

      %__ECHO% XCOPY "%SRCBINDIR40%\x64\Spilornis.dll" "%NETFX40_NUGETBASEPATH%\bin\x64\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SRCBINDIR40%\x64\Spilornis.dll" to "%NETFX40_NUGETBASEPATH%\bin\x64\".
        GOTO errors
      )

      %__ECHO% XCOPY "%SRCBINDIR40%\x64\Spilornis.pdb" "%NETFX40_NUGETBASEPATH%\bin\x64\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SRCBINDIR40%\x64\Spilornis.pdb" to "%NETFX40_NUGETBASEPATH%\bin\x64\".
        GOTO errors
      )
    )

    IF DEFINED STABLE (
      %_CECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.CLRv4.nuspec" /BasePath "%NETFX40_NUGETBASEPATH%" /OutputDirectory "%NETFX40_NUGETOUTPUTDIR%\NuGet"
      %__ECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.CLRv4.nuspec" /BasePath "%NETFX40_NUGETBASEPATH%" /OutputDirectory "%NETFX40_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Failed to create NuGet package from "%ROOT%\NuGet\Eagle.CLRv4.nuspec".
        GOTO errors
      )
    ) ELSE (
      REM
      REM HACK: This assumes that the "Eagle.Beta" package should contain the
      REM       binaries for the .NET Framework 4.0.  In the future, this may
      REM       change.
      REM
      %_CECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.Beta.nuspec" /BasePath "%NETFX40_NUGETBASEPATH%" /OutputDirectory "%NETFX40_NUGETOUTPUTDIR%\NuGet"
      %__ECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.Beta.nuspec" /BasePath "%NETFX40_NUGETBASEPATH%" /OutputDirectory "%NETFX40_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Failed to create NuGet package from "%ROOT%\NuGet\Eagle.Beta.nuspec".
        GOTO errors
      )
    )
  )

  IF NOT DEFINED NOUNIX (
    IF NOT EXIST "%UNIX_NUGETBASEPATH%" (
      %__ECHO% MKDIR "%UNIX_NUGETBASEPATH%"

      IF ERRORLEVEL 1 (
        ECHO Could not create directory "%UNIX_NUGETBASEPATH%".
        GOTO errors
      )
    )

    IF NOT EXIST "%UNIX_NUGETOUTPUTDIR%\NuGet" (
      %__ECHO% MKDIR "%UNIX_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Could not create directory "%UNIX_NUGETOUTPUTDIR%\NuGet".
        GOTO errors
      )
    )

    %__ECHO% XCOPY "%UNIXSRCBINDIR%\Eagle.dll" "%UNIX_NUGETBASEPATH%\bin\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%UNIXSRCBINDIR%\Eagle.dll" to "%UNIX_NUGETBASEPATH%\bin\".
      GOTO errors
    )

    IF DEFINED STABLE (
      %_CECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.Mono.nuspec" /BasePath "%UNIX_NUGETBASEPATH%" /OutputDirectory "%UNIX_NUGETOUTPUTDIR%\NuGet"
      %__ECHO% "%NUGET_EXE%" pack "%ROOT%\NuGet\Eagle.Mono.nuspec" /BasePath "%UNIX_NUGETBASEPATH%" /OutputDirectory "%UNIX_NUGETOUTPUTDIR%\NuGet"

      IF ERRORLEVEL 1 (
        ECHO Failed to create NuGet package from "%ROOT%\NuGet\Eagle.Mono.nuspec".
        GOTO errors
      )
    )
  )
)

REM ****************************************************************************
REM ********************** Verify Release Archives Files ***********************
REM ****************************************************************************

:verify_Release

SET WITHHASHES=true

IF DEFINED NOVERIFYHASH (
  SET WITHHASHES=false
)

IF NOT DEFINED NOVERIFY (
  %__ECHO% EagleShell.exe -file "%TOOLS%\verify.eagle" "%RELEASES%" %WITHHASHES% false

  IF ERRORLEVEL 1 (
    ECHO Verifying release archive files in "%RELEASES%" failed.
    GOTO errors
  )
)

REM ****************************************************************************
REM **************************** Hash All Packages *****************************
REM ****************************************************************************

IF NOT DEFINED NOHASH (
  IF EXIST "%TOOLS%\fossil.exe" (
    IF EXIST "%RELEASES%\hash.txt" (
      %__ECHO% DEL "%RELEASES%\hash.txt"

      IF ERRORLEVEL 1 (
        ECHO Could not delete file "%RELEASES%\hash.txt".
        GOTO errors
      )
    )

    %__ECHO% ECHO Hashed "%DATE%" at "%TIME%" by "%USERDOMAIN%\%USERNAME%" %APPEND% "%RELEASES%\hash.txt"
    %__ECHO% ECHO. %APPEND% "%RELEASES%\hash.txt"

    FOR %%B IN (%HASHBASES%) DO (
      FOR %%E IN (%HASHEXTS%) DO (
        FOR /F "delims=" %%F IN ('DIR /B /S /ON "%RELEASES%\%%B*%PATCHLEVEL%.%%E" 2^> NUL') DO (
          %_AECHO% Hashing file "%%F"...
          %_CECHO% "%TOOLS%\fossil.exe" sha1sum "%%F" %_CAPPEND% "%RELEASES%\hash.txt"
          %__ECHO% "%TOOLS%\fossil.exe" sha1sum "%%F" %APPEND% "%RELEASES%\hash.txt"

          IF ERRORLEVEL 1 (
            ECHO Hashing file "%%F" failed.
            GOTO errors
          )
        )

        IF NOT DEFINED NOPACKAGE (
          IF DEFINED PACKAGE_PATCHLEVEL (
            IF /I NOT "%PACKAGE_PATCHLEVEL%" == "%PATCHLEVEL%" (
              FOR /F "delims=" %%F IN ('DIR /B /S /ON "%RELEASES%\%%B*%PACKAGE_PATCHLEVEL%.%%E" 2^> NUL') DO (
                %_AECHO% Hashing file "%%F"...
                %_CECHO% "%TOOLS%\fossil.exe" sha1sum "%%F" %_CAPPEND% "%RELEASES%\hash.txt"
                %__ECHO% "%TOOLS%\fossil.exe" sha1sum "%%F" %APPEND% "%RELEASES%\hash.txt"

                IF ERRORLEVEL 1 (
                  ECHO Hashing file "%%F" failed.
                  GOTO errors
                )
              )
            )
          )
        )
      )
    )
  ) ELSE (
    %_AECHO% Hashing tool "%TOOLS%\fossil.exe" not found, skipping...
  )
)

REM ****************************************************************************
REM *************************** Push NuGet Packages ****************************
REM ****************************************************************************

IF NOT DEFINED NONUGET (
  IF DEFINED NONUGETPUSH (
    %_AECHO% Skipping push of NuGet and SymbolSource packages...
    GOTO skip_nuGetPush
  )

  REM
  REM NOTE: Emit the necessary commands to the console to push the created
  REM       NuGet and SymbolSource packages; however, do not actually push the
  REM       created packages.  Eventually, we may decide to migrate to
  REM       automatically pushing the created packages; however, this will
  REM       have to be done with great care because we cannot really undo it.
  REM
  IF NOT DEFINED NUGET_API_KEY (
    ECHO The NUGET_API_KEY environment variable must be set first.
    GOTO usage
  )

  IF NOT DEFINED SYMBOLSOURCE_API_KEY (
    ECHO The SYMBOLSOURCE_API_KEY environment variable must be set first.
    GOTO usage
  )

  IF NOT DEFINED NONETFX20 (
    IF EXIST "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.%PATCHLEVEL%.nupkg" (
      %_CECHO% "%NUGET_EXE%" push "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"
      ECHO "%NUGET_EXE%" push "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"

      IF ERRORLEVEL 1 (
        ECHO Failed to push NuGet package "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.%PATCHLEVEL%.nupkg".
        GOTO errors
      )
    )

    IF EXIST "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.Tools.%PATCHLEVEL%.nupkg" (
      %_CECHO% "%NUGET_EXE%" push "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.Tools.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"
      ECHO "%NUGET_EXE%" push "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.Tools.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"

      IF ERRORLEVEL 1 (
        ECHO Failed to push NuGet package "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.Tools.%PATCHLEVEL%.nupkg".
        GOTO errors
      )
    )

    IF EXIST "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.Test.%PATCHLEVEL%.nupkg" (
      %_CECHO% "%NUGET_EXE%" push "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.Test.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"
      ECHO "%NUGET_EXE%" push "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.Test.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"

      IF ERRORLEVEL 1 (
        ECHO Failed to push NuGet package "%NETFX20_NUGETOUTPUTDIR%\NuGet\Eagle.Test.%PATCHLEVEL%.nupkg".
        GOTO errors
      )
    )

    IF EXIST "%NETFX20_NUGETOUTPUTDIR%\SymbolSource\Eagle.%PATCHLEVEL%.nupkg" (
      %_CECHO% "%NUGET_EXE%" push "%NETFX20_NUGETOUTPUTDIR%\SymbolSource\Eagle.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -source "%SYMBOLSOURCE_URL%"
      ECHO "%NUGET_EXE%" push "%NETFX20_NUGETOUTPUTDIR%\SymbolSource\Eagle.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -source "%SYMBOLSOURCE_URL%"

      IF ERRORLEVEL 1 (
        ECHO Failed to push SymbolSource package "%NETFX20_NUGETOUTPUTDIR%\SymbolSource\Eagle.%PATCHLEVEL%.nupkg".
        GOTO errors
      )
    )
  )

  IF NOT DEFINED NONETFX40 (
    IF EXIST "%NETFX40_NUGETOUTPUTDIR%\NuGet\Eagle.CLRv4.%PATCHLEVEL%.nupkg" (
      %_CECHO% "%NUGET_EXE%" push "%NETFX40_NUGETOUTPUTDIR%\NuGet\Eagle.CLRv4.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"
      ECHO "%NUGET_EXE%" push "%NETFX40_NUGETOUTPUTDIR%\NuGet\Eagle.CLRv4.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"

      IF ERRORLEVEL 1 (
        ECHO Failed to push NuGet package "%NETFX40_NUGETOUTPUTDIR%\NuGet\Eagle.CLRv4.%PATCHLEVEL%.nupkg".
        GOTO errors
      )
    )

    IF EXIST "%NETFX40_NUGETOUTPUTDIR%\NuGet\Eagle.Beta.%PATCHLEVEL%.nupkg" (
      %_CECHO% "%NUGET_EXE%" push "%NETFX40_NUGETOUTPUTDIR%\NuGet\Eagle.Beta.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"
      ECHO "%NUGET_EXE%" push "%NETFX40_NUGETOUTPUTDIR%\NuGet\Eagle.Beta.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"

      IF ERRORLEVEL 1 (
        ECHO Failed to push NuGet package "%NETFX40_NUGETOUTPUTDIR%\NuGet\Eagle.Beta.%PATCHLEVEL%.nupkg".
        GOTO errors
      )
    )
  )

  IF NOT DEFINED NOUNIX (
    IF EXIST "%UNIX_NUGETOUTPUTDIR%\NuGet\Eagle.Mono.%PATCHLEVEL%.nupkg" (
      %_CECHO% "%NUGET_EXE%" push "%UNIX_NUGETOUTPUTDIR%\NuGet\Eagle.Mono.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"
      ECHO "%NUGET_EXE%" push "%UNIX_NUGETOUTPUTDIR%\NuGet\Eagle.Mono.%PATCHLEVEL%.nupkg" "%NUGET_API_KEY%" -Source "%NUGET_URL%"

      IF ERRORLEVEL 1 (
        ECHO Failed to push NuGet package "%UNIX_NUGETOUTPUTDIR%\NuGet\Eagle.Mono.%PATCHLEVEL%.nupkg".
        GOTO errors
      )
    )
  )
)

:skip_nuGetPush

GOTO no_errors

REM ****************************************************************************
REM ****************** Epilogue / Functions / Return Handling ******************
REM ****************************************************************************

:fn_VerifyFileAlongPath
  SET VALUE=%1
  IF DEFINED VALUE (
    FOR %%T IN (%VALUE%) DO (
      SET %%T_PATH=%%~dp$PATH:T
    )
    IF DEFINED %VALUE%_PATH (
      %_AECHO% The file "%VALUE%" does exist along the PATH.
    ) ELSE (
      ECHO The file "%VALUE%" does not exist along the PATH.
      CALL :fn_SetErrorLevel
    )
  )
  GOTO :EOF

:fn_UnquoteVariable
  IF NOT DEFINED %1 GOTO :EOF
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  SET VALUE=%VALUE:"=%
  REM "
  ENDLOCAL && SET %1=%VALUE%
  GOTO :EOF

:fn_PrependToPath
  IF NOT DEFINED %1 GOTO :EOF
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  SET VALUE=%VALUE:"=%
  REM "
  ENDLOCAL && SET PATH=%VALUE%;%PATH%
  GOTO :EOF

:fn_AppendToPath
  IF NOT DEFINED %1 GOTO :EOF
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  SET VALUE=%VALUE:"=%
  REM "
  ENDLOCAL && SET PATH=%PATH%;%VALUE%
  GOTO :EOF

:fn_ShowVariable
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%2%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    IF NOT "%%V" == "" (
      IF NOT "%%V" == "%%%2%%" (
        %_VECHO% %1 = '%%V'
      )
    )
  )
  ENDLOCAL
  GOTO :EOF

:fn_AppendVariable
  SET __ECHO_CMD=ECHO %%%1%%
  IF DEFINED %1 (
    FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
      SET %1=%%V%~2
    )
  ) ELSE (
    SET %1=%~2
  )
  SET __ECHO_CMD=
  CALL :fn_ResetErrorLevel
  GOTO :EOF

:fn_UnsetVariable
  SETLOCAL
  SET VALUE=%1
  IF DEFINED VALUE (
    SET VALUE=
    ENDLOCAL
    SET %VALUE%=
  ) ELSE (
    ENDLOCAL
  )
  CALL :fn_ResetErrorLevel
  GOTO :EOF

:fn_writePatchLevel
  REM
  REM NOTE: All files named "PatchLevel.cs" must be updated with the correct
  REM       build number prior to building any non-default ^(NetFX20^) build
  REM       configuration.  If this is not done, building of the non-default
  REM       build configurations may fail because various projects may try
  REM       to include an AssemblyVersion attribute with a null value, due
  REM       to the EaglePatchLevel build property being true for those build
  REM       configurations.
  REM
  IF NOT DEFINED PATCHLEVELFILES (
    ECHO The PATCHLEVELFILES environment variable must be set.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  IF NOT DEFINED ROOT (
    ECHO The ROOT environment variable must be set.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  IF NOT DEFINED TOOLS (
    ECHO The TOOLS environment variable must be set.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  FOR %%T IN (%PATCHLEVELFILES%) DO (
    REM
    REM NOTE: The TAGPATTERN environment variable may not be set and that
    REM       is fine.
    REM
    FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" PatchLevelMode "%%F"

      IF ERRORLEVEL 1 (
        ECHO Updating "%%F" failed.
        CALL :fn_SetErrorLevel
        GOTO :EOF
      )
    )
  )
  GOTO :EOF

:fn_writeDateTime
  IF NOT DEFINED PATCHLEVELFILES (
    ECHO The PATCHLEVELFILES environment variable must be set.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  IF NOT DEFINED ROOT (
    ECHO The ROOT environment variable must be set.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  IF NOT DEFINED TOOLS (
    ECHO The TOOLS environment variable must be set.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  FOR %%T IN (%PATCHLEVELFILES%) DO (
    REM
    REM NOTE: The TAGPATTERN environment variable may not be set and that
    REM       is fine.
    REM
    FOR /F "delims=" %%F IN ('DIR /B /S "%ROOT%%TAGPATTERN%\%%T" 2^> NUL') DO (
      %__ECHO% EagleShell.exe -file "%TOOLS%\versionTag.eagle" AssemblyDateTimeMode "%%F"

      IF ERRORLEVEL 1 (
        ECHO Updating "%%F" failed.
        CALL :fn_SetErrorLevel
        GOTO :EOF
      )
    )
  )
  GOTO :EOF

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [configuration] [...]
  ECHO.
  ECHO The "junction.exe" tool is required to be present along the PATH or in the
  ECHO directory specified by the EagleJunctionDir environment variable.  It can be
  ECHO downloaded for free from:
  ECHO.
  ECHO                          http://www.sysinternals.com/
  ECHO.
  ECHO The "sn.exe" tool is required to be present along the PATH or in the directory
  ECHO specified by the EagleSnDir environment variable.  It is distributed with the
  ECHO .NET Framework SDK.
  ECHO.
  ECHO The "SigCheck.exe" tool is required to be present along the PATH or in the
  ECHO directory specified by the EagleSigCheckDir environment variable.  It can be
  ECHO downloaded for free from:
  ECHO.
  ECHO                          http://www.sysinternals.com/
  ECHO.
  ECHO The "NuGet4.exe" tool is required to be present along the PATH or in the
  ECHO directory specified by the EagleNuGetDir environment variable.  It can be
  ECHO downloaded for free from:
  ECHO.
  ECHO                          https://www.nuget.org/
  ECHO.
  ECHO The NUGET_API_KEY environment variable must be set to the NuGet API key.
  ECHO.
  ECHO The SYMBOLSOURCE_API_KEY environment variable must be set to the SymbolSource
  ECHO API key.
  ECHO.
  ECHO The latest Eagle binaries ^(including the "EagleShell.exe" tool^) are required to
  ECHO exist in the "%%LKG%%\Eagle\bin" directory or somewhere along your PATH.  They
  ECHO can be downloaded for free from:
  ECHO.
  ECHO                          https://eagle.to/
  ECHO.
  ECHO Several of the tasks performed by this tool require [elevated] administrator
  ECHO privileges.  Therefore, this tool will refuse to run without them.
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Flight failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Flight success, no errors were encountered.
  GOTO end_of_file

:end_of_file
ECHO FLIGHT STOPPED ON %DATE% AT %TIME% BY %USERDOMAIN%\%USERNAME%
%__ECHO% EXIT /B %ERRORLEVEL%
