@echo off

dotnet restore
dotnet build

pushd "%~dp0ServerTests\"
dotnet test
popd
