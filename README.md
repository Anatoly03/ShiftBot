# Traditional Shiftbot

## Table of Contents

- [Game Process](#game-process)

## Game Process

```cs
public static async Task ContinueGame()
{
    [...]

    await ClearGameArea();

    Thread.Sleep(6000);
    await BuildMap(Maps.ElementAt(new Random().Next(0, Maps.Count)));
    await CreateExit();

    [...]

    Thread.Sleep(2000);
    await MakeGravity();
    await OpenEntrance();

    Thread.Sleep(2000);
    await ReleasePlayers();

    [...]

    Thread.Sleep(2000);
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
- Releasing the players and starting the countdown (to both, closing the doors and recording the players' `startTime`).
