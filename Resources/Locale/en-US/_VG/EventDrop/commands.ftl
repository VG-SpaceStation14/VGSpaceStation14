# Queue Management Commands
cmd-eventdropadd-desc = Adds an item or preset to the event capsule queue
cmd-eventdropadd-help = drop_add <prototype_id|preset:preset_name> [amount]
cmd-eventdropadd-error-id = Please specify a prototype ID or preset (preset:name)
cmd-eventdropadd-error-prototype = Prototype {$prototype} not found in the database!
cmd-eventdropadd-error-preset = Preset '{$preset}' not found!
cmd-eventdropadd-error-amount = Invalid item amount specified
cmd-eventdropadd-success-item = Added {$amount}x {$prototype}. Total items in queue: {$total}
cmd-eventdropadd-success-preset = Added preset '{$preset}' ({$description}) - {$count} items. Total in queue: {$total}

cmd-eventdropclear-desc = Clears the current event capsule item queue
cmd-eventdropclear-help = drop_clear
cmd-eventdropclear-success = Queue cleared.
cmd-eventdropclear-empty = Queue is already empty.

cmd-eventdropsend-desc = Calls a droppod with prepared items at your position
cmd-eventdropsend-help = drop_send
cmd-eventdropsend-error-empty = Queue is empty! Use drop_add first.
cmd-eventdropsend-success = Capsule sent to coordinates: {$coordinates}. Items: {$count}

# Preset Management Commands
cmd-eventdrop-preset-save-desc = Save current queue as a preset
cmd-eventdrop-preset-save-help = drop_preset_save <preset_id> [description]
cmd-eventdrop-preset-save-error-id = Please specify a preset ID.
cmd-eventdrop-preset-save-error-empty = Queue is empty! Nothing to save.
cmd-eventdrop-preset-save-success = Preset '{$preset}' saved! Items: {$total}, unique: {$unique}
cmd-eventdrop-preset-save-success-desc = Description: {$description}
cmd-eventdrop-preset-save-error-failed = Failed to save preset '{$preset}'.

cmd-eventdrop-preset-load-desc = Load a preset into the queue (clears current queue)
cmd-eventdrop-preset-load-help = drop_preset_load <preset_id>
cmd-eventdrop-preset-load-error-id = Please specify a preset ID.
cmd-eventdrop-preset-load-error-notfound = Preset '{$preset}' not found!
cmd-eventdrop-preset-load-success = Loaded preset '{$preset}': {$count} items
cmd-eventdrop-preset-load-success-desc = Description: {$description}
cmd-eventdrop-preset-load-success-meta = Created by: {$author}, {$date}

cmd-eventdrop-preset-list-desc = Show a list of all available presets
cmd-eventdrop-preset-list-help = drop_preset_list
cmd-eventdrop-preset-list-empty = No saved presets found.
cmd-eventdrop-preset-list-header = === Available Presets ({$count}) ===
cmd-eventdrop-preset-list-item = {$id} - {$total} items ({$unique} unique)
cmd-eventdrop-preset-list-item-desc = {$description}
cmd-eventdrop-preset-list-item-error = {$id} - (load error)

cmd-eventdrop-preset-delete-desc = Delete a preset
cmd-eventdrop-preset-delete-help = drop_preset_delete <preset_id>
cmd-eventdrop-preset-delete-error-id = Please specify a preset ID.
cmd-eventdrop-preset-delete-error-notfound = Preset '{$preset}' not found!
cmd-eventdrop-preset-delete-success = Preset '{$preset}' deleted.
cmd-eventdrop-preset-delete-error-failed = Failed to delete preset '{$preset}'.

# Common Messages
eventdrop-preset-default-description = No description