#!/usr/bin/env python3
# Installs git hooks, updates them, updates submodules, that kind of thing.

import subprocess
import sys
import os
import shutil
import time
from pathlib import Path
from typing import List

SOLUTION_PATH = Path("..") / "SpaceStation14.sln"
# If this doesn't match the saved version we overwrite them all.
CURRENT_HOOKS_VERSION = "2"
QUIET = len(sys.argv) == 2 and sys.argv[1] == "--quiet"

# ВАШ ФОРК ДВИЖКА
YOUR_ENGINE_FORK = "https://github.com/VG-SpaceStation14/RobustToolbox.git"


def run_command(command: List[str], capture: bool = False) -> subprocess.CompletedProcess:
    """
    Runs a command with pretty output.
    """
    text = ' '.join(command)
    if not QUIET:
        print("$ {}".format(text))

    sys.stdout.flush()

    completed = None

    if capture:
        completed = subprocess.run(command, cwd="..", stdout=subprocess.PIPE)
    else:
        completed = subprocess.run(command, cwd="..")

    if completed.returncode != 0:
        print("Error: command exited with code {}!".format(completed.returncode))

    return completed


def update_submodules():
    """
    Updates all submodules, forcing use of your engine fork.
    """

    if ('GITHUB_ACTIONS' in os.environ):
        return

    if os.path.isfile("DISABLE_SUBMODULE_AUTOUPDATE"):
        return

    if shutil.which("git") is None:
        raise FileNotFoundError("git not found in PATH")

    # Принудительно устанавливаем URL подмодуля на ваш форк
    try:
        # Проверяем существует ли подмодуль
        result = subprocess.run(
            ["git", "config", "--file", ".gitmodules", "--get", "submodule.RobustToolbox.url"],
            cwd="..",
            capture_output=True,
            text=True
        )
        
        if result.returncode == 0:
            current_url = result.stdout.strip()
            if current_url != YOUR_ENGINE_FORK:
                print(f"Меняем URL подмодуля с {current_url} на {YOUR_ENGINE_FORK}")
                
                # Меняем URL в .gitmodules
                subprocess.run(
                    ["git", "config", "--file", ".gitmodules", "submodule.RobustToolbox.url", YOUR_ENGINE_FORK],
                    cwd="..",
                    check=True
                )
                
                # Синхронизируем с локальной конфигурацией
                subprocess.run(["git", "submodule", "sync"], cwd="..", check=True)
                
                # Переинициализируем подмодуль
                subprocess.run(["git", "submodule", "update", "--init", "--force"], cwd="..", check=True)
                
                print("Подмодуль успешно переключен на ваш форк!")
        else:
            # Если подмодуль не настроен, добавляем его
            print("Подмодуль RobustToolbox не найден, добавляем ваш форк...")
            subprocess.run(
                ["git", "submodule", "add", YOUR_ENGINE_FORK, "RobustToolbox"],
                cwd="..",
                check=True
            )
            
    except Exception as e:
        print(f"Предупреждение при настройке подмодуля: {e}")
        # Продолжаем выполнение - стандартное обновление подмодулей

    # Стандартное обновление подмодулей
    run_command(["git", "submodule", "update", "--init", "--recursive"])


def install_hooks():
    """
    Installs the necessary git hooks into .git/hooks.
    """

    # Read version file.
    if os.path.isfile("INSTALLED_HOOKS_VERSION"):
        with open("INSTALLED_HOOKS_VERSION", "r") as f:
            if f.read() == CURRENT_HOOKS_VERSION:
                if not QUIET:
                    print("No hooks change detected.")
                return

    with open("INSTALLED_HOOKS_VERSION", "w") as f:
        f.write(CURRENT_HOOKS_VERSION)

    print("Hooks need updating.")

    hooks_target_dir = Path("..")/".git"/"hooks"
    hooks_source_dir = Path("hooks")

    # Clear entire tree since we need to kill deleted files too.
    for filename in os.listdir(str(hooks_target_dir)):
        os.remove(str(hooks_target_dir/filename))

    for filename in os.listdir(str(hooks_source_dir)):
        print("Copying hook {}".format(filename))
        shutil.copy2(str(hooks_source_dir/filename),
                        str(hooks_target_dir/filename))


def reset_solution():
    """
    Force VS to think the solution has been changed to prompt the user to reload it, thus fixing any load errors.
    """

    with SOLUTION_PATH.open("r") as f:
        content = f.read()

    with SOLUTION_PATH.open("w") as f:
        f.write(content)

def check_for_zip_download():
    # Check if .git exists,
    cur_dir = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
    if not os.path.isdir(os.path.join(cur_dir, ".git")):
        print("It appears that you downloaded this repository directly from GitHub. (Using the .zip download option) \n"
              "When downloading straight from GitHub, it leaves out important information that git needs to function. "
              "Such as information to download the engine or even the ability to even be able to create contributions. \n"
              "Please read and follow https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html \n"
              "If you just want a Sandbox Server, you are following the wrong guide! You can download a premade server following the instructions here:"
              "https://docs.spacestation14.com/en/general-development/setup/server-hosting-tutorial.html \n"
              "Closing automatically in 30 seconds.")
        time.sleep(30)
        exit(1)

if __name__ == '__main__':
    check_for_zip_download()
    install_hooks()
    update_submodules()