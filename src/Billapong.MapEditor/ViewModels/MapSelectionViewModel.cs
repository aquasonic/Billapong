﻿namespace Billapong.MapEditor.ViewModels
{
    using System.Collections.ObjectModel;
    using Converter;
    using Core.Client.UI;
    using Models;
    using Services;

    public class MapSelectionViewModel : ViewModelBase
    {
        /// <summary>
        /// The is data loading
        /// </summary>
        private bool isDataLoading;

        private readonly MapEditorServiceClient proxy;

        /// <summary>
        /// Gets or sets a value indicating whether the view data is loading.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the view data is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataLoading
        {
            get
            {
                return this.isDataLoading;
            }

            set
            {
                this.isDataLoading = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Map> Maps { get; private set; }

        public MapSelectionViewModel()
        {
            this.proxy = new MapEditorServiceClient();
            this.Maps = new ObservableCollection<Map>();
            this.LoadMaps();
        }

        private async void LoadMaps()
        {
            this.IsDataLoading = true;
            var maps = await this.proxy.GetMapsAsync();
            foreach (var map in maps)
            {
                this.Maps.Add(map.ToEntity());
            }

            this.IsDataLoading = false;
        } 
    }
}
