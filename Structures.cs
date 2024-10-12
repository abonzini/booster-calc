using System;
using System.Security;
public class Player
{
    public string name;
    public Player(string player_name)
    {
        name = player_name;
    }

    public Dictionary<string, int> type_counter = new Dictionary<string, int>(); // Type counter
    public HashSet<string> species_owned = new HashSet<string>();
    public List<Tuple<string, int>> chosen_packs = new List<Tuple<string, int>>();
    public List<List<string>> pack_results = new List<List<string>>(); // Contains pack results
};

public class MonData
{
    public MonData()
    {
        dex_file = File.ReadAllLines("./Dex.csv"); // open dex file
        int i = 0;
        foreach (string index in dex_file[0].Split(',')) // Check where types are (which col)
        {
            if (index.ToLower() == "pokemon")
            {
                index_name = i;
            }
            else if (index.ToLower() == "type 1")
            {
                index_type1 = i;
            }
            else if (index.ToLower() == "type 2")
            {
                index_type2 = i;
            }
            else if (index.ToLower() == "species")
            {
                index_species = i;
            }
            i++;
        }
        if (index_name == -1 || index_type1 == -1 || index_type2 == -1 || index_species == -1)
        {
            throw new Exception("Dex doesn't contain pokemon and/or type 1 and/or type 2 and/or species indices!");
        }
    }
    int index_name = -1;
    int index_type1 = -1, index_type2 = -1;
    int index_species = -1;
    string[] dex_file;
    // Dictionary that contains types
    Dictionary<string, Tuple<string, string>> Types = new Dictionary<string, Tuple<string, string>>();
    // Dictionary that contains species
    Dictionary<string, string> Species = new Dictionary<string, string>();

    public Tuple<string, string> GetTypes(string mon) // Check types of mon (stores it for later use)
    {
        string type1, type2;
        mon = mon.ToLower();
        Tuple<string, string> result;
        if (!Types.ContainsKey(mon)) // if mon not parsed, parse it
        {
            foreach (string mon_data in dex_file)
            {
                string[] indices = mon_data.Split(",");
                if (mon == indices[index_name].ToLower())
                {
                    type1 = indices[index_type1].ToLower();
                    type2 = indices[index_type2].ToLower();
                    result = new Tuple<string, string>(type1, type2);
                    Types.Add(mon, result);
                    break;
                }
            }
        }
        if (!Types.ContainsKey(mon))
        {
            throw new Exception($"Dex doesn't contain data for {mon}");
        }
        result = Types[mon];
        return result;
    }
    public string GetSpecies(string mon)
    {
        mon = mon.ToLower();
        if (!Species.ContainsKey(mon)) // if mon not parsed, parse it
        {
            foreach (string mon_data in dex_file)
            {
                string[] indices = mon_data.Split(",");
                if (mon == indices[index_name].ToLower())
                {
                    string species = indices[index_species].ToLower();
                    Species.Add(mon, species);
                    break;
                }
            }
        }
        if (!Species.ContainsKey(mon))
        {
            throw new Exception($"Dex doesn't contain data for {mon}");
        }
        return Species[mon];
    }
}
public class PackPools
{
    public PackPools()
    {
        string[] packs_file = File.ReadAllLines("./Packs.csv");
        pack_data = new List<Tuple<string, List<string>>>();
        string line1 = packs_file[0];
        foreach (string pack_name in line1.Split(',')) // Read all pack names
        {
            Tuple<string, List<string>> new_pack = new Tuple<string, List<string>>(pack_name.ToLower(), new List<string>());
            pack_data.Add(new_pack);
        }
        for(int i =  1; i < packs_file.Length; i++) // Rest of lines contain each mon
        {
            string line = packs_file[i];
            string[] mons = line.Split(",");
            for(int j = 0; j < mons.Length; j++)
            {
                string mon = mons[j].ToLower();
                if(mon == "")
                {
                    continue;
                }    
                pack_data[j].Item2.Add(mon); // Add mon to corresponding pack
            }
        }
        // Finally, log
        foreach(Tuple<string, List<string>> pack in pack_data)
        {
            Console.WriteLine($"\t- Pack {pack.Item1} contains {pack.Item2.Count} entries");
        }
    }

    public List<string> GetPackMons(string pack_name)
    {
        foreach (Tuple<string, List<string>> pack in pack_data)
        {
            if (pack.Item1 == pack_name.ToLower())
            {
                return pack.Item2;
            }
        }
        throw new Exception($"Pack name invalid! Looking for {pack_name} but didn't find it");
    }
    public void RemoveMon(string mon)
    {
        foreach (Tuple<string, List<string>> pack in pack_data)
        {
            pack.Item2.Remove(mon);
        }
    }
    public void DebugVerifyPacksMons(MonData dex)
    {
        HashSet<string> types = new HashSet<string>();
        foreach (Tuple<string, List<string>> pack in pack_data)
        {
            foreach (string mon in pack.Item2)
            {
                dex.GetSpecies(mon);
                Tuple<string, string> these_types = dex.GetTypes(mon); // This runs and verifies grammar
                if (!types.Contains(these_types.Item1))
                {
                    types.Add(these_types.Item1);
                }
                if (these_types.Item2 != "" && !types.Contains(these_types.Item2))
                {
                    types.Add(these_types.Item2);
                }
            }
        }
        if(types.Count != 18)
        {
            throw new Exception("Mon type format error:" + types);
        }
    }

    List<Tuple<string, List<string>>> pack_data;
}