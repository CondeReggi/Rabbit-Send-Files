using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Cliente;
using Cliente.Models;

public class FileRepository
{
    private readonly DataBaseContext _dbContext;

    public FileRepository(DataBaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddFileRecordAsync(FileRecord fileRecord)
    {
        _dbContext.FileRecords.Add(fileRecord);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateFileRecordAsync(FileRecord fileRecord)
    {
        _dbContext.FileRecords.Update(fileRecord);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<FileRecord> GetFileRecordByIdAsync(int id)
    {
        return await _dbContext.FileRecords.FindAsync(id);
    }

    public async Task DeleteFileRecordAsync(int id)
    {
        var fileRecord = await GetFileRecordByIdAsync(id);
        if (fileRecord != null)
        {
            _dbContext.FileRecords.Remove(fileRecord);
            await _dbContext.SaveChangesAsync();
        }
    }
}
