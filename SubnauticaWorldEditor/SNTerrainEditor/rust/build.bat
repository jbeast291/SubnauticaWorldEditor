@echo off

if "%~1"=="--ci" (
    cargo +stable run --locked --package optoctrees-builder -- %*
) else (
    cargo +stable run --package optoctrees-builder -- %*
)