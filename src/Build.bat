@echo off
pushd "%~dp0"
dotnet msbuild Abc.Zebus.MessageDsl.slnx /v:m /t:Restore,Rebuild,Pack /nodeReuse:false /bl /p:Configuration=Release
popd
pause
