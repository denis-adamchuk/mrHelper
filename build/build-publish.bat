:: TODO: Pass version here
:: TODO: Pass password here
:: TODO: Update version in SharedAssembly.cs
:: TODO: Update version in AppxManifest.xml

call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\Tools\VsDevCmd.bat"

msbuild /nologo /v:q /property:GenerateFullPaths=true /t:Build /p:Configuration="Release" /m:6 ../mrHelper.sln

makepri new /pr ..\submodules\mrHelper.Install\appx /cf ..\submodules\mrHelper.Install\appx\manual\priconfig.xml /mn ..\submodules\mrHelper.Install\appx\manual\AppxManifest.xml /of ..\submodules\mrHelper.Install\appx\manual\resources.pri /o

makeappx pack /m ../submodules/mrHelper.Install/appx/manual/AppxManifest.xml /f ../submodules/mrHelper.Install/appx/manual/package.map.txt /p ../submodules/mrHelper.Install/appx/manual/output/mrHelper.2.0.1.msix

SignTool sign /fd SHA256 ../submodules/mrHelper.Install/appx/manual/output/mrHelper.2.0.1.msix

