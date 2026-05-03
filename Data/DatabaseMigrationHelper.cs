using Microsoft.Data.Sqlite;

namespace ai_clinic.Data;

/// <summary>
/// Helper class for manual database migrations
/// Used when EnsureCreated cannot alter existing tables
/// </summary>
public static class DatabaseMigrationHelper
{
    /// <summary>
    /// Add profile_photo column to patient_profiles table if it doesn't exist
    /// </summary>
    public static async Task AddProfilePhotoColumnAsync(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Check if column exists in patient_profiles
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('patient_profiles') 
            WHERE name='profile_photo'";
        
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            // Add column
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = @"
                ALTER TABLE patient_profiles 
                ADD COLUMN profile_photo BLOB";
            
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Added profile_photo column to patient_profiles table");
        }
        else
        {
            Console.WriteLine("ℹ️ profile_photo column already exists in patient_profiles");
        }

        // Check if column exists in doctor_profiles
        checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('doctor_profiles') 
            WHERE name='profile_photo'";
        
        exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            // Add column
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = @"
                ALTER TABLE doctor_profiles 
                ADD COLUMN profile_photo BLOB";
            
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Added profile_photo column to doctor_profiles table");
        }
        else
        {
            Console.WriteLine("ℹ️ profile_photo column already exists in doctor_profiles");
        }
    }
}
