@ECHO OFF

REM ***** Build All *****
"%~dp0tools\CSBuild.exe" rebuild /p:Configuration=Release
"%~dp0lib\NUnit\tools\NUnit-Console.exe" /nologo /noshadow "%~dp0src\ProtocolBuffers.Rpc.Test\bin\Release\Google.ProtocolBuffers.Rpc.Test.dll"

REM ***** Packaging *****
PUSHD "%~dp0tools" 
NUGET pack Google.ProtocolBuffers.Rpc.nuspec -Version "%1" -Symbols
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL
NUGET push Google.ProtocolBuffers.Rpc.%1.nupkg
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL
MOVE Google.ProtocolBuffers.Rpc.%1.nupkg Google.ProtocolBuffers.Rpc.%1.zip
NUGET push Google.ProtocolBuffers.Rpc.%1.symbols.nupkg
IF NOT "%ERRORLEVEL%" == "0" GOTO FAIL
MOVE Google.ProtocolBuffers.Rpc.%1.symbols.nupkg Google.ProtocolBuffers.Rpc.%1.symbols.zip
POPD

HG commit -m "1.11.1016.3"
HG tag 1.11.1016.3

GOTO EXIT

:FAIL
POPD
ECHO Build Failed.
EXIT /B 1

:EXIT
