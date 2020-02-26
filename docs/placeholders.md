## Placeholders

Placeholders are used in Quotes, Expressions, Greet/Bye messages, playing statuses, and a few other places.

They can be used to make the message more user friendly, generate random numbers, or post basic information about relevant user, channel, or server.

Some features have their own specific placeholders which are noted in that feature's command help. Some placeholders are not available in certain features because they don't make sense there.

### List of Placeholders

**If you're using placeholders in embeds, don't use %user.mention% and %bot.mention% in titles, footers and field names. They will not show properly.**

**Bot placeholders**

- `%bot.name%` - Bot username
- `%bot.mention%` - Bot mention (clickable)
- `%bot.fullname%` - Bot username#discriminator
- `%bot.time%` - Bot time (usually the time of the server it's hosted on)
- `%bot.discrim%` - Bot's discriminator
- `%bot.id%` - Bot's user ID
- `%bot.avatar%` - Bot's avatar url
- `%bot.shardid%` or `%shard.id%` - The shard that's currently servicing your server
- `%bot.shards%` - Amount of shards that are currently up

**Server placeholders**

- `%prefix%` - Shows the bot's prefix on the server
- `%server.id%` - Server ID
- `%server.name%` or `%server%` - Server name
- `%server.members%` - Member count
- `%server.time%` - Server time (requires `.timezone` to be set)

**Channel placeholders**

- `%channel.mention%` - Channel mention (clickable)
- `%channel.name%` - Channel name
- `%channel.id%` - Channel ID
- `%channel.created%` - Channel creation date
- `%channel.created_time%` - Channel creation date (only local time)
- `%channel.created_date%` - Channel creation date (without local time)
- `%channel.nsfw%` - Returns either `True` or `False`, depending on if the channel is designated as NSFW using Discord
- `%channel.topic%` - Channel topic

**User placeholders**

- `%user.mention%` - User mention
- `%user.fullname%` - Username#discriminator
- `%user.name%` or `%user%` - Username
- `%user.discrim%` - Discriminator
- `%user.avatar%` - User's avatar url
- `%user.id%` - User ID
- `%user.created%` - Account creation date 
- `%user.created_time%` - Account creation time (only local time)
- `%user.created_date%` - Account creation date (without local time)
- `%user.joined_time%` - Account join time (only local time)
- `%user.joined_date%` - Account join date (without local time)

**Bot stats placeholders**

- `%servers%` - Server count bot has joined
- `%users%` - Combined user count on servers the bot has joined

**Miscellaneous placeholders**

- `%rngX-Y%` - Returns a random number between X and Y
- `%stats.servercount%` - The amount of servers serviced by the current shard

![img](https://i.imgur.com/0wZsi1n.png)


