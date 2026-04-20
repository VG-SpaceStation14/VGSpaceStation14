# Команды управления очередью
cmd-eventdropadd-desc = Добавляет предмет или пресет в очередь ивент-капсулы
cmd-eventdropadd-help = drop_add <prototype_id|preset:preset_name> [amount]
cmd-eventdropadd-error-prototype = Прототип {$prototype} не найден в базе!
cmd-eventdropadd-error-preset = Пресет '{$preset}' не найден!
cmd-eventdropadd-error-amount = Указано неверное количество предметов
cmd-eventdropadd-success-item = Добавлено {$amount}x {$prototype}. Всего предметов в очереди: {$total}
cmd-eventdropadd-success-preset = Добавлен пресет '{$preset}' ({$description}) - {$count} предметов. Всего в очереди: {$total}

cmd-eventdropclear-desc = Очищает текущую очередь предметов ивент-капсулы
cmd-eventdropclear-help = drop_clear
cmd-eventdropclear-success = Очередь очищена.
cmd-eventdropclear-empty = Очередь и так пуста.

cmd-eventdropsend-desc = Вызывает дроппод с подготовленными предметами на вашу позицию
cmd-eventdropsend-help = drop_send
cmd-eventdropsend-error-empty = Очередь пуста! Сначала используйте drop_add.
cmd-eventdropsend-success = Капсула отправлена на координаты: {$coordinates}. Предметов: {$count}

# Команды управления пресетами
cmd-eventdrop-preset-save-desc = Сохранить текущую очередь как пресет
cmd-eventdrop-preset-save-help = drop_preset_save <preset_id> [description]
cmd-eventdrop-preset-save-error-empty = Очередь пуста! Нечего сохранять.
cmd-eventdrop-preset-save-error-id = Укажите ID пресета.
cmd-eventdrop-preset-save-success = Пресет '{$preset}' сохранен! Предметов: {$total}, уникальных: {$unique}
cmd-eventdrop-preset-save-success-desc = Описание: {$description}
cmd-eventdrop-preset-save-error-failed = Не удалось сохранить пресет '{$preset}'.

cmd-eventdrop-preset-load-desc = Загрузить пресет в очередь (очищает текущую очередь)
cmd-eventdrop-preset-load-help = drop_preset_load <preset_id>
cmd-eventdrop-preset-load-error-id = Укажите ID пресета.
cmd-eventdrop-preset-load-error-notfound = Пресет '{$preset}' не найден!
cmd-eventdrop-preset-load-success = Загружен пресет '{$preset}': {$count} предметов
cmd-eventdrop-preset-load-success-desc = Описание: {$description}
cmd-eventdrop-preset-load-success-meta = Создан: {$author}, {$date}

cmd-eventdrop-preset-list-desc = Показать список всех доступных пресетов
cmd-eventdrop-preset-list-help = drop_preset_list
cmd-eventdrop-preset-list-empty = Нет сохраненных пресетов.
cmd-eventdrop-preset-list-header = === Доступные пресеты ({$count}) ===
cmd-eventdrop-preset-list-item = {$id} - {$total} предметов ({$unique} уникальных)
cmd-eventdrop-preset-list-item-desc = {$description}
cmd-eventdrop-preset-list-item-error = {$id} - (ошибка загрузки)

cmd-eventdrop-preset-delete-desc = Удалить пресет
cmd-eventdrop-preset-delete-help = drop_preset_delete <preset_id>
cmd-eventdrop-preset-delete-error-id = Укажите ID пресета.
cmd-eventdrop-preset-delete-error-notfound = Пресет '{$preset}' не найден!
cmd-eventdrop-preset-delete-success = Пресет '{$preset}' удален.
cmd-eventdrop-preset-delete-error-failed = Не удалось удалить пресет '{$preset}'.

# Общие сообщения
eventdrop-preset-default-description = Без описания
eventdrop-date-format = {$date}