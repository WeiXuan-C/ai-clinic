# Supabase Serialization Fix

## Problem
When creating a patient profile, the application threw a serialization error:
```
The type 'Postgrest.Attributes.PrimaryKeyAttribute' is not a supported dictionary key using converter
```

This occurred because the `PatientProfile` model (and other profile models) inherit from `BaseModel` and use Postgrest attributes like `[PrimaryKey]` and `[Column]`. When `System.Text.Json.JsonSerializer` tried to serialize these objects directly, it attempted to serialize the attribute metadata itself, causing the error.

## Root Cause
The custom `SupabaseHttpClient` was directly serializing entities that inherit from `Postgrest.Models.BaseModel`. The `BaseModel` class contains properties like `PrimaryKey` and `TableName` that include complex attribute objects which are not JSON-serializable.

## Solution
Modified `SupabaseHttpClient.cs` to convert entities to plain DTOs before serialization:

### Changes Made

1. **Added `ConvertToDto()` method**: Converts entities with Postgrest attributes to plain dictionaries
   - Uses reflection to read property values
   - Extracts column names from `[Column]` attributes
   - Skips `BaseModel` properties (`PrimaryKey`, `TableName`)
   - Converts property names to snake_case for database compatibility

2. **Added `ToSnakeCase()` helper**: Converts PascalCase property names to snake_case

3. **Updated `PostAsync()` and `PatchAsync()`**: Now call `ConvertToDto()` before serialization

### Code Example
```csharp
private Dictionary<string, object?> ConvertToDto(object entity)
{
    var dto = new Dictionary<string, object?>();
    var type = entity.GetType();
    var properties = type.GetProperties();

    foreach (var prop in properties)
    {
        // Skip BaseModel properties
        if (prop.Name == "PrimaryKey" || prop.Name == "TableName")
            continue;

        var value = prop.GetValue(entity);
        
        // Get column name from attribute or convert to snake_case
        var columnAttr = prop.GetCustomAttributes(typeof(Postgrest.Attributes.ColumnAttribute), false)
            .FirstOrDefault() as Postgrest.Attributes.ColumnAttribute;
        
        var columnName = columnAttr?.ColumnName ?? ToSnakeCase(prop.Name);
        
        if (value != null)
        {
            dto[columnName] = value;
        }
    }

    return dto;
}
```

## Alternative Approach (Recommended for Future)
According to the [official Supabase C# documentation](https://supabase.com/docs/reference/csharp/introduction), the recommended approach is to use the official Supabase C# client library:

```csharp
// Official way
await supabase.From<PatientProfile>().Insert(model);
```

The official client handles serialization internally and avoids these issues. Consider migrating to the official client in the future.

## Testing
- Build succeeded without errors
- The fix applies to all profile types (Patient, Doctor, Admin)
- Serialization now produces clean JSON without Postgrest metadata

## Files Modified
- `Database/SupabaseHttpClient.cs`
