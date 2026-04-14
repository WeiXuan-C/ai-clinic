using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class OtpRepository : IOtpRepository
{
    private readonly SupabaseContext _context;

    public OtpRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<OtpToken?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<OtpToken>()
            .Where(o => o.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<OtpToken>> GetAllAsync()
    {
        var response = await _context.Client
            .From<OtpToken>()
            .Get();
        return response.Models;
    }

    public async Task<OtpToken> AddAsync(OtpToken entity)
    {
        var response = await _context.Client
            .From<OtpToken>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<OtpToken> UpdateAsync(OtpToken entity)
    {
        var response = await _context.Client
            .From<OtpToken>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<OtpToken>()
            .Where(o => o.Id == id)
            .Delete();
        return true;
    }

    public async Task<OtpToken?> GetValidOtpAsync(string email, string code)
    {
        var response = await _context.Client
            .From<OtpToken>()
            .Where(o => o.Email == email && o.Code == code && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .Single();
        return response;
    }

    public async Task MarkAsUsedAsync(Guid id)
    {
        var otp = await GetByIdAsync(id);
        if (otp != null)
        {
            otp.IsUsed = true;
            await UpdateAsync(otp);
        }
    }
}
