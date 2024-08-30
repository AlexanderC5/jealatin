using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Enums
{     
     public enum Color
     {
          Blue = 0,
          Violet = 1,
          Red = 2,
          Orange = 3,
          Yellow = 4,
          Green = 5,
          None = 6
     }

     public enum Action
     {
          North = 0,
          East = 1,
          South = 2,
          West = 3,
          Bump = 4,
          Reset = 5
     }

     public enum GameMode
     {
          Game = 0,
          ColorSelect = 1,
          MainMenu = 2,
          PauseMenu = 3

     }

     public enum Touch // Used when checking for collisions from the Player
     {
          None = 0,
          Collider = 1,
          Object = 2
     }
}
