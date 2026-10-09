using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public enum UpdateUserResult
{
    Updated,
    NotFound,
    DuplicateEmail
}

public class UserStore
{
    private readonly List<User> _users = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public (List<User> Items, int TotalCount) GetPage(
        int page,
        int pageSize)
    {
        lock (_lock)
        {
            var totalCount = _users.Count;

            var items = _users
                .OrderBy(user => user.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(Copy)
                .ToList();

            return (items, totalCount);
        }
    }

    public User? GetById(int id)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(user => user.Id == id);

            return user is null ? null : Copy(user);
        }
    }

    public bool TryCreate(
        string name,
        string email,
        out User? createdUser)
    {
        lock (_lock)
        {
            var emailExists = _users.Any(user =>
                string.Equals(
                    user.Email,
                    email,
                    StringComparison.OrdinalIgnoreCase));

            if (emailExists)
            {
                createdUser = null;
                return false;
            }

            var user = new User
            {
                Id = _nextId++,
                Name = name.Trim(),
                Email = email.Trim()
            };

            _users.Add(user);
            createdUser = Copy(user);

            return true;
        }
    }

    public UpdateUserResult TryUpdate(
        int id,
        string name,
        string email,
        out User? updatedUser)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(user => user.Id == id);

            if (user is null)
            {
                updatedUser = null;
                return UpdateUserResult.NotFound;
            }

            var duplicateEmail = _users.Any(existing =>
                existing.Id != id &&
                string.Equals(
                    existing.Email,
                    email,
                    StringComparison.OrdinalIgnoreCase));

            if (duplicateEmail)
            {
                updatedUser = null;
                return UpdateUserResult.DuplicateEmail;
            }

            user.Name = name.Trim();
            user.Email = email.Trim();

            updatedUser = Copy(user);

            return UpdateUserResult.Updated;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(user => user.Id == id);

            if (user is null)
            {
                return false;
            }

            _users.Remove(user);
            return true;
        }
    }

    private static User Copy(User user)
    {
        return new User
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }
}