#!/bin/bash

# ========== НАСТРОЙКИ (измени под себя) ==========
SERVER_DIR="$HOME/ss14server/VGSpaceStation14"
BACKUP_DIR="$SERVER_DIR/DatabaseBackups"
# =================================================

cd "$SERVER_DIR" || { echo "Ошибка: директория $SERVER_DIR не найдена"; exit 1; }

# Создаём папку для бекапов, если её нет
mkdir -p "$BACKUP_DIR"

# Функция для сохранения только конфига (тихая)
backup_config_only() {
    CONFIG_SOURCE="$SERVER_DIR/bin/Content.IntegrationTests/server_config.toml"
    
    if [ ! -f "$CONFIG_SOURCE" ]; then
        return 1
    fi
    
    # Генерируем имя папки с датой и временем
    datetime=$(date +"%Y%m%d_%H%M%S")
    BACKUP_FOLDER="$BACKUP_DIR/config_backup_$datetime"
    
    # Создаём папку бекапа
    mkdir -p "$BACKUP_FOLDER"
    
    # Копируем только конфиг
    cp "$CONFIG_SOURCE" "$BACKUP_FOLDER/"
    return 0
}

# Функция для восстановления только конфига
restore_config_only() {
    echo
    echo "Поиск последнего конфига для восстановления..."
    
    # Ищем самую свежую папку с конфигом
    LATEST_CONFIG=$(ls -d "$BACKUP_DIR"/config_backup_* 2>/dev/null | head -n1)
    
    if [ -z "$LATEST_CONFIG" ]; then
        echo "[!] Конфигов не найдено. Конфиг не будет восстановлен."
        return 1
    fi
    
    echo "[OK] Найден последний конфиг: $(basename "$LATEST_CONFIG")"
    
    # Восстанавливаем конфиг
    if [ -f "$LATEST_CONFIG/server_config.toml" ]; then
        echo "Восстанавливаем server_config.toml..."
        cp "$LATEST_CONFIG/server_config.toml" "$SERVER_DIR/bin/Content.IntegrationTests/"
        echo "[OK] Конфиг восстановлен"
    fi
    return 0
}

# Главное меню
while true; do
    clear
    echo "================================================"
    echo "         SS14 Server Manager - Меню"
    echo "================================================"
    echo "[1] Rebuild + Запуск сервера (сброс БД, сохр. конфиг)"
    echo "[2] Rebuild (без сохранения/восстановления БД)"
    echo "========== БИНАРНИКИ =========="
    echo "[3] Запустить сервер (bin)"
    echo "[4] Запустить клиент (bin)"
    echo "[5] Запустить сервер + клиент (bin)"
    echo "========== DEV SERVER =========="
    echo "[6] Запустить DEV сервер (runserver.bat)"
    echo "[7] Запустить DEV клиент (runclient.bat)"
    echo "[8] Запустить DEV сервер + клиент"
    echo "========== БЕКАПЫ =========="
    echo "[9] Сохранить БД и конфиг (полный бекап)"
    echo "[10] Сохранить только конфиг"
    echo "[11] Восстановить из бекапа"
    echo "[12] Показать список бекапов"
    echo "[0] Выход"
    echo "================================================"
    read -p "Выберите пункт: " choice

    case $choice in
        1)  # REBUILD + СБРОС БД
            clear
            echo "[1] Начинаем Rebuild со сбросом БД..."
            
            # Сохраняем ТОЛЬКО конфиг
            backup_config_only
            
            # Удаляем bin и билдим
            if [ -d "bin" ]; then
                echo "Удаляем старую папку bin..."
                rm -rf "bin"
            fi
            
            echo "Запускаем билд..."
            python3 ./RUN_THIS.py
            dotnet build --configuration Release
            
            # Восстанавливаем ТОЛЬКО конфиг
            restore_config_only
            
            # Запускаем сервер
            if [ -f "bin/Content.IntegrationTests/Content.Server" ]; then
                cd "$SERVER_DIR/bin/Content.IntegrationTests"
                echo "Запускаем Content.Server..."
                gnome-terminal -- bash -c "./Content.Server; exec bash" 2>/dev/null || \
                xterm -e "./Content.Server" 2>/dev/null || \
                konsole -e "./Content.Server" 2>/dev/null || \
                ./Content.Server &
                cd "$SERVER_DIR"
                echo "Сервер запущен в отдельном окне."
            else
                echo "Ошибка: Сервер не собран!"
            fi
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        2)  # ТОЛЬКО REBUILD
            clear
            echo "[2] Rebuild без сохранения/восстановления БД..."
            
            if [ -d "bin" ]; then
                echo "Удаляем старую папку bin..."
                rm -rf "bin"
            fi
            
            echo "Запускаем билд..."
            python3 ./RUN_THIS.py
            dotnet build --configuration Release
            
            echo "Билд завершён! БД не сохранялась и не восстанавливалась."
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        3)  # ЗАПУСК СЕРВЕРА (BIN)
            clear
            echo "[3] Запуск сервера из bin..."
            
            if [ ! -f "bin/Content.IntegrationTests/Content.Server" ]; then
                echo "Ошибка: Сервер не собран! Сначала выполните Rebuild."
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            cd "$SERVER_DIR/bin/Content.IntegrationTests"
            echo "Запускаем Content.Server..."
            gnome-terminal -- bash -c "./Content.Server; exec bash" 2>/dev/null || \
            xterm -e "./Content.Server" 2>/dev/null || \
            konsole -e "./Content.Server" 2>/dev/null || \
            ./Content.Server &
            cd "$SERVER_DIR"
            echo "Сервер запущен в отдельном окне."
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        4)  # ЗАПУСК КЛИЕНТА (BIN)
            clear
            echo "[4] Запуск клиента из bin..."
            
            if [ ! -f "bin/Content.IntegrationTests/Content.Client" ]; then
                echo "Ошибка: Клиент не собран! Сначала выполните Rebuild."
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            cd "$SERVER_DIR/bin/Content.IntegrationTests"
            echo "Запускаем Content.Client..."
            gnome-terminal -- bash -c "./Content.Client; exec bash" 2>/dev/null || \
            xterm -e "./Content.Client" 2>/dev/null || \
            konsole -e "./Content.Client" 2>/dev/null || \
            ./Content.Client &
            cd "$SERVER_DIR"
            echo "Клиент запущен в отдельном окне."
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        5)  # ЗАПУСК СЕРВЕРА + КЛИЕНТА (BIN)
            clear
            echo "[5] Запуск сервера и клиента из bin..."
            
            if [ ! -f "bin/Content.IntegrationTests/Content.Server" ]; then
                echo "Ошибка: Сервер не собран! Сначала выполните Rebuild."
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            if [ ! -f "bin/Content.IntegrationTests/Content.Client" ]; then
                echo "Ошибка: Клиент не собран! Сначала выполните Rebuild."
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            # Запускаем сервер
            cd "$SERVER_DIR/bin/Content.IntegrationTests"
            echo "Запускаем сервер..."
            gnome-terminal -- bash -c "./Content.Server; exec bash" 2>/dev/null || \
            xterm -e "./Content.Server" 2>/dev/null || \
            konsole -e "./Content.Server" 2>/dev/null || \
            ./Content.Server &
            
            # Небольшая пауза
            sleep 2
            
            # Запускаем клиент
            echo "Запускаем клиент..."
            gnome-terminal -- bash -c "./Content.Client; exec bash" 2>/dev/null || \
            xterm -e "./Content.Client" 2>/dev/null || \
            konsole -e "./Content.Client" 2>/dev/null || \
            ./Content.Client &
            
            cd "$SERVER_DIR"
            echo "Сервер и клиент запущены в отдельных окнах."
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        6)  # ЗАПУСК DEV СЕРВЕРА
            clear
            echo "[6] Запуск DEV сервера..."
            
            if [ ! -f "runserver.bat" ]; then
                echo "Ошибка: runserver.bat не найден!"
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            echo "Запускаем runserver.bat..."
            gnome-terminal -- bash -c "./runserver.bat; exec bash" 2>/dev/null || \
            xterm -e "./runserver.bat" 2>/dev/null || \
            konsole -e "./runserver.bat" 2>/dev/null || \
            ./runserver.bat &
            echo "DEV сервер запущен в отдельном окне."
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        7)  # ЗАПУСК DEV КЛИЕНТА
            clear
            echo "[7] Запуск DEV клиента..."
            
            if [ ! -f "runclient.bat" ]; then
                echo "Ошибка: runclient.bat не найден!"
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            echo "Запускаем runclient.bat..."
            gnome-terminal -- bash -c "./runclient.bat; exec bash" 2>/dev/null || \
            xterm -e "./runclient.bat" 2>/dev/null || \
            konsole -e "./runclient.bat" 2>/dev/null || \
            ./runclient.bat &
            echo "DEV клиент запущен в отдельном окне."
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        8)  # ЗАПУСК DEV СЕРВЕРА + КЛИЕНТА
            clear
            echo "[8] Запуск DEV сервера и клиента..."
            
            if [ ! -f "runserver.bat" ]; then
                echo "Ошибка: runserver.bat не найден!"
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            if [ ! -f "runclient.bat" ]; then
                echo "Ошибка: runclient.bat не найден!"
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            # Запускаем DEV сервер
            echo "Запускаем DEV сервер..."
            gnome-terminal -- bash -c "./runserver.bat; exec bash" 2>/dev/null || \
            xterm -e "./runserver.bat" 2>/dev/null || \
            konsole -e "./runserver.bat" 2>/dev/null || \
            ./runserver.bat &
            
            sleep 3
            
            # Запускаем DEV клиент
            echo "Запускаем DEV клиент..."
            gnome-terminal -- bash -c "./runclient.bat; exec bash" 2>/dev/null || \
            xterm -e "./runclient.bat" 2>/dev/null || \
            konsole -e "./runclient.bat" 2>/dev/null || \
            ./runclient.bat &
            
            echo "DEV сервер и клиент запущены в отдельных окнах."
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        9)  # СОХРАНИТЬ ПОЛНЫЙ БЕКАП
            clear
            DATA_SOURCE="$SERVER_DIR/bin/Content.IntegrationTests/data"
            CONFIG_SOURCE="$SERVER_DIR/bin/Content.IntegrationTests/server_config.toml"
            
            if [ ! -d "$DATA_SOURCE" ]; then
                echo "Папка data не найдена. Бекап невозможен."
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            # Генерируем имя папки с датой и временем
            datetime=$(date +"%Y%m%d_%H%M%S")
            BACKUP_FOLDER="$BACKUP_DIR/db_backup_$datetime"
            
            echo "Создаём полный бекап (БД + конфиг)..."
            
            # Создаём папку бекапа
            mkdir -p "$BACKUP_FOLDER"
            
            # Копируем папку data
            echo "  Копируем data..."
            cp -r "$DATA_SOURCE" "$BACKUP_FOLDER/"
            
            # Копируем конфиг, если он существует
            if [ -f "$CONFIG_SOURCE" ]; then
                echo "  Копируем server_config.toml..."
                cp "$CONFIG_SOURCE" "$BACKUP_FOLDER/"
            else
                echo "  Предупреждение: server_config.toml не найден"
            fi
            
            echo "[OK] Полный бекап сохранён: $BACKUP_FOLDER"
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        10) # СОХРАНИТЬ ТОЛЬКО КОНФИГ
            clear
            CONFIG_SOURCE="$SERVER_DIR/bin/Content.IntegrationTests/server_config.toml"
            
            if [ ! -f "$CONFIG_SOURCE" ]; then
                echo "Конфиг не найден. Бекап невозможен."
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            # Генерируем имя папки с датой и временем
            datetime=$(date +"%Y%m%d_%H%M%S")
            BACKUP_FOLDER="$BACKUP_DIR/config_backup_$datetime"
            
            echo "Сохраняем только конфиг..."
            
            # Создаём папку бекапа
            mkdir -p "$BACKUP_FOLDER"
            
            # Копируем только конфиг
            cp "$CONFIG_SOURCE" "$BACKUP_FOLDER/"
            
            echo "[OK] Конфиг сохранён: $BACKUP_FOLDER"
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        11) # ВОССТАНОВИТЬ ИЗ БЕКАПА
            clear
            echo "Доступные бекапы:"
            echo "-----------------------------------------------"
            echo "== Полные бекапы (БД + конфиг) =="
            ls -d "$BACKUP_DIR"/db_backup_* 2>/dev/null | sed 's|.*/||'
            echo
            echo "== Бекапы только конфига =="
            ls -d "$BACKUP_DIR"/config_backup_* 2>/dev/null | sed 's|.*/||'
            if [ $? -ne 0 ]; then
                echo "Бекапов пока нет."
            fi
            echo "-----------------------------------------------"
            echo
            
            read -p "Введите имя папки бекапа (или 0 для отмены): " backup_choice
            if [ "$backup_choice" = "0" ]; then
                continue
            fi
            
            if [ ! -d "$BACKUP_DIR/$backup_choice" ]; then
                echo "Папка не найдена!"
                read -p "Нажмите Enter для продолжения..."
                continue
            fi
            
            echo "Восстанавливаем из $backup_choice..."
            
            # Проверяем тип бекапа
            if [ -d "$BACKUP_DIR/$backup_choice/data" ]; then
                echo "Найден бекап с БД и конфигом..."
                
                if [ -d "$SERVER_DIR/bin/Content.IntegrationTests/data" ]; then
                    echo "Удаляем старую папку data..."
                    rm -rf "$SERVER_DIR/bin/Content.IntegrationTests/data"
                fi
                
                mkdir -p "$SERVER_DIR/bin/Content.IntegrationTests"
                
                echo "Восстанавливаем data..."
                cp -r "$BACKUP_DIR/$backup_choice/data" "$SERVER_DIR/bin/Content.IntegrationTests/"
            fi
            
            if [ -f "$BACKUP_DIR/$backup_choice/server_config.toml" ]; then
                echo "Восстанавливаем server_config.toml..."
                cp "$BACKUP_DIR/$backup_choice/server_config.toml" "$SERVER_DIR/bin/Content.IntegrationTests/"
            fi
            
            echo "[OK] Восстановление завершено"
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        12) # СПИСОК БЕКАПОВ
            clear
            echo "Список бекапов в $BACKUP_DIR:"
            echo "-----------------------------------------------"
            echo "== Полные бекапы (БД + конфиг) =="
            ls -d "$BACKUP_DIR"/db_backup_* 2>/dev/null | sed 's|.*/||'
            echo
            echo "== Бекапы только конфига =="
            ls -d "$BACKUP_DIR"/config_backup_* 2>/dev/null | sed 's|.*/||'
            if [ $? -ne 0 ]; then
                echo "Бекапов пока нет."
            fi
            echo "-----------------------------------------------"
            read -p "Нажмите Enter для продолжения..."
            ;;
            
        0)  # Выход
            echo "Выход..."
            exit 0
            ;;
            
        *)  # Неверный выбор
            echo "Неверный выбор"
            read -p "Нажмите Enter для продолжения..."
            ;;
    esac
done