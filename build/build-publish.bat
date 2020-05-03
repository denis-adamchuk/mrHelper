:: TODO All paths and file names shall be passed into the batch and not hard-coded here

call ".\set-env.bat"

msbuild /nologo /v:q /property:GenerateFullPaths=true /t:Build /p:Configuration="Release" /m:6 ../mrHelper.sln

makepri new /pr ..\submodules\mrHelper.Install\appx\manual /cf ..\submodules\mrHelper.Install\appx\manual\priconfig.xml /mn ..\submodules\mrHelper.Install\appx\manual\AppxManifest.xml /of ..\submodules\mrHelper.Install\appx\manual\resources.pri /o

if %ERRORLEVEL% NEQ 0 EXIT /B 1

makeappx pack /m ../submodules/mrHelper.Install/appx/manual/AppxManifest.xml /f ../submodules/mrHelper.Install/appx/manual/package.map.txt /p ../submodules/mrHelper.Install/appx/manual/output/mrHelper.msix

if %ERRORLEVEL% NEQ 0 EXIT /B 1

SignTool sign /fd SHA256 ../submodules/mrHelper.Install/appx/manual/output/mrHelper.msix

if %ERRORLEVEL% NEQ 0 EXIT /B 1

EXIT /B 0

