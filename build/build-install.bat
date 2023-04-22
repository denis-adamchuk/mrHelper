call ".\set-env.bat"

@if "%1"=="" (
   @echo Error: required parameter Target Path is missing
   @exit 1
)

@set target_path=%1

call .\build-release.bat
if %ERRORLEVEL% NEQ 0 EXIT /B 1

devenv ../mrHelper.sln /build Install
if %ERRORLEVEL% NEQ 0 EXIT /B 1

signtool sign /fd SHA256 /td SHA256 /a /tr http://timestamp.digicert.com %target_path%
if %ERRORLEVEL% NEQ 0 EXIT /B 1

EXIT /B 0

