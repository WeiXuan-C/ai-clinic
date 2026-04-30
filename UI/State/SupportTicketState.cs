using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.UI.State;

/// <summary>
/// Scoped Support Ticket State for Blazor (Redux-like pattern)
/// State owns Supabase HTTP Client and makes all database calls
/// Manages cache and returns standardized objects
/// </summary>
public class SupportTicketState
{
    private readonly SupabaseHttpClient _supabase;
    private List<SupportTicket> _tickets = [];
    private SupportTicket? _selectedTicket;
    private bool _isLoading;
    private string? _errorMessage;

    public SupportTicketState(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public event Action? OnChange;

    public IReadOnlyList<SupportTicket> Tickets => _tickets.AsReadOnly();
    public SupportTicket? SelectedTicket
    {
        get => _selectedTicket;
        set
        {
            _selectedTicket = value;
            NotifyStateChanged();
        }
    }
    public bool IsLoading => _isLoading;
    public string? ErrorMessage => _errorMessage;

    // ==================== CRUD Operations (State calls Supabase directly) ====================

    public async Task<IEnumerable<SupportTicket>> GetAllAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var filter = "order=created_at.desc";
            var tickets = await _supabase.GetAsync<SupportTicket>("support_tickets", filter);
            _tickets = [.. tickets];
            return tickets;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return [];
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var ticket = await _supabase.GetSingleAsync<SupportTicket>("support_tickets", $"id=eq.{id}");
            
            if (ticket != null)
            {
                var index = _tickets.FindIndex(t => t.Id == id);
                if (index >= 0)
                    _tickets[index] = ticket;
                else
                    _tickets.Add(ticket);
            }
            
            return ticket;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<SupportTicket>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var filter = $"user_id=eq.{userId}&order=created_at.desc";
            var tickets = await _supabase.GetAsync<SupportTicket>("support_tickets", filter);
            _tickets = [.. tickets];
            return tickets;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return [];
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<SupportTicket>> GetByStatusAsync(string status)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var filter = $"status=eq.{status}&order=created_at.desc";
            var tickets = await _supabase.GetAsync<SupportTicket>("support_tickets", filter);
            _tickets = [.. tickets];
            return tickets;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return [];
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<SupportTicket?> CreateAsync(SupportTicket ticket)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var created = await _supabase.PostAsync<SupportTicket>("support_tickets", ticket);
            if (created != null)
                _tickets.Add(created);
            
            return created;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<SupportTicket?> UpdateAsync(SupportTicket ticket)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var updated = await _supabase.PatchAsync<SupportTicket>("support_tickets", $"id=eq.{ticket.Id}", ticket);
            if (updated != null)
            {
                var index = _tickets.FindIndex(t => t.Id == ticket.Id);
                if (index >= 0)
                    _tickets[index] = updated;
                
                if (_selectedTicket?.Id == ticket.Id)
                    _selectedTicket = updated;
            }
            
            return updated;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var success = await _supabase.DeleteAsync("support_tickets", $"id=eq.{id}");
            
            if (success)
            {
                _tickets.RemoveAll(t => t.Id == id);
                if (_selectedTicket?.Id == id)
                    _selectedTicket = null;
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return false;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public void ClearCache()
    {
        _tickets.Clear();
        _selectedTicket = null;
        _errorMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
