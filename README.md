Executable is in the BOOSTER PICKER folder.

Customize the results by changing the Dex/Pack/Picks csv files, you can obtain them by exporting directly from google sheets.
A sample sheet can be found in https://docs.google.com/spreadsheets/d/1cER2sAZnk388ce0FwG8r_w3ywD2XbzTfa7gIcVg6fFU/edit?usp=sharing.
Just export the changed page to a csv called ```Packs.csv```, ```Picks.csv``` or ```Dex.csv``` respectively.
Optional ```Bans.csv``` file also possible.

# Dex

You can create your custom dex, but the following columns are mandatory with the exact name:
- ```Pokemon```, colum with the pokemon name
- ```Type 1```/```Type 2```, columns with the respective types
- ```Species```, a column with a "species" name, for example charizard, or whatever (to filter out megas and regionals)

Avoid typos, as the program verifies the mons in the boosters exist in the pokedex, and that 18 (and only 18) types exist.
Avoid trailing or leading spaces as this breaks everything.

# Packs

The .csv exported from a spreadsheet, where a column describes a booster.

- ```NAME OF BOOSTER```, an unique name
- The following rows contain the pokemon in that booster

Boosters can have different pokemon

# Picks

Each row for each pick. They have this format, separated by commas:

- ```PLAYER_NAME```, ```STARTER_CHOICE```, ```PACK_1```, ```PULLS_FROM_PACK_1```, ```PACK_2```, ```PULLS_FROM_PACK_2```, ```PACK_X```, ```PULLS_FROM_PACK_X```

If a player doesn't pick, no worries, just leave empty elements.
However player needs a starter (for now?), as it makes the program work:

- ```PLAYER_NAME```, ```STARTER_CHOICE```,,,,,,

# Bans

If a player didn't choose in time, and has empty boosters, their starters have to be "banned" from booster packs.
At the same time, when player pulls later, all previously pulled mons need to be banned from their pools to avoid duplicates.

There's an (optional) ```Bans.csv``` file that, if added, can contain the list of banned pokemon.
The file can contain any amount of rows and elements per row, as long as the name is as found in the dex and there's no leading and trailing spaces.
Example:

>BANNED_1,BANNED_2

>BANNED_3,BANNED_4,BANNED_5