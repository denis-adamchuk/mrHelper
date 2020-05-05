call ".\set-env.bat"

msbuild /nologo /v:q /property:GenerateFullPaths=true /t:Build /p:Configuration="Debug" /m:6 ../mrHelper.sln
if %ERRORLEVEL% NEQ 0 EXIT /B 1

EXIT /B 0

