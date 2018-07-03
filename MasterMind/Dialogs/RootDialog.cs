using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MasterMind.Dialogs
{
    [LuisModel("985dfa83-0508-4ad4-927e-c0fdc5d1aea0", "54ba7ab28b1542a4b4caeb17456e20c5")]
    [Serializable]
    public class RootDialog : LuisDialog<object>
    {
        private IEnumerable<string> matches_list = new List<string>();
        String secretWord = "";
        public string[] words = new string[8015];
        public string[] uwords = new string[21];
        public String mode = "";
        int chance = 1;
        int score = 100;
        int game = 0;

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }


        [LuisIntent("Greetings")]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hello... Welcome ");
            await context.PostAsync("Wanna know about me. *Either type **About**  or **start the game***");
        }

        [LuisIntent("About")]
        public async Task About(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("It is a mind Game.");
            await Task.Delay(1000);
            await context.PostAsync(" In this I will imagine a word.");
            await Task.Delay(1000);
            await context.PostAsync("And you have to  guess that word ");
            await Task.Delay(1000);
            await context.PostAsync(" **Note :** Don't imagine  proper nouns and words with repeated characters");
            await Task.Delay(1000);
            await context.PostAsync(" **Note :** If u want to quit the game, type **either numbers or special characters**");
            await Task.Delay(1000);
            await context.PostAsync("*To start the game type **Start***");
        }

        [LuisIntent("GameStart")]
        private async Task ChooseMode(IDialogContext context, LuisResult result)
        {
                var options = new Selection[] { Selection.Easy, Selection.Difficult };
                var descriptions = new string[] { "Easy", "Difficult" };
                PromptDialog.Choice<Selection>(context, ModeSelection, options, "Choose mode", descriptions: descriptions);
        }

        private enum Selection
        {
            Easy, Difficult
        }
        
        private async Task ResumeAfterIntrest(IDialogContext context, IAwaitable<bool> result)
        {
            var confirmation = await result;
            if (confirmation)
            {

                var options = new Selection[] { Selection.Easy, Selection.Difficult };
                var descriptions = new string[] { "Easy", "Difficult" };
                PromptDialog.Choice<Selection>(context, ModeSelection, options, "Choose mode ", descriptions: descriptions);
            }
            else
            {
                await context.PostAsync("**See u soon :)** ");
                var cardMsg = context.MakeMessage();
                var attachment = BotWelcomeCard2("Have a nice day !!!", "");
                cardMsg.Attachments.Add(attachment);
                await context.PostAsync(cardMsg);
                context.Done(this);
            }
        }

        private enum confirm
        {
            Start, Quit
        }

        private async Task ModeSelection(IDialogContext context, IAwaitable<Selection> result)
        {
            var selection = await result;
            String msg = "";
            chance = 0;
            Array.Clear(uwords, 0, 21);
            score = 100;
            if (selection.ToString().ToLower().Equals("easy"))
            {
                mode = "Easy";
                msg = "I will imagine a 4 letter word, start guessing ";
                
            }
            else
            {
                mode = "Difficult";
                msg = "I will imagine a 5 letter word, start guessing ";
                
            }
            var options = new confirm[] { confirm.Start, confirm.Quit };
            var descriptions = new string[] { "Start", "Quit" };
            PromptDialog.Choice<confirm>(context, StartGame, options, msg, descriptions: descriptions);
        }




        private async Task StartGame(IDialogContext context, IAwaitable<confirm> result)
        {
            var selection = await result;
            if (selection.ToString().ToLower().Equals("start")) {
                game = 1;
                ReadDataFromFile rj = new ReadDataFromFile();
                int count = 0;
                if (mode.Equals("Easy"))
                    count = rj.ReadWordsFromFile(words, "output.txt");
                else
                {
                    count = rj.ReadWordsFromFile(words, "fiveletterwords.txt");
                }
                await GuessBot(count, context);
            }
            else
            {
                await context.PostAsync("**see u soon :)**");
                var cardMsg = context.MakeMessage();
                var attachment = BotWelcomeCard2("Have a nice day !!!", "");
                cardMsg.Attachments.Add(attachment);
                await context.PostAsync(cardMsg);
                
            }
        }

        public async Task GuessBot(int count, IDialogContext context)
        {
            Random r = new Random();
            int guessX = (int)(r.Next(count));
             secretWord = words[guessX];
            await context.PostAsync("you have **20 chances** to guess");
            context.Wait(MessageReceivedAsync1);
        }


        private enum playAgain
        {
            Yes, No
        }


        private async Task MessageReceivedAsync1(IDialogContext context, IAwaitable<object> result)
        {
            var selection = await result as Activity;
            string guessedWord = selection.Text;
            guessedWord = guessedWord.ToLower();
            var regexp = new Regex("^[a-zA-z]*$");
            String distinctString = String.Join("", guessedWord.Distinct());
            if (!regexp.IsMatch(guessedWord)  && game == 1)
            {
                game = 0;
                await context.PostAsync("*Here is the Secret word*  " +"**" + secretWord+"**");
                PromptDialog.Confirm(context, ResumeAfterIntrest, "Are you intrested to play Again?");
                context.Done(this);
            }
            else if(game == 1)
            {
                if (guessedWord.Length != secretWord.Length && game == 1)
                {
                    await context.PostAsync("You should have to guess a " + secretWord.Length + " letter word");
                    context.Wait(MessageReceivedAsync1);
                }
                else
                {
                    if (uwords.Contains(guessedWord) && game == 1)
                    {
                        await context.PostAsync("**You already guessed this word**");
                    }
                    else
                    {
                        uwords[chance++] = guessedWord;
                        score = score - 5;
                        char[] distletr = distinctString.ToCharArray();
                        int matches = 0;
                        //await context.PostAsync(secretWord);
                        string st = "";
                        foreach (char c in distletr)
                        {
                            if (secretWord.Contains(c))
                            {
                                matches++;
                                st += c + " ";
                            }
                        }
                        if (guessedWord.Equals(secretWord))
                        {
                            var cardMsg = context.MakeMessage();
                            score = score + 5;
                            var attachment = BotWelcomeCard("You Won !!!", "Your score is " + score);
                            cardMsg.Attachments.Add(attachment);
                            await context.PostAsync(cardMsg);
                            await Task.Delay(3000);
                            PromptDialog.Confirm(context, ResumeAfterIntrest, "Are you intrested to play Again?");
                        }
                        else
                        {
                            if (chance > 15 && chance < 20)
                            {
                                await context.PostAsync("You are left with " + (20 - chance) + " chance(s)");
                                //await context.PostAsync(chance + " chances");
                            }
                            if (matches == secretWord.Length && chance != 20)
                            {
                                
                                await context.PostAsync("All the characters are correct but not the word");
                               
                            }
                            else if(chance != 20)
                            {
                                if (matches != 0 && chance < 20)
                                {
                                    var message = context.MakeMessage();
                                    Attachment attachment = null;
                                    if (matches == 1)
                                        attachment = GetThumbnailCard("Matched Characters : " + st, "C:\\Users\\priya\\Documents\\MasterMind\\MasterMind\\emoji.jpg");
                                    else
                                        attachment = GetThumbnailCard("Matched Characters : " + st, "C:\\Users\\priya\\Documents\\MasterMind\\MasterMind\\clap.jpg");
                                    message.Attachments.Add(attachment);
                                    await context.PostAsync(message);
                                }
                                else
                                {
                                    var message = context.MakeMessage();
                                    Attachment attachment = null;
                                    attachment = GetThumbnailCard("Zero Characters Matched ", "C:\\Users\\priya\\Documents\\MasterMind\\MasterMind\\sad.jpg");
                                    message.Attachments.Add(attachment);
                                    await context.PostAsync(message);
                                }
                            }
                            //context.Wait(MessageReceived);
                            context.Wait(MessageReceivedAsync1);
                        }
                    }
                }
            }
            if (chance >= 20)
            {
                
                var cardMsg = context.MakeMessage();
                //score = score + 5;
                var attachment = BotWelcomeCard1("You Lose !!!","The word is "+ secretWord);
  
                cardMsg.Attachments.Add(attachment);
                await context.PostAsync(cardMsg);
                await Task.Delay(3000);
                chance = 0;
                game = 0;
                //PromptDialog.Confirm(context, ResumeAfterIntrest, "Are you intrested to play Again?");
                
                await context.PostAsync("**Are you interested to play again? Then type *Start the game***");

                context.Done(true);
            }
        }

        private static Attachment GetThumbnailCard(string responseFromQNAMaker, string userQuery)
        {
            var heroCard = new ThumbnailCard
            {
                Title = responseFromQNAMaker,
                Images = new List<CardImage> { new CardImage(userQuery) },
            };
            return heroCard.ToAttachment();
        }



        private static Attachment BotWelcomeCard(string responseFromQNAMaker, string userQuery)
        {
            var heroCard = new HeroCard
            {
                Title = userQuery,
                Images = new List<CardImage> { new CardImage("C:\\Users\\priya\\Documents\\MasterMind\\MasterMind\\congratulation.gif") },
            };
            return heroCard.ToAttachment();
        }

        private static Attachment BotWelcomeCard1(string responseFromQNAMaker, string userQuery)
        {
            var heroCard = new HeroCard
            {
                Title = userQuery,
                Images = new List<CardImage> { new CardImage("C:\\Users\\priya\\Documents\\MasterMind\\MasterMind\\gameover.jpg") },
            };
            return heroCard.ToAttachment();
        }

        private static Attachment BotWelcomeCard2(string responseFromQNAMaker, string userQuery)
        {
            var heroCard = new HeroCard
            {
                Title = userQuery,
                Images = new List<CardImage> { new CardImage("C:\\Users\\priya\\Documents\\MasterMind\\MasterMind\\niceDay.gif") },
            };
            return heroCard.ToAttachment();
        }

    }
}
