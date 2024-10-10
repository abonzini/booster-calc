﻿using System;
using System.IO;

const int MAX_NUMBER_OF_SAME_TYPE = 3;
const int STARTER_TYPE_WEIGHT = 2;

void print_string(string str, ConsoleColor fg_color, ConsoleColor bg_color)
{
    Console.ForegroundColor = fg_color;
    Console.BackgroundColor = bg_color;
    Console.WriteLine(str);
    Console.ResetColor();
}

int seed = Guid.NewGuid().GetHashCode();
Random rng = new Random(seed); // rng
print_string($"For debug purposes, random seed is {seed}", ConsoleColor.Yellow, ConsoleColor.Black);

print_string("HIGO AUTOMATED BOOSTER PACK SELECTOR", ConsoleColor.Green, ConsoleColor.Black);

Console.WriteLine("Loading Dex");
MonData dex = new MonData();

string[] picks_file = File.ReadAllLines("./Picks.csv");

bool picks_succesful = false;
List<Player> players = null;
while (!picks_succesful)
{
    print_string("--------- Start of Booster Selection Try ---------", ConsoleColor.Black, ConsoleColor.White);
    Console.WriteLine("Initialising Booster Pack Pools");
    PackPools packs = new PackPools();
    packs.DebugVerifyPacksMons(dex);
    // Initialised pack pools
    Console.WriteLine("Parsing players and picks");
    players = new List<Player>(); // Check player and picks
    foreach (string pick_line in picks_file)
    {
        string[] fields = pick_line.Split(',');
        Player new_player = new Player(fields[0]);
        string starter = fields[1].ToLower();
        Tuple<string, string> starter_types = dex.GetTypes(starter);
        new_player.type_counter.Add(starter_types.Item1, STARTER_TYPE_WEIGHT); // Add starter types to counter
        if(starter_types.Item2 != "")
        {
            new_player.type_counter.Add(starter_types.Item2, STARTER_TYPE_WEIGHT);
        }
        for(int i = 2; i < fields.Length; i+=2) // Add rest of data as booster packs
        {
            Tuple<string, int> next_pack = new Tuple<string, int>(fields[i], int.Parse(fields[i+1]));
            new_player.chosen_packs.Add(next_pack);
        }
        players.Add(new_player);
    }
    Console.WriteLine("\t- All players loaded");
    // By this step, all players have been loaded

    Console.WriteLine("Booster selection begins!");
    picks_succesful = true; // If nothing breaks, we won!
    bool finished;
    int player = 0;
    int booster = 0;
    int direction = 1;
    do // Snake draft, one by one
    {
        finished = true; // if state doesn't advance nor break, means all players picked succesfully
        if (booster < players[player].chosen_packs.Count) // if player still has boosters to choose
        {
            Console.WriteLine($"\t- Pick #{booster + 1} for {players[player].name}: {players[player].chosen_packs[booster]}");
            players[player].pack_results.Add(new List<string>()); // Will contain results for next booster
            List<string> pack = packs.GetPackMons(players[player].chosen_packs[booster].Item1); // Get mons for desired booster!
            int picks_per_pack = players[player].chosen_packs[booster].Item2; // How many pulls of this pack
            pack = pack.OrderBy(x => rng.Next()).ToList(); // RANDOMIZE!
            // Now, pick the next (2), but skip if types are incompatible, and definitely skip if booster ran out
            int obtained_mons = 0; // Mons I retrieved succesfulyl from booster
            for(int booster_next = 0; booster_next < pack.Count; booster_next++) // Navigate the whole thing
            {
                bool pick_ok = true;
                Console.WriteLine($"\t\t- Chosen {pack[booster_next]}");
                Tuple<string, string> booster_mon_types = dex.GetTypes(pack[booster_next].ToLower());
                // Check first type!
                if (players[player].type_counter.ContainsKey(booster_mon_types.Item1))
                {
                    if (players[player].type_counter[booster_mon_types.Item1] >= MAX_NUMBER_OF_SAME_TYPE)
                    {
                        print_string($"\t\t\t- Type limit exceeded: {booster_mon_types.Item1}", ConsoleColor.Magenta, ConsoleColor.Black);
                        pick_ok = false; // this type exceeded
                    }
                } // ok for first type, now check second type!
                if (booster_mon_types.Item2 != "")
                {
                    if (pick_ok && players[player].type_counter.ContainsKey(booster_mon_types.Item2))
                    {
                        if (players[player].type_counter[booster_mon_types.Item2] >= MAX_NUMBER_OF_SAME_TYPE)
                        {
                            pick_ok = false; // this type exceeded
                        }
                    }
                } // If ok, then types are all right
                if(!pick_ok) // This mon won't go
                {
                    print_string($"\t\t- {pack[booster_next]} discarded, type limit exceeded", ConsoleColor.Magenta, ConsoleColor.Black);
                }
                else
                {
                    players[player].pack_results[booster].Add(pack[booster_next]); // Add mon to options
                    if (!players[player].type_counter.ContainsKey(booster_mon_types.Item1)) // Increase type to counter
                    {
                        players[player].type_counter.Add(booster_mon_types.Item1, 1);
                    }
                    else players[player].type_counter[booster_mon_types.Item1]++;
                    // If 2nd type, also add
                    if (booster_mon_types.Item2 != "")
                    {
                        if (booster_mon_types.Item2 != "" && !players[player].type_counter.ContainsKey(booster_mon_types.Item2)) // Increase type to counter
                        {
                            players[player].type_counter.Add(booster_mon_types.Item2, 1);
                        }
                        else players[player].type_counter[booster_mon_types.Item2]++;
                    }
                    obtained_mons++; // Get this mon, defo
                    if(obtained_mons == picks_per_pack)
                    {
                        finished = false; // Means that booster selection advanced state so let's continue...
                        break; // Finished with booster, onwards...
                    }
                }
            }
            // Booster selection complete, now to verify if all good
            if(obtained_mons != picks_per_pack) // If not managed to pick (2), means booster ran out, unfortunately need to restart
            {
                print_string($"\t- Player {players[player].name} ran out of possible boosters or available picks, need to start over", ConsoleColor.Red, ConsoleColor.White);
                picks_succesful = false;
                break;
            }
            // Finally, remove mons from pack
            foreach(string mon in players[player].pack_results[booster])
            {
                packs.RemoveMon(mon);
            }
        }
        // Ok, now to next person, booster, etc whatever
        player += direction; // Go to "next" player
        if(player == players.Count || player == -1) // Reached end of players, need to turn around, go to next booster
        {
            direction *= -1;
            player += direction;
            booster++; 
        }
    } while (!finished);
    // If i reach here, booster selection is finalized, hopefully succesfully
}
print_string("PICKS SUCCESFUL! THESE ARE THE RESULTS!", ConsoleColor.Green, ConsoleColor.Black);
using (StreamWriter writetext = new StreamWriter("./output.txt"))
{
    foreach (Player player in players)
    {
        writetext.WriteLine($"- {player.name}:");
        Console.WriteLine($"- {player.name}:");
        for(int i = 0; i < player.chosen_packs.Count; i++)
        {
            Console.Write($"\t- PACK {player.chosen_packs[i].Item1.ToUpper()}:");
            writetext.Write($"\t- PACK {player.chosen_packs[i].Item1.ToUpper()}:");
            foreach ( string mon in player.pack_results[i])
            {
                Console.Write(mon + ",");
                writetext.Write(mon + ",");
            }
            Console.Write("\n");
            writetext.Write("\n");
        }
    }
}

print_string("ENJOY!", ConsoleColor.Green, ConsoleColor.Black);
Console.WriteLine("Press any key to close program...");

Console.ReadKey();