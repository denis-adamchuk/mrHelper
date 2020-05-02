call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\Tools\VsDevCmd.bat"

makepri new /pr ..\submodules\mrHelper.Install\appx /cf ..\submodules\mrHelper.Install\appx\manual\priconfig.xml /mn ..\submodules\mrHelper.Install\appx\manual\AppxManifest.xml /of ..\submodules\mrHelper.Install\appx\manual\resources.pri /o

makeappx pack /m ../submodules/mrHelper.Install/appx/manual/AppxManifest.xml /f ../submodules/mrHelper.Install/appx/manual/package.map.txt /p ../submodules/mrHelper.Install/appx/manual/output/mrHelper.msix

SignTool sign /fd SHA256 ../submodules/mrHelper.Install/appx/manual/output/mrHelper.msix

