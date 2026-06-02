namespace World
{
    public static class DungeonGrid
    {
        public const int GRID_WIDTH = 10;
        public const int GRID_HEIGHT = 10;
        #region Helpers
        public static int GetIndex(int x, int y) => y * GRID_WIDTH + x;
        public static int GetX(int index) => index % GRID_WIDTH;
        public static int GetY(int index) => index / GRID_WIDTH;
        #endregion
    }
}

