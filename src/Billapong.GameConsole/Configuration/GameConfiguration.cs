﻿namespace Billapong.GameConsole.Configuration
{
    using System;

    /// <summary>
    /// The game configuration
    /// </summary>
    public static class GameConfiguration
    {
        /// <summary>
        /// The game window width
        /// </summary>
        public const int GameWindowWidth = 200;

        /// <summary>
        /// The game window height
        /// </summary>
        public const int GameWindowHeight = 200;

        /// <summary>
        /// The game grid size
        /// </summary>
        public const int GameGridSize = 15;

        /// <summary>
        /// The game grid element size
        /// </summary>
        public const double GameGridElementSize = GameWindowWidth / (double)GameGridSize;

        /// <summary>
        /// The base animation duration in milliseconds
        /// </summary>
        public const double BaseAnimationDuration = 1000;

        /// <summary>
        /// The hole diameter
        /// </summary>
        public const double HoleDiameter = GameGridElementSize;

        /// <summary>
        /// The ball diameter
        /// </summary>
        public const double BallDiameter = HoleDiameter * 0.667;

        /// <summary>
        /// The length of the queue line
        /// </summary>
        public const double QueueLineLength = BallDiameter * 4;

        /// <summary>
        /// The maximum number of window rows
        /// </summary>
        public const int MaxNumberOfWindowRows = 3;

        /// <summary>
        /// The maximum number of windows per row
        /// </summary>
        public const int MaxNumberOfWindowsPerRow = 4;

        /// <summary>
        /// The game types
        /// </summary>
        public enum GameType
        {
            /// <summary>
            /// The single player training
            /// </summary>
            SinglePlayerTraining,

            /// <summary>
            /// The single player game
            /// </summary>
            SinglePlayerGame,

            /// <summary>
            /// The multi player game
            /// </summary>
            MultiPlayerGame
        }

        /// <summary>
        /// Gets the maximum animation distance.
        /// </summary>
        /// <value>
        /// The maximum animation distance.
        /// </value>
        public static double MaxAnimationDistance
        {
            get
            {
                return Math.Sqrt(Math.Pow(GameWindowWidth, 2) + Math.Pow(GameWindowHeight, 2));
            }
        }
    }
}
