namespace WebAppExperimental266.Models.Settings
{
    public class CrudDataSettings
    {
        public string Provider { get; set; } = "Sqlite";

        public string SqliteDatabasePath { get; set; } = "App_Data/crud-records.db";

        public string CosmosContainerName { get; set; } = "CrudRecords";

        public bool UseCosmos =>
            string.Equals(Provider, "Cosmos", StringComparison.OrdinalIgnoreCase);

        public string ResolveSqliteDatabasePath(string contentRootPath)
        {
            if (Path.IsPathRooted(SqliteDatabasePath))
            {
                return SqliteDatabasePath;
            }

            return Path.Combine(contentRootPath, SqliteDatabasePath);
        }
    }
}
