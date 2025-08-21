Scans for existing magazine issues on a website.

Needs an appsettings.json in the base directory of the project to run. Use your own appropriate values.

```
{
  "discordToken": "",
  "serverId": "",
  "generalChannelId": "",
  "emailAddress": "",
  "smtpClient": "smtp.gmail.com",
  "smptPort": "587",
  "emailAddressPassword": "",
  "detailsURL": "", //The one with magazineId and issueId
  "detailsAPIURL": "", //The one with secretId
  "secretId": "",
  "magazineId": 1,
  "localDownloadFolder": "c:/temp/",
  "localLogLocation": "c:/temp/",
  "sendToDiscordBot": "false",
  "sendToEmailAddress": "false",
  "logToTextFile": "true",
  "dataStartIndexValue": "",
  "issueTitleLength": 22
}
