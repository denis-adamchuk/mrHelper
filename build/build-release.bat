call ".\set-env.bat"

msbuild /nologo /v:q /p:GenerateFullPaths=true /t:Build /p:Configuration="Release" /m:6 ../mrHelper.sln
if %ERRORLEVEL% NEQ 0 EXIT /B 1

call .\sign_release_binaries.bat
if %ERRORLEVEL% NEQ 0 EXIT /B 1

EXIT /B 0

