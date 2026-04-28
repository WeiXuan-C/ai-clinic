using AiClinic.Interfaces;
using Supabase;

namespace AiClinic.UI.State;

/// <summary>
/// Scoped Support Ticket State for Blazor (Redux-like pattern)
/// State owns Supabase Client and makes all database calls
/// Manages cache and returns standardized objects
/// </summary>
public class SupportTicketState
{
    private readonly Client _supabase;
    private List<SupportTicket> _tickets = new();
    private SupportTicket? _selectedTicket;
    private bool _isLoading;
    private string? _errorMessage;

    public SupportTicketState(Client supabase)
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

            // State calls Supabase, gets JSON, Supabase converts to objects
            var response = await _supabase
                .From<SupportTicket>()
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
            
            _tickets = response.Models.ToList();
            return response.Models;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<SupportTicket>();
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

            var response = await _supabase
                .From<SupportTicket>()
                .Where(x => x.Id == id)
                .Single();
            
            if (response != null)
            {
                var index = _tickets.FindIndex(t => t.Id == id);
                if (index >= 0)
                    _tickets[index] = response;
                else
                    _tickets.Add(response);
            }
            
            return response;
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

            var response = await _supabase
                .From<SupportTicket>()
                .Where(x => x.UserId == userId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
            
            _tickets = response.Models.ToList();
            return response.Models;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<SupportTicket>();
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

            var response = await _supabase
                .From<SupportTicket>()
                .Where(x => x.Status == status)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
            
            _tickets = response.Models.ToList();
            return response.Models;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<SupportTicket>();
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

            var response = await _supabase
                .From<SupportTicket>()
                .Insert(ticket);
            
            var created = response.Models.FirstOrDefault();
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

            var response = await _supabase
                .From<SupportTicket>()
                .Where(x => x.Id == ticket.Id)
                .Update(ticket);
            
            var updated = response.Models.FirstOrDefault();
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

            await _supabase
                .From<SupportTicket>()
                .Where(x => x.Id == id)
                .Delete();
            
            _tickets.RemoveAll(t => t.Id == id);
            if (_selectedTicket?.Id == id)
                _selectedTicket = null;
            
            return true;
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
