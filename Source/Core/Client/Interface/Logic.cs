﻿using System;
using Microsoft.Xna.Framework;
using MonoForce.Controls;

namespace Bricklayer.Client.Interface
{
    /// <summary>
    /// Handles the logical updating of UI elements
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static readonly TimeSpan second = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The current FPS.
        /// </summary>
        internal int FPS { get; private set; }

        private TimeSpan elapsedTime = TimeSpan.Zero;
        private int totalFrames;

        protected override void Update(GameTime gameTime)
        {
            //Update current screen
            ScreenManager.Update(gameTime);

            //Calculate FPS
            elapsedTime += gameTime.ElapsedGameTime;
            if (elapsedTime > second)
            {
                elapsedTime -= second;
                FPS = totalFrames;
                totalFrames = 0;
            }
            totalFrames++;

            base.Update(gameTime);
        }
    }
}