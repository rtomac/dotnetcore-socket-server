@echo off

pushd "%~dp0Client\"
start "Client" dotnet run
popd
