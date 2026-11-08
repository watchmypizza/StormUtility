// im keeping this here incase stormy decides otherwise sometime to indeed, implement a warning system
// I would've strcutured it like this to not forget:
// 1. Create the command and the database with following entries: UserId (not unique, make it have multiple entries), warn reason, warn severity, warn duration, warn issued, expired
// 2. Insert the database into autostart.cs
// 3. create the command class "moderation"
// 4. insert the warn command into the moderation class and have a helper file talk to the database so the file remains simple "WarnHelper.cs"
// 5. build, backup, sync, restart, test, git
// 6. idk?
