using OpenTK.Mathematics;

namespace HuePat.Util.Math.Grids {
    public static class Extensions {
        public static bool IsGridDirectionDiagonal(
                this Vector3i gridDirection) {

            return gridDirection.X.Abs() 
                    + gridDirection.Y.Abs() 
                    + gridDirection.Z.Abs() 
                > 1;
        }

        public static bool IsWithinBounds(
                this Vector3i gridCoordinate,
                Vector3i gridSize) {

            return gridCoordinate.X >= 0
                && gridCoordinate.Y >= 0
                && gridCoordinate.Z >= 0
                && gridCoordinate.X < gridSize.X
                && gridCoordinate.Y < gridSize.Y
                && gridCoordinate.Z < gridSize.Z;
        }

        public static Vector3i GetSize<T>(
                this T[,,] grid) {

            return new Vector3i(
                grid.GetLength(0),
                grid.GetLength(1),
                grid.GetLength(2));
        }

        public static T Get<T>(
                this T[,,] grid,
                Vector3i gridCoordinate) {

            return grid[
                gridCoordinate.X,
                gridCoordinate.Y,
                gridCoordinate.Z];
        }

        public static void Set<T>(
                this T[,,] grid,
                Vector3i gridCoordinate,
                T value) {

            grid[
                gridCoordinate.X,
                gridCoordinate.Y,
                gridCoordinate.Z] = value;
        }
    }
}
