#!/usr/bin/env sh

if [ "$1" = "--ci" ]; then
    cargo +stable run --locked --package optoctrees-builder -- "$@"
else
    cargo +stable run --package optoctrees-builder -- "$@"
fi
