@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

:: ========== НАСТРОЙКИ (измени под себя) ==========
set "SERVER_DIR=D:\ss14server\VGSpaceStation14"
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
echo [1] Полный Rebuild + Запуск сервера
echo [2] Только Rebuild (авто-бекап + авто-восстановление)
echo [3] Запустить сервер
echo [4] Запустить клиент
echo [5] Запустить сервер + клиент
echo [6] Сохранить БД вручную
echo [7] Восстановить БД из бекапа
echo [8] Показать список бекапов
echo [0] Выход
echo ================================================
set /p choice="Выберите пункт: "

if "%choice%"=="1" goto FULL_REBUILD_AND_RUN
if "%choice%"=="2" goto FULL_REBUILD
if "%choice%"=="3" goto RUN_SERVER
if "%choice%"=="4" goto RUN_CLIENT
if "%choice%"=="5" goto RUN_BOTH
if "%choice%"=="6" goto BACKUP_DB
if "%choice%"=="7" goto RESTORE_DB
if "%choice%"=="8" goto LIST_BACKUPS
if "%choice%"=="0" exit /b
echo Неверный выбор & pause & goto MENU

:: ========== ПОЛНЫЙ REBUILD + ЗАПУСК СЕРВЕРА ==========
:FULL_REBUILD_AND_RUN
cls
echo [1] Начинаем полный Rebuild...

:: Сохраняем текущую БД перед удалением
call :BACKUP_DB_SILENT

:: Удаляем bin и билдим
if exist "bin" (
    echo Удаляем старую папку bin...
    rmdir /s /q "bin"
)

echo Запускаем билд...
python ./RUN_THIS.py
dotnet build --configuration Release

:: АВТОМАТИЧЕСКИ ВОССТАНАВЛИВАЕМ ПОСЛЕДНЮЮ БД
call :RESTORE_LATEST_BACKUP

:: Запускаем сервер
call :RUN_SERVER

goto MENU

:: ========== ТОЛЬКО REBUILD ==========
:FULL_REBUILD
cls
echo [2] Rebuild с автоматическим восстановлением последней БД...

:: Сохраняем текущую БД перед удалением
call :BACKUP_DB_SILENT

:: Удаляем bin и билдим
if exist "bin" (
    echo Удаляем старую папку bin...
    rmdir /s /q "bin"
)

echo Запускаем билд...
python ./RUN_THIS.py
dotnet build --configuration Release

:: АВТОМАТИЧЕСКИ ВОССТАНАВЛИВАЕМ ПОСЛЕДНЮЮ БД
call :RESTORE_LATEST_BACKUP

echo Билд завершён! Последняя БД автоматически восстановлена.
pause
goto MENU

:: ========== ЗАПУСК СЕРВЕРА ==========
:RUN_SERVER
cls
echo [3] Запуск сервера...

if not exist "bin\Content.IntegrationTests\Content.Server.exe" (
    echo Ошибка: Сервер не собран! Сначала выполните Rebuild.
    pause
    goto MENU
)

cd /d "%SERVER_DIR%\bin\Content.IntegrationTests"
echo Запускаем Content.Server.exe...
start "SS14 Server" "Content.Server.exe"
cd /d "%SERVER_DIR%"
echo Сервер запущен в отдельном окне.
pause
goto MENU

:: ========== ЗАПУСК КЛИЕНТА ==========
:RUN_CLIENT
cls
echo [4] Запуск клиента...

if not exist "bin\Content.IntegrationTests\Content.Client.exe" (
    echo Ошибка: Клиент не собран! Сначала выполните Rebuild.
    pause
    goto MENU
)

cd /d "%SERVER_DIR%\bin\Content.IntegrationTests"
echo Запускаем Content.Client.exe...
start "SS14 Client" "Content.Client.exe"
cd /d "%SERVER_DIR%"
echo Клиент запущен в отдельном окне.
pause
goto MENU

:: ========== ЗАПУСК СЕРВЕРА + КЛИЕНТА ==========
:RUN_BOTH
cls
echo [5] Запуск сервера и клиента...

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
start "SS14 Server" "Content.Server.exe"

:: Небольшая пауза, чтобы сервер успел инициализироваться
timeout /t 2 /nobreak >nul

:: Запускаем клиент
cd /d "%SERVER_DIR%\bin\Content.IntegrationTests"
echo Запускаем клиент...
start "SS14 Client" "Content.Client.exe"

cd /d "%SERVER_DIR%"
echo Сервер и клиент запущены в отдельных окнах.
pause
goto MENU

:: ========== СОХРАНИТЬ БД ВРУЧНУЮ ==========
:BACKUP_DB
cls
call :BACKUP_DB_SILENT
echo.
echo Подсказка: этот бекап будет автоматически использован при следующем билде
pause
goto MENU

:: ========== ТИХОЕ СОХРАНЕНИЕ БД (папка data) ==========
:BACKUP_DB_SILENT
set "DB_SOURCE=%SERVER_DIR%\bin\Content.IntegrationTests\data"
if not exist "%DB_SOURCE%" (
    echo Папка с БД не найдена. Пропускаем бекап.
    exit /b
)

:: Генерируем имя папки с датой и временем
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set "BACKUP_FOLDER=%BACKUP_DIR%\db_backup_%datetime:~0,8%_%datetime:~8,6%"

echo Создаём бекап текущей БД из папки data...

:: Если папка бекапа уже существует - удаляем её
if exist "%BACKUP_FOLDER%" (
    rmdir /s /q "%BACKUP_FOLDER%"
)

:: Копируем всю папку data целиком
xcopy "%DB_SOURCE%" "%BACKUP_FOLDER%\" /e /i /h /q >nul

echo [OK] БД сохранена: %BACKUP_FOLDER%
exit /b

:: ========== ВОССТАНОВИТЬ ПОСЛЕДНИЙ БЕКАП ==========
:RESTORE_LATEST_BACKUP
echo.
echo Поиск последнего бекапа для автоматического восстановления...

:: Ищем самую свежую папку с бекапом (по дате создания)
set "LATEST="
for /f "delims=" %%i in ('dir "%BACKUP_DIR%\db_backup_*" /b /o-d /ad 2^>nul') do (
    set "LATEST=%%i"
    goto :FOUND
)

echo [!] Бекапов не найдено. БД не будет восстановлена.
exit /b

:FOUND
echo [OK] Найден последний бекап: %LATEST%

if exist "%SERVER_DIR%\bin\Content.IntegrationTests\data" (
    echo Удаляем старую папку data...
    rmdir /s /q "%SERVER_DIR%\bin\Content.IntegrationTests\data"
)

:: Создаём папку назначения, если её нет
if not exist "%SERVER_DIR%\bin\Content.IntegrationTests" mkdir "%SERVER_DIR%\bin\Content.IntegrationTests"

:: Копируем последний бекап как папку data
echo Восстанавливаем БД из бекапа в папку data...
xcopy "%BACKUP_DIR%\%LATEST%" "%SERVER_DIR%\bin\Content.IntegrationTests\data\" /e /i /h /q >nul

echo [OK] БД автоматически восстановлена из последнего бекапа: %LATEST%
exit /b

:: ========== ВОССТАНОВИТЬ БД ИЗ БЕКАПА (ВРУЧНУЮ) ==========
:RESTORE_DB
cls
echo Доступные бекапы (папки):
echo ------------------------------------------------
dir "%BACKUP_DIR%\db_backup_*" /b /ad 2>nul
if errorlevel 1 (
    echo Бекапов пока нет.
    pause
    goto MENU
)
echo ------------------------------------------------
echo.

set /p backup_choice="Введите имя папки бекапа (или 0 для отмены): "
if "%backup_choice%"=="0" goto MENU
if not exist "%BACKUP_DIR%\%backup_choice%" (
    echo Папка не найдена!
    pause
    goto RESTORE_DB
)

:: Удаляем текущую БД и копируем выбранный бекап
echo Восстанавливаем БД из %backup_choice%...

if exist "%SERVER_DIR%\bin\Content.IntegrationTests\data" (
    echo Удаляем старую папку data...
    rmdir /s /q "%SERVER_DIR%\bin\Content.IntegrationTests\data"
)

:: Создаём папку назначения, если её нет
if not exist "%SERVER_DIR%\bin\Content.IntegrationTests" mkdir "%SERVER_DIR%\bin\Content.IntegrationTests"

:: Копируем выбранный бекап как папку data
xcopy "%BACKUP_DIR%\%backup_choice%" "%SERVER_DIR%\bin\Content.IntegrationTests\data\" /e /i /h /q

echo [OK] БД восстановлена из %backup_choice% в папку data
pause
goto MENU

:: ========== СПИСОК БЕКАПОВ ==========
:LIST_BACKUPS
cls
echo Список бекапов в %BACKUP_DIR%:
echo ------------------------------------------------
dir "%BACKUP_DIR%\db_backup_*" /b /ad 2>nul
if errorlevel 1 echo Бекапов пока нет.
echo ------------------------------------------------
echo.
echo Последний бекап будет автоматически использован при билде.
pause
goto MENU