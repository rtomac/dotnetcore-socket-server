@echo off

dotnet build

pushd "%~dp0ServerTests\"
dotnet test
popd
