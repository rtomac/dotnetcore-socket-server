@echo off

pushd "%~dp0Server\"
dotnet restore
popd

pushd "%~dp0Client\"
dotnet restore
popd

pushd "%~dp0ServerTests\"
dotnet restore
popd

dotnet build

pushd "%~dp0ServerTests\"
dotnet test
popd
