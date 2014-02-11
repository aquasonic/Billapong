﻿namespace Billapong.MapEditor.Models
{
    public class Hole
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the x coordinate.
        /// </summary>
        /// <value>
        /// The x coordinate.
        /// </value>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the y coordinate.
        /// </summary>
        /// <value>
        /// The y coordinate.
        /// </value> 
        public int Y { get; set; }

        public double Diameter { get; set; }

        public double Left
        {
            get
            {
                return this.X*this.Diameter;
            }
        }

        public double Top
        {
            get
            {
                return this.Y*this.Diameter;
            }
        }
    }
}
