using Supabase;

namespace AiClinic.Infrastructure.Data;

public class SupabaseContext
{
    private readonly Client _client;

    public SupabaseContext(Client client)
    {
        _client = client;
    }

    public Client Client => _client;
}
