# Permissions Overview

In this guide we will be explaining **how to use the permission commands correctly** and even **cover a few common questions**! Every command we discuss here can be found in the [Commands List].

## Why do we use the Permissions Commands?

Permissions are very handy at setting who can use what commands in a server. All commands and modules are enabled by default.

Keep in mind, nadeko permission system is separate from discord permissions system - you can't edit which **discord permissions** the user needs to run a command. For example,m  `.kick` and `.voicemute`, they need **Kick** and **Mute Members** server permissions, respectively.

With the permissions system it possible, for example, to restrict who can skip the current song, pick NadekoFlowers or use the NSFW module.

## First Time Setup

To change permissions you **must** meet the following requirements:

**Have Administrator Server Permission.**

**If you are NOT the server owner or an admin, get the role set to `.permrole` (there is no permission role by default).**

## Basics & Hierarchy

The [Commands List] is a great tool which lists **all** available commands, however we'll go over a few of them here.

The permissions system works as a chain. Everytime a command is used, the permissions chain is checked. Starting from the top of it, the command is compared to a rule, if it isn't either allowed or disallowed by that rule it proceeds to check the next rule all the way till it reaches the bottom rule, which allows all commands.

To view this permissions chain, do `.lp`. The rule at the top of the chain takes priority over all rules below it.

If you want to remove a permission from the chain of permissions, do `.rp X` to remove rule number X and similarly, do `.mp X Y` to move rule number X to number Y (moving, not swapping!).

If you use `.verbose true`, the next time a user runs a command which they are not allowed to use, they will see which permission is preventing them.

## Commonly Asked Questions

#### How do I restrict all commands to a single channel?

To allow users to only use commands in a specific text channel, follow these steps:

1. `.asm disable`
    - Disables all modules on the entire server
2. `.acm enable #bot-spammerino`
    - Enables all modules in the #bot-spammerino channel

#### How do I allow only one module to be used in a specific channel?

To allow users to only use commands from a certain module, let's say **gambling**, in a specific text channel, follow these steps:

1. `.acm disable #gamblers-den`
    - Disables all modules in the #gamblers-den channel
2. `.cm Gambling enable #gamblers-den`
    - Enables usage of the Gambling module in the #gamblers-den channel

#### How do I create a music DJ?

To allow users to only see the current song and have a DJ role for queuing follow these steps:

1. `.sm Music disable`
    - Disables music commands for everybody
2. `.sc .nowplaying enable`
    - Enables the "nowplaying" command for everyone
3. `.sc .listqueue enable`
    - Enables the "listqueue" command for everyone
4. `.rm Music enable DJ`
    - Enables all music commands only for the DJ role

#### How do I create a NSFW role?

Say you want to only enable NSFW commands for a specific role, just do the following two steps.

1. `.sm NSFW disable`
    - Disables the NSFW module from being used
2. `.rm NSFW enable Lewd`
    - Enables usage of the NSFW module for the Lewd role

#### How do I disable expressions (formerly custom reactions) from triggering?

If you don't want server or global expressions, just block the module that controls their usage:

1. `.sm ActualExpressions disable`
    - Disables the actual expressions which are added globally, or on the server

**Note**: The `ActualExpressions` module controls the usage of expressions. The `Expressions` module controls commands related to expressions (such as `.expradd`, `.exprli`, `.exprdel`, etc).

#### I've broken permissions and am stuck, can I reset permissions?

Yes, there is a way, in one easy command!

1. `.resetperms`
    - This resets the permission chain back to default

*-- Thanks to @applemac for providing the template for this guide*

[Commands List]: https://nadeko.bot/commands/v3
