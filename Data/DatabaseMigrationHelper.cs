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

    /// <summary>
    /// Add doctor settings columns to doctor_profiles table if they don't exist
    /// </summary>
    public static async Task AddDoctorSettingsColumnsAsync(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var columnsToAdd = new Dictionary<string, string>
        {
            { "auto_accept_appointments", "INTEGER DEFAULT 0" },
            { "max_daily_patients", "INTEGER DEFAULT 30" },
            { "notify_urgent_consultations", "INTEGER DEFAULT 1" },
            { "notify_new_appointments", "INTEGER DEFAULT 1" },
            { "notify_ai_assessments", "INTEGER DEFAULT 1" },
            { "notify_email_summaries", "INTEGER DEFAULT 0" },
            { "session_timeout_minutes", "INTEGER DEFAULT 30" }
        };

        foreach (var column in columnsToAdd)
        {
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = $@"
                SELECT COUNT(*) 
                FROM pragma_table_info('doctor_profiles') 
                WHERE name='{column.Key}'";
            
            var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

            if (!exists)
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = $@"
                    ALTER TABLE doctor_profiles 
                    ADD COLUMN {column.Key} {column.Value}";
                
                await alterCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"✅ Added {column.Key} column to doctor_profiles table");
            }
            else
            {
                Console.WriteLine($"ℹ️ {column.Key} column already exists in doctor_profiles");
            }
        }
    }

    /// <summary>
    /// Fix documents table to make conversation_id nullable
    /// This allows medical documents to be uploaded without a conversation
    /// </summary>
    public static async Task MakeDocumentsConversationIdNullableAsync(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Check if the migration is needed by checking table structure
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT sql 
            FROM sqlite_master 
            WHERE type='table' AND name='documents'";
        
        var tableSql = await checkCommand.ExecuteScalarAsync() as string;
        
        // Check if conversation_id is currently NOT NULL
        if (!string.IsNullOrEmpty(tableSql) && tableSql.Contains("conversation_id TEXT NOT NULL"))
        {
            Console.WriteLine("🔧 Migrating documents table to make conversation_id nullable...");
            
            using var transaction = connection.BeginTransaction();
            try
            {
                // Step 1: Create new table with correct schema
                var createNewTable = connection.CreateCommand();
                createNewTable.Transaction = transaction;
                createNewTable.CommandText = @"
                    CREATE TABLE documents_new (
                        id TEXT PRIMARY KEY DEFAULT (lower(hex(randomblob(16)))),
                        conversation_id TEXT REFERENCES conversations(id) ON DELETE CASCADE,
                        uploaded_by_user_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                        
                        file_name TEXT NOT NULL,
                        file_type TEXT NOT NULL CHECK (file_type IN ('medical_record', 'lab_result', 'prescription', 'image', 'other')),
                        file_size_bytes INTEGER NOT NULL,
                        file_url TEXT NOT NULL,
                        mime_type TEXT,
                        
                        is_processed INTEGER DEFAULT 0,
                        extracted_text TEXT,
                        
                        description TEXT,
                        tags TEXT,
                        
                        patient_id TEXT REFERENCES patient_profiles(user_id) ON DELETE CASCADE,
                        title TEXT,
                        document_type_string TEXT,
                        file_data BLOB,
                        
                        created_at TEXT DEFAULT (datetime('now')),
                        CONSTRAINT file_size_positive CHECK (file_size_bytes > 0)
                    )";
                await createNewTable.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Created documents_new table with nullable conversation_id");

                // Step 2: Copy all data
                var copyData = connection.CreateCommand();
                copyData.Transaction = transaction;
                copyData.CommandText = @"
                    INSERT INTO documents_new 
                    SELECT * FROM documents";
                await copyData.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Copied all data to documents_new");

                // Step 3: Drop old table
                var dropOld = connection.CreateCommand();
                dropOld.Transaction = transaction;
                dropOld.CommandText = "DROP TABLE documents";
                await dropOld.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Dropped old documents table");

                // Step 4: Rename new table
                var rename = connection.CreateCommand();
                rename.Transaction = transaction;
                rename.CommandText = "ALTER TABLE documents_new RENAME TO documents";
                await rename.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Renamed documents_new to documents");

                // Step 5: Recreate indexes
                var createIndexes = connection.CreateCommand();
                createIndexes.Transaction = transaction;
                createIndexes.CommandText = @"
                    CREATE INDEX IF NOT EXISTS idx_documents_conversation_id ON documents(conversation_id);
                    CREATE INDEX IF NOT EXISTS idx_documents_patient_id ON documents(patient_id);
                    CREATE INDEX IF NOT EXISTS idx_documents_uploaded_by ON documents(uploaded_by_user_id);
                    CREATE INDEX IF NOT EXISTS idx_documents_created_at ON documents(created_at);";
                await createIndexes.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Recreated indexes");

                transaction.Commit();
                Console.WriteLine("✅ Migration completed: conversation_id is now nullable in documents table");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"❌ Migration failed: {ex.Message}");
                throw;
            }
        }
        else
        {
            Console.WriteLine("ℹ️ Documents table already has nullable conversation_id or table doesn't exist");
        }
    }

    /// <summary>
    /// Add AI consultation summary columns to conversations table if they don't exist
    /// </summary>
    public static async Task AddAiConsultationSummaryColumnsAsync(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Check and add ai_generated_summary column
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('conversations') 
            WHERE name='ai_generated_summary'";
        
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE conversations ADD COLUMN ai_generated_summary TEXT";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Added ai_generated_summary column to conversations table");
        }
        else
        {
            Console.WriteLine("ℹ️ ai_generated_summary column already exists in conversations");
        }

        // Check and add ai_model_used column
        checkCommand = connection.CreateCommand();
        checkCommand.CommandText = @"
            SELECT COUNT(*) 
            FROM pragma_table_info('conversations') 
            WHERE name='ai_model_used'";
        
        exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE conversations ADD COLUMN ai_model_used TEXT";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Added ai_model_used column to conversations table");
        }
        else
        {
            Console.WriteLine("ℹ️ ai_model_used column already exists in conversations");
        }
    }
}
