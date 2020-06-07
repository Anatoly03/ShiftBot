# Traditional Shiftbot

Bot created by LeoGiaco ana Anatoly

## Table of Contents

- [Setting up](#setup)
- [Game Process](#game-process)
- [Commands](#moderation)

## Setup

### Create the cookie file

To login, create the file `cookie.txt` at the level of `Program.cs` The first line defines the login style, the second line the TOKEN.

#### Google

```
google
TOKEN
```

#### 15-minutes token

```
token
TOKEN
```

### Create the configuration file

The file `config.txt` keeps individual settings like worldid

Example for a 100x100 world:

```
corner 31 64
worldid tPy8SPndRwTI
save false
```

- `corner x y` defines the top left corner of the level, **not** the arrows. So the minimum value should be 1 1 (for)
- `worldid id` defines the world to connect
- `save bool` defines wether player data should be saved or not.

## Game Process

```cs
public static async Task ContinueGame()
{
    [...]

    await ClearGameArea();

    [...]

    await BuildMap(Maps.ElementAt(new Random().Next(0, Maps.Count)));
    await CreateExit();

    [...]

    await MakeGravity();
    await OpenEntrance();

    [...]

    await ReleasePlayers();

    [...]

    await CreateSafeArea();

    [...]
}
```

The game process is a cycle, consisting of non-changing elements

- Clearing the game area
- Building a map
  - Creating exit (coin doors)
  - Creating the gravity tunnels
  - Opening the entrance (which happens on top of the gravity tunnels)
- Releasing the players and starting the countdowns.

### Multi-dependent events

#### Closing the door

The door closes if one of the following conditions is met:

- Half a second after active players' movement detected
- Five seconds in general passed after players released

#### Ending the game

The game ends if one of the following conditions is met:

- 50% of active players finished.
- 5-20 seconds after the first player has finished passed.
- 150 seconds timelimit overdue.

## Moderation

TODO: Add MOD COMMAMDS
