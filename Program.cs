using System;
using System.IO;

int TYPE_LIMIT = 7;
int STARTER_TYPE_WEIGHT = 2;
int MEGA_LIMIT = 2;
int STARTER_MEGA_WEIGHT = MEGA_LIMIT;

void print_string(string str, ConsoleColor fg_color, ConsoleColor bg_color)
{
    Console.ForegroundColor = fg_color;
    Console.BackgroundColor = bg_color;
    Console.WriteLine(str);
    Console.ResetColor();
}

bool mon_is_valid(Player player, string mon, MonData dex, StreamWriter logtext)
{
    Tuple<string, string> booster_mon_types = dex.GetTypes(mon);
    // Check first type!
    if (player.type_counter.ContainsKey(booster_mon_types.Item1))
    {
        if (player.type_counter[booster_mon_types.Item1] >= TYPE_LIMIT)
        {
            print_string($"\t\t\t- Type limit exceeded: {booster_mon_types.Item1}", ConsoleColor.Magenta, ConsoleColor.Black);
            logtext.WriteLine($"\t\t\t- Type limit exceeded: {booster_mon_types.Item1}");
            return false; // this type exceeded
        }
    }
    // If ok for first type, now check second type!
    if (booster_mon_types.Item2 != "")
    {
        if (player.type_counter.ContainsKey(booster_mon_types.Item2))
        {
            if (player.type_counter[booster_mon_types.Item2] >= TYPE_LIMIT)
            {
                print_string($"\t\t\t- Type limit exceeded: {booster_mon_types.Item2}", ConsoleColor.Magenta, ConsoleColor.Black);
                logtext.WriteLine($"\t\t\t- Type limit exceeded: {booster_mon_types.Item2}");
                return false; // this type exceeded
            }
        }
    } // If ok, then types are all right
    // Check species clause
    string booster_mon_species = dex.GetSpecies(mon);
    if (booster_mon_species != "") // If it's a species with multiple forms
    {
        if (player.species_owned.Contains(booster_mon_species)) // If already own the species
        {
            print_string($"\t\t\t- Species clause violated: {booster_mon_species} species", ConsoleColor.Magenta, ConsoleColor.Black);
            logtext.WriteLine($"\t\t\t- Species clause violated: {booster_mon_species} species");
            return false; // this species already owned!
        }
    }
    // Check mega clause
    if(mon.ToLower().Contains("-mega")) // Mega pokemon
    {
        if(player.mega_counter >= MEGA_LIMIT)
        {
            print_string($"\t\t\t- Mega clause violated: {mon} mega", ConsoleColor.Magenta, ConsoleColor.Black);
            logtext.WriteLine($"\t\t\t- Mega clause violated: {mon} mega");
            return false; // this species already owned!
        }
    }
    return true; // Otherwise this was a success
}

void add_mon(Player player, string mon, MonData dex, int round)
{
    Tuple<string, string> booster_mon_types = dex.GetTypes(mon);
    string booster_mon_species = dex.GetSpecies(mon);

    player.pack_results[round].Add(mon); // Add mon to options
    if (!player.type_counter.ContainsKey(booster_mon_types.Item1)) // Increase type counter
    {
        player.type_counter.Add(booster_mon_types.Item1, 1);
    }
    else player.type_counter[booster_mon_types.Item1]++;
    // If 2nd type, also add
    if (booster_mon_types.Item2 != "")
    {
        if (!player.type_counter.ContainsKey(booster_mon_types.Item2)) // Increase type to counter
        {
            player.type_counter.Add(booster_mon_types.Item2, 1);
        }
        else player.type_counter[booster_mon_types.Item2]++;
    }
    player.species_owned.Add(booster_mon_species); // Also add the species
    if (mon.ToLower().Contains("-mega")) // Also add if mega pokemon
    {
        player.mega_counter++;
    }
}

if (File.Exists("./Settings.csv"))
{

    string[] settings_file = File.ReadAllLines("./Settings.csv");
    foreach(string setting in settings_file)
    {
        string[] this_setting = setting.Split(',');
        if (this_setting[0].ToLower() == "type_limit")
        {
            TYPE_LIMIT = int.Parse(this_setting[1]);
        }
        else if(this_setting[0].ToLower() == "starter_type_weight")
        {
            STARTER_TYPE_WEIGHT = int.Parse(this_setting[1]);

        }
        else if (this_setting[0].ToLower() == "mega_limit")
        {
            MEGA_LIMIT = int.Parse(this_setting[1]);

        }
        else if (this_setting[0].ToLower() == "starter_mega_weight")
        {
            STARTER_MEGA_WEIGHT = int.Parse(this_setting[1]);
        }
    }
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
            if (starter.ToLower().Contains("-mega")) // If starter is a mega
            {
                new_player.mega_counter += STARTER_MEGA_WEIGHT;
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
                // Now, pick the next, but skip if types are incompatible, and definitely skip if booster ran out
                int obtained_mons = 0; // Mons I retrieved succesfully from booster
                
                for (int booster_next = 0; booster_next < pack.Count; booster_next++) // Navigate the whole thing
                {
                    string mon_picked = pack[booster_next].ToLower();
                    Console.WriteLine($"\t\t- Chosen {mon_picked}");
                    logtext.WriteLine($"\t\t- Chosen {mon_picked}");
                    bool pick_ok = mon_is_valid(players[player], mon_picked, dex, logtext);
                    
                    if (!pick_ok) // This mon won't go
                    {
                        print_string($"\t\t- {mon_picked} discarded", ConsoleColor.Magenta, ConsoleColor.Black);
                        logtext.WriteLine($"\t\t- {mon_picked} discarded");
                    }
                    else
                    {
                        add_mon(players[player], mon_picked, dex, round);
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