using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enums : MonoBehaviour
{
   public enum Color
   {
        Blue = 0,
        Violet = 1,
        Red = 2,
        Orange = 3,
        Yellow = 4,
        Green = 5,
   }

   public enum Dir
   {
        North = 0,
        East = 1,
        South = 2,
        West = 3
   }

   public enum GameMode
   {
        Game = 0,
        MainMenu = 1,
        PauseMenu = 2

   }
}
