using ECommerce.Domain.Entities;
using ECommerce.Domain.Ports;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class UserRepository : IUserRepository
{
  private readonly AppDbContext _dbContext;
  public UserRepository(AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
  {
    _dbContext.Users.Add(user);
    await _dbContext.SaveChangesAsync(cancellationToken);
    return user;
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var user = await _dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
    if (user != null)
    {
      _dbContext.Users.Remove(user);
      await _dbContext.SaveChangesAsync(cancellationToken);
    }
    return true;
  }

  public IQueryable<User> GetAllAsync()
  {
    return _dbContext.Users
          .Include(u => u.Orders)
          .ThenInclude(o => o.OrderItems)
          .AsQueryable();
  }

  public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
  {
    return await _dbContext.Users
        .AsNoTracking()
        .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
  }

  public async Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await _dbContext.Users
        .Include(u => u.Orders)
        .ThenInclude(o => o.OrderItems)
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken) ?? throw new Exception("User not found!");
  }

  public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
  {
    _dbContext.Users.Update(user);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

}