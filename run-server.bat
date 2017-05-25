@echo off

pushd "%~dp0Server\"
start "Server" dotnet run
popd
