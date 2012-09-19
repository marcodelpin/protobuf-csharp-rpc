@ECHO OFF

REM ***** Build All *****
"%~dp0tools\CSBuild.exe" rebuild /p:Configuration=Release
"%~dp0lib\NUnit\tools\NUnit-Console.exe" /nologo /noshadow "%~dp0src\ProtocolBuffers.Rpc.Test\bin\Release\Google.ProtocolBuffers.Rpc.Test.dll"

C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe src\ProtocolBuffers.Rpc\ProtocolBuffers.Rpc.csproj /target:Rebuild /toolsversion:4.0 "/p:Configuration=Release40" /fl /verbosity:minimal
C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe src\ProtocolBuffers.Rpc\ProtocolBuffersLite.Rpc.csproj /target:Rebuild /toolsversion:4.0 "/p:Configuration=Release40" /fl /verbosity:minimal


REM ***** Packaging *****
PUSHD "%~dp0tools" 
NUGET pack Google.ProtocolBuffers.Rpc.nuspec -Version "%1" -Symbols
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL
NUGET pack Google.ProtocolBuffersLite.Rpc.nuspec -Version "%1" -Symbols
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL

PAUSE Push package for version %1?

NUGET push Google.ProtocolBuffers.Rpc.%1.nupkg
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL
MOVE Google.ProtocolBuffers.Rpc.%1.nupkg Google.ProtocolBuffers.Rpc.%1.zip
NUGET push Google.ProtocolBuffers.Rpc.%1.symbols.nupkg
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL
MOVE Google.ProtocolBuffers.Rpc.%1.symbols.nupkg Google.ProtocolBuffers.Rpc.%1.symbols.zip

NUGET push Google.ProtocolBuffersLite.Rpc.%1.nupkg
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL
MOVE Google.ProtocolBuffersLite.Rpc.%1.nupkg Google.ProtocolBuffersLite.Rpc.%1.zip
NUGET push Google.ProtocolBuffersLite.Rpc.%1.symbols.nupkg
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL
MOVE Google.ProtocolBuffersLite.Rpc.%1.symbols.nupkg Google.ProtocolBuffersLite.Rpc.%1.symbols.zip
POPD

HG commit -m "%1"
HG tag %1
HG push

GOTO EXIT

:FAIL
POPD
ECHO Build Failed.
EXIT /B 1

:EXIT
