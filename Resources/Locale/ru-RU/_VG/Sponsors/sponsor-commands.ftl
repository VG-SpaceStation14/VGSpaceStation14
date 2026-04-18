### Sponsor commands

cmd-sponsoradd-desc = Добавляет статус спонсора игроку
cmd-sponsoradd-help = sponsoradd <ник> <уровень> [дней] [заметки]
cmd-sponsoradd-invalid-tier = Уровень должен быть числом от 1 до 3
cmd-sponsoradd-user-not-found = Игрок '{ $username }' не найден или не в сети
cmd-sponsoradd-success = Игроку { $username } успешно выдан { $tier } уровень спонсора
cmd-sponsoradd-queued = Игрок '{ $username }' не в сети. Выдача спонсорки будет применена при его заходе.

cmd-sponsorremove-desc = Снимает статус спонсора с игрока
cmd-sponsorremove-help = sponsorremove <ник>
cmd-sponsorremove-user-not-found = Игрок '{ $username }' не найден
cmd-sponsorremove-success = Статус спонсора успешно снят с { $username }
cmd-sponsorremove-queued = Игрок '{ $username }' не в сети. Снятие спонсорки будет применено при его заходе.

cmd-sponsorlist-desc = Показывает список всех спонсоров
cmd-sponsorlist-help = sponsorlist
cmd-sponsorlist-empty = Спонсоры не найдены
cmd-sponsorlist-header = === Список спонсоров ===
cmd-sponsorlist-line = { $username } - Уровень { $tier } - Истекает: { $expire }
cmd-sponsorlist-notes =   Заметки: { $notes }
cmd-sponsorlist-never = Никогда

cmd-sponsoraddloadout-desc = Добавляет кастомный лодаут спонсору
cmd-sponsoraddloadout-help = sponsoraddloadout <ник> <id_лодаута>
cmd-sponsoraddloadout-loadout-not-found = Прототип лодаута '{ $loadoutId }' не найден!
cmd-sponsoraddloadout-user-not-found = Игрок '{ $username }' не найден!
cmd-sponsoraddloadout-success = Лодаут '{ $loadoutId }' добавлен игроку { $username }
cmd-sponsoraddloadout-queued = Игрок '{ $username }' не в сети. Добавление лодаута будет применено при его заходе.

cmd-sponsorremoveloadout-desc = Удаляет кастомный лодаут у спонсора
cmd-sponsorremoveloadout-help = sponsorremoveloadout <ник> <id_лодаута>
cmd-sponsorremoveloadout-user-not-found = Игрок '{ $username }' не найден!
cmd-sponsorremoveloadout-success = Лодаут '{ $loadoutId }' удалён у игрока { $username }
cmd-sponsorremoveloadout-queued = Игрок '{ $username }' не в сети. Удаление лодаута будет применено при его заходе.