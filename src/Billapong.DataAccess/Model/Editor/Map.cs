﻿namespace Billapong.DataAccess.Model.Editor
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Map : IEntity
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Required]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the windows.
        /// </summary>
        /// <value>
        /// The windows.
        /// </value>
        public virtual ICollection<Window> Windows { get; set; }
    }
}
