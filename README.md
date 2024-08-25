# CSGORollDropWebhookNotifier


[CSGORoll](https://csgoroll.com/r/EGAR) Gem Drop Webhook Notifier
- It will notify you via discord webhook when the team 2x boost, golden drop, or regular drop occurs
- You can set at what percentage it will notify you and how frequently
- No need to login to a steam or CSGORoll account

Use code "Egar" to help support me. 


If you have issues or want any customizations made to the discord notifications such as removing the advertisement, feel free to contact me on discord: TerminatorIsGod


### Checkout my free [CSGORoll auto daily rewards collector](github.com/TerminatorIsGod/CSGORoll-Daily-Rewards-Bot)!


# How to use

1. Download the program from [releases](https://github.com/TerminatorIsGod/CSGORollDropWebhookNotifier/releases)

2. Go to a discord channel in a server where you have permission to create a webhook. Click edit channel -> integrations -> create webhook -> click on new bot -> copy webhook url
<br>

   ![image](https://github.com/user-attachments/assets/c4e428f2-6066-462e-a9da-fe976092f959)

   
   ![image](https://github.com/user-attachments/assets/d6980aee-c4de-4dd3-83ae-6e15d1b5aa04)
   ![image](https://github.com/user-attachments/assets/e07546d4-9ef0-41bc-a68f-43c8a1536282)
   ![image](https://github.com/user-attachments/assets/7613c352-e645-4899-9273-b3a180d4014f)





4. Open the file 'config.egario' using notepad or whatever text editor you prefer.

5. On line 2 replace the URL part with the URL you copied in discord. The first line in the config file sets at what percentage should the program send you a notification in discord. The third line sets how often (in seconds) the program will resend notifications/fix itself if it breaks. For example if this value is set to 60, every minute that the percentage is above the threshold you set it will send a notification in discord.

![image](https://github.com/user-attachments/assets/8aaae8d9-24ce-4582-b339-ac505b09a497)

6. Save the config file and launch the program.
