using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Enums
{
     public enum Color
     {
          None = 0,                     // 0
          Blue    = 0b0000001,          // 1
          Yellow    = 0b0000010,        // 2
          Red   = 0b0000100,            // 4

          Green = Blue | Yellow,        // 3
          Orange = Yellow | Red,        // 6
          Violet = Blue | Red,          // 5
          Black = Blue | Yellow | Red   // 7
     }

     public enum Action
     {
          North = 0,
          East = 1,
          South = 2,
          West = 3,
          None = 4,
          Bump = 5,
          Push = 6
     }

     public enum GameMode
     {
          Game = 0,
          NoInteraction = 1, // For animations & other things that do not accept keypresses
          ColorSelect = 2,
          LevelClear = 3, // Win condition = escape the current screen
          MainMenu = 4,
          LevelSelect = 5,
          PauseMenu = 6,
          Cutscene = 7

     }

     public enum Touch // Used when checking for collisions from the Player
     {
          None = 0,
          Collider = 1,
          ColorChange = 2,
          Pushable = 3,
          Consume = 4 // Same color as object, can walk on it
     }
}
