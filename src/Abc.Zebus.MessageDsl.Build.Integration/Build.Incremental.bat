@echo off
pushd "%~dp0"
dotnet msbuild Abc.Zebus.MessageDsl.Build.Integration.csproj /v:m /t:Build /nodeReuse:false /bl
popd
pause
