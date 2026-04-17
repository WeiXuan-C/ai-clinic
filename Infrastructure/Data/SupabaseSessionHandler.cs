using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace AiClinic.Infrastructure.Data;

/// <summary>
/// Session handler for Supabase to persist sessions across page refreshes
/// Uses in-memory storage for Blazor Server (sessions are maintained server-side)
/// </summary>
public class SupabaseSessionHandler : IGotrueSessionPersistence<Session>
{
    private Session? _cachedSession;

    public void SaveSession(Session session)
    {
        _cachedSession = session;
    }

    public void DestroySession()
    {
        _cachedSession = null;
    }

    public Session? LoadSession()
    {
        return _cachedSession;
    }
}
