﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Web.Script.Serialization;

using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;

namespace BGLogPlugin
{
    public class LogParser : IPlugin
    {
        private enum SaveState { Ready, Used };

        private const string CHECK_PLACE_TAG = "tag=PLAYER_LEADERBOARD_PLACE";
        private const string CHECK_PLACE_HERO = "cardId=";
        private const string CHECK_PLACE_VALUE = "value=";

        private SaveState LogState = SaveState.Ready;
        private ActivePlayer TurnState = ActivePlayer.Player;
        private ParamJson LogJson = new ParamJson();

        private List<string> ResultLog = new List<string>();

        private void SetInit()
        {
            LogState = SaveState.Ready;
            TurnState = ActivePlayer.Player;

            LogJson.PlayerID = "";
            LogJson.HeroID = "";
            LogJson.Version = Version;
            LogJson.Placement = 1;
            LogJson.MMR = 0;
            LogJson.LeaderBoard = new List<string>();
            LogJson.UsedCard = new List<string>();
            LogJson.TurnBoard = new List<string>();
            LogJson.TurnCount = 0;

            ResultLog.Clear();
        }

        private void OnTurnStart(ActivePlayer player)
        {
            if (TurnState != player && player == ActivePlayer.Player)
            {
                LogJson.TurnCount++;
                foreach (Entity ent in Core.Game.Player.Board)
                {
                    if (ent.Card.Type == "Minion")
                    {
                        LogJson.TurnBoard.Add(String.Format("{0}@#{1}@#{2}@#{3}", ent.Card.Id, ent.Card.Name, ent.Card.Attack, ent.Card.Health));
                    }
                }
            }
            TurnState = player;
        }

        private void OnGameStart()
        {
            SetInit();
            LogState = SaveState.Used;
        }

        private void OnGameWon()
        {
            Save();
            SetInit();
        }

        private void OnGameLost()
        {
            Save();
            SetInit();
        }

        private void OnGameEnd()
        {
            Save();
            SetInit();
        }

        private void Save()
        {
            if (LogState == SaveState.Used && Core.Game.CurrentGameMode == GameMode.Battlegrounds)
            {
                LogJson.PlayerID = Core.Game.Player.Name;

                /*
                //LogJson.MMR = Core.Game.BattlegroundsRatingInfo.PrevRating.GetValueOrDefault();
                try
                {
                    LogJson.UsedCard.Add(String.Format("Rating: {0}", Core.Game.BattlegroundsRatingInfo.PrevRating));

                }
                catch(Exception ex)
                {
                    LogJson.UsedCard.Add(ex.Message);
                }
                finally
                {
                    LogJson.UsedCard.Add("finally");
                }
                */

                foreach (Entity ent in Core.Game.Player.Board)
                {
                    if (ent.Card.Type == "Minion")
                    {
                        LogJson.LeaderBoard.Add(String.Format("{0}@#{1}@#{2}@#{3}", ent.Card.Id, ent.Card.Name, ent.Card.Attack, ent.Card.Health));
                    }
                }
                JavaScriptSerializer jSer = new JavaScriptSerializer();
                ResultLog.Add(jSer.Serialize(LogJson));
                FileHelper.Write(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Result.txt"), ResultLog);

                LogJson.UsedCard.Clear();
                LogJson.LeaderBoard.Clear();
                LogJson.TurnBoard.Clear();
            }
        }

        //  Save placement
        private void DoInPowerLog(string line)
        {
            if (LogJson.HeroID != ""
                && line.IndexOf(CHECK_PLACE_TAG) > 0
                && line.IndexOf(CHECK_PLACE_HERO + LogJson.HeroID) > 0
                && line.IndexOf(CHECK_PLACE_VALUE) > 0)
            {
                int idx = line.IndexOf(CHECK_PLACE_VALUE);
                LogJson.Placement = Int32.Parse(line.Substring(idx + CHECK_PLACE_VALUE.Length, 1));
            }
        }

        //  Save hero
        private void OnPlayerPlay(Card card)
        {
            if (card.Type == "Hero")
            {
                LogJson.HeroID = card.Id;
            }
            else if (card.Type == "Minion")
            {
                LogJson.UsedCard.Add(String.Format("{0}@#{1}@#{2}@#{3}@#{4}", LogJson.TurnCount, card.Id, card.Name, card.Attack, card.Health));
            }
        }

        public void OnLoad()
        {
            LogEvents.OnPowerLogLine.Add(DoInPowerLog);

            GameEvents.OnGameStart.Add(OnGameStart);
            GameEvents.OnTurnStart.Add(OnTurnStart);

            if (TurnState == ActivePlayer.Player)
            {
                GameEvents.OnPlayerPlay.Add(OnPlayerPlay);
            }

            GameEvents.OnGameWon.Add(OnGameWon);
            GameEvents.OnGameLost.Add(OnGameLost);
            GameEvents.OnGameEnd.Add(OnGameEnd);
        }
        public void OnButtonPress() { }
        public void OnUnload() { }
        public void OnUpdate() { }
        public string Name => "Save Battleground Log";
        public string Description => "Upload Battleground Log to <Battleground-Lab Server>";
        public string ButtonText => "DO NOT PUSH THIS BUTTON!";
        public string Author => "shyuniz";
        public Version Version => new Version(1, 0, 0);
        public MenuItem MenuItem => null;
    }
}