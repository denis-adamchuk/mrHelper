call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"
msbuild /nologo /v:q /property:GenerateFullPaths=true /t:Build /p:Configuration="Release" /m:6 ../mrHelper.sln
