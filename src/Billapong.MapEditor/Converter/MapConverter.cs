﻿namespace Billapong.MapEditor.Converter
{
    using System.Linq;
    using Models;

    public static class MapConverter
    {
        public static Map ToEntity(this Contract.Data.Map.Map source)
        {
            return new Map
            {
                Id = source.Id,
                Name = source.Name,
                IsPlayable = source.IsPlayable,
                NumberOfWindows = source.Windows.Count,
                NumberOfHoles = source.Windows.Sum(window => window.Holes.Count)
            };
        }
    }
}
