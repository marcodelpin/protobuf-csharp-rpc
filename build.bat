@ECHO OFF

REM ***** Update dependencies *****
"%~dp0tools\NuGet.exe" install -x -OutputDirectory "%~dp0lib" NUnit -Version 2.5.10.11092
"%~dp0tools\NuGet.exe" install -x -OutputDirectory "%~dp0lib" CSharpTest.Net.RpcLibrary -Version 1.11.924.348
"%~dp0tools\NuGet.exe" install -x -OutputDirectory "%~dp0lib" Google.ProtocolBuffers -Version 2.4.1.473

REM ***** Generate Source *****
"%~dp0tools\CmdTool.exe" build src\*.csproj

REM ***** Build All *****
"%~dp0tools\CSBuild.exe" rebuild

REM ***** Test *****
"%~dp0lib\NUnit\tools\NUnit-Console.exe" /nologo /noshadow "%~dp0src\ProtocolBuffers.Rpc.Test\bin\Debug\Google.ProtocolBuffers.Rpc.Test.dll"

ECHO.