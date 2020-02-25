# Frequently Asked Questions

### 1: How do I get Nadeko to join my server?

- Send Nadeko a Direct Message with `.h` and follow the link or simply [click here](https://invite.nadeko.bot/).  
Only users with **Manage Server permission** can add bots to a server.

### 2: I want to change bot permissions, but it isn't working!

- You must have **Administrator Server permission** or the `.permrole` to be able to use any command from the Permissions module.  
- For more information about permissions, check the [Permissions](http://nadekobot.readthedocs.io/en/latest/Permissions.md) guide.

### 3: I want to enable NSFW on a channel.


- To enable the NSFW module on one channel, you need to mark the channel as NSFW in the channel's settings. [Click here](https://cdn.discordapp.com/attachments/422985724053159946/429510585097650186/nsfwenable2.gif) to learn how.

### 4: How do I get NadekoFlowers/Currency 🌸?


- On public Nadeko, you can get 🌸 by:
    - [Voting](https://discordbots.org/bot/nadeko/vote) for her on discordbots website (once you vote you will start getting 10🌸 per hour for 24h)
    - Keeping an eye for events in `#giveaways` channel on [NadekoLog](https://discord.nadeko.bot) discord server
    - Picking (`.pick`) flowers in [NadekoLog](https://discord.nadeko.bot) `#chat` channel when they appear,   
    - [Donating❤️](Contribute.md)
- If you already have some flowers, but want more, you can gamble them for potential profit with `.betflip`, `.betroll` and other gambling commands (`.cmds gambling`).  

- On **self-hosts** (not public bot), bot owner can set-up timely `.timely`

### 5: I have an issue/bug/suggestion, where do I put it so it gets noticed?

- If you're unsure about something, or have a question, Head on over to [NadekoLog](https://discord.nadeko.bot) and ask for help in the `#help` channel
- If you are **sure** you've found a bug, you can post it in the [issues](https://gitlab.com/Kwoth/nadekobot/issues) section on gitlab
- If you have a suggestion, then check the [suggestions](https://nadeko.bot/suggest) page

### 6: How do I use this command?


- You can see the description and usage of certain commands by using `.h command` i.e. `.h .sm`. Additionally, you can check all commands within a module by typing `.cmds moduleName`.

- Alternatively, you can check out the [the full list of commands](https://nadeko.bot/commands)

### 7: Music isn't working?

- Music is disabled on public Nadeko due to large hosting costs.
- **If you would like music in the meantime, you must [host Nadeko yourself](Guides.md)**. *Read the section on setting up music **carefully***

### 9: I want to change flowers to something else, or make other changes, how?

- You can't change currency sign and other configuration on public bot `@Nadeko#6685`. For that you need to [host Nadeko yourself](Guides.md)

### 10: The .greet and .bye commands don't work, but everything else does!

- Set a greeting message by using `.greetmsg YourMessageHere` and a bye-message by using `.byemsg YourMessageHere`. Don't forget that `.greet` and `.bye` only apply to users joining a server, not coming online/offline. Also, keep in mind that these messages are automatically deleted after 30 seconds of being sent. If you don't want that, disable automatic deletion by setting `.greetdel` and `.byedel` to zero.

### 11: I made an application, but I can't add that new bot to my server, how do I invite it to my server?

- You need to use oauth link to add it to you server, just copy your **CLIENT ID** (that's in the same [Developer page](https://discordapp.com/developers/applications/me) where you brought your token) and replace `12345678` in the link below: **https://discordapp.com/oauth2/authorize?client_id=`12345678`&scope=bot&permissions=66186303**

### 13: My bot has all permissions but it's still saying, "Failed to add roles. Bot has insufficient permissions". How do I fix this?

- Discord has added a few new features and the roles now follow the role hierarchy, which means you need to place your bot's role above every other role your server has to fix the role hierarchy issue. [Here](https://support.discordapp.com/hc/en-us/articles/214836687-Role-Management-101) is a link to Discord's Role Management 101.

- **Please Note:** *The bot can only set/add all roles below its own highest role. It cannot assign it's "highest role" to anyone else.*

### 14: I've broken permissions and am stuck. Can I reset it?

- Yes, there is a way, in one easy command! Just run `.resetperms` and all the permissions you've set through **Permissions Module** will reset.
