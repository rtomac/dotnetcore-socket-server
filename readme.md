Introduction
============
A fun exercise to experiment with raw TCP socket comms, using .NET Core, striving to optimize throughput.

In some areas of the implementation there are CLR constructs (async/await) and BCL classes that could make this simpler, but I'm striving to do all the thread management, parallelization, and synchronization of resources manually. For fun.

Building
========
#### Get .NET Core SDK with .NET 1.1 runtime

On Windows:
```
choco install -y dotnetcore-sdk
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

Fire it up, and it will start listening for connections on port 4000.

```
run-server.bat
```

`run-server.bat` simply calls `dotnet run` from the server app/project dir.

For command-line usage of the server app, run:

```
cd Server
dotnet run -- -?
```

#### Client

When started, it will connect to the server on port 4000 and start blasting data at it.

```
run-client.bat
```

`run-client.bat` simply calls `dotnet run` from the client app/project dir.

Again, for command-line usage, run:

```
cd Client
dotnet run -- -?
```

The code
=========
Here is a brief overview of the code/class design (in the server app):
- `Program`: Entry point for the server app. Handles command-line parsing, etc., but delegates the real work to the `Application` class.
- `Application`: Class that orchestrates and controls the lifetime of the different components in the application, including the socket listener, the log file writer, and the status reporter.
- `LocalhostSocketListener`: Binds to localhost:4000 (or specified port), listens for incoming connections on a background thread, and dispatches new threads to handle each of them.
- `SocketStreamReader`: Reads data from a network stream over a socket connection, parses and processes it, and hands the good stuff off to other components for further processing (i.e. writing to log file).
- `QueueingLogWriter`: Writes de-duped values transmitted to the server into a log file. Does this by managing an in-memory queue of values to be written, and processing that queue on a background worker thread, to isolate the file I/O and prevent it from blocking the other threads that are processing data from their network connections.
- `StatusReporter`: Keeps track of aggregate statistics and periodically (on a background thread) writes a status report to stdout.

The source code has a bit more documentation on how all of these classes are doing their job. Check it out.
