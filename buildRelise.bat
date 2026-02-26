@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

:: ========== НАСТРОЙКИ (измени под себя) ==========
set "SERVER_DIR=D:\ss14myserver\VGSpaceStation14"
set "BACKUP_DIR=%SERVER_DIR%\DatabaseBackups"
:: =================================================

title Space Station 14 Server Manager
cd /d "%SERVER_DIR%"

:: Создаём папку для бекапов, если её нет
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

:MENU
cls
echo ================================================
echo         SS14 Server Manager - Меню
echo ================================================
echo [1] Rebuild + Запуск сервера (сброс БД, сохр. конфиг)
echo [2] Rebuild (без сохранения/восстановления БД)
echo ========== БИНАРНИКИ ==========
echo [3] Запустить сервер (bin)
echo [4] Запустить клиент (bin)
echo [5] Запустить сервер + клиент (bin)
echo ========== DEV SERVER ==========
echo [6] Запустить DEV сервер (runserver.bat)
echo [7] Запустить DEV клиент (runclient.bat)
echo [8] Запустить DEV сервер + клиент
echo ========== БЕКАПЫ ==========
echo [9] Сохранить БД и конфиг (полный бекап)
echo [10] Сохранить только конфиг
echo [11] Восстановить из бекапа
echo [12] Показать список бекапов
echo [0] Выход
echo ================================================
set /p choice="Выберите пункт: "

if "%choice%"=="1" goto REBUILD_RESET_DB
if "%choice%"=="2" goto REBUILD_ONLY
if "%choice%"=="3" goto RUN_SERVER
if "%choice%"=="4" goto RUN_CLIENT
if "%choice%"=="5" goto RUN_BOTH
if "%choice%"=="6" goto RUN_DEV_SERVER
if "%choice%"=="7" goto RUN_DEV_CLIENT
if "%choice%"=="8" goto RUN_DEV_BOTH
if "%choice%"=="9" goto BACKUP_FULL
if "%choice%"=="10" goto BACKUP_CONFIG_ONLY_MENU
if "%choice%"=="11" goto RESTORE_DB
if "%choice%"=="12" goto LIST_BACKUPS
if "%choice%"=="0" exit /b
echo Неверный выбор & pause & goto MENU

:: ========== REBUILD + СБРОС БД (сохраняем только конфиг) ==========
:REBUILD_RESET_DB
cls
echo [1] Начинаем Rebuild со сбросом БД...

:: Сохраняем ТОЛЬКО конфиг (БД не сохраняем)
call :BACKUP_CONFIG_ONLY

:: Удаляем bin и билдим
if exist "bin" (
    echo Удаляем старую папку bin...
    rmdir /s /q "bin"
)

echo Запускаем билд...
python ./RUN_THIS.py
dotnet build --configuration Release

:: Восстанавливаем ТОЛЬКО конфиг (БД не восстанавливаем)
call :RESTORE_CONFIG_ONLY

:: Запускаем сервер
call :RUN_SERVER

goto MENU

:: ========== ТОЛЬКО REBUILD (БЕЗ СОХРАНЕНИЯ БД) ==========
:REBUILD_ONLY
cls
echo [2] Rebuild без сохранения/восстановления БД...

:: Удаляем bin и билдим
if exist "bin" (
    echo Удаляем старую папку bin...
    rmdir /s /q "bin"
)

echo Запускаем билд...
python ./RUN_THIS.py
dotnet build --configuration Release

echo Билд завершён! БД не сохранялась и не восстанавливалась.
pause
goto MENU

:: ========== ЗАПУСК СЕРВЕРА (BIN) ==========
:RUN_SERVER
cls
echo [3] Запуск сервера из bin...

if not exist "bin\Content.IntegrationTests\Content.Server.exe" (
    echo Ошибка: Сервер не собран! Сначала выполните Rebuild.
    pause
    goto MENU
)

cd /d "%SERVER_DIR%\bin\Content.IntegrationTests"
echo Запускаем Content.Server.exe...
start "SS14 Server (bin)" "Content.Server.exe"
cd /d "%SERVER_DIR%"
echo Сервер запущен в отдельном окне.
pause
goto MENU

:: ========== ЗАПУСК КЛИЕНТА (BIN) ==========
:RUN_CLIENT
cls
echo [4] Запуск клиента из bin...

if not exist "bin\Content.IntegrationTests\Content.Client.exe" (
    echo Ошибка: Клиент не собран! Сначала выполните Rebuild.
    pause
    goto MENU
)

cd /d "%SERVER_DIR%\bin\Content.IntegrationTests"
echo Запускаем Content.Client.exe...
start "SS14 Client (bin)" "Content.Client.exe"
cd /d "%SERVER_DIR%"
echo Клиент запущен в отдельном окне.
pause
goto MENU

:: ========== ЗАПУСК СЕРВЕРА + КЛИЕНТА (BIN) ==========
:RUN_BOTH
cls
echo [5] Запуск сервера и клиента из bin...

:: Проверяем существование файлов
if not exist "bin\Content.IntegrationTests\Content.Server.exe" (
    echo Ошибка: Сервер не собран! Сначала выполните Rebuild.
    pause
    goto MENU
)

if not exist "bin\Content.IntegrationTests\Content.Client.exe" (
    echo Ошибка: Клиент не собран! Сначала выполните Rebuild.
    pause
    goto MENU
)

:: Запускаем сервер
cd /d "%SERVER_DIR%\bin\Content.IntegrationTests"
echo Запускаем сервер...
start "SS14 Server (bin)" "Content.Server.exe"

:: Небольшая пауза, чтобы сервер успел инициализироваться
timeout /t 2 /nobreak >nul

:: Запускаем клиент
cd /d "%SERVER_DIR%\bin\Content.IntegrationTests"
echo Запускаем клиент...
start "SS14 Client (bin)" "Content.Client.exe"

cd /d "%SERVER_DIR%"
echo Сервер и клиент запущены в отдельных окнах.
pause
goto MENU

:: ========== ЗАПУСК DEV СЕРВЕРА ==========
:RUN_DEV_SERVER
cls
echo [6] Запуск DEV сервера...

if not exist "runserver.bat" (
    echo Ошибка: runserver.bat не найден!
    pause
    goto MENU
)

echo Запускаем runserver.bat...
start "SS14 DEV Server" "runserver.bat"
echo DEV сервер запущен в отдельном окне.
pause
goto MENU

:: ========== ЗАПУСК DEV КЛИЕНТА ==========
:RUN_DEV_CLIENT
cls
echo [7] Запуск DEV клиента...

if not exist "runclient.bat" (
    echo Ошибка: runclient.bat не найден!
    pause
    goto MENU
)

echo Запускаем runclient.bat...
start "SS14 DEV Client" "runclient.bat"
echo DEV клиент запущен в отдельном окне.
pause
goto MENU

:: ========== ЗАПУСК DEV СЕРВЕРА + КЛИЕНТА ==========
:RUN_DEV_BOTH
cls
echo [8] Запуск DEV сервера и клиента...

:: Проверяем существование файлов
if not exist "runserver.bat" (
    echo Ошибка: runserver.bat не найден!
    pause
    goto MENU
)

if not exist "runclient.bat" (
    echo Ошибка: runclient.bat не найден!
    pause
    goto MENU
)

:: Запускаем DEV сервер
echo Запускаем DEV сервер...
start "SS14 DEV Server" "runserver.bat"

:: Небольшая пауза, чтобы сервер успел инициализироваться
timeout /t 3 /nobreak >nul

:: Запускаем DEV клиент
echo Запускаем DEV клиент...
start "SS14 DEV Client" "runclient.bat"

echo DEV сервер и клиент запущены в отдельных окнах.
pause
goto MENU

:: ========== СОХРАНИТЬ ПОЛНЫЙ БЕКАП (БД + КОНФИГ) ==========
:BACKUP_FULL
cls
set "DATA_SOURCE=%SERVER_DIR%\bin\Content.IntegrationTests\data"
set "CONFIG_SOURCE=%SERVER_DIR%\bin\Content.IntegrationTests\server_config.toml"

if not exist "%DATA_SOURCE%" (
    echo Папка data не найдена. Бекап невозможен.
    pause
    goto MENU
)

:: Генерируем имя папки с датой и временем
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set "BACKUP_FOLDER=%BACKUP_DIR%\db_backup_%datetime:~0,8%_%datetime:~8,6%"

echo Создаём полный бекап (БД + конфиг)...

:: Если папка бекапа уже существует - удаляем её
if exist "%BACKUP_FOLDER%" (
    rmdir /s /q "%BACKUP_FOLDER%"
)

:: Создаём папку бекапа
mkdir "%BACKUP_FOLDER%"

:: Копируем папку data
echo   Копируем data...
xcopy "%DATA_SOURCE%" "%BACKUP_FOLDER%\data\" /e /i /h /q >nul

:: Копируем конфиг, если он существует
if exist "%CONFIG_SOURCE%" (
    echo   Копируем server_config.toml...
    copy "%CONFIG_SOURCE%" "%BACKUP_FOLDER%\" >nul
) else (
    echo   Предупреждение: server_config.toml не найден
)

echo [OK] Полный бекап сохранён: %BACKUP_FOLDER%
pause
goto MENU

:: ========== СОХРАНИТЬ ТОЛЬКО КОНФИГ ==========
:BACKUP_CONFIG_ONLY_MENU
cls
set "CONFIG_SOURCE=%SERVER_DIR%\bin\Content.IntegrationTests\server_config.toml"

if not exist "%CONFIG_SOURCE%" (
    echo Конфиг не найден. Бекап невозможен.
    pause
    goto MENU
)

:: Генерируем имя папки с датой и временем
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set "BACKUP_FOLDER=%BACKUP_DIR%\config_backup_%datetime:~0,8%_%datetime:~8,6%"

echo Сохраняем только конфиг...

:: Если папка бекапа уже существует - удаляем её
if exist "%BACKUP_FOLDER%" (
    rmdir /s /q "%BACKUP_FOLDER%"
)

:: Создаём папку бекапа
mkdir "%BACKUP_FOLDER%"

:: Копируем только конфиг
copy "%CONFIG_SOURCE%" "%BACKUP_FOLDER%\" >nul

echo [OK] Конфиг сохранён: %BACKUP_FOLDER%
pause
goto MENU

:: ========== СОХРАНИТЬ ТОЛЬКО КОНФИГ (ТИХОЕ) ==========
:BACKUP_CONFIG_ONLY
set "CONFIG_SOURCE=%SERVER_DIR%\bin\Content.IntegrationTests\server_config.toml"

if not exist "%CONFIG_SOURCE%" (
    exit /b
)

:: Генерируем имя папки с датой и временем
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set "BACKUP_FOLDER=%BACKUP_DIR%\config_backup_%datetime:~0,8%_%datetime:~8,6%"

:: Если папка бекапа уже существует - удаляем её
if exist "%BACKUP_FOLDER%" (
    rmdir /s /q "%BACKUP_FOLDER%"
)

:: Создаём папку бекапа
mkdir "%BACKUP_FOLDER%"

:: Копируем только конфиг
copy "%CONFIG_SOURCE%" "%BACKUP_FOLDER%\" >nul
exit /b

:: ========== ВОССТАНОВИТЬ ТОЛЬКО КОНФИГ ==========
:RESTORE_CONFIG_ONLY
echo.
echo Поиск последнего конфига для восстановления...

:: Ищем самую свежую папку с конфигом (по дате создания)
set "LATEST_CONFIG="
for /f "delims=" %%i in ('dir "%BACKUP_DIR%\config_backup_*" /b /o-d /ad 2^>nul') do (
    set "LATEST_CONFIG=%%i"
    goto :FOUND_CONFIG
)

echo [!] Конфигов не найдено. Конфиг не будет восстановлен.
exit /b

:FOUND_CONFIG
echo [OK] Найден последний конфиг: %LATEST_CONFIG%

:: Восстанавливаем конфиг
if exist "%BACKUP_DIR%\%LATEST_CONFIG%\server_config.toml" (
    echo Восстанавливаем server_config.toml...
    copy "%BACKUP_DIR%\%LATEST_CONFIG%\server_config.toml" "%SERVER_DIR%\bin\Content.IntegrationTests\" >nul
    echo [OK] Конфиг восстановлен
)
exit /b

:: ========== ВОССТАНОВИТЬ ИЗ БЕКАПА (ВРУЧНУЮ) ==========
:RESTORE_DB
cls
echo Доступные бекапы:
echo ------------------------------------------------
echo == Полные бекапы (БД + конфиг) ==
dir "%BACKUP_DIR%\db_backup_*" /b /ad 2>nul
echo.
echo == Бекапы только конфига ==
dir "%BACKUP_DIR%\config_backup_*" /b /ad 2>nul
if errorlevel 1 echo Бекапов пока нет.
echo ------------------------------------------------
echo.

set /p backup_choice="Введите имя папки бекапа (или 0 для отмены): "
if "%backup_choice%"=="0" goto MENU
if not exist "%BACKUP_DIR%\%backup_choice%" (
    echo Папка не найдена!
    pause
    goto RESTORE_DB
)

echo Восстанавливаем из %backup_choice%...

:: Проверяем тип бекапа (с БД или только конфиг)
if exist "%BACKUP_DIR%\%backup_choice%\data" (
    :: Это бекап с БД
    echo Найден бекап с БД и конфигом...
    
    if exist "%SERVER_DIR%\bin\Content.IntegrationTests\data" (
        echo Удаляем старую папку data...
        rmdir /s /q "%SERVER_DIR%\bin\Content.IntegrationTests\data"
    )
    
    if not exist "%SERVER_DIR%\bin\Content.IntegrationTests" mkdir "%SERVER_DIR%\bin\Content.IntegrationTests"
    
    echo Восстанавливаем data...
    xcopy "%BACKUP_DIR%\%backup_choice%\data" "%SERVER_DIR%\bin\Content.IntegrationTests\data\" /e /i /h /q
)

if exist "%BACKUP_DIR%\%backup_choice%\server_config.toml" (
    echo Восстанавливаем server_config.toml...
    copy "%BACKUP_DIR%\%backup_choice%\server_config.toml" "%SERVER_DIR%\bin\Content.IntegrationTests\"
)

echo [OK] Восстановление завершено
pause
goto MENU

:: ========== СПИСОК БЕКАПОВ ==========
:LIST_BACKUPS
cls
echo Список бекапов в %BACKUP_DIR%:
echo ------------------------------------------------
echo == Полные бекапы (БД + конфиг) ==
dir "%BACKUP_DIR%\db_backup_*" /b /ad 2>nul
echo.
echo == Бекапы только конфига ==
dir "%BACKUP_DIR%\config_backup_*" /b /ad 2>nul
if errorlevel 1 echo Бекапов пока нет.
echo ------------------------------------------------
pause
goto MENU