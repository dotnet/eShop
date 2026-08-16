@echo off
setlocal

call "%~dp0mvnw.cmd" -q -DskipTests -pl eshop-foundation-smoke-app -am package
if errorlevel 1 exit /b %errorlevel%

java -jar "%~dp0eshop-foundation-smoke-app\target\app.jar"
