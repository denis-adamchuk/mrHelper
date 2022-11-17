::call ".\set-env.bat"

cd ..\bin\Release

for %%f in (mrHelper*.exe) do (
   signtool sign /a /tr http://timestamp.digicert.com %%f
   if %ERRORLEVEL% NEQ 0 EXIT /B 1
)

for %%f in (mrHelper*.dll) do (
   signtool sign /a /tr http://timestamp.digicert.com %%f
   if %ERRORLEVEL% NEQ 0 EXIT /B 1
)

signtool sign /a /tr http://timestamp.digicert.com GitLabSharp.dll
if %ERRORLEVEL% NEQ 0 EXIT /B 1

signtool sign /a /tr http://timestamp.digicert.com Aga.Controls.dll
if %ERRORLEVEL% NEQ 0 EXIT /B 1

cd ..\..\build

EXIT /B 0

