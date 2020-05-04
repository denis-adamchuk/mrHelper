call ".\set-env.bat"

@if "%1"=="" (
   @echo Error: required parameter Target Path is missing
   @exit 1
)

@if "%2"=="" (
   @echo Error: required parameter Manifest Path is missing
   @exit 1
)

@set target_path=%1
@set manifest_path=%2
For %%A in ("%manifest_path%") do (
    Set root_folder=%%~dpA
)

@set priconfig_path=%root_folder%priconfig.xml
@set resources_pri_path=%root_folder%resources.pri
@set package_map_path=%root_folder%package.map.txt

msbuild /nologo /v:q /p:DefineConstants="DesktopUWP" /p:GenerateFullPaths=true /t:Build /p:Configuration="Release" /m:6 ../mrHelper.sln
if %ERRORLEVEL% NEQ 0 EXIT /B 1

makepri new /pr %root_folder% /cf %priconfig_path% /mn %manifest_path% /of %resources_pri_path% /o
if %ERRORLEVEL% NEQ 0 EXIT /B 1

makeappx pack /m %manifest_path% /f %package_map_path% /p %target_path%
if %ERRORLEVEL% NEQ 0 EXIT /B 1

SignTool sign /a /fd SHA256 %target_path%
if %ERRORLEVEL% NEQ 0 EXIT /B 1

EXIT /B 0

