@echo off
pushd "%~dp0"
dotnet msbuild Abc.Zebus.MessageDsl.Build.Integration.csproj /v:m /t:Restore,Rebuild /nodeReuse:false /bl
popd
pause
