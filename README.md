# Traditional Shiftbot

## Table of Contents

- [Game Process](#game-process)
- [Commands](#moderation)

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
