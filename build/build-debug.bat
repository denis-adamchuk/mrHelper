call "D:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"
msbuild /nologo /v:q /property:GenerateFullPaths=true /t:Build /p:Configuration="Debug" /m:6 ../mrHelper.sln
