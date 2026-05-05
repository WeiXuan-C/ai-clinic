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

    /// <summary>
    /// Add medical document fields to documents table if they don't exist
    /// </summary>
    public static async Task AddMedicalDocumentFieldsAsync(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Check and add patient_id column
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('documents') 
            WHERE name='patient_id'";
        
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE documents ADD COLUMN patient_id TEXT";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Added patient_id column to documents table");
        }
        else
        {
            Console.WriteLine("ℹ️ patient_id column already exists in documents");
        }

        // Check and add title column
        checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('documents') 
            WHERE name='title'";
        
        exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE documents ADD COLUMN title TEXT";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Added title column to documents table");
        }
        else
        {
            Console.WriteLine("ℹ️ title column already exists in documents");
        }

        // Check and add document_type_string column
        checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('documents') 
            WHERE name='document_type_string'";
        
        exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE documents ADD COLUMN document_type_string TEXT";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Added document_type_string column to documents table");
        }
        else
        {
            Console.WriteLine("ℹ️ document_type_string column already exists in documents");
        }

        // Check and add file_data column
        checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('documents') 
            WHERE name='file_data'";
        
        exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE documents ADD COLUMN file_data BLOB";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Added file_data column to documents table");
        }
        else
        {
            Console.WriteLine("ℹ️ file_data column already exists in documents");
        }

        // Create index for patient_id
        var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
            CREATE INDEX IF NOT EXISTS idx_documents_patient_id 
            ON documents(patient_id)";
        await indexCommand.ExecuteNonQueryAsync();
        Console.WriteLine("✅ Created index on documents.patient_id");
    }
}
