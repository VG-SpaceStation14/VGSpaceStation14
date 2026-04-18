### Sponsor commands

cmd-sponsoradd-desc = Adds sponsor status to a player
cmd-sponsoradd-help = sponsoradd <username> <tier> [days] [notes]
cmd-sponsoradd-invalid-tier = Tier must be a number between 1 and 3
cmd-sponsoradd-user-not-found = User '{ $username }' not found or not online
cmd-sponsoradd-success = Successfully added sponsor tier { $tier } to { $username }
cmd-sponsoradd-queued = Player '{ $username }' is offline. Sponsor addition queued and will be applied when they connect.

cmd-sponsorremove-desc = Removes sponsor status from a player
cmd-sponsorremove-help = sponsorremove <username>
cmd-sponsorremove-user-not-found = User '{ $username }' not found
cmd-sponsorremove-success = Successfully removed sponsor status from { $username }
cmd-sponsorremove-queued = Player '{ $username }' is offline. Sponsor removal queued and will be applied when they connect.

cmd-sponsorlist-desc = Lists all sponsors
cmd-sponsorlist-help = sponsorlist
cmd-sponsorlist-empty = No sponsors found
cmd-sponsorlist-header = === Sponsors List ===
cmd-sponsorlist-line = { $username } - Tier { $tier } - Expires: { $expire }
cmd-sponsorlist-notes =   Notes: { $notes }
cmd-sponsorlist-never = Never

cmd-sponsoraddloadout-desc = Adds a custom loadout to a sponsor
cmd-sponsoraddloadout-help = sponsoraddloadout <username> <loadoutId>
cmd-sponsoraddloadout-loadout-not-found = Loadout prototype '{ $loadoutId }' not found!
cmd-sponsoraddloadout-user-not-found = User '{ $username }' not found!
cmd-sponsoraddloadout-success = Added loadout '{ $loadoutId }' to { $username }
cmd-sponsoraddloadout-queued = Player '{ $username }' is offline. Loadout addition queued and will be applied when they connect.

cmd-sponsorremoveloadout-desc = Removes a custom loadout from a sponsor
cmd-sponsorremoveloadout-help = sponsorremoveloadout <username> <loadoutId>
cmd-sponsorremoveloadout-user-not-found = User '{ $username }' not found!
cmd-sponsorremoveloadout-success = Removed loadout '{ $loadoutId }' from { $username }
cmd-sponsorremoveloadout-queued = Player '{ $username }' is offline. Loadout removal queued and will be applied when they connect.