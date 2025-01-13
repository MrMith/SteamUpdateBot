<p align="center">
    <a href="https://discord.com/oauth2/authorize?client_id=634251158617063424&permissions=2048&scope=bot">
        <img src="https://img.shields.io/badge/-Invite%20Bot!-blue?style=for-the-badge&logo=appveyor?link=http://left&link=https://tinyurl.com/SteamUpdateBot"
            alt="Invite bot to your server!"></a>
</p>

<div>
  <h1 align="center">SteamUpdateBot - Game Update Companion Bot for Discord</h1>  

  <p align="center">This powerful Discord bot, built with <a href="https://github.com/DSharpPlus/DSharpPlus">DSharp+</a> and <a href="https://github.com/SteamRE/SteamKit">SteamKit</a>, bridges the gap between Steam and your server, ensuring you never miss an important game patch, content update, or announcement.</p>  

  <h2>Key Features:</h2>  
  <ul style="display: inline-block; text-align: left;">
    <li><strong>Focused Updates:</strong> By default, it filters out minor changes, delivering only the updates that matter—like new content, bug fixes, and major game improvements.</li>  
    <li><strong>Real-Time Notifications:</strong> Automatically alerts you in Discord whenever a Steam application receives an update.</li>  
    <li><strong>Customizable Alerts:</strong> Configure which applications you want to track, ensuring you only get the updates you care about most.</li>  
  </ul>  
</div>
  
<p align="center">
    <img src="https://i.imgur.com/OLV4GZP.gif" width="300" height="300" />
</p>

<h3 align="center">Use the !feedback bot command to report any issues or suggestions.</h3>

## Discord Commands
| Commands Option        | Value Type      | Description       | Alias | Example |
|   :---:                |     :---:       |    :---:    |    :---:    |  :---:  | 
| help                   | N/A             | Help command that will return these commands | N/A | !help|
| feedback                   | String            | Provide Feedback to the bot developer! | N/A | !feedback Make the bot betterist! |
| status                 | N/A             | Shows some small data about the bot like if steam is down, ping and total updates processed.      | N/A| !status|
| sub**                    | Integer         | Add a Steam Application to the notify list | addapp, subscribeapp, add, subscribe, subapp              | !add 570 |
| del**                    | Integer         | Removes a Steam Application from the notify list | removeapp, delapp, deleteapp, remove, unsubscribe              | !remove 570 |
| list                   | N/A             | Lists all steam applications you have set to notify for this given Channel/DM | N/A | !list |
| showall**                | Boolean         | Should we notify for every single change from store tag changes to content changes (Default False) | all | !showall true |
| public**                 | Boolean         | Should we only show updates that are pushed to the default public branches (Default False) | N/A | !public true |
| branch                 | Integer         | Displays all branches for a steam application and when they were last updated.       | N/A | !branch 570 |
| name                   | Integer         | Gets the steam application name from the steam application ID      | N/A | !branch 570 |
| history                   | Integer         | Lists all of updates in located within the database of this project.     | N/A | !history 570 |
 > Commands denoted with `**` are reserved to anyone with Admin, Manage Channels or All permissions.

## License
[MIT](https://choosealicense.com/licenses/mit/)
