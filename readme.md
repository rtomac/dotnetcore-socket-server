Introduction
============
A fun exercise to experiment with raw TCP socket comms, using .NET Core, striving to optimize throughput.

In some areas of the implementation there are CLR constructs (async/await) and BCL classes that could make this simpler, but I'm striving to do all the thread management, parallelization, and synchronization of resources manually. For fun.

Building
========
#### Get .NET Core SDK with .NET 1.1 runtime

```
choco install dotnetcore-sdk
```

#### Clone repo

```
git clone https://github.com/rtomac/dotnetcore-socket-server.git
```

#### Build

```
cd dotnetcore-socket-server
build.bat
```

build.bat simply calls `dotnet restore`, `dotnet build`, and `dotnet test`.

Running
=======
There are two command-line applications here:

#### Server

Fire it up, and it will start listening for connections (by default) on port 4000.

```
run-server.bat
```

run-server.bat simply calls `dotnet run` from the server app/project dir.

For command-line usage of the server app, run:

```
cd Server
dotnet run -- -?
```

#### Client

When started, and it will connect to the server (by default) on port 4000 and start blasting data at it.

```
run-client.bat
```

run-client.bat simply calls `dotnet run` from the client app/project dir.

Again, for command-line usage, run:

```
cd Client
dotnet run -- -?
```
