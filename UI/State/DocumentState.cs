using AiClinic.Interfaces;

namespace AiClinic.UI.State;

/// <summary>
/// Scoped Document State for Blazor (Redux-like pattern)
/// Manages document data, cache, and Supabase CRUD operations
/// </summary>
public class DocumentState
{
    private readonly IDocumentRepository _repository;
    private List<Document> _documents = new();
    private Document? _selectedDocument;
    private bool _isLoading;
    private string? _errorMessage;

    public DocumentState(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public event Action? OnChange;

    public IReadOnlyList<Document> Documents => _documents.AsReadOnly();
    public Document? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            _selectedDocument = value;
            NotifyStateChanged();
        }
    }
    public bool IsLoading => _isLoading;
    public string? ErrorMessage => _errorMessage;

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var documents = await _repository.GetAllAsync();
            _documents = documents.ToList();
            return documents;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Document>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var document = await _repository.GetByIdAsync(id);
            if (document != null)
            {
                var index = _documents.FindIndex(d => d.Id == id);
                if (index >= 0)
                    _documents[index] = document;
                else
                    _documents.Add(document);
            }
            return document;
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

    public async Task<IEnumerable<Document>> GetByConversationIdAsync(Guid conversationId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var documents = await _repository.GetByConversationIdAsync(conversationId);
            _documents = documents.ToList();
            return documents;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Document>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Document>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var documents = await _repository.GetByUserIdAsync(userId);
            _documents = documents.ToList();
            return documents;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Document>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Document>> GetProcessedDocumentsAsync(Guid conversationId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var documents = await _repository.GetProcessedDocumentsAsync(conversationId);
            return documents;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Document>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Document?> CreateAsync(Document document)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var created = await _repository.AddAsync(document);
            _documents.Add(created);
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

    public async Task<Document?> UpdateAsync(Document document)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var updated = await _repository.UpdateAsync(document);
            var index = _documents.FindIndex(d => d.Id == document.Id);
            if (index >= 0)
                _documents[index] = updated;
            if (_selectedDocument?.Id == document.Id)
                _selectedDocument = updated;
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

            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _documents.RemoveAll(d => d.Id == id);
                if (_selectedDocument?.Id == id)
                    _selectedDocument = null;
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
        _documents.Clear();
        _selectedDocument = null;
        _errorMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
