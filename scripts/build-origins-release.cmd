@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat"
if errorlevel 1 exit /b %errorlevel%
set "VCPKG_ROOT=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\vcpkg"
set "CMAKE_EXE=C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
"%CMAKE_EXE%" --preset windows-x64 -B out/build/windows-x64-vs2022
if errorlevel 1 exit /b %errorlevel%
"%CMAKE_EXE%" --build out/build/windows-x64-vs2022 --config Release --parallel 2
if errorlevel 1 exit /b %errorlevel%
dotnet publish launcher\OriginsLauncher.csproj -c Release --no-restore
