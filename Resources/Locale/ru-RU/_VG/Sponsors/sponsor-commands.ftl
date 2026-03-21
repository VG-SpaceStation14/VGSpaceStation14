cmd-sponsoradd-desc = Добавляет или обновляет спонсора
cmd-sponsoradd-help = Использование: sponsoradd <username> <tier> [expire_days] [notes]
cmd-sponsoradd-invalid-tier = Уровень должен быть 1, 2 или 3
cmd-sponsoradd-user-not-found = Пользователь { $username } не найден в сети
cmd-sponsoradd-success = Спонсор { $username } (Уровень { $tier }) добавлен/обновлен

cmd-sponsorremove-desc = Удаляет спонсора
cmd-sponsorremove-help = Использование: sponsorremove <username>
cmd-sponsorremove-user-not-found = Пользователь { $username } не найден в сети
cmd-sponsorremove-success = Спонсор { $username } удален

cmd-sponsorlist-desc = Показывает список всех спонсоров
cmd-sponsorlist-help = Использование: sponsorlist
cmd-sponsorlist-empty = Спонсоры не найдены
cmd-sponsorlist-header = === Список спонсоров ===
cmd-sponsorlist-never = никогда
cmd-sponsorlist-line = { $username } - Уровень { $tier } - Истекает: { $expire }
cmd-sponsorlist-notes =   Заметки: { $notes }