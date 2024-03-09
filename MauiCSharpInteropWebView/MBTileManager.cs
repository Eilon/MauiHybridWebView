using SQLite;

namespace MauiCSharpInteropWebView
{
    /// <summary>
    /// Creates an SQLite connection to an MBTiles file and retrieves tiles from it.
    /// An MBTiles file is a SQLite database that contains map tiles and has a standard table format.
    /// https://wiki.openstreetmap.org/wiki/MBTiles
    /// </summary>
    public class MBTileManager
    {
        private SQLiteConnection _conn;

        /// <summary>
        /// Creates an SQLite connection to an MBTiles file and retrieves tiles from it.
        /// An MBTiles file is a SQLite database that contains map tiles and has a standard table format.
        /// https://wiki.openstreetmap.org/wiki/MBTiles
        /// </summary>
        /// <param name="assetPath">The path to the MBTiles file in the app's assets folder.</param>
        public MBTileManager(string assetPath)
        {
            //Copy asset to app data storage.
            var localPath = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(assetPath));

            //Check to see if the file exists in the app data storage already.
            if (!File.Exists(localPath))
            {
                //Need to copy the file to local app data storage as Sqlite can't access Raw folder.
                FileSystem.OpenAppPackageFileAsync(assetPath).ContinueWith((task) =>
                {
                    using (var asset = task.Result)
                    {
                        using (var file = File.Create(localPath))
                        {
                            asset.CopyTo(file);
                        }
                    }

                    _conn = new SQLiteConnection(localPath);
                });
            }
            else
            {
                _conn = new SQLiteConnection(localPath);
            }
        }

        /// <summary>
        /// Retrieves a tile from the MBTiles file.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public byte[] GetTile(long? x, long? y, long? z)
        {
            if (_conn != null && x != null && y != null && z != null)
            {
                //Inverse Y value as mbtiles uses TMS
                long inverseY = ((long)Math.Pow(2, z.Value) - 1) - y.Value;

                var tileResults = _conn.Query<TileResult>(String.Format("SELECT tile_data, tile_row, tile_column, zoom_level FROM tiles WHERE tile_column = {0} and tile_row = {1} and zoom_level = {2};", x, inverseY, z));

                if (tileResults.Count > 0 && tileResults[0].tile_data != null)
                {
                    return tileResults[0].tile_data;
                }
            }

            return null;
        }

        /// <summary>
        /// Represents a tile in an MBTiles file.
        /// </summary>
        public class TileResult
        {
            /// <summary>
            /// Binary tile data. Could be a raster (jpeg/png image) or vector tile (pbf).
            /// </summary>
            public byte[] tile_data { get; set; }

            public long tile_row { get; set; }

            public long tile_column { get; set; }

            public long zoom_level { get; set; }
        }
    }
}
