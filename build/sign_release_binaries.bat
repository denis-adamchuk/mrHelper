call ".\set-env.bat"

cd ..\bin\Release

for %%f in (mrHelper*.exe) do (
   signtool sign /a /t http://timestamp.globalsign.com/scripts/timstamp.dll %%f
   if %ERRORLEVEL% NEQ 0 EXIT /B 1
)

for %%f in (mrHelper*.dll) do (
   signtool sign /a /t http://timestamp.globalsign.com/scripts/timstamp.dll %%f
   if %ERRORLEVEL% NEQ 0 EXIT /B 1
)

signtool sign /a /t http://timestamp.globalsign.com/scripts/timstamp.dll GitLabSharp.dll
if %ERRORLEVEL% NEQ 0 EXIT /B 1

cd ..\..\build

EXIT /B 0

