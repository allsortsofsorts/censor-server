# CensorServer

# For D
If you a bot was setup and you want to send it commands you should just need to be in a server with the bot and it will read all messages but only respond to ones with !
If you wanted to have your own server with bots that you give out you could as well. Follow the For S steps, be wary that giving out the bot's token will allow anyone to read the messages the bot can see. You should issue a new token after you are done with the bot if you go this route so it doesn't get misused.

## Commands

### !test

This causes the bot to respond, used to tell if the bot is setup and can see messages.

### !shutdown

This causes the bot to shutdown the machine that is running the bot.

### !pink times=<number>  active_duration=<number seconds>  inactive_duration=<number seconds> opacity=<number 0-255> color_r=<number 0-255> color_g=<number 0-255> color_b=<number 0-255>
!pink times=1  active_duration=3  inactive_duration=0 opacity=255 color_r=219 color_g=136 color_b=154

This causes the bot to overlay a screen that is the specified color. 
Opacity of 255 is solid, opacity of 0 is see through, values between that are semi transparent.
times is how many times it should cycle on and then off
active_duration is how long an on cycle lasts
inactive_duration is how long an off cycle lasts

### !image-overlay mode=<string> active_duration=<number in seconds> opacity=<number 0-255>
image-overlay mode=maximize active_duration=10 opacity=255

This causes the bot to overlay an image for the specified amount of time. Gifs and most image formats are supported
Opacity of 255 is solid, opacity of 0 is see through, values between that are semi transparent.
Valid modes: 
maximize, makes the image as big as will fit on the monitor without distortion
stretch, makes the image  fill the monitor but will distort the image
no mode specified, will draw the image on some random location on the monitor.

# For S
## Setup a Discord Bot

In order to use the additional discord functionality you will need a Discord Token. You can use your own user token, but that is against Discord TOS, so this will describe a proper bot setup instead.

### Setup a Server for the bot
1. Go to https://discord.com/channels/@me
1. Click the + in the left menu
1. Click Create My Own
1. Pick anything here
1. Name your server something with your username in it.

### Setup the bot
1. Go to https://discord.com/developers/applications
1. Click New Application in the top right corner - Name it something similiar to your username but append -bot to it.
1. Click Bot in the left menu
1. Toggle on Message Content Intent on the right menu.
1. Click OAuth2 in the left Menu
1. Click URL Generator in the left Menu
1. Click bot under the scopes in the right side of the page
1. Click Send Messages and Read Message History under bot permissions >> text permissions 
1. Copy the Generated URL at the bottom of the page and open it
1. Select the Server you created earlier and add the bot to it.

### Consume the bot token
1. Go to https://discord.com/developers/applications
1. Click Bot in the left menu
1. Uncheck Public Bot
1. Click Reset Token, copy this token down. DO NOT SHARE IT WITH ANYONE.
1. In the same folder that you have BetaCensor.Server.exe make a new file named token.txt, paste the token there.
1. Start the server, if everything is correct you should see your bot is online.
1. You can confirm the bot is working by messaging "!test" if its good the bot will respond.

### Invite whoever you want to talk to the bot to your server