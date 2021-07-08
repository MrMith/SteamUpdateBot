# SteamUpdateBot

This is a discord bot that connects to steam (Via SteamKit2) and listens for any steam application updates so a Text Channel/DM message can be sent to notify people.

## All Commands.
| Commands Option        | Value Type      | Description       | Alias |
|   :---:                |     :---:       |    :---:    |    :---:    |
| help                   | N/A             | Help command that will return these commands | N/A |
| status                 | N/A             | Shows some small data about the bot like if steam is down, ping and total updates processed.      | N/A|
| sub                    | Integer         | Add a Steam Application to the notify list | addapp, subscribeapp, add, subscribe, subapp              |
| del                    | Integer         | Removes a Steam Application from the notify list | removeapp, delapp, deleteapp, remove, unsubscribe              |
| list                   | N/A             | Lists all steam applications you have set to notify for this given Channel/DM | N/A |
| showall                | Boolean         | Should we notify for every single change from store tag changes to content changes (Default False) | all |
| public                 | Boolean         | Should we only show updates that are pushed to the default public branches (Default False) | N/A |
| branch                 | Integer         | Displays all branches for a steam application and when they were last updated.       | N/A |
| name                   | Integer         | Gets the steam application name from the steam application ID      | N/A |
| debug                  | Boolean         | *[WARNING]* Every steam update goes through as if you were subscribed to it.       | N/A |
