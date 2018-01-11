/*
 * SpilornisInt.h -- Eagle Native Utility Library (Spilornis)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _SPILORNIS_INT_H_
#define _SPILORNIS_INT_H_

/*
 * NOTE: The LIBRARY_TRACE macro is used to report important diagnostics when
 *       other means are not available.  Currently, this macro is enabled by
 *       default; however, it may be overridden via the compiler command line.
 */

#ifndef LIBRARY_TRACE
#  define LIBRARY_TRACE(x)			EagleTracePrintf x
#endif

/*
 * NOTE: The LIBRARY_DEBUG macro is used to report important diagnostics when
 *       compiled for debugging.  When not compiled for debugging, it simply
 *       does nothing.
 */

#ifndef LIBRARY_DEBUG
#  ifndef NDEBUG
#    define LIBRARY_DEBUG(x)			EagleTracePrintf x
#  else
#    define LIBRARY_DEBUG(x)
#  endif
#endif

/*****************************************************************************/

#define STRINGIFY(x)				STRINGIFY1(x)
#define STRINGIFY1(x)				#x

#define UNICODE_TEXT(x)				UNICODE_TEXT1(x)
#define UNICODE_TEXT1(x)			L##x

/*****************************************************************************/

#define LIBRARY_UNICODE_NAME			UNICODE_TEXT(LIBRARY_NAME)
#define LIBRARY_UNICODE_PATCH_LEVEL		UNICODE_TEXT(STRINGIFY(LIBRARY_PATCH_LEVEL))
#define LIBRARY_UNICODE_SOURCE_ID		UNICODE_TEXT(SOURCE_ID)
#define LIBRARY_UNICODE_SOURCE_TIMESTAMP	UNICODE_TEXT(SOURCE_TIMESTAMP)

/*****************************************************************************/

#ifndef NDEBUG
#  define LIBRARY_FREED_MEMORY			(0xEA)
#endif

#define LIBRARY_RESULT_SIZE			(192)
#define LIBRARY_VERSION_SIZE			(384)
#define LIBRARY_LOCAL_FLAGS			(20)
#define LIBRARY_TRACE_BUFFER_SIZE		((SIZE_T)(4096-sizeof(DWORD)))

/*****************************************************************************/

#ifndef FALSE
#  define FALSE					(0)
#endif

#ifndef TRUE
#  define TRUE					(1)
#endif

/*****************************************************************************/

#ifndef _CONST_DEFINED
#define _CONST_DEFINED
#define CONST const
#endif

#ifndef _CHAR_DEFINED
#define _CHAR_DEFINED
typedef char CHAR;
#endif

#ifndef _LPSTR_DEFINED
#define _LPSTR_DEFINED
typedef CHAR *LPSTR;
#endif

#ifndef _LPCSTR_DEFINED
#define _LPCSTR_DEFINED
typedef CONST CHAR *LPCSTR;
#endif

#ifndef _BYTE_DEFINED
#define _BYTE_DEFINED
typedef unsigned char BYTE;
#endif

#ifndef _LPBYTE_DEFINED
#define _LPBYTE_DEFINED
typedef BYTE *LPBYTE;
#endif

#ifndef _BOOL_DEFINED
#define _BOOL_DEFINED
typedef int BOOL;
#endif

#ifndef _LPBOOL_DEFINED
#define _LPBOOL_DEFINED
typedef BOOL *LPBOOL;
#endif

#ifndef _INT_DEFINED
#define _INT_DEFINED
typedef int INT;
#endif

#ifndef _UINT_DEFINED
#define _UINT_DEFINED
typedef unsigned int UINT;
#endif

#ifndef _UCSCHAR_DEFINED
#define _UCSCHAR_DEFINED
typedef unsigned long UCSCHAR;
#endif

#ifndef _LPUCSCHAR_DEFINED
#define _LPUCSCHAR_DEFINED
typedef UCSCHAR *LPUCSCHAR;
#endif

#ifndef _BSTR_DEFINED
#define _BSTR_DEFINED
typedef wchar_t *BSTR;
#endif

#ifndef _FLAGS_DEFINED
#define _FLAGS_DEFINED
typedef int FLAGS;
#endif

#ifndef _DWORD_DEFINED
#define _DWORD_DEFINED
typedef unsigned long DWORD;
#endif

#endif /* _SPILORNIS_INT_H_ */
