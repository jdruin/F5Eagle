###############################################################################
#
# common.tcl --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Eagle Common Tools Package
#
# Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
#
# See the file "license.terms" for information on usage and redistribution of
# this file, and for a DISCLAIMER OF ALL WARRANTIES.
#
# RCS: @(#) $Id: $
#
###############################################################################

#
# NOTE: This script file uses features that are only present in Tcl 8.4 or
#       higher (e.g. the "eq" operator for [expr], etc).
#
if {![package vsatisfies [package provide Tcl] 8.4]} then {
  error "need Tcl 8.4 or higher"
}

#
# NOTE: This script file uses features that are not available or not needed
#       in Eagle (e.g. the "http" and "tls" packages, etc).
#
if {[catch {package present Eagle}] == 0} then {
  error "need native Tcl"
}

###############################################################################

namespace eval ::Eagle::Tools::Common {
  #
  # NOTE: *HACK* Skip defining this procedure if it is already defined in the
  #       global namespace.
  #
  if {[llength [info commands ::appendArgs]] == 0} then {
    #
    # NOTE: This procedure was stolen from the "auxiliary.eagle" script.
    #       This procedure accepts an any number of arguments.  The arguments
    #       are appended into one big string, verbatim.  The resulting string
    #       is returned.  Normally, this procedure is used to avoid undesired
    #       string interpolation operations.
    #
    proc appendArgs { args } {
      eval append result $args
    }
  }

  #
  # NOTE: *HACK* Skip defining this procedure if it is already defined in the
  #       global namespace.
  #
  if {[llength [info commands ::makeBinaryChannel]] == 0} then {
    #
    # NOTE: This procedure was stolen from the "file1.eagle" script.  This
    #       procedure reconfigures the specified channel to full binary mode.
    #
    proc makeBinaryChannel { channel } {
      fconfigure $channel -encoding binary -translation binary; # BINARY DATA
    }
  }

  #
  # NOTE: *HACK* Skip defining this procedure if it is already defined in the
  #       global namespace.
  #
  if {[llength [info commands ::writeFile]] == 0} then {
    #
    # NOTE: This procedure was stolen from the "file1.eagle" script.  This
    #       procedure writes all data to the specified binary file and returns
    #       an empty string.  Previous data contained in the file, if any, is
    #       lost.
    #
    proc writeFile { fileName data } {
      set channel [open $fileName {WRONLY CREAT TRUNC}]
      makeBinaryChannel $channel
      puts -nonewline $channel $data
      close $channel
      return ""
    }
  }

  #
  # NOTE: This procedure was stolen from the "getEagle.tcl" script.  This
  #       procedure sets up the default values for all HTTP configuration
  #       parameters used by this package.  If the force argument is
  #       non-zero, any existing values will be overwritten and set back
  #       to their default values.
  #
  proc setupCommonVariables { force } {
    #
    # NOTE: Should the HTTP request processor attempt to force the use of
    #       HTTPS for URIs that were originally HTTP?  This setting is only
    #       applicable to native Tcl.
    #
    variable forceSecureUri; # DEFAULT: true

    if {$force || ![info exists forceSecureUri]} then {
      set forceSecureUri true
    }

    #
    # NOTE: Is this HTTP request processor allowed to use plain HTTP if/when
    #       the "tls" package is not available?  This should only be changed
    #       if the "tls" package cannot be easily installed for use with the
    #       native Tcl interpreter in use.  It should be noted here that the
    #       official package repository server reserves the right to refuse
    #       plain HTTP connections, which means that changing this setting
    #       may be totally pointless.
    #
    variable allowInsecureUri; # DEFAULT: false

    if {$force || ![info exists allowInsecureUri]} then {
      set allowInsecureUri false
    }

    #
    # NOTE: Emit diagnostic messages when the [::http::geturl] procedure is
    #       about to be called?
    #
    variable verboseGetUrl; # DEFAULT: false

    if {$force || ![info exists verboseGetUrl]} then {
      set verboseGetUrl false
    }

    #
    # NOTE: Is this HTTP request processor allowed to use plain HTTP if/when
    #       the server responds with an HTTP redirect location to an original
    #       URI that was HTTPS?  Otherwise, a script error will result.
    #
    variable allowInsecureRedirect; # DEFAULT: false

    if {![info exists allowInsecureRedirect]} then {
      set allowInsecureRedirect false
    }
  }

  #
  # NOTE: This procedure was stolen from the "getEagle.tcl" script.  It is
  #       designed to emit a message to the console.  The channel argument
  #       is the channel where the message should be written.  The string
  #       argument is the content of the message to emit.  If the channel
  #       argument is an empty string, nothing is written.
  #
  proc pageOut { channel string } {
    if {[string length $channel] > 0} then {
      catch {
        puts -nonewline $channel $string; flush $channel
      }
    }
  }

  #
  # NOTE: This procedure was stolen from the "getEagle.tcl" script.  It is
  #       designed to emit a message to the HTTP client log.  The string
  #       argument is the content of the message to emit.  If the string
  #       argument is an empty string, nothing is written.
  #
  proc pageLog { string } {
    if {[string length $string] > 0} then {
      catch {
        tclLog [appendArgs \
            [pid] " : " [clock seconds] " : http : " $string]
      }
    }
  }

  #
  # NOTE: This procedure was stolen from the "getEagle.tcl" script.  It is
  #       designed to emit a progress indicator while an HTTP request is
  #       being processed.  The channel argument is the Tcl channel where
  #       the progress indicator should be emitted.  The type argument is
  #       the single-character progress indicator.  The milliseconds
  #       argument is the number of milliseconds to wait until the next
  #       periodic progress indicator should be emitted.  This procedure
  #       reschedules its own execution.
  #
  proc pageProgress { channel type milliseconds } {
    #
    # NOTE: This variable is used to keep track of the currently scheduled
    #       (i.e. pending) [after] event.
    #
    variable afterForPageProgress

    #
    # NOTE: Show that something is happening...
    #
    pageOut $channel $type

    #
    # NOTE: Make sure that we are scheduled to run again, if requested.
    #
    if {$milliseconds > 0} then {
      set afterForPageProgress [after $milliseconds \
          [namespace code [list pageProgress $channel $type \
          $milliseconds]]]
    } else {
      unset -nocomplain afterForPageProgress
    }
  }

  #
  # NOTE: This procedure was stolen from the "getEagle.tcl" script.  It is
  #       designed to process a single HTTP request, including any HTTP
  #       3XX redirects (up to the specified limit), and return the raw
  #       HTTP response data.  It may raise any number of script errors.
  #
  proc getFileViaHttp { uri redirectLimit channel quiet args } {
    #
    # NOTE: This global variable is used to check the running version of
    #       Tcl.
    #
    global tcl_version

    #
    # NOTE: This variable is used to determine if plain HTTP URIs should be
    #       converted to HTTPS, if the "tls" package is available.
    #
    variable forceSecureUri

    #
    # NOTE: This variable is used to determine if plain HTTP is allowed if
    #       the "tls" package is not available.
    #
    variable allowInsecureUri

    #
    # NOTE: This variable is used to determine if a diagnostic message is
    #       emitted when [::http::geturl] is about to be called.
    #
    variable verboseGetUrl

    #
    # NOTE: This variable is used to determine if plain HTTP is allowed if
    #       an HTTP redirect response contains an HTTP URI and the original
    #       URI was HTTPS.
    #
    variable allowInsecureRedirect

    #
    # NOTE: This variable is used to keep track of the currently scheduled
    #       (i.e. pending) [after] event.
    #
    variable afterForPageProgress

    #
    # NOTE: This procedure requires the modern version of the HTTP package,
    #       which is typically included with the Tcl core distribution.
    #
    package require http 2.0

    #
    # NOTE: Tcl 8.6 added support for IPv6; however, on some machines this
    #       support can cause sockets to hang for a long time.  Therefore,
    #       for now, always force the use of IPv4.
    #
    if {[info exists tcl_version] && $tcl_version >= 8.6} then {
      namespace eval ::tcl::unsupported {}
      set ::tcl::unsupported::socketAF inet
    }

    #
    # NOTE: If the 'tls' package is available, always attempt to use HTTPS;
    #       otherwise, only attempt to use HTTP if explicitly allowed.
    #
    if {[catch {package require tls}] == 0} then {
      ::http::register https 443 [list ::tls::socket -tls1 true]

      if {$forceSecureUri} then {
        if {[string range $uri 0 6] eq "http://"} then {
          set uri [appendArgs https:// [string range $uri 7 end]]
        }
      }
    } else {
      if {$allowInsecureUri} then {
        if {[string range $uri 0 7] eq "https://"} then {
          set uri [appendArgs http:// [string range $uri 8 end]]
        }
      }
    }

    #
    # NOTE: Unless the caller forbids it, display progress messages during
    #       the download.
    #
    if {!$quiet} then {
      pageProgress $channel . 250
    }

    #
    # NOTE: All downloads are handled synchronously, which is not ideal;
    #       however, it is simple.  Keep going as long as there are less
    #       than X redirects.
    #
    set redirectCount 0

    while {1} {
      #
      # NOTE: Issue the HTTP request now, grabbing the resulting token.
      #
      if {$verboseGetUrl} then {
        #
        # NOTE: Emit important diagnostic information related to this
        #       HTTP request here.  This may be enhanced in the future.
        #
        pageLog [appendArgs \
            "attempting to download URL \"" $uri \"...]
      }

      set token [eval ::http::geturl [list $uri] $args]

      #
      # NOTE: Grab the HTTP response code and data now as they are needed
      #       in almost all cases.
      #
      set code [::http::ncode $token]; set data [::http::data $token]

      #
      # NOTE: If the HTTP response code is an empty string that may
      #       indicate a serious bug in the tls (or http) package for
      #       this platform.  So far, this issue has only been seen
      #       with the tls 1.6.1 package that shipped with macOS.
      #
      if {[string length $code] == 0} then {
        error [appendArgs \
            "received empty HTTP response code for URL \"" $uri \
            "\", the \"tls\" (and/or \"http\") package(s) may be " \
            "broken for this platform"]
      }

      #
      # NOTE: Check the HTTP response code, in order to follow any HTTP
      #       redirect responses.
      #
      switch -glob -- $code {
        100 -
        101 -
        102 {
          ::http::cleanup $token; error [appendArgs \
              "unsupported informational HTTP response status code " \
              $code ", data: " $data]
        }
        200 -
        201 -
        202 -
        203 -
        204 -
        205 -
        206 -
        207 -
        208 -
        226 {
          #
          # NOTE: Ok, the HTTP response is actual data of some kind (which
          #       may be empty).
          #
          ::http::cleanup $token; break
        }
        301 -
        302 -
        303 -
        307 -
        308 {
          #
          # NOTE: Unless the caller forbids it, display progress messages
          #       when an HTTP redirect is returned.
          #
          if {!$quiet} then {
            pageProgress $channel > 0
          }

          #
          # NOTE: We hit another HTTP redirect.  Stop if there are more
          #       than X.
          #
          incr redirectCount

          #
          # TODO: Maybe make this limit more configurable?
          #
          if {$redirectCount > $redirectLimit} then {
            #
            # NOTE: Just "give up" and raise a script error.
            #
            ::http::cleanup $token; error [appendArgs \
                "redirection limit of " $redirectLimit " exceeded"]
          }

          #
          # NOTE: Grab the metadata associated with this HTTP response.
          #
          array set meta [::http::meta $token]

          #
          # NOTE: Is there actually a new URI (location) to use?
          #
          if {[info exist meta(Location)]} then {
            #
            # NOTE: Ok, grab it now.  Later, at the top of the loop,
            #       it will be used in the subsequent HTTP request.
            #
            set location $meta(Location); unset meta

            #
            # NOTE: For security, by default, do NOT follow an HTTP
            #       redirect if it attempts to redirect from HTTPS
            #       to HTTP.
            #
            if {!$allowInsecureRedirect && \
                [string range $uri 0 7] eq "https://" && \
                [string range $location 0 7] ne "https://"} then {
              #
              # NOTE: Just "give up" and raise a script error.
              #
              ::http::cleanup $token; error [appendArgs \
                  "refused insecure redirect from \"" $uri "\" to \"" \
                  $location \"]
            }

            #
            # NOTE: Replace the original URI with the new one, for
            #       use in the next HTTP request.
            #
            set uri $location

            #
            # NOTE: Cleanup the current HTTP token now beause a new
            #       one will be created for the next request.
            #
            ::http::cleanup $token
          } else {
            #
            # NOTE: Just "give up" and raise a script error.
            #
            ::http::cleanup $token; error [appendArgs \
                "redirect from \"" $uri "\" missing location, code " \
                $code ", data: " $data]
          }
        }
        300 -
        304 -
        305 -
        306 {
          ::http::cleanup $token; error [appendArgs \
              "unsupported redirection HTTP response status code " $code \
              ", data: " $data]
        }
        4?? {
          ::http::cleanup $token; error [appendArgs \
              "client error HTTP response status code " $code ", data: " \
              $data]
        }
        5?? {
          ::http::cleanup $token; error [appendArgs \
              "server error HTTP response status code " $code ", data: " \
              $data]
        }
        default {
          ::http::cleanup $token; error [appendArgs \
              "unrecognized HTTP response status code " $code ", data: " \
              $data]
        }
      }
    }

    #
    # NOTE: If there is a currently scheduled [after] event, cancel it.
    #
    if {[info exists afterForPageProgress]} then {
      catch {after cancel $afterForPageProgress}
      unset -nocomplain afterForPageProgress
    }

    #
    # NOTE: If progress messages were emitted, start a fresh line.
    #
    if {!$quiet} then {
      pageOut $channel [appendArgs " " $uri \n]
    }

    return $data
  }

  #
  # NOTE: First, setup the variables associated with this package.
  #
  setupCommonVariables false

  #
  # NOTE: Export the procedures from this namespace that are designed to be
  #       used by external scripts.
  #
  namespace export appendArgs getFileViaHttp pageOut writeFile

  #
  # NOTE: Provide the package to the interpreter.
  #
  package provide Eagle.Tools.Common 1.0
}
