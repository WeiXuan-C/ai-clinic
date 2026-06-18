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

    /// <summary>
    /// Add AI model management features to ai_assistant_settings table
    /// Adds columns for model types, patient availability, display order, and inserts default AI models
    /// </summary>
    public static async Task AddAiModelManagementAsync(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Check and add model_type column
            var checkCommand = connection.CreateCommand();
            checkCommand.Transaction = transaction;
            checkCommand.CommandText = @"
                SELECT COUNT(*) 
                FROM pragma_table_info('ai_assistant_settings') 
                WHERE name='model_type'";
            
            var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

            if (!exists)
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.Transaction = transaction;
                alterCommand.CommandText = "ALTER TABLE ai_assistant_settings ADD COLUMN model_type INTEGER NOT NULL DEFAULT 0";
                await alterCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Added model_type column to ai_assistant_settings table");
            }
            else
            {
                Console.WriteLine("ℹ️ model_type column already exists in ai_assistant_settings");
            }

            // Check and add is_available_for_patients column
            checkCommand = connection.CreateCommand();
            checkCommand.Transaction = transaction;
            checkCommand.CommandText = @"
                SELECT COUNT(*) 
                FROM pragma_table_info('ai_assistant_settings') 
                WHERE name='is_available_for_patients'";
            
            exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

            if (!exists)
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.Transaction = transaction;
                alterCommand.CommandText = "ALTER TABLE ai_assistant_settings ADD COLUMN is_available_for_patients INTEGER NOT NULL DEFAULT 1";
                await alterCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Added is_available_for_patients column to ai_assistant_settings table");
            }
            else
            {
                Console.WriteLine("ℹ️ is_available_for_patients column already exists in ai_assistant_settings");
            }

            // Check and add display_order column
            checkCommand = connection.CreateCommand();
            checkCommand.Transaction = transaction;
            checkCommand.CommandText = @"
                SELECT COUNT(*) 
                FROM pragma_table_info('ai_assistant_settings') 
                WHERE name='display_order'";
            
            exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

            if (!exists)
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.Transaction = transaction;
                alterCommand.CommandText = "ALTER TABLE ai_assistant_settings ADD COLUMN display_order INTEGER NOT NULL DEFAULT 0";
                await alterCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Added display_order column to ai_assistant_settings table");
            }
            else
            {
                Console.WriteLine("ℹ️ display_order column already exists in ai_assistant_settings");
            }

            // Check and add description column
            checkCommand = connection.CreateCommand();
            checkCommand.Transaction = transaction;
            checkCommand.CommandText = @"
                SELECT COUNT(*) 
                FROM pragma_table_info('ai_assistant_settings') 
                WHERE name='description'";
            
            exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

            if (!exists)
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.Transaction = transaction;
                alterCommand.CommandText = "ALTER TABLE ai_assistant_settings ADD COLUMN description TEXT";
                await alterCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Added description column to ai_assistant_settings table");
            }
            else
            {
                Console.WriteLine("ℹ️ description column already exists in ai_assistant_settings");
            }

            // Create index for better query performance
            var indexCommand = connection.CreateCommand();
            indexCommand.Transaction = transaction;
            indexCommand.CommandText = @"
                CREATE INDEX IF NOT EXISTS idx_ai_assistant_settings_available 
                ON ai_assistant_settings(is_available_for_patients, is_active, display_order)";
            await indexCommand.ExecuteNonQueryAsync();
            Console.WriteLine("✅ Created index on ai_assistant_settings");

            // Check and add current_ai_model_type column to conversations
            checkCommand = connection.CreateCommand();
            checkCommand.Transaction = transaction;
            checkCommand.CommandText = @"
                SELECT COUNT(*) 
                FROM pragma_table_info('conversations') 
                WHERE name='current_ai_model_type'";
            
            exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

            if (!exists)
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.Transaction = transaction;
                alterCommand.CommandText = "ALTER TABLE conversations ADD COLUMN current_ai_model_type INTEGER";
                await alterCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Added current_ai_model_type column to conversations table");
            }
            else
            {
                Console.WriteLine("ℹ️ current_ai_model_type column already exists in conversations");
            }

            // Check if default models exist
            var countCommand = connection.CreateCommand();
            countCommand.Transaction = transaction;
            countCommand.CommandText = "SELECT COUNT(*) FROM ai_assistant_settings";
            var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

            if (count == 0)
            {
                // Insert default AI models
                var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
                    INSERT INTO ai_assistant_settings (id, model_name, model_type, is_active, is_available_for_patients, display_order, description, system_prompt, enable_document_analysis, enable_symptom_checker, enable_doctor_recommendation, created_at, updated_at)
                    VALUES 
                        (lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
                         'Gemma 4', 0, 1, 1, 1, 'Google Gemma 4 - Advanced medical analysis and consultation',
                         'You are a professional medical AI assistant powered by Gemma 4. Provide accurate, helpful, and empathetic medical guidance.', 1, 1, 1, datetime('now'), datetime('now')),
                        (lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
                         'MiniMax', 1, 1, 1, 2, 'MiniMax - Efficient and fast medical consultations',
                         'You are a professional medical AI assistant powered by MiniMax. Provide quick, accurate, and helpful medical guidance.', 1, 1, 1, datetime('now'), datetime('now')),
                        (lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
                         'Nemotron', 2, 1, 1, 3, 'NVIDIA Nemotron - Specialized medical diagnostics',
                         'You are a professional medical AI assistant powered by Nemotron. Provide specialized diagnostic support and medical guidance.', 1, 1, 1, datetime('now'), datetime('now')),
                        (lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
                         'Owlapha', 3, 1, 1, 4, 'Owlapha - Comprehensive medical knowledge assistant',
                         'You are a professional medical AI assistant powered by Owlapha. Provide comprehensive medical knowledge and patient guidance.', 1, 1, 1, datetime('now'), datetime('now'))";
                await insertCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Inserted 4 default AI models into ai_assistant_settings table");
            }
            else
            {
                Console.WriteLine($"ℹ️ ai_assistant_settings table already has {count} model(s), skipping default inserts");
            }

            // Update existing conversations to use Gemma4 as default
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = @"
                UPDATE conversations 
                SET current_ai_model_type = 0 
                WHERE ai_model_used IS NOT NULL AND current_ai_model_type IS NULL";
            var updated = await updateCommand.ExecuteNonQueryAsync();
            if (updated > 0)
            {
                Console.WriteLine($"✅ Updated {updated} conversation(s) with default AI model type");
            }

            transaction.Commit();
            Console.WriteLine("✅ AI Model Management migration completed successfully");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"❌ AI Model Management migration failed: {ex.Message}");
            throw;
        }
    }
}
