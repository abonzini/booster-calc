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

using (StreamWriter logtext = new StreamWriter("./log.txt"))
{
    int seed = Guid.NewGuid().GetHashCode();
    Random rng = new Random(seed); // rng
    print_string($"For debug purposes, random seed is {seed}", ConsoleColor.Yellow, ConsoleColor.Black);
    logtext.WriteLine($"For debug purposes, random seed is {seed}");
    print_string("HIGO AUTOMATED BOOSTER PACK SELECTOR", ConsoleColor.Green, ConsoleColor.Black);
    logtext.WriteLine("HIGO AUTOMATED BOOSTER PACK SELECTOR");

    Console.WriteLine("Loading Dex");
    logtext.WriteLine("Loading Dex");
    MonData dex = new MonData();

    // Initial validation of mons in boosters, to catch typos and such
    PackPools packs = new PackPools();
    packs.DebugVerifyPacksMons(dex);

    string[] picks_file = File.ReadAllLines("./Picks.csv");
    bool picks_succesful = false;
    List<Player> players = null;
    while (!picks_succesful)
    {
        print_string("--------- Start of Booster Selection Try ---------", ConsoleColor.Black, ConsoleColor.White);
        logtext.WriteLine("--------- Start of Booster Selection Try ---------");
        Console.WriteLine("Initialising Booster Pack Pools");
        logtext.WriteLine("Initialising Booster Pack Pools");
        packs = new PackPools();
        if (File.Exists("./Bans.csv"))
        {
            Console.WriteLine("Banned mons file found, removing them");
            logtext.WriteLine("Banned mons file found, removing them");
            string[] ban_file = File.ReadAllLines("./Bans.csv");
            foreach(string line in ban_file)
            {
                foreach (string banned_mon in line.Split(','))
                {
                    if(banned_mon != "")
                    {
                        Console.WriteLine($"\t- Removed {banned_mon}");
                        logtext.WriteLine($"\t- Removed {banned_mon}");
                        packs.RemoveMon(banned_mon.ToLower());
                    }
                }
            }
        }
        // Initialised pack pools
        Console.WriteLine("Parsing players and picks");
        logtext.WriteLine("Parsing players and picks");
        int packs_remaining = 0; // How many packs need to be pulled
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
            string starter_species = dex.GetSpecies(starter);
            if(starter_species != "") // Add species if exists
            {
                new_player.species_owned.Add(starter_species); // Also add the species
            }
            for (int i = 2; i < fields.Length; i+=2) // Add rest of data as booster packs
            {
                if(fields[i] == "" || fields[i+1] == "")
                {
                    continue;
                }
                Tuple<string, int> next_pack = new Tuple<string, int>(fields[i], int.Parse(fields[i+1]));
                new_player.chosen_packs.Add(next_pack);
                packs_remaining++;
            }
            packs.RemoveMon(starter); // This mon will be unavailable in packs
            players.Add(new_player);
        }
        Console.WriteLine("\t- All players loaded");
        logtext.WriteLine("\t- All players loaded");
        // By this step, all players have been loaded

        Console.WriteLine("Booster selection begins!");
        logtext.WriteLine("Booster selection begins!");
        picks_succesful = true; // If nothing breaks, we won!
        int player = 0;
        int round = 0;
        int direction = 1;
        while(packs_remaining > 0) // While there's stuff to pull
        { 
            if (players[player].ongoing_pack < players[player].chosen_packs.Count) // If player still has boosters to choose
            {
                Console.WriteLine($"\t- Pick #{round + 1} for {players[player].name}: {players[player].chosen_packs[players[player].ongoing_pack]}");
                logtext.WriteLine($"\t- Pick #{round + 1} for {players[player].name}: {players[player].chosen_packs[players[player].ongoing_pack]}");
                players[player].pack_results.Add(new List<string>()); // Will contain results for next booster
                List<string> pack = packs.GetPackMons(players[player].chosen_packs[players[player].ongoing_pack].Item1); // Get mons for desired booster!
                int picks_per_pack = players[player].chosen_packs[players[player].ongoing_pack].Item2; // How many pulls of this pack
                pack = pack.OrderBy(x => rng.Next()).ToList(); // RANDOMIZE!
                // Now, pick the next (2), but skip if types are incompatible, and definitely skip if booster ran out
                int obtained_mons = 0; // Mons I retrieved succesfully from booster
                
                for (int booster_next = 0; booster_next < pack.Count; booster_next++) // Navigate the whole thing
                {
                    bool pick_ok = true;
                    Console.WriteLine($"\t\t- Chosen {pack[booster_next]}");
                    logtext.WriteLine($"\t\t- Chosen {pack[booster_next]}");
                    Tuple<string, string> booster_mon_types = dex.GetTypes(pack[booster_next].ToLower());
                    // Check first type!
                    if (players[player].type_counter.ContainsKey(booster_mon_types.Item1))
                    {
                        if (players[player].type_counter[booster_mon_types.Item1] >= MAX_NUMBER_OF_SAME_TYPE)
                        {
                            print_string($"\t\t\t- Type limit exceeded: {booster_mon_types.Item1}", ConsoleColor.Magenta, ConsoleColor.Black);
                            logtext.WriteLine($"\t\t\t- Type limit exceeded: {booster_mon_types.Item1}");
                            pick_ok = false; // this type exceeded
                        }
                    } // ok for first type, now check second type!
                    if (booster_mon_types.Item2 != "")
                    {
                        if (pick_ok && players[player].type_counter.ContainsKey(booster_mon_types.Item2))
                        {
                            if (players[player].type_counter[booster_mon_types.Item2] >= MAX_NUMBER_OF_SAME_TYPE)
                            {
                                print_string($"\t\t\t- Type limit exceeded: {booster_mon_types.Item2}", ConsoleColor.Magenta, ConsoleColor.Black);
                                logtext.WriteLine($"\t\t\t- Type limit exceeded: {booster_mon_types.Item2}");
                                pick_ok = false; // this type exceeded
                            }
                        }
                    } // If ok, then types are all right
                        // Check species clause
                    string booster_mon_species = dex.GetSpecies(pack[booster_next].ToLower());
                    if (booster_mon_species != "") // If it's a species with multiple forms
                    {
                        if (pick_ok && players[player].species_owned.Contains(booster_mon_species)) // If already own the species
                        {
                            print_string($"\t\t\t- Species clause violated: {booster_mon_species} species", ConsoleColor.Magenta, ConsoleColor.Black);
                            logtext.WriteLine($"\t\t\t- Species clause violated: {booster_mon_species} species");
                            pick_ok = false; // this type exceeded
                        }
                    }
                    if (!pick_ok) // This mon won't go
                    {
                        print_string($"\t\t- {pack[booster_next]} discarded", ConsoleColor.Magenta, ConsoleColor.Black);
                        logtext.WriteLine($"\t\t- {pack[booster_next]} discarded");
                    }
                    else
                    {
                        players[player].pack_results[round].Add(pack[booster_next]); // Add mon to options
                        if (!players[player].type_counter.ContainsKey(booster_mon_types.Item1)) // Increase type counter
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
                        players[player].species_owned.Add(booster_mon_species); // Also add the species
                        obtained_mons++; // Get this mon, defo
                        if (obtained_mons == picks_per_pack)
                        {
                            break; // Finished with booster, onwards...
                        }
                    }
                }
                // Booster selection complete, now to verify if all good
                if(obtained_mons != picks_per_pack) // If not managed to pick (2), means booster ran out, unfortunately need to restart
                {
                    print_string($"\t- Player {players[player].name} ran out of possible boosters or available picks, need to start over", ConsoleColor.Red, ConsoleColor.White);
                    logtext.WriteLine($"\t- Player {players[player].name} ran out of possible boosters or available picks, need to start over");
                    picks_succesful = false;
                    break;
                }
                else
                {
                    // Otherwise, pack is done, can now remove it!
                    players[player].ongoing_pack++; // Player moves to their next pack
                    packs_remaining--; // one more pack done
                }
                // Finally, remove mons from latest pack
                foreach(string mon in players[player].pack_results.Last())
                {
                    packs.RemoveMon(mon);
                }
            }
            else // Player ran out of booster
            {
                Console.WriteLine($"\t- Pick #{round + 1} for {players[player].name} skipped: no more packs");
                logtext.WriteLine($"\t- Pick #{round + 1} for {players[player].name} skipped: no more packs");
            }    
            // Ok, now to next person, booster, etc whatever
            player += direction; // Go to "next" player
            if(player == players.Count || player == -1) // Reached end of players, need to turn around, go to next booster
            {
                direction *= -1;
                player += direction;
                round++; 
            }
        }
        // If i reach here, booster selection is finalized, hopefully succesfully
    }
    print_string("PICKS SUCCESFUL! THESE ARE THE RESULTS!", ConsoleColor.Green, ConsoleColor.Black);
    logtext.WriteLine("PICKS SUCCESFUL! THESE ARE THE RESULTS!");
    using (StreamWriter outputtext = new StreamWriter("./output.txt"))
    {
        foreach (Player player in players)
        {
            Console.WriteLine($"- {player.name}:");
            logtext.WriteLine($"- {player.name}:");
            for(int i = 0; i < player.chosen_packs.Count; i++)
            {
                Console.Write($"\t- PACK {player.chosen_packs[i].Item1.ToUpper()}: ");
                logtext.Write($"\t- PACK {player.chosen_packs[i].Item1.ToUpper()}: ");
                for(int j=0; j < player.pack_results[i].Count; j++)
                {
                    Console.Write(player.pack_results[i][j]);
                    logtext.Write(player.pack_results[i][j]);
                    outputtext.Write(player.pack_results[i][j]);
                    if (j != player.pack_results[i].Count - 1)
                    {
                        Console.Write(", ");
                        logtext.Write(", ");
                    }
                    if(i != player.chosen_packs.Count - 1)
                    {
                        outputtext.Write(',');
                    }
                    else if (j != player.pack_results[i].Count - 1)
                    {
                        outputtext.Write(',');
                    }
                }
                Console.Write("\n");
                logtext.Write("\n");
            }
            outputtext.Write("\n");
        }
    }

    print_string("ENJOY!", ConsoleColor.Green, ConsoleColor.Black);
    logtext.WriteLine("ENJOY!");
}
Console.WriteLine("Press enter to close program...");
Console.ReadLine();