﻿namespace Billapong.Core.Server.Map
{
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using Billapong.Core.Server.Tracing;
    using Billapong.Core.Server.Utilities;
    using Contract.Exceptions;
    using Contract.Service;
    using DataAccess.Model.Map;
    using DataAccess.UnitOfWork;

    /// <summary>
    /// The map controller
    /// </summary>
    public class MapController
    {
        /// <summary>
        /// The database writer lock object
        /// </summary>
        private static readonly object WriterLockObject = new object();

        /// <summary>
        /// The callback lock object
        /// </summary>
        private static readonly object CallbackLockObject = new object();

        /// <summary>
        /// All editors which are currently connected to the server
        /// </summary>
        private readonly IDictionary<long, MapEditor> editors = new Dictionary<long, MapEditor>();
        
        /// <summary>
        /// The map repository
        /// </summary>
        private readonly UnitOfWork unitOfWork;

        #region Singleton Implementation

        /// <summary>
        /// Initializes static members of the <see cref="MapController"/> class.
        /// </summary>
        static MapController()
        {
            Current = new MapController();
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="MapController"/> class from being created.
        /// </summary>
        private MapController()
        {
            this.unitOfWork = new UnitOfWork();
        }

        /// <summary>
        /// Gets the current instance.
        /// </summary>
        /// <value>
        /// The current instance.
        /// </value>
        public static MapController Current { get; private set; }

        #endregion

        /// <summary>
        /// Gets the maps.
        /// </summary>
        /// <param name="onlyPlayable">if set to <c>true</c> only currently playable maps will be returned.</param>
        /// <returns>
        /// Available maps in the database
        /// </returns>
        public IEnumerable<Map> GetMaps(bool onlyPlayable = false)
        {
            var maps = this.unitOfWork.MapRepository.Get(includeProperties: "Windows, Windows.Holes");
            if (onlyPlayable)
            {
                maps = maps.Where(map => map.IsPlayable);
            }
    
            return maps.ToList();
        }

        /// <summary>
        /// Gets the map by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="onlyPlayable">if set to <c>true</c> only currently playable maps will be returned.</param>
        /// <returns>Map object from the database</returns>
        public Map GetMapById(long id, bool onlyPlayable = false)
        {
            var maps = this.unitOfWork.MapRepository.Get(filter: map => map.Id == id, includeProperties: "Windows, Windows.Holes");
            if (onlyPlayable)
            {
                maps = maps.Where(map => map.IsPlayable);
            }
            
            return maps.FirstOrDefault();
        }

        /// <summary>
        /// Deletes a map.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public void DeleteMap(long id)
        {
            lock (WriterLockObject)
            {
                this.unitOfWork.MapRepository.Remove(id);
                this.unitOfWork.MapRepository.Save();
            }
        }

        /// <summary>
        /// Creates a new map.
        /// </summary>
        /// <returns>Newly created map</returns>
        public Map CreateMap()
        {
            lock (WriterLockObject)
            {
                return this.GetMap();
            }
        }

        /// <summary>
        /// Determines whether a callback is registered to a specific map.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>Boolean value if the callback for the map is registered or not</returns>
        public bool IsCallbackRegistered(long mapId, IMapEditorCallback callback)
        {
            lock (CallbackLockObject)
            {
                if (!this.editors.ContainsKey(mapId)) return false;
                var editor = this.editors[mapId];
                return editor.Callbacks.Contains(callback);
            }
        }

        /// <summary>
        /// Updates the name.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="name">The name.</param>
        public void UpdateName(long mapId, string name)
        {
            lock (WriterLockObject)
            {
                var map = this.GetMap(mapId);
                if (map == null) return;

                map.Name = name;
                this.unitOfWork.Save();
            }

            // send the callback
            Task.Run(() => this.SendUpdateNameCallback(mapId, name));
        }

        /// <summary>
        /// Updates the is playable flag.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="isPlayable">if set to <c>true</c> the map is playable.</param>
        public void UpdateIsPlayable(long mapId, bool isPlayable)
        {
            lock (WriterLockObject)
            {
                var map = this.GetMap(mapId);
                if (map == null) return;

                // verify if current map is really playable
                if (isPlayable)
                {
                    isPlayable = MapUtil.IsPlayable(map);
                }

                map.IsPlayable = isPlayable;
                this.unitOfWork.Save();
            }

            // send the callback
            Task.Run(() => this.SendUpdateIsPlayableCallback(mapId, isPlayable));
        }

        /// <summary>
        /// Adds a window to the map.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="coordX">The coord x.</param>
        /// <param name="coordY">The coord y.</param>
        public void AddWindow(long mapId, int coordX, int coordY)
        {
            Map map;
            var window = new Window { X = coordX, Y = coordY };
            lock (WriterLockObject)
            {
                map = this.GetMap(mapId);
                if (map == null) return;

                // do nothing if window already exists
                if (map.Windows.FirstOrDefault(innerWindow => innerWindow.X == coordX && innerWindow.Y == coordY) != null) return;

                map.Windows.Add(window);
                this.unitOfWork.Save();
            }

             // send the callback
            Task.Run(() => this.SendAddWindowCallback(mapId, window.Id, coordX, coordY));
            Task.Run(() => this.VerifyIsPlayable(map));
        }

        /// <summary>
        /// Removes a window.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="windowId">The window identifier.</param>
        public void RemoveWindow(long mapId, long windowId)
        {
            int coordX, coordY;
            Map map;
            lock (WriterLockObject)
            {
                map = this.GetMap(mapId);
                if (map == null) return;

                var window = map.Windows.FirstOrDefault(gameWindow => gameWindow.Id == windowId);
                if (window == null) return;

                coordX = window.X;
                coordY = window.Y;
                this.unitOfWork.WindowRepository.Remove(window.Id);
                this.unitOfWork.Save();
            }

             // send the callback
            Task.Run(() => this.SendRemoveWindowCallback(mapId, windowId, coordX, coordY));
            Task.Run(() => this.VerifyIsPlayable(map));
        }

        /// <summary>
        /// Adds a hole to a window.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="windowId">The window identifier.</param>
        /// <param name="coordX">The coord x.</param>
        /// <param name="coordY">The coord y.</param>
        public void AddHole(long mapId, long windowId, int coordX, int coordY)
        {
            var hole = new Hole { X = coordX, Y = coordY };
            Window window;
            lock (WriterLockObject)
            {
                var map = this.GetMap(mapId);
                if (map == null) return;

                window = map.Windows.FirstOrDefault(gameWindow => gameWindow.Id == windowId);
                if (window == null) return;

                // do nothing if hole already exists
                if (window.Holes.FirstOrDefault(innerHole => innerHole.X == coordX && innerHole.Y == coordY) != null) return;

                window.Holes.Add(hole);
                this.unitOfWork.Save();
            }

            // send the callback
            Task.Run(() => this.SendAddHoleCallback(mapId, windowId, window.X, window.Y, hole.Id, coordX, coordY));
        }

        /// <summary>
        /// Removes a hole.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="windowId">The window identifier.</param>
        /// <param name="holeId">The hole identifier.</param>
        public void RemoveHole(long mapId, long windowId, long holeId)
        {
            Window window;
            lock (WriterLockObject)
            {
                var map = this.GetMap(mapId);
                if (map == null) return;

                window = map.Windows.FirstOrDefault(gameWindow => gameWindow.Id == windowId);
                if (window == null) return;

                var hole = window.Holes.FirstOrDefault(gameHole => gameHole.Id == holeId);
                if (hole == null) return;

                this.unitOfWork.HoleRepository.Remove(hole.Id);
                this.unitOfWork.Save();
            }

            // send the callback
            Task.Run(() => this.SendRemoveHoleCallback(mapId, windowId, window.X, window.Y, holeId));
        }

        /// <summary>
        /// Registers a callback to a specific map.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="callback">The callback.</param>
        public void RegisterCallback(long id, IMapEditorCallback callback)
        {
            lock (CallbackLockObject)
            {
                MapEditor editor;
                if (this.editors.ContainsKey(id))
                {
                    editor = this.editors[id];
                }
                else
                {
                    editor = new MapEditor();
                    this.editors.Add(id, editor);
                }

                editor.Callbacks.Add(callback);
            }
        }

        /// <summary>
        /// Unregisters a callback form a specific map.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="callback">The callback.</param>
        public void UnregisterCallback(long id, IMapEditorCallback callback)
        {
            lock (CallbackLockObject)
            {
                if (!this.editors.ContainsKey(id))
                {
                    return;
                }

                var editor = this.editors[id];
                editor.Callbacks.Remove(callback);
            }
        }

        /// <summary>
        /// Gets the high scores.
        /// </summary>
        /// <returns>The highscore of each map</returns>
        public IEnumerable<HighScore> GetHighScores()
        {
            return this.unitOfWork.MapRepository.Get()
                .Select(map => map.HighScores.OrderByDescending(score => score.Score).ThenByDescending(score => score.Timestamp).FirstOrDefault())
                .Where(score => score != null)
                .ToList();
        }

        /// <summary>
        /// Gets the high scores for a specific map.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <returns>All scores for specific map</returns>
        public IEnumerable<HighScore> GetHighScores(long mapId)
        {
            return this.unitOfWork.HighScoreRepository
                .Get(filter: score => score.Map.Id == mapId, includeProperties: "Map")
                .OrderByDescending(score => score.Score)
                .ThenByDescending(score => score.Timestamp);
        }

        /// <summary>
        /// Sends the update name callback.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="name">The name.</param>
        private void SendUpdateNameCallback(long mapId, string name)
        {
            foreach (var callback in this.GetMapEditorCallbacks(mapId))
            {
                Tracer.Debug(string.Format("MapController :: Send UpdateName() callback with mapId={0}, name={1}", mapId, name));
                callback.UpdateName(name);
            }
        }

        /// <summary>
        /// Sends the update is playable callback.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="isPlayable">if set to <c>true</c> the map is playable.</param>
        private void SendUpdateIsPlayableCallback(long mapId, bool isPlayable)
        {
            foreach (var callback in this.GetMapEditorCallbacks(mapId))
            {
                Tracer.Debug(string.Format("MapController :: Send UpdateIsPlayable() callback with mapId={0}, isPlayable={1}", mapId, isPlayable));
                callback.UpdateIsPlayable(isPlayable);
            }
        }

        /// <summary>
        /// Sends the add window callback.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="windowId">The window identifier.</param>
        /// <param name="coordX">The coord x coords.</param>
        /// <param name="coordY">The coord y coords.</param>
        private void SendAddWindowCallback(long mapId, long windowId, int coordX, int coordY)
        {
            foreach (var callback in this.GetMapEditorCallbacks(mapId))
            {
                Tracer.Debug(string.Format("MapController :: Send AddWindow() callback with mapId={0}, windowId={1}, coordX={2}, coordY={3}", mapId, windowId, coordX, coordY));
                callback.AddWindow(windowId, coordX, coordY);
            }
        }

        /// <summary>
        /// Sends the remove window callback.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="windowId">The window identifier.</param>
        /// <param name="coordX">The coord x coords.</param>
        /// <param name="coordY">The coord y coords.</param>
        private void SendRemoveWindowCallback(long mapId, long windowId, int coordX, int coordY)
        {
            foreach (var callback in this.GetMapEditorCallbacks(mapId))
            {
                Tracer.Debug(string.Format("MapController :: Send RemoveWindow() callback with mapId={0}, windowId={1}, coordX={2}, coordY={3}", mapId, windowId, coordX, coordY));
                callback.RemoveWindow(windowId, coordX, coordY);
            }
        }

        /// <summary>
        /// Sends the add hole callback.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="windowId">The window identifier.</param>
        /// <param name="windowX">The window x coords.</param>
        /// <param name="windowY">The window y coords.</param>
        /// <param name="holeId">The hole identifier.</param>
        /// <param name="holeX">The hole x coords.</param>
        /// <param name="holeY">The hole y coords.</param>
        private void SendAddHoleCallback(long mapId, long windowId, int windowX, int windowY, long holeId, int holeX, int holeY)
        {
            foreach (var callback in this.GetMapEditorCallbacks(mapId))
            {
                Tracer.Debug(string.Format("MapController :: Send AddHole() callback with mapId={0}, windowId={1}, windowX={2}, windowY={3}, holeId={4}, holeX={5}, holeY={6}", mapId, windowId, windowX, windowY, holeId, holeX, holeY));
                callback.AddHole(windowId, windowX, windowY, holeId, holeX, holeY);
            }
        }

        /// <summary>
        /// Sends the remove hole callback.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <param name="windowId">The window identifier.</param>
        /// <param name="windowX">The window x coords.</param>
        /// <param name="windowY">The window y coords.</param>
        /// <param name="holeId">The hole identifier.</param>
        private void SendRemoveHoleCallback(long mapId, long windowId, int windowX, int windowY, long holeId)
        {
            foreach (var callback in this.GetMapEditorCallbacks(mapId))
            {
                Tracer.Debug(string.Format("MapController :: Send RemoveHole() callback with mapId={0}, windowId={1}, windowX={2}, windowY={3}, holeId={4}", mapId, windowId, windowX, windowY, holeId));
                callback.RemoveHole(windowId, windowX, windowY, holeId);
            }
        }

        /// <summary>
        /// Gets the map editor callbacks.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <returns>Callbacks registered for a specific map</returns>
        private IEnumerable<IMapEditorCallback> GetMapEditorCallbacks(long mapId)
        {
            var callbacks = new List<IMapEditorCallback>();
            lock (CallbackLockObject)
            {
                var editor = this.editors.ContainsKey(mapId) ? this.editors[mapId] : null;
                if (editor == null) return Enumerable.Empty<IMapEditorCallback>();

                foreach (var callback in editor.Callbacks.ToList())
                {
                    if (((ICommunicationObject)callback).State != CommunicationState.Opened)
                    {
                        editor.Callbacks.Remove(callback);
                        continue;
                    }

                    callbacks.Add(callback);
                }
            }

            return callbacks;
        }

        /// <summary>
        /// Gets a map from the database.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Map from the database or a new map if id was 0</returns>
        private Map GetMap(long id = 0)
        {
            if (id > 0)
            {
                var databaseMap = this.unitOfWork.MapRepository.GetById(id);
                if (databaseMap == null)
                {
                    throw new FaultException<MapNotFoundException>(new MapNotFoundException(id), "Map not found");
                }

                return databaseMap;
            }

            var map = new Map
            {
                Name = "<Unnamed>",
                IsPlayable = false
            };

            this.unitOfWork.MapRepository.Add(map);
            this.unitOfWork.Save();
            return map;
        }

        /// <summary>
        /// Verifies if current map is playable and set flag to false if not.
        /// </summary>
        /// <param name="map">The map.</param>
        private void VerifyIsPlayable(Map map)
        {
            if (!map.IsPlayable || MapUtil.IsPlayable(map)) return;

            lock (WriterLockObject)
            {
                map.IsPlayable = false;
                this.unitOfWork.Save();
            }

            this.SendUpdateIsPlayableCallback(map.Id, false);
        }
    }
}
