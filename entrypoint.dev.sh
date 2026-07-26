#!/bin/sh
set -e

# dotnet watch's browser-refresh WebSocket must bind to loopback
# (DOTNET_WATCH_AUTO_RELOAD_WS_HOSTNAME=127.0.0.1) so it reports a URL the
# browser can actually connect to (ws://localhost:<port>). Loopback isn't
# reachable through Docker's published port mapping, so relay a separate,
# container-internal-only port (WS_RELAY_LISTEN_PORT, published to the host
# instead) to Kestrel's loopback port. The two must be different ports:
# binding the relay to 0.0.0.0 on the SAME port Kestrel binds to 127.0.0.1
# would collide (0.0.0.0 reserves the port across all local addresses).
if [ -n "$DOTNET_WATCH_AUTO_RELOAD_WS_PORT" ] && [ -n "$WS_RELAY_LISTEN_PORT" ]; then
  socat TCP-LISTEN:"$WS_RELAY_LISTEN_PORT",fork,reuseaddr TCP:127.0.0.1:"$DOTNET_WATCH_AUTO_RELOAD_WS_PORT" &
fi

exec dotnet watch run --no-launch-profile
