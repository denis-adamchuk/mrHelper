call ".\set-env.bat"

msbuild /nologo /v:q /property:GenerateFullPaths=true /t:Build /p:Configuration="Release" /m:6 ../mrHelper.sln
